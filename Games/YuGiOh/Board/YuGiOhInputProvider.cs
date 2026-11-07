using ArC.CardGames;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Setup;
using ArC.CardGames.YuGiOh;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static YuGiOhQuestions;

/// <summary>
/// The human player of YuGiOh, and the screen showing the game. Every question ArC asks goes, in order, to:
/// <list type="number">
/// <item>the answer a strategy already gave for it (a gesture that answered several questions at once);</item>
/// <item>the current strategy (e.g. YuGiOhMainPhaseStrategy in the main phase), which passes what it doesn't answer to</item>
/// <item>DefaultYuGiOhAnswers, which answers every question with plain prompts.</item>
/// </list>
/// Questions wait for the board to finish showing what happened first.
/// </summary>
public partial class YuGiOhInputProvider : Control, IYuGiOhPlayerInputProvider, IProvide<PresentationContext>, IProvide<IActivationGate>
{
    YuGiOhBoard board = null!;
    public YuGiOhBoard Board => board;
    CardList CardListComponent = null!;
    SelectFromCardList SelectFromCardListComponent = null!;
    CardDetailsView CardDetails = null!;
    // The chain being responded to, shown beside the responses
    CardListPanel ChainView = null!;
    ArcDriver? driver;
    // The zone whose cards CardListComponent is showing
    IZoneView? listedZone;

    InputBoard inputBoard = null!;
    YuGiOhInput? input;
    DefaultYuGiOhAnswers? defaults;
    // Who answers now: the phase's strategy, on top of the default answers
    IYuGiOhAnswers? answers;

    /// <summary>What the card details may activate: nothing, unless the current scope puts its gate in (InputScope.Activatable).</summary>
    public ActivationGateSlot Activations { get; } = new();
    IActivationGate IProvide<IActivationGate>.Value() => Activations;

    public YuGiOhPlayArea PlayArea { get; private set; } = null!;
    public YuGiOhPlayArea OpponentPlayArea { get; private set; } = null!;
    public YuGiOhSkillService SkillService { get; private set; } = null!;
    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;

    public Presenter Presenter { get; } = new();

    public override void _Ready()
    {
        board = GetNode<YuGiOhBoard>($"%{nameof(YuGiOhBoard)}");
        CardListComponent = GetNode<CardList>($"%{nameof(CardListComponent)}");
        CardListComponent.CardPressed += OnListedCardPressed;
        SelectFromCardListComponent = GetNode<SelectFromCardList>($"%{nameof(SelectFromCardListComponent)}");
        // A card offered to pick (a search, a cost, materials): its details, to read before picking
        SelectFromCardListComponent.CardPressed += node =>
        {
            if(node.CurrentCard is { } card) CardDetails.Show(new CardDetail(card));
        };
        CardDetails = GetNode<CardDetailsView>($"%{nameof(CardDetails)}");
        ChainView = GetNode<CardListPanel>($"%{nameof(ChainView)}");
        ChainView.Hide();
        // The details and the chain stay usable while a card list is open (reading them doesn't close it)
        CardListComponent.LetThrough(CardDetails);
        CardListComponent.LetThrough(ChainView);
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
        driver.BindAll<ArC.CardGames.Rules.GameSession>(this, session);
        inputBoard.Attach(driver);
    }

    /// <summary>Makes this the player of <paramref name="playArea"/> (the session builder calls it once the game started).</summary>
    public void Activate(YuGiOhPlayArea playArea, YuGiOhPlayArea opponentPlayArea, YuGiOhSkillService skillService)
    {
        PlayArea = playArea;
        OpponentPlayArea = opponentPlayArea;
        SkillService = skillService;
        input = new(this, inputBoard, this, SelectFromCardListComponent, CardListComponent, ChainView, CardDetails.Show, CardDetails.SkillTextInterpreter());
        answers = defaults = new(input);
    }

    IDisposable? phaseSubscription;

