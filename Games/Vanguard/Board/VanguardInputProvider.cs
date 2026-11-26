using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using ArC.CardGames.Skills;
using ArC.CardGames.Stats;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>
/// The human player of Vanguard, and the screen showing the game. ArC asks four questions; each waits for the board to
/// finish showing what happened, then goes to:
/// <list type="bullet">
/// <item>ChooseAction: the strategy of the phase or window asking (ride, main, attack, the guard step), which matches a
/// gesture to one of the offered requests; an unknown phase or window throws;</item>
/// <item>Choose and Confirm: VanguardChoices, by the question's purpose and shape (the mulligan, triggers, costs, targets).</item>
/// </list>
/// Every answer is checked against what was offered (Expect) before it goes back. Tapping a card shows its details (unless the tap
/// answers what is being asked), long pressing a vanguard lists its soul, pressing the drop zone lists it.
/// </summary>
public partial class VanguardInputProvider : Control, IPlayerInput, IProvide<IActivationGate>
{
    VanguardBoardView board = null!;
    public VanguardBoardView Board => board;
    CardList CardListComponent = null!;
    SelectFromCardList SelectFromCardListComponent = null!;
    SelectCardsComponent SelectCardsComponent = null!;
    CardDetailsOverlay CardDetails = null!;
    FieldSelectScreen FieldSelect = null!;
    CardSelectScreen CardSelect = null!;
    SwipeUpPicker SwipeUp = null!;
    VanguardTriggerSelectScreen TriggerSelect = null!;
    VanguardGuardScreen GuardScreen = null!;
    VanguardGameSession? session;

    /// <summary>The location tags on cards offered to pick (VanguardCardTags.tres).</summary>
    [Export] public VanguardCardTags CardTags { get; set; } = null!;
    // The player's skills being used, for every view of the skill asking (the pinned details, the screens' banners)
    readonly ResolvingSkills resolvingSkills = new();
    ArcDriver? driver;
    // The zone whose cards CardListComponent is showing
    ICardSpace? listedZone;

    InputBoard inputBoard = null!;
    VanguardInput? input;
    VanguardChoices? choices;
    VanguardGuardPrompt? guard;

    /// <summary>What the card details may activate: nothing, unless the current scope puts its gate in (InputScope.Activatable).</summary>
    public ActivationGateSlot Activations { get; } = new();
    IActivationGate IProvide<IActivationGate>.Value() => Activations;

    public VanguardPlayArea PlayArea { get; private set; } = null!;
    public VanguardPlayArea OpponentPlayArea { get; private set; } = null!;

    // Board animations play from this queue; prompts wait until it's done
    public Presenter Presenter { get; } = new();

    public override void _Ready()
    {
        board = GetNode<VanguardBoardView>($"%{nameof(VanguardBoard)}");
        CardListComponent = GetNode<CardList>($"%{nameof(CardListComponent)}");
        CardListComponent.CardPressed += OnListedCardPressed;
        SelectFromCardListComponent = GetNode<SelectFromCardList>($"%{nameof(SelectFromCardListComponent)}");
        SelectCardsComponent = GetNode<SelectCardsComponent>($"%{nameof(SelectCardsComponent)}");
        CardDetails = GetNode<CardDetailsOverlay>($"%{nameof(CardDetails)}");
        FieldSelect = GetNode<FieldSelectScreen>($"%{nameof(FieldSelect)}");
        FieldSelect.CardPressed += card => CardDetails.Show(new CardDetail(card));
        CardSelect = GetNode<CardSelectScreen>($"%{nameof(CardSelect)}");
        CardSelect.CardPressed += card => CardDetails.Show(new CardDetail(card));
        SwipeUp = GetNode<SwipeUpPicker>($"%{nameof(SwipeUp)}");
        TriggerSelect = GetNode<VanguardTriggerSelectScreen>($"%{nameof(TriggerSelect)}");
        TriggerSelect.CardPressed += card => CardDetails.Show(new CardDetail(card));
        GuardScreen = GetNode<VanguardGuardScreen>($"%{nameof(GuardScreen)}");
        GuardScreen.CardPressed += card => CardDetails.Show(new CardDetail(card));
        if(CardTags is null) throw new InvalidOperationException($"{GetPath()}: set {nameof(CardTags)} (VanguardCardTags.tres)");
        CardTags.EnsureComplete();
        foreach(var resolving in this.Descendants<ResolvingSkillDetails>()) resolving.Follow(resolvingSkills);
        // A card offered to pick (a heal, a soul blast, the mulligan): its details, to read before picking
        SelectFromCardListComponent.CardPressed += node => ShowDetails(node);
        SelectCardsComponent.CardPressed += node => ShowDetails(node);
        // The details stay usable while a card list is open (reading them doesn't close it)
        CardListComponent.LetThrough(CardDetails.Details);
        // Card flights draw above the board and below the HUD (see the scene)
        Presenter.Flights = GetNode<CardFlightLayer>("%CardFlights");
        // The HUD's buttons, wherever they are in the scene (the board holds them); the hint is the player's play area's
        // (each play area scene has one under its phase label)
        inputBoard = new(Activations, this.Descendants<BoardButtonView>(),
            board.PlayerArea.Descendants<InputHintLabel>().FirstOrDefault(), this.Descendants<AimIndicatorLayer>().FirstOrDefault());
        // A skill activated from the details: they close, the skill goes on on the board
        inputBoard.Gestures.Subscribe<SkillActivated>(_ => CardDetails.Hide());
    }

