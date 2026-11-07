using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using ArC.CardGames.Vanguard;
using Godot;
using static VanguardQuestions;

/// <summary>
/// The human player of Vanguard, and the screen showing the game. Every question ArC asks goes, in order, to:
/// <list type="number">
/// <item>the answer a strategy already gave for it (a gesture that answered several questions at once);</item>
/// <item>the current phase's strategy (mulligan, ride, main, attack), which passes what it doesn't answer to</item>
/// <item>DefaultVanguardAnswers, which answers every question with the old board's mobile prompts (guarding too).</item>
/// </list>
/// Questions wait for the board to finish showing what happened first. Tapping a card shows its details (unless the tap
/// answers what is being asked), long pressing a vanguard lists its soul, pressing the drop zone lists it.
/// </summary>
public partial class VanguardInputProvider : Control, IVanguardPlayerInputProvider, IProvide<IActivationGate>
{
    VanguardBoardView board = null!;
    public VanguardBoardView Board => board;
    CardList CardListComponent = null!;
    SelectFromCardList SelectFromCardListComponent = null!;
    SelectCardsComponent SelectCardsComponent = null!;
    CardDetailsOverlay CardDetails = null!;
    ArcDriver? driver;
    // The zone whose cards CardListComponent is showing
    ICardSpace? listedZone;

    InputBoard inputBoard = null!;
    VanguardInput? input;
    DefaultVanguardAnswers? defaults;
    // Who answers now: the phase's strategy, on top of the default answers
    IVanguardAnswers? answers;

    /// <summary>What the card details may activate: nothing, unless the current scope puts its gate in (InputScope.Activatable).</summary>
    public ActivationGateSlot Activations { get; } = new();
    IActivationGate IProvide<IActivationGate>.Value() => Activations;

