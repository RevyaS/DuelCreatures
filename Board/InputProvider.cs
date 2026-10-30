using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using Godot;

public partial class InputProvider : Control, IVanguardPlayerInputProvider
{
    VanguardBoard board = null!;
    public VanguardBoard Board => board;

    public VanguardPlayArea PlayArea { get; private set; } = null!;

    public VanguardPlayArea OpponentPlayArea { get; private set; } = null!;

    public VanguardSkillService SkillService { get; private set; } = null!;

    // Board animations play from this queue; prompts wait until it's done
    public Presenter Presenter { get; } = new();

    VanguardCard CurrentVanguard => PlayArea.Vanguard.Card!;
    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;
    GameContext GameContext = null!;

    private bool Active = false;

    IInputProviderStrategy strategy = null!;

    SelectCardsComponent SelectCardsComponent = null!;
    SelectFromCardList SelectFromCardListComponent = null!;
    CardList CardListComponent = null!;
    CardInfo CardInfoComponent = null!;

    Stack<IInputProviderStrategy> strategyStack = new();

    public override void _Ready()
    {
        board = GetNode<VanguardBoard>($"%{nameof(VanguardBoard)}");

        // Card flights draw above the board and below the HUD
        var flights = new CardFlightLayer { Name = "CardFlights" };
        AddChild(flights);
        MoveChild(flights, board.GetIndex() + 1);
        Presenter.Flights = flights;
        SelectCardsComponent = GetNode<SelectCardsComponent>($"%{nameof(SelectCardsComponent)}");
        SelectFromCardListComponent = GetNode<SelectFromCardList>($"%{nameof(SelectFromCardListComponent)}");
        CardListComponent = GetNode<CardList>($"%{nameof(CardListComponent)}");
        CardListComponent.CardPressed += OnCardListCardPressed;

        CardInfoComponent = GetNode<CardInfo>($"%{nameof(CardInfoComponent)}");

        board.PlayerArea.DropZone.SetCardList(CardListComponent);
        board.OppArea.DropZone.SetCardList(CardListComponent);

        Board.PlayerSoulPressed += OnPlayerSoulPressed;
        Board.OppSoulPressed += OnOppSoulPressed;
        Board.HandCardPressed += OnHandCardPressed;
        Board.UnitCircleCardPressed += OnUnitCircleCardPressed;
        Board.PlayerUnitCircleCardPressed += OnPlayerUnitCircleCardPressed;
        Board.DamageZoneCardPressed += OnDamageZoneCardPressed;
        Board.PublicCardPressed += OnPublicCardPressed;
        SelectCardsComponent.CardPressed += OnPublicCardPressed;
        SelectFromCardListComponent.CardPressed += OnPublicCardPressed;
    }

    private void OnPlayerUnitCircleCardPressed(UnitCircleComponent unitCircle, Card card)
    {
        var enableActivateButton = VanguardGameRules.UnitCircleCanActivateSkill(this, unitCircle.UnitCircle);
        ShowCardInfo(card, enableActivateButton, GrantedSkills(unitCircle));
    }

    // Skills a unit got from effects (e.g. "this unit gets '[AUTO] ...' until end of turn")
    private VanguardSkill[] GrantedSkills(UnitCircleComponent? unitCircle)
    {
        if(unitCircle?.UnitCircle is not { } circle) return [];
        return PlayArea.EffectService.GetGrantedSkills(circle).Cast<VanguardSkill>().ToArray();
    }

    private void OnPlayerSoulPressed()
    {
        CardListComponent.Show("Soul", PlayArea.Soul.Cards.Cast<VanguardCard>().ToList());
    }

    private void OnOppSoulPressed()
    {
        CardListComponent.Show("Opponent's Soul", OpponentPlayArea.Soul.Cards.Cast<VanguardCard>().ToList());
    }

    public void Activate(VanguardPlayArea playArea, VanguardPlayArea oppPlayArea, VanguardSkillService vanguardSkillService, GameContext gameContext)
    {
        Active = true;
        OpponentPlayArea = oppPlayArea;
        PlayArea = playArea;
        GameContext = gameContext;
        SkillService = vanguardSkillService;
    }

