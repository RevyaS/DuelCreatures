using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using Godot;

public partial class InputProvider : Control, IVanguardPlayerInputProvider
{
    DuelCreaturesBoard board = null!;
    public DuelCreaturesBoard Board => board;

    public VanguardPlayArea PlayArea { get; private set; } = null!;

    public VanguardPlayArea OpponentPlayArea { get; private set; } = null!;

    public VanguardSkillService SkillService { get; private set; } = null!;

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
        board = GetNode<DuelCreaturesBoard>($"%{nameof(DuelCreaturesBoard)}");
        SelectCardsComponent = GetNode<SelectCardsComponent>($"%{nameof(SelectCardsComponent)}");
        SelectFromCardListComponent = GetNode<SelectFromCardList>($"%{nameof(SelectFromCardListComponent)}");
        CardListComponent = GetNode<CardList>($"%{nameof(CardListComponent)}");
        CardListComponent.CardPressed += OnCardListCardPressed;

        CardInfoComponent = GetNode<CardInfo>($"%{nameof(CardInfoComponent)}");

        board.PlayerDropZone.SetCardList(CardListComponent);
        board.OppDropZone.SetCardList(CardListComponent);

        Board.PlayerSoulPressed += OnPlayerSoulPressed;
        Board.HandCardPressed += OnHandCardPressed;
        Board.UnitCircleCardPressed += OnUnitCircleCardPressed;
        Board.PlayerUnitCircleCardPressed += OnPlayerUnitCircleCardPressed;
        Board.DamageZoneCardPressed += OnDamageZoneCardPressed;
    }

    private void OnPlayerUnitCircleCardPressed(UnitCircleComponent unitCircle, Card card)
    {
        var enableActivateButton = VanguardGameRules.UnitCircleCanActivateSkill(this, unitCircle.UnitCircle);
        ShowCardInfo(card, enableActivateButton);
    }

    private void OnPlayerSoulPressed()
    {
        CardListComponent.Show("Soul", PlayArea.Soul.Cards.Cast<VanguardCard>().ToList());
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
        eventBus.OnGuardRequested += OnGuardRequested;
        eventBus.SkillExecution += OnSkillExecution;
        eventBus.SkillExecuted += OnSkillExecuted;
    }

    private void OnSkillExecuted(UnitCircle circle, VanguardSkill skill)
    {
        if(PlayArea.OwnsUnitCircle(circle))
        {
            PopProviderStrategy();
        }
    }

    private void OnSkillExecution(UnitCircle circle, VanguardSkill skill)
    {
        if(PlayArea.OwnsUnitCircle(circle))
        {
            PushProviderStrategy(new SkillExecutionStrategy(board, PlayArea, CardListComponent, SelectFromCardListComponent));
        }
    }

    private Task OnGuardRequested(UnitCircle circle)
    {
        if(PlayArea.OwnsUnitCircle(circle))
        {
            SetProviderStrategy(new GuardPhaseStrategy(board, board.GetPlayerUnitCircleComponent(circle)));
        }
        return Task.CompletedTask;
    }

    private void OnPhaseChanged(IPhase phase)
    {
        if(phase is not IManualPhase) return;

        switch(phase)
        {
            case MulliganPhase:
                SetProviderStrategy(new MulliganPhaseStrategy(board, SelectCardsComponent));
                return;
            case RidePhase:
                SetProviderStrategy(new RidePhaseStrategy(board));
                return;
            case MainPhase:
                SetProviderStrategy(new MainPhaseStrategy(board, CardInfoComponent, GameContext));
                return;
            case VanguardAttackPhase:
                SetProviderStrategy(new AttackPhaseStrategy(board, GameContext));
                return;
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
        ShowCardInfo(card);
    }

    private void OnUnitCircleCardPressed(Card card)
    {
        ShowCardInfo(card);
    }

    private void OnCardListCardPressed(Card card)
    {
        ShowCardInfo(card);
    }

    private void OnDamageZoneCardPressed(Card card)
    {
        ShowCardInfo(card);
    }

    private void ShowCardInfo(Card card, bool canActivate = false)
    {
        CardInfoComponent.Show((VanguardCard)card.CurrentCard, canActivate);
    }

    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        return ((ISelectCardsFromHand)strategy).SelectCardsFromHand();
    }

    public Task<CardBase?> SelectCardFromHandOrNot()
    {
        return ((ISelectCardFromHandOrNot)strategy).SelectCardFromHandOrNot(CurrentVanguard);
    }

    public Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        return ((IRequestMainPhaseAction)strategy).RequestMainPhaseAction(actions);
    }

    public Task<RearGuard> SelectOwnRearguard()
    {
        return ((ISelectOwnRearguard)strategy).SelectOwnRearguard();
    }

    public Task<UnitCircle> SelectOpponentCircle(UnitSelector selector)
    {
        return ((ISelectOpponentCircle)strategy).SelectOpponentCircle(selector);
    }

    public Task<UnitCircle> SelectOwnUnitCircle()
    {
        return ((ISelectOwnUnitCircle)strategy).SelectOwnUnitCircle();
    }

    public Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills)
    {
        return ((ISelectSkillToActivate)strategy).SelectSkillToActivate(skills);
    }

    public Task<UnitCircle> SelectCircleToProvideCritical()
    {
        throw new NotImplementedException();
    }

    public Task<UnitCircle> SelectCircleToProvidePower()
    {
        throw new NotImplementedException();
    }

    public Task<VanguardCard> SelectCardFromDamageZone()
    {
        throw new NotImplementedException();
    }

    public Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade)
    {
        return ((ISelectCardFromDeck)strategy).SelectCardFromDeck(minGrade, maxGrade);
    }

    public Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount)
    {
        return ((ISelectCardsFromDamageZone)strategy).SelectCardsFromDamageZone(amount);
    }

    public Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
    {
        return ((ISelectCardsFromSoul)strategy).SelectCardsFromSoul(amount);
    }

    public Task<bool> QueryActivateSkill(VanguardCard Invoker, VanguardAutomaticSkill Skill)
    {
        return ((IQueryActivateSkill)strategy).QueryActivateSkill(Invoker, Skill);
    }

    public Task<CardBase> SelectCardFromHand()
    {
        return ((ISelectCardFromHand)strategy).SelectCardFromHand();
    }

    public Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        return ((IRequestAttackPhaseAction)strategy).RequestAttackPhaseAction(actions);
    }
}
