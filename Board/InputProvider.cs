using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Components;
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

    public VanguardPlayArea OpponentPlayArea => throw new NotImplementedException();

    public VanguardSkillService SkillService => throw new NotImplementedException();


    VanguardCard CurrentVanguard => PlayArea.Vanguard.Card!;
    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;

    IInputProviderStrategy strategy = null!;

    SelectCardsFromHandComponent SelectCardsFromHandComponent = null!;

    public override void _Ready()
    {
        board = GetNode<DuelCreaturesBoard>($"%{nameof(DuelCreaturesBoard)}");
        SelectCardsFromHandComponent = GetNode<SelectCardsFromHandComponent>($"%{nameof(SelectCardsFromHandComponent)}");
        SelectCardsFromHandComponent.CardReturned += OnCardReturned;
        SelectCardsFromHandComponent.CardSelected += OnCardSelected;

        Board.HandCardPressed += OnHandCardPressed;
    }

    public void Setup(VanguardPlayArea playArea)
    {
        PlayArea = playArea;
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
    }

    private void OnPhaseChanged(IPhase phase)
    {
        if(phase is not IManualPhase) return;

        switch(phase)
        {
            case MulliganPhase:
                SetProviderStrategy(new MulliganPhaseStrategy(SelectCardsFromHandComponent));
                return;
            case RidePhase:
                SetProviderStrategy(new RidePhaseStrategy(board));
                return;
            case MainPhase:
                SetProviderStrategy(new MainRidePhaseStrategy(board));
                return;
        }
        throw new NotSupportedException($"{phase.GetType().Name} is not supported yet");
    }

    public void SetProviderStrategy(IInputProviderStrategy strategy)
    {
        this.strategy = strategy;
    }

    private void OnCardSelected(Card card)
    {
        board.PlayerHand.RemoveCard(card);
    }

    private void OnCardReturned(Card card)
    {
        board.PlayerHand.AddCard(card);
        card.CurrentlyDragged = false;
    }

    private void OnHandCardPressed(Card card)
    {
        VanguardCardComponent component = (VanguardCardComponent)card;
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
        throw new NotImplementedException();
    }

    public Task<UnitCircle> SelectOpponentFrontRow(UnitSelector selector)
    {
        throw new NotImplementedException();
    }

    public Task<UnitCircle> SelectOwnUnitCircle()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
    {
        throw new NotImplementedException();
    }

    public Task<bool> QueryActivateSkill(VanguardSkillCost SkillCost)
    {
        throw new NotImplementedException();
    }

    public Task<CardBase> SelectCardFromHand()
    {
        throw new NotImplementedException();
    }

    public Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        throw new NotImplementedException();
    }
}
