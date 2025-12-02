using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class InputProvider : Control
{
    DuelCreaturesBoard board;
    public DuelCreaturesBoard Board => board;

    #region States
    bool DragHandToZone = false;
    #endregion


    SelectCardsFromHandComponent SelectCardsFromHandComponent;

    public override void _Ready()
    {
        board = GetNode<DuelCreaturesBoard>($"%{nameof(DuelCreaturesBoard)}");
        SelectCardsFromHandComponent = GetNode<SelectCardsFromHandComponent>($"%{nameof(SelectCardsFromHand)}");
        SelectCardsFromHandComponent.CardReturned += OnCardReturned;
        SelectCardsFromHandComponent.CardSelected += OnCardSelected;

        Board.HandCardPressed += OnHandCardPressed;
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

    public async Task<List<CardBase>> SelectCardsFromHand()
    {
        DragHandToZone = true;
        TaskCompletionSource<List<CardBase>> completionSource = new();
        SelectCardsFromHandComponent.Activate(0, 5);
        
        Action<List<Card>> handler = (cards) =>
        {
            SelectCardsFromHandComponent.Deactivate();
            completionSource.SetResult(cards.Cast<VanguardCardComponent>().Select(x => x.Card).Cast<CardBase>().ToList());
        };
        SelectCardsFromHandComponent.ConfirmedCards += handler;
        
        var result = await completionSource.Task;
        SelectCardsFromHandComponent.ConfirmedCards -= handler;
        return result;
    }

    public async Task<CardBase> RideVanguardFromHandOrNot(VanguardCard currentVanguard)
    {
        DragHandToZone = true;
        Board.PlayerVanguard.Droppable = true;
        Board.ShowEndPhaseButton();
        
        VanguardCard newVanguard = null;

        while(true)
        {
            TaskCompletionSource<Card?> completionSource = new();
            Action<Card> selectionHandler = completionSource.SetResult;
            Board.PlayerVanguard.CardDropped += selectionHandler; 
            Action endPhaseHandler = () =>
            {
                completionSource.SetResult(null);
            };
            Board.EndPhasePressed += endPhaseHandler;

            var result = await completionSource.Task;
            Board.PlayerVanguard.CardDropped -= selectionHandler; 
            Board.EndPhasePressed -= endPhaseHandler; 

            if(result is null)
            {
                break;
            }

            newVanguard = ((VanguardCardComponent)result).Card;
            var exception = VanguardValidator.ValidateRide(currentVanguard, newVanguard);

            if (exception is not null)
            {
                result.CurrentlyDragged = false;
                GD.PushError(exception.Message);
            } else
            {
                break;
            }
        }

        Board.PlayerVanguard.Droppable = true;

        return newVanguard;
    }
}
