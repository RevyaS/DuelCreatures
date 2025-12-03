using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;
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
            completionSource.SetResult(cards.Select(x => x.CurrentCard).ToList());
        };
        SelectCardsFromHandComponent.ConfirmedCards += handler;
        
        var result = await completionSource.Task;
        SelectCardsFromHandComponent.ConfirmedCards -= handler;
        return result;
    }

    public async Task<CardBase> RideVanguardFromHandOrNot(VanguardCard currentVanguard)
    {
        DragHandToZone = true;
        Board.ShowEndPhaseButton();
        Board.EnablePlayerVanguardDropping();
        
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

            newVanguard = (VanguardCard)result.CurrentCard;
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

        Board.DisablePlayerVanguardDropping();
        Board.HideEndPhaseButton();

        return newVanguard;
    }

    public async Task<IMainPhaseAction> AskForMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();
        Board.ShowEndPhaseButton();
        Action endPhaseHandler = () => {
            var selected = actions.FirstOf<EndMainPhase>();
            completionSource.SetResult(selected);
        };
        Board.EndPhasePressed +=  endPhaseHandler;


        var result = await completionSource.Task;
        Board.EndPhasePressed -=  endPhaseHandler;
        Board.HideEndPhaseButton();
        return result;
    }
}