    /// <summary>Shows <paramref name="session"/> on the board and the HUD, face-down cards in <paramref name="sleeves"/>. Call once its game has started.</summary>
    public void Show(VanguardGameSession session, BoardSleeves sleeves)
    {
        driver?.Dispose();
        driver = board.Show(session, Presenter, sleeves);
        FieldSelect.Attach(inputBoard, driver, board);
        TriggerSelect.Attach(inputBoard, driver, board);
        this.session = session;
        driver.ZoneInspected += OnZoneInspected;
        driver.CardInspected += OnCardInspected;
        driver.LongPressed += OnLongPressed;
        inputBoard.Attach(driver);
    }

    /// <summary>Makes this the player of <paramref name="playArea"/> (the session builder calls it once the game started).</summary>
    public void Activate(VanguardPlayArea playArea, VanguardPlayArea opponentPlayArea)
    {
        PlayArea = playArea;
        OpponentPlayArea = opponentPlayArea;
        var presentation = new PresentationContext(Presenter, board.Sfx, BoardSide.Player);
        input = new(this, inputBoard, presentation, this, SelectFromCardListComponent, SelectCardsComponent, CardListComponent, FieldSelect, CardSelect, SwipeUp, TriggerSelect, GuardScreen,
            () => session ?? throw new InvalidOperationException($"{nameof(VanguardInputProvider)} was asked about the game before it was shown"),
            resolvingSkills, CardTags, CardDetails.Show);
        choices = new(input);
        guard = new(input);
    }

    List<IDisposable> skillSubscriptions = [];
    List<IDisposable> sfxSubscriptions = [];

    public void SetEventBus(VanguardEventBus eventBus)
    {
        // The player's own skill, pinned while it's used (what they're asked meanwhile is for it: its cost, its targets)
        foreach(var subscription in skillSubscriptions) subscription.Dispose();
        skillSubscriptions =
        [
            eventBus.Subscribe<SkillUseStartedEvent>(started =>
            {
                Cue(VanguardSfx.SkillActivated);
                if(IsOwn(started.Run)) resolvingSkills.Started(started.Run.Self, started.Run.Skill);
            }),
            eventBus.Subscribe<SkillUseEndedEvent>(ended =>
            {
                if(IsOwn(ended.Run)) resolvingSkills.Finished(ended.Run.Skill);
            }),
        ];

        foreach(var subscription in sfxSubscriptions) subscription.Dispose();
        sfxSubscriptions = [
            eventBus.Subscribe<PhaseStartedEvent>(phaseStarted => { 
                if(phaseStarted.Phase is VanguardAttackPhase or EndPhase) {
                    Cue(VanguardSfx.PhaseChange);
                }
            }),
            eventBus.Subscribe<DeckShuffledEvent>(_ => { 
                Cue(VanguardSfx.Shuffle);
            }),
            eventBus.Subscribe<CardsMoved>(cardsMoved => { 
                if(cardsMoved.From is Deck && cardsMoved.To is Hand && cardsMoved.Reason is Drawn) {
                    Cue(VanguardSfx.Draw);
                }
            }),
            eventBus.Subscribe<StatChanged<int>>(statChanged => { 
                if(ReferenceEquals(statChanged.Which, VanguardStats.Power) 
                    && statChanged.New > statChanged.Old
                    && statChanged.Cause is not GuardShieldRequest
                    && statChanged.Cause is not AttackRequest { Boosted: true }) {
                    Cue(VanguardSfx.PowerUp);
                }
            })
        ];
    }

    // A sound for something the game did: played in step with the board (ArC runs ahead of what is shown), so e.g. the
    // second drive check's power up sounds when that check is shown, not together with the first
    private void Cue(SfxCue cue)
    {
        if(input is not { } current) return;
        current.Presentation.Present(() => current.Presentation.PlaySfx(cue));
    }

