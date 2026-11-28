using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using ArC.CardGames.Skills;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>
/// The human player of YuGiOh, and the screen showing the game. ArC asks four questions; each waits for the board to
/// finish showing what happened, then goes to:
/// <list type="bullet">
/// <item>ChooseAction: the strategy of the phase or window asking (the Main Phases, the Battle Phase, the chain
/// responses, priority and summon windows), which matches a gesture to one of the offered requests; an unknown phase or
/// window throws;</item>
/// <item>Choose and Confirm: YuGiOhChoices, by the question's shape (zones, cards by where they are, options, effects),
/// on the input screens Vanguard uses (field selection, card selection, swipe-up picker).</item>
/// </list>
/// Every answer is checked against what was offered (Expect) before it goes back. Tapping a card shows its details (unless
/// the tap answers what is being asked), tapping a listing zone lists it.
/// </summary>
public partial class YuGiOhInputProvider : Control, IPlayerInput, IProvide<PresentationContext>, IProvide<IActivationGate>
{
    YuGiOhBoard board = null!;
    public YuGiOhBoard Board => board;
    CardList CardListComponent = null!;
    CardDetailsOverlay CardDetails = null!;
    InputScreens screens = null!;
    YuGiOhSwipeUpPicker SwipeUp = null!;
    // The board and the HUD rows under it: what the field selection screen enlarges (no container holds it)
    Control BoardLayout = null!;
    ArcDriver? driver;

    /// <summary>The location tags on cards offered to pick (YuGiOhCardTags.tres).</summary>
    [Export] public YuGiOhCardTags CardTags { get; set; } = null!;
    // The zone whose cards CardListComponent is showing
    IZoneView? listedZone;

    InputBoard inputBoard = null!;
    YuGiOhInput? input;
    YuGiOhChoices? choices;
    YuGiOhResponsesPrompt? responses;

    /// <summary>What the card details may activate: nothing, unless the current scope puts its gate in (InputScope.Activatable).</summary>
    public ActivationGateSlot Activations { get; } = new();
    IActivationGate IProvide<IActivationGate>.Value() => Activations;

    public YuGiOhPlayArea PlayArea { get; private set; } = null!;
    public YuGiOhPlayArea OpponentPlayArea { get; private set; } = null!;

    // Board animations play from this queue; prompts wait until it's done
    public Presenter Presenter { get; } = new();

    public override void _Ready()
    {
        board = GetNode<YuGiOhBoard>($"%{nameof(YuGiOhBoard)}");
        CardListComponent = GetNode<CardList>($"%{nameof(CardListComponent)}");
        CardListComponent.CardPressed += OnListedCardPressed;
        CardDetails = GetNode<CardDetailsOverlay>($"%{nameof(CardDetails)}");
        BoardLayout = GetNode<Control>($"%{nameof(BoardLayout)}");
        screens = InputScreens.Find(this, CardDetails.Show);
        SwipeUp = screens.SwipeUp as YuGiOhSwipeUpPicker
            ?? throw new InvalidOperationException($"{GetPath()}: %{nameof(InputScreens.SwipeUp)} is a {nameof(YuGiOhSwipeUpPicker)} (YuGiOhSwipeUpPicker.tscn), to show the chain");
        if(CardTags is null) throw new InvalidOperationException($"{GetPath()}: set {nameof(CardTags)} (YuGiOhCardTags.tres)");
        CardTags.EnsureComplete();
        // The details stay usable while a card list is open (reading them doesn't close it)
        CardListComponent.LetThrough(CardDetails.Details);
        // Card flights draw above the board and below the HUD (see the scene)
        Presenter.Flights = GetNode<CardFlightLayer>("%CardFlights");
        // The HUD's buttons and hint, wherever they are in the scene
        inputBoard = new(Activations, this.Descendants<BoardButtonView>(),
            this.Descendants<InputHintLabel>().FirstOrDefault(), this.Descendants<AimIndicatorLayer>().FirstOrDefault());
    }

    // For the board's and HUD's components showing the game in step with the board (e.g. the phase label, life points)
    PresentationContext? hudPresentation;
    PresentationContext IProvide<PresentationContext>.Value() => hudPresentation ??= new(Presenter, board.Sfx, BoardSide.Both);

    /// <summary>Shows <paramref name="session"/> on the board and the HUD. Call once its game has started.</summary>
    public void Show(YuGiOhGameSession session, BoardSleeves sleeves)
    {
        driver?.Dispose();
        driver = board.Show(session, Presenter, sleeves);
        driver.ZoneInspected += OnZoneInspected;
        driver.CardInspected += OnCardInspected;
        driver.BindAll<GameSession>(this, session);
        screens.Attach(inputBoard, driver, BoardLayout);
        inputBoard.Attach(driver);
    }