    public void SetEventBus(YuGiOhEventBus eventBus)
    {
        phaseSubscription?.Dispose();
        // Published before each phase runs (pre-phases too), so the strategy is set before the phase asks anything
        phaseSubscription = eventBus.Subscribe<ArC.CardGames.Rules.PhaseStarted>(started => OnPhaseStarted(started.Phase));

        // The player's own skill, pinned while it's activated or resolved (what they're asked meanwhile is for it)
        foreach(var subscription in skillSubscriptions) subscription.Dispose();
        var resolving = GetNode<ResolvingSkillDetails>("%ResolvingSkill");
        skillSubscriptions =
        [
            eventBus.Subscribe<SkillStarting>(started =>
            {
                if(ReferenceEquals(started.Owner, PlayArea)) resolving.Started(started.Source.Card, started.Skill);
            }),
            eventBus.Subscribe<SkillFinished>(finished =>
            {
                if(ReferenceEquals(finished.Owner, PlayArea)) resolving.Finished(finished.Skill);
            }),
        ];
    }

    List<IDisposable> skillSubscriptions = [];

    // Each phase's strategy on top of the default answers (main and battle by touch gestures; the rest by the defaults)
    private Task OnPhaseStarted(IPhase phase)
    {
        if(input is null || defaults is null) return Task.CompletedTask;
        answers = phase switch
        {
            MainPhase => new YuGiOhMainPhaseStrategy(input, defaults),
            AttackPhase => new YuGiOhBattlePhaseStrategy(input, defaults),
            _ => defaults,
        };
        return Task.CompletedTask;
    }

    #region Questions
    // A known answer first; else the board catches up, then the strategies answer
    private async Task<T> Ask<T>(Question<T> question, Func<IYuGiOhAnswers, Task<T>> ask)
    {
        var current = input ?? throw new InvalidOperationException($"{nameof(YuGiOhInputProvider)} was asked {question} before it was activated");
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
    public Task<IYuGiOhZone> SelectOwnZone(ZoneSelector selector) => Ask(OwnZone, answers => answers.SelectOwnZone(selector));
    public Task<IYuGiOhZone> SelectOpponentZone(ZoneSelector selector) => Ask(OpponentZone, answers => answers.SelectOpponentZone(selector));
    public Task<List<IYuGiOhZone>> SelectOwnZonesRange(ZoneSelector selector, int minimum, int maximum) => Ask(OwnZones, answers => answers.SelectOwnZonesRange(selector, minimum, maximum));
    public Task<YuGiOhNonPassiveSkill> SelectSkillToActivate(List<YuGiOhNonPassiveSkill> skills) => Ask(SkillToActivate, answers => answers.SelectSkillToActivate(skills));
    public Task<bool> QuerySetCard<TCard>(TCard card) where TCard : YuGiOhCard => Ask(SetCard, answers => answers.QuerySetCard(card));
    public Task<YuGiOhCard> SelectCardFromExtraDeck() => Ask(CardFromExtraDeck, answers => answers.SelectCardFromExtraDeck());
    public Task<YuGiOhCard> SelectCardFromDeck() => Ask(CardFromDeck, answers => answers.SelectCardFromDeck());
    public Task<YuGiOhCard> SelectCardFromGraveyard() => Ask(CardFromGraveyard, answers => answers.SelectCardFromGraveyard());
    public Task<YuGiOhCard> SelectCardFromOpponentHand() => Ask(CardFromOpponentHand, answers => answers.SelectCardFromOpponentHand());
    public Task<YuGiOhCard> SelectCardFromOpponentGraveyard() => Ask(CardFromOpponentGraveyard, answers => answers.SelectCardFromOpponentGraveyard());
    public Task<string> DeclareCardName() => Ask(CardName, answers => answers.DeclareCardName());
    public Task<List<YuGiOhCard>> SelectXYZMaterials(MonsterZone zone, int amount) => Ask(MaterialsOfXYZ, answers => answers.SelectXYZMaterials(zone, amount));
    public Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain) => Ask(ChainResponse, answers => answers.SelectChainResponse(chain));
    public Task<bool> QueryActivateSkill(YuGiOhCard invoker, YuGiOhAutomaticSkill skill) => Ask(ActivateTrigger, answers => answers.QueryActivateSkill(invoker, skill));
    public Task<YuGiOhSkills.SkillAction> SelectSkillOption(IReadOnlyList<YuGiOhSkills.SkillAction> options, YuGiOhSkillContextObject context)
        => Ask(SkillOption, answers => answers.SelectSkillOption(options, context));
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

    private void OnCardInspected(IZoneView view, CardBase card)
    {
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
