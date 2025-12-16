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

    public VanguardSkillService SkillService => throw new NotImplementedException();

    VanguardCard CurrentVanguard => PlayArea.Vanguard.Card!;
    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;
    GameContext GameContext = null!;

    IInputProviderStrategy strategy = null!;

    SelectCardsComponent SelectCardsComponent = null!;
    CardList CardListComponent = null!;
    CardInfo CardInfoComponent = null!;

    public override void _Ready()
    {
        board = GetNode<DuelCreaturesBoard>($"%{nameof(DuelCreaturesBoard)}");
        SelectCardsComponent = GetNode<SelectCardsComponent>($"%{nameof(SelectCardsComponent)}");
        CardListComponent = GetNode<CardList>($"%{nameof(CardListComponent)}");
        CardListComponent.CardPressed += OnCardListCardPressed;

        CardInfoComponent = GetNode<CardInfo>($"%{nameof(CardInfoComponent)}");

        board.PlayerDropZone.SetCardList(CardListComponent);
        board.OppDropZone.SetCardList(CardListComponent);

        Board.PlayerSoulPressed += OnPlayerSoulPressed;
        Board.HandCardPressed += OnHandCardPressed;
        Board.UnitCircleCardPressed += OnUnitCircleCardPressed;
        Board.DamageZoneCardPressed += OnDamageZoneCardPressed;
    }

    private void OnPlayerSoulPressed()
    {
        CardListComponent.Show("Soul", PlayArea.Soul.Cards.Cast<VanguardCard>().ToList());
    }

    public void Setup(VanguardPlayArea playArea, VanguardPlayArea oppPlayArea, GameContext gameContext)
    {
        OpponentPlayArea = oppPlayArea;
        PlayArea = playArea;
        GameContext = gameContext;
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
        eventBus.OnGuardRequested += OnGuardRequested;
        eventBus.QueryActivateSkillRequested += OnQueryActivateSkillRequested;
        eventBus.CounterBlastRequested += OnCounterBlastRequested;
    }

    private void OnCounterBlastRequested(DamageZone zone)
    {
        if(ReferenceEquals(PlayArea.DamageZone, zone))
        {
            SetProviderStrategy(new CounterBlastSkillStrategy(board));
        }
    }

    private void OnQueryActivateSkillRequested(UnitCircle invoker, VanguardAutomaticSkill skill)
    {
        if(PlayArea.OwnsUnitCircle(invoker))
        {
            SetProviderStrategy(new QueryActivateSkillStrategy(CardListComponent));
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
                SetProviderStrategy(new MainPhaseStrategy(board, PlayArea, GameContext));
                return;
            case VanguardAttackPhase:
                SetProviderStrategy(new AttackPhaseStrategy(board, GameContext));
                return;
        }
        throw new NotSupportedException($"{phase.GetType().Name} is not supported yet");
    }

    public void SetProviderStrategy(IInputProviderStrategy strategy)
    {
        this.strategy = strategy;
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

    private void ShowCardInfo(Card card)
    {
        CardInfoComponent.Show((VanguardCard)card.CurrentCard);
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

    public Task<UnitCircle> SelectOpponentFrontRow(UnitSelector selector)
    {
        return ((ISelectOpponentFrontRow)strategy).SelectOpponentFrontRow(selector);
    }

    public Task<UnitCircle> SelectOwnUnitCircle()
    {
        return ((ISelectOwnUnitCircle)strategy).SelectOwnUnitCircle();
    }

    public Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount)
    {
        return ((ISelectCardsFromDamageZone)strategy).SelectCardsFromDamageZone(amount);
    }

    public Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
    {
        throw new NotImplementedException();
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