    public VanguardPlayArea PlayArea { get; private set; } = null!;
    public VanguardPlayArea OpponentPlayArea { get; private set; } = null!;
    public VanguardSkillService SkillService { get; private set; } = null!;
    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;

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
        // A card offered to pick (a heal, a soul blast, the mulligan): its details, to read before picking
        SelectFromCardListComponent.CardPressed += node => ShowDetails(node);
        SelectCardsComponent.CardPressed += node => ShowDetails(node);
        // The details stay usable while a card list is open (reading them doesn't close it)
        CardListComponent.LetThrough(CardDetails.Details);
        // Card flights draw above the board and below the HUD (see the scene)
        Presenter.Flights = GetNode<CardFlightLayer>("%CardFlights");
        // The HUD's buttons, wherever they are in the scene (the board holds them); the hint is the player's play area's
        // (each play area scene has one under its phase label)
        inputBoard = new(Activations, Descendants(this).OfType<BoardButtonView>(),
            Descendants(board.PlayerArea).OfType<InputHintLabel>().FirstOrDefault(), Descendants(this).OfType<AimIndicatorLayer>().FirstOrDefault());
        // A skill activated from the details: they close, the skill goes on on the board
        inputBoard.Gestures.Subscribe<SkillActivated>(_ => CardDetails.Hide());
    }

    /// <summary>Shows <paramref name="session"/> on the board and the HUD, face-down cards in <paramref name="sleeves"/>. Call once its game has started.</summary>
    public void Show(VanguardGameSession session, BoardSleeves sleeves)
    {
        driver?.Dispose();
        driver = board.Show(session, Presenter, sleeves);
        driver.ZoneInspected += OnZoneInspected;
        driver.CardInspected += OnCardInspected;
        driver.LongPressed += OnLongPressed;
        inputBoard.Attach(driver);
    }

    /// <summary>Makes this the player of <paramref name="playArea"/> (the session builder calls it once the game started).</summary>
    public void Activate(VanguardPlayArea playArea, VanguardPlayArea opponentPlayArea, VanguardSkillService skillService, GameContext gameContext)
    {
        PlayArea = playArea;
        OpponentPlayArea = opponentPlayArea;
        SkillService = skillService;
        var presentation = new PresentationContext(Presenter, board.Sfx, BoardSide.Player);
        input = new(this, gameContext, inputBoard, presentation, this, SelectFromCardListComponent, SelectCardsComponent, CardListComponent, CardDetails.Show);
        answers = defaults = new(input);
    }

    IDisposable? phaseSubscription;

    public void SetEventBus(VanguardEventBus eventBus)
    {
        phaseSubscription?.Dispose();
        // Published before each phase runs (pre-phases too), so the strategy is set before the phase asks anything
        phaseSubscription = eventBus.Subscribe<PhaseStarted>(started => OnPhaseStarted(started.Phase));
    }

    // Each phase's strategy on top of the default answers (guarding, skills and triggers are the defaults' prompts)
    private Task OnPhaseStarted(IPhase phase)
    {
        if(input is null || defaults is null) return Task.CompletedTask;
        answers = phase switch
        {
            MulliganPhase => new VanguardMulliganStrategy(input, defaults),
            RidePhase => new VanguardRidePhaseStrategy(input, defaults),
            MainPhase => new VanguardMainPhaseStrategy(input, defaults),
            VanguardAttackPhase => new VanguardAttackPhaseStrategy(input, defaults),
            _ => defaults,
        };
        return Task.CompletedTask;
    }

    #region Questions
    // A known answer first; else the board catches up, then the strategies answer
    private async Task<T> Ask<T>(Question<T> question, Func<IVanguardAnswers, Task<T>> ask)
    {
        var current = input ?? throw new InvalidOperationException($"{nameof(VanguardInputProvider)} was asked {question} before it was activated");
        if(current.Answers.TryTake(question, out var known)) return known;
        await Presenter.WhenIdle();
        return await ask(answers!);
    }

    // A new action starts: whatever the last one was given must have been asked for
    private void StartAction()
    {
        input?.Answers.EnsureNoneLeft("by the last action");
        input?.Narrowing.Clear();
    }

    public Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        StartAction();
        return Ask(MainPhaseAction, answers => answers.RequestMainPhaseAction(actions));
    }

    public Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        StartAction();
        return Ask(AttackPhaseAction, answers => answers.RequestAttackPhaseAction(actions));
    }

    public Task<CardBase> SelectCardFromHand() => Ask(CardFromHand, answers => answers.SelectCardFromHand());
    public Task<CardBase?> SelectCardFromHandOrNot() => Ask(CardFromHandOrNot, answers => answers.SelectCardFromHandOrNot());
    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum) => Ask(CardsFromHand, answers => answers.SelectCardsFromHandRange(minimum, maximum));
    public Task<UnitCircle> SelectOwnUnitCircle(UnitSelector selector) => Ask(OwnCircle, answers => answers.SelectOwnUnitCircle(selector));
    public Task<UnitCircle> SelectOpponentCircle(UnitSelector selector) => Ask(OpponentCircle, answers => answers.SelectOpponentCircle(selector));
    public Task<List<UnitCircle>> SelectOwnUnitCircles(UnitSelector selector) => Ask(OwnCircles, answers => answers.SelectOwnUnitCircles(selector));
    public Task<List<UnitCircle>> SelectOwnUnitCirclesRange(UnitSelector selector, int minimum, int maximum) => Ask(OwnCirclesRange, answers => answers.SelectOwnUnitCirclesRange(selector, minimum, maximum));
    public Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills) => Ask(SkillToActivate, answers => answers.SelectSkillToActivate(skills));
    public Task<PendingSkill> SelectSkillToInvoke(IReadOnlyList<PendingSkill> skills) => Ask(SkillToInvoke, answers => answers.SelectSkillToInvoke(skills));
    public Task<VanguardCard> SelectCardFromDamageZone() => Ask(CardFromDamageZone, answers => answers.SelectCardFromDamageZone());
    public Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade) => Ask(CardFromDeck, answers => answers.SelectCardFromDeck(minGrade, maxGrade));
    public Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount) => Ask(CardsFromDamageZone, answers => answers.SelectCardsFromDamageZone(amount));
    public Task<List<VanguardCard>> SelectCardsFromSoul(int amount) => Ask(CardsFromSoul, answers => answers.SelectCardsFromSoul(amount));
    public Task<bool> QueryActivateSkill(VanguardCard invoker, VanguardAutomaticSkill automaticSkill) => Ask(ActivateTrigger, answers => answers.QueryActivateSkill(invoker, automaticSkill));
    public Task<VanguardSkills.SkillAction> SelectSkillOption(IReadOnlyList<VanguardSkills.SkillAction> options, VanguardSkillContextObject context)
        => Ask(SkillOption, answers => answers.SelectSkillOption(options, context));
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

    // Skills a unit got from effects (e.g. "this unit gets '[AUTO] ...' until end of turn")
    private IReadOnlyList<ISkill> GrantedSkills(ICardSpace? zone)
        => zone is UnitCircle circle && SkillService is not null ? PlayArea.EffectService.GetGrantedSkills(circle) : [];
    #endregion

    public override void _ExitTree()
    {
        driver?.Dispose();
    }

    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach(var child in node.GetChildren())
        {
            yield return child;
            foreach(var descendant in Descendants(child)) yield return descendant;
        }
    }
}
