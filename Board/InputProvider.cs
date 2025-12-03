using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;
using Godot;

public partial class InputProvider : Control
{
    DuelCreaturesBoard board = null!;
    public DuelCreaturesBoard Board => board;
    IInputProviderStrategy strategy = null!;

    SelectCardsFromHandComponent SelectCardsFromHandComponent = null!;

    public override void _Ready()
    {
        board = GetNode<DuelCreaturesBoard>($"%{nameof(DuelCreaturesBoard)}");
        SelectCardsFromHandComponent = GetNode<SelectCardsFromHandComponent>($"%{nameof(SelectCardsFromHand)}");
        SelectCardsFromHandComponent.CardReturned += OnCardReturned;
        SelectCardsFromHandComponent.CardSelected += OnCardSelected;

        Board.HandCardPressed += OnHandCardPressed;
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
    }

    private void OnPhaseChanged(IPhase phase)
    {
        switch(phase)
        {
            case MulliganPhase:
                SetProviderStrategy(new MulliganPhaseStrategy(SelectCardsFromHandComponent));
                break;
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

    public Task<List<CardBase>> SelectCardsFromHand()
    {
        return strategy.SelectCardsFromHand();
    }

    public Task<CardBase?> SelectCardFromHandOrNot(VanguardCard currentVanguard)
    {
        return strategy.SelectCardFromHandOrNot(currentVanguard);
    }

    public async Task<IMainPhaseAction> AskForMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();
        Board.ShowEndPhaseButton();
        Action endPhaseHandler = () => {
            var selected = actions.FirstOf<EndMainPhase>();
            completionSource.SetResult(selected);
        };
        Action<UnitCircleComponent, Card> onCardPlacedToRGHandler = (unitCircle, card) =>
        {

        };
        Board.EndPhasePressed +=  endPhaseHandler;
        Board.CardDroppedToPlayerRearguard +=  onCardPlacedToRGHandler;


        var result = await completionSource.Task;
        Board.EndPhasePressed -=  endPhaseHandler;
        Board.HideEndPhaseButton();
        return result;
    }
}