    public void Deactivate()
    {
        Active = false;
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
        eventBus.SkillExecution += OnSkillExecution;
        eventBus.SkillExecuted += OnSkillExecuted;
        eventBus.OnDamageChecked += OnDamageChecked;
        eventBus.OnDriveChecked += OnDriveChecked;
        eventBus.TriggerResolved += OnTriggerResolved;
    }

    private Task OnDriveChecked(VanguardPlayArea area, VanguardCard card)
    {
        if(ReferenceEquals(PlayArea, area))
        {
            PushProviderStrategy(new TriggerStrategy(board, PlayArea, SelectFromCardListComponent, GameContext));
        }
        return Task.CompletedTask;
    }

    private Task OnDamageChecked(VanguardPlayArea area, VanguardCard card)
    {
        if(ReferenceEquals(PlayArea, area))
        {
            PushProviderStrategy(new TriggerStrategy(board, PlayArea, SelectFromCardListComponent, GameContext));
        }
        return Task.CompletedTask;
    }

    private Task OnTriggerResolved()
    {
        if(strategy is TriggerStrategy)
        {
            PopProviderStrategy();
        }
        return Task.CompletedTask;
    }

    private void OnSkillExecuted(VanguardSkillInvocationSource invocationSource, VanguardSkill skill)
    {
        if(IsOwnSkill(invocationSource))
        {
            PopProviderStrategy();
        }
    }

    private void OnSkillExecution(VanguardSkillInvocationSource invocationSource, VanguardSkill skill)
    {
        if(IsOwnSkill(invocationSource))
        {
            PushProviderStrategy(new SkillExecutionStrategy(board, PlayArea, CardListComponent, SelectFromCardListComponent, invocationSource.Card.Name));
        }
    }

    // Skills can come from a circle, the hand, the soul or the (shared) guardian circle
    private bool IsOwnSkill(VanguardSkillInvocationSource source) => source switch
    {
        VanguardUnitCircleInvocationSource unitCircle => PlayArea.OwnsUnitCircle(unitCircle.UnitCircle),
        VanguardHandInvocationSource hand => ReferenceEquals(hand.Hand, PlayArea.Hand),
        VanguardSoulInvocationSource soul => ReferenceEquals(soul.Soul, PlayArea.Soul),
        // Both players share one guardian circle: its skills are the defender's
        VanguardGuardianCircleInvocationSource => CurrentGuard() is { } guard && PlayArea.OwnsUnitCircle(guard.TargetCircle),
        _ => false,
    };

    private GuardState? CurrentGuard() => GameContext.States.OfType<GuardState>().FirstOrDefault();

    GuardState? guardStrategyFor = null;

    // ArC guards inside a GuardState (no event): when it asks this player for guards or
    // interceptors, switch to the guard step's UI, once per battle
    private void EnsureGuardStrategy()
    {
        var guard = CurrentGuard();
        if(guard is null || !PlayArea.OwnsUnitCircle(guard.TargetCircle) || ReferenceEquals(guard, guardStrategyFor)) return;

        guardStrategyFor = guard;
        SetProviderStrategy(new GuardPhaseStrategy(board, board.GetPlayerUnitCircleComponent(guard.TargetCircle), PlayArea, guard));
    }

    private Task OnPhaseChanged(IPhase phase)
    {
        if(phase is not IManualPhase) return Task.CompletedTask;

        switch(phase)
        {
            case MulliganPhase:
                SetProviderStrategy(new MulliganPhaseStrategy(board, SelectCardsComponent));
                return Task.CompletedTask;
            case RidePhase:
                SetProviderStrategy(new RidePhaseStrategy(board));
                return Task.CompletedTask;
            case MainPhase:
                SetProviderStrategy(new MainPhaseStrategy(board, CardInfoComponent, GameContext));
                return Task.CompletedTask;
            case VanguardAttackPhase:
                SetProviderStrategy(new AttackPhaseStrategy(board, GameContext));
                return Task.CompletedTask;
        }
        throw new NotSupportedException($"{phase.GetType().Name} is not supported yet");
    }

    public void SetProviderStrategy(IInputProviderStrategy strategy)
    {
        strategyStack.Clear();
        strategyStack.Push(strategy);
        SetProviderStrategyCore();
    }