    /// <summary>Makes this the player of <paramref name="playArea"/> (the session builder calls it once the game started).</summary>
    public void Activate(YuGiOhPlayArea playArea, YuGiOhPlayArea opponentPlayArea)
    {
        PlayArea = playArea;
        OpponentPlayArea = opponentPlayArea;
        input = new(this, inputBoard, this, CardListComponent, screens, SwipeUp, CardTags, CardDetails.Show);
        choices = new(input);
        responses = new(input);
    }

    List<IDisposable> subscriptions = [];

    // The player's skills being used (pinned while they ask something)
    ResolvingSkills resolving = new();

    /// <summary>Follows the game's skills and chain: the player's own skill is pinned while it's used; the chain so far is kept for the responses.</summary>
    public void SetEventBus(EventBus eventBus)
    {
        foreach(var subscription in subscriptions) subscription.Dispose();
        resolving = new ResolvingSkills();
        InputScreens.FollowSkills(this, resolving);
        subscriptions =
        [
            eventBus.Subscribe<SkillUseStartedEvent>(started =>
            {
                input?.Runs.Add(started.Run);
                if(IsOwn(started.Run)) resolving.Started(started.Run.Self, started.Run.Skill);
                return Task.CompletedTask;
            }),
            eventBus.Subscribe<SkillUseEndedEvent>(ended =>
            {
                input?.Runs.Remove(ended.Run);
                if(IsOwn(ended.Run)) resolving.Finished(ended.Run.Skill);
                return Task.CompletedTask;
            }),
            eventBus.Subscribe<ChainLinkAdded>(added =>
            {
                input?.Chain.Add(added.Link);
                return Task.CompletedTask;
            }),
            eventBus.Subscribe<ChainLinkResolved>(resolved =>
            {
                input?.Chain.Remove(resolved.Link);
                return Task.CompletedTask;
            }),
            eventBus.Subscribe<ChainResolved>(_ =>
            {
                input?.Chain.Clear();
                return Task.CompletedTask;
            }),
        ];
    }

    private bool IsOwn(SkillRun run) => PlayArea is not null && ReferenceEquals(run.PlayArea, PlayArea);

    #region Questions
    public async Task<Request> ChooseAction(ActionChoice choice)
    {
        var current = Asked();
        await Presenter.WhenIdle();
        var offered = new Offered(choice);
        var chosen = await resolving.Asking(async () => choice.Where switch
        {
            YuGiOhMainPhase => await new YuGiOhMainPhaseStrategy(current, offered).Answer(),
            YuGiOhBattlePhase => await new YuGiOhBattlePhaseStrategy(current, offered).Answer(),
            OpenWindow open when IsResponseWindow(open) => await responses!.Answer(offered, open),
            var where => throw new NotSupportedException($"{nameof(YuGiOhInputProvider)} has no gesture for a choice in {where}. Offered: {offered.Describe()}"),
        });
        return Answered(choice.Expect(chosen));
    }

    private static bool IsResponseWindow(OpenWindow open)
        => ReferenceEquals(open.Window, YuGiOhWindows.Responses) || ReferenceEquals(open.Window, YuGiOhWindows.Priority) || ReferenceEquals(open.Window, YuGiOhWindows.Summon);

    public async Task<IReadOnlyList<T>> Choose<T>(Choosing<T> question)
    {
        Asked();
        await Presenter.WhenIdle();
        return Answered(question.Expect(await resolving.Asking(() => choices!.Choose(question))));
    }

    public async Task<bool> Confirm(Confirmation question)
    {
        Asked();
        await Presenter.WhenIdle();
        return Answered(await resolving.Asking(() => choices!.Confirm(question)));
    }

    // YuGiOh asks for no order yet
    public Task<IReadOnlyList<T>> Order<T>(Ordering<T> question) => throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(T), "ordering has no gesture yet");

    // Answered: the details shown while answering go, so they don't cover what the answer starts
    private T Answered<T>(T answer)
    {
        CardDetails.Hide();
        return answer;
    }

    private YuGiOhInput Asked() => input ?? throw new InvalidOperationException($"{nameof(YuGiOhInputProvider)} was asked before it was activated");
    #endregion

    #region Inspecting
    private void OnZoneInspected(IZoneView view)
    {
        if(view.Space is null) return;
        var cards = ZoneSnapshot.Of(view.Space).Cards.Select(card => card.Card).ToList();
        if(cards.Count == 0) return;
        listedZone = view;
        CardListComponent.Show(((Node)view).Name, cards);
    }

    // A tap that answers what is asked (e.g. choosing a zone) isn't also a look at the card
    private void OnCardInspected(IZoneView view, CardBase card)
    {
        if(view.Space is { } zone && inputBoard.Takes(new CardClicked(card, zone))) return;
        CardDetails.Show(new CardDetail(card, view.Space, []));
    }

    private void OnListedCardPressed(Card node)
    {
        // While the list offers effects to pick (cards draggable), the pick shows each card's own effect instead
        if(CardListComponent.CardsDraggable) return;
        if(node.CurrentCard is { } card) CardDetails.Show(new CardDetail(card, listedZone?.Space, []));
    }
    #endregion

    public override void _ExitTree()
    {
        driver?.Dispose();
    }
}