    private bool IsOwn(SkillRun run) => PlayArea is not null && ReferenceEquals(run.PlayArea, PlayArea);

    #region Questions
    public async Task<Request> ChooseAction(ActionChoice choice)
    {
        var current = Asked();
        await Presenter.WhenIdle();
        var offered = new Offered(choice);
        var chosen = await resolvingSkills.Asking(async () => choice.Where switch
        {
            RidePhase => await new VanguardRidePhaseStrategy(current, offered).Answer(),
            VanguardMainPhase => await new VanguardMainPhaseStrategy(current, offered).Answer(),
            VanguardAttackPhase => await new VanguardAttackPhaseStrategy(current, offered).Answer(),
            OpenWindow open when ReferenceEquals(open.Window, VanguardAttackPhase.GuardStep) => await guard!.Answer(offered, open),
            var where => throw new NotSupportedException($"{nameof(VanguardInputProvider)} has no gesture for a choice in {where}. Offered: {offered.Describe()}"),
        });
        return Answered(choice.Expect(chosen));
    }

    public async Task<IReadOnlyList<T>> Choose<T>(Choosing<T> question)
    {
        Asked();
        await Presenter.WhenIdle();
        return Answered(question.Expect(await resolvingSkills.Asking(() => choices!.Choose(question))));
    }

    public async Task<bool> Confirm(Confirmation question)
    {
        Asked();
        await Presenter.WhenIdle();
        return Answered(await resolvingSkills.Asking(() => choices!.Confirm(question)));
    }

    /// <summary>Cards onto circles in one answer (a superior call): placed on the enlarged field (FieldSelectScreen).</summary>
    public async Task<IReadOnlyList<Placement<TCard, TSpace>>> Assign<TCard, TSpace>(Assigning<TCard, TSpace> question)
        where TCard : CardBase
        where TSpace : class, ISingleSpace, ICardDestination<TCard>
    {
        Asked();
        await Presenter.WhenIdle();
        return Answered(question.Expect(await resolvingSkills.Asking(() => FieldSelect.Assign(question))));
    }

    // Vanguard asks for no order yet
    public Task<IReadOnlyList<T>> Order<T>(Ordering<T> question) => throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(T), "ordering has no gesture yet");

    // Answered: the details shown while answering go, so they don't cover what the answer starts (a counter blast's
    // flips, a call, a skill resolving)
    private T Answered<T>(T answer)
    {
        CardDetails.Hide();
        return answer;
    }

    private VanguardInput Asked() => input ?? throw new InvalidOperationException($"{nameof(VanguardInputProvider)} was asked before it was activated");
    #endregion

    #region Inspecting
    private void OnZoneInspected(IZoneView view)
    {
        if(view.Space is null) return;
        ShowList(((Node)view).Name, view.Space);
    }

    // A tap that answers what is asked (e.g. choosing a circle) isn't also a look at the card
    private void OnCardInspected(IZoneView view, CardBase card)
    {
        if(view.Space is { } zone && inputBoard.Takes(new CardClicked(card, zone))) return;
        CardDetails.Show(new CardDetail(card, view.Space, GrantedSkills(view.Space)));
    }

    // Long pressing a vanguard lists its soul (unless the long press answers what is asked)
    private void OnLongPressed(IZoneView view, CardBase? card)
    {
        if(view.Space is not Vanguard vanguard) return;
        if(card is not null && inputBoard.Takes(new CardLongPressed(card, vanguard))) return;
        var own = ReferenceEquals(vanguard, PlayArea.Vanguard);
        var area = own ? PlayArea : OpponentPlayArea;
        ShowList(own ? "Soul" : "Opponent's Soul", area.Soul);
    }

    private void ShowList(string title, ICardSpace zone)
    {
        var cards = ZoneSnapshot.Of(zone).Cards.Select(card => card.Card).ToList();
        if(cards.Count == 0) return;
        listedZone = zone;
        CardListComponent.Show(title, cards);
    }

    private void OnListedCardPressed(Card node)
    {
        // While the list offers skills or cards to pick (cards draggable), the pick shows what each card stands for instead
        if(CardListComponent.CardsDraggable) return;
        if(node.CurrentCard is { } card) CardDetails.Show(new CardDetail(card, listedZone, []));
    }

    private void ShowDetails(Card node)
    {
        if(node.CurrentCard is { } card) CardDetails.Show(new CardDetail(card));
    }

    // Skills a unit got from effects: none shown until the rewritten ArC's granted skills are read here
    private static IReadOnlyList<ISkill> GrantedSkills(ICardSpace? zone) => [];
    #endregion

    public override void _ExitTree()
    {
        driver?.Dispose();
    }
}