    public void PushProviderStrategy(IInputProviderStrategy strategy)
    {
        strategyStack.Push(strategy);
        SetProviderStrategyCore();
    }

    public void PopProviderStrategy()
    {
        strategyStack.Pop();
        SetProviderStrategyCore();
    }

    private void SetProviderStrategyCore()
    {
        strategy = strategyStack.Peek();
    }

    private void OnHandCardPressed(Card card)
    {
        bool enableActivateSkill = false;
        if(GameContext.GameState is SelectMainPhaseActionState)
        {
            if(VanguardGameRules.HandCardCanActivateSkill(this, Board.PlayerHand.Hand, (VanguardCard)card.CurrentCard))
            {
                enableActivateSkill = true;
            }
        }
        ShowCardInfo(card, enableActivateSkill);
    }

    private void OnUnitCircleCardPressed(Card card)
    {
        var circle = Board.OppArea.Circles.FirstOrDefault(circle => ReferenceEquals(circle.CurrentCard, card));
        ShowCardInfo(card, grantedSkills: GrantedSkills(circle));
    }

    private void OnCardListCardPressed(Card card)
    {
        ShowCardInfo(card);
    }

    private void OnDamageZoneCardPressed(Card card)
    {
        ShowCardInfo(card);
    }

    private void OnPublicCardPressed(Card card)
    {
        ShowCardInfo(card);
    }

    private void ShowCardInfo(Card card, bool canActivate = false, VanguardSkill[]? grantedSkills = null)
    {
        // Above every other HUD panel, e.g. the card selection it was opened from
        CardInfoComponent.MoveToFront();
        CardInfoComponent.Show((VanguardCard)card.CurrentCard, canActivate, grantedSkills);
    }

    // Don't prompt the player before the board has caught up with the game
    private async Task<T> WhenPresented<T>(Func<Task<T>> request)
    {
        await Presenter.WhenIdle();
        return await request();
    }

    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        return WhenPresented(() =>
        {
            EnsureGuardStrategy();
            return ((ISelectCardsFromHand)strategy).SelectCardsFromHand(minimum, maximum);
        });
    }

    public Task<CardBase?> SelectCardFromHandOrNot()
    {
        return WhenPresented(() => ((ISelectCardFromHandOrNot)strategy).SelectCardFromHandOrNot(CurrentVanguard));
    }

    public Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        return WhenPresented(() => ((IRequestMainPhaseAction)strategy).RequestMainPhaseAction(actions));
    }

    public Task<UnitCircle> SelectOpponentCircle(UnitSelector selector)
    {
        return WhenPresented(() => ((ISelectOpponentCircle)strategy).SelectOpponentCircle(selector));
    }

    public Task<UnitCircle> SelectOwnUnitCircle(UnitSelector selector)
    {
        return WhenPresented(() => ((ISelectOwnUnitCircle)strategy).SelectOwnUnitCircle(selector));
    }

    public Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills)
    {
        return WhenPresented(() => ((ISelectSkillToActivate)strategy).SelectSkillToActivate(skills));
    }

    public Task<VanguardCard> SelectCardFromDamageZone()
    {
        return WhenPresented(() => ((ISelectCardFromDamageZone)strategy).SelectCardFromDamageZone());
    }

    public Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade)
    {
        return WhenPresented(() => ((ISelectCardFromDeck)strategy).SelectCardFromDeck(minGrade, maxGrade));
    }

    public Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount)
    {
        return WhenPresented(() => {
            if(GameContext.GameState is CounterBlastPhaseState)
            {
                return ((ISelectCardsToCounterBlast)strategy).SelectCardsFromDamageZone(amount);
            }
            if(GameContext.GameState is CounterChargePhaseState)
            {
                return ((ISelectCardsToCounterCharge)strategy).SelectCardsFromDamageZone(amount);
            }
            throw new NotImplementedException();
        });
    }

    public Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
    {
        return WhenPresented(() => ((ISelectCardsFromSoul)strategy).SelectCardsFromSoul(amount));
    }

    public Task<bool> QueryActivateSkill(VanguardCard Invoker, VanguardAutomaticSkill Skill)
    {
        return WhenPresented(() => ((IQueryActivateSkill)strategy).QueryActivateSkill(Invoker, Skill));
    }

    public Task<CardBase> SelectCardFromHand()
    {
        return WhenPresented(() => ((ISelectCardFromHand)strategy).SelectCardFromHand());
    }

    public Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        return WhenPresented(() => ((IRequestAttackPhaseAction)strategy).RequestAttackPhaseAction(actions));
    }

    public Task<PendingSkill> SelectSkillToInvoke(IReadOnlyList<PendingSkill> skills)
    {
        return WhenPresented(() => PromptSkillOrder(skills));
    }

    // Several of this player's skills triggered at once: drag the one to resolve first out of
    // the list (the rest are offered again after it, as it may change what they can do)
    private async Task<PendingSkill> PromptSkillOrder(IReadOnlyList<PendingSkill> skills)
    {
        if(skills.Count == 1) return skills[0];

        CardListComponent.Show("Select the skill to resolve first", skills.Select(skill => skill.Source.Card).ToList());
        CardListComponent.CanClose = false;
        CardListComponent.BaseDroppable = true;
        CardListComponent.CardsDraggable = true;

        TaskCompletionSource<PendingSkill> completionSource = new();
        Action<Card> cardDroppedHandler = (card) =>
        {
            var chosen = skills.FirstOrDefault(skill => ReferenceEquals(skill.Source.Card, card.CurrentCard)) ?? skills[0];
            completionSource.TrySetResult(chosen);
        };
        CardListComponent.CardDroppedOutside += cardDroppedHandler;

        var result = await completionSource.Task;

        CardListComponent.CardDroppedOutside -= cardDroppedHandler;
        CardListComponent.BaseDroppable = false;
        CardListComponent.CardsDraggable = false;
        CardListComponent.CanClose = true;
        CardListComponent.Hide();
        return result;
    }

    public Task<List<UnitCircle>> SelectOwnUnitCirclesRange(UnitSelector selector, int minimum, int maximum)
    {
        return WhenPresented(() => PromptOwnUnitCircles(selector, minimum, maximum));
    }

    // Select buttons on the matching circles, then Confirm once enough are selected
    private async Task<List<UnitCircle>> PromptOwnUnitCircles(UnitSelector selector, int minimum, int maximum)
    {
        List<UnitCircle> selected = [];
        board.PushPlayerPhaseIndicatorText(minimum == maximum ? $"Select {maximum} unit(s)" : $"Select {minimum}-{maximum} units");
        board.EnableSelectOwnUnitCircle(selector);
        board.ShowLeftButton(TextConstants.Confirm);
        board.EnableLeftButton(minimum == 0);

        TaskCompletionSource completionSource = new();
        Action<UnitCircleComponent> selectedHandler = (circle) =>
        {
            if(selected.Count >= maximum)
            {
                circle.ResetSelection();
                return;
            }
            selected.Add(circle.UnitCircle);
            board.EnableLeftButton(selected.Count >= minimum);
        };
        Action<UnitCircleComponent> deselectedHandler = (circle) =>
        {
            selected.Remove(circle.UnitCircle);
            board.EnableLeftButton(selected.Count >= minimum);
        };
        Action confirmHandler = () => completionSource.TrySetResult();
        board.PlayerCircleSelected += selectedHandler;
        board.PlayerCircleDeselected += deselectedHandler;
        board.LeftButtonPressed += confirmHandler;

        await completionSource.Task;

        board.PlayerCircleSelected -= selectedHandler;
        board.PlayerCircleDeselected -= deselectedHandler;
        board.LeftButtonPressed -= confirmHandler;
        board.DisableSelectOwnUnitCircle();
        board.EnableLeftButton();
        board.HideLeftButton();
        board.PopPlayerPhaseIndicatorText();
        return selected;
    }

    public Task<List<UnitCircle>> SelectOwnUnitCircles(UnitSelector selector)
    {
        return WhenPresented(() =>
        {
            EnsureGuardStrategy();
            return ((ISelectOwnUnitCircles)strategy).SelectOwnUnitCircles(selector);
        });
    }
}
