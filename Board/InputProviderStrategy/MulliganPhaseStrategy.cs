using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;

public class MulliganPhaseStrategy(DuelCreaturesBoard board, SelectCardsFromHandComponent selectCardsFromHandComponent) : IInputProviderStrategy, ISelectCardsFromHand
{
    public async Task<List<CardBase>> SelectCardsFromHand()
    {
        board.EnablePlayerHandDragging();

        TaskCompletionSource<List<CardBase>> completionSource = new();
        selectCardsFromHandComponent.Activate(0, 5);
        
        Action<CardBaseComponent> cardDraggingHandler = (card) =>
        {
            selectCardsFromHandComponent.Droppable = true;
        };
        board.PlayerHand.CardDragging += cardDraggingHandler;

        Action<Card> cardSelectedHandler = (card) => { 
            selectCardsFromHandComponent.Droppable = false;
            board.PlayerHand.RemoveCard(card);
        };
        selectCardsFromHandComponent.CardSelected += cardSelectedHandler;

        Action<Card> cardReturnedHandler = (card) =>
        {
            board.PlayerHand.AddCard(card);
            card.CurrentlyDragged = false;
        };
        selectCardsFromHandComponent.CardReturned += cardReturnedHandler;

        Action<List<Card>> confirmedCardHandler = (cards) =>
        {
            selectCardsFromHandComponent.Deactivate();
            completionSource.SetResult(cards.Select(x => x.CurrentCard).ToList());
        };
        selectCardsFromHandComponent.ConfirmedCards += confirmedCardHandler;
        
        var result = await completionSource.Task;

        selectCardsFromHandComponent.ConfirmedCards -= confirmedCardHandler;
        board.PlayerHand.CardDragging -= cardDraggingHandler;
        selectCardsFromHandComponent.CardSelected -= cardSelectedHandler;
        selectCardsFromHandComponent.CardReturned -= cardReturnedHandler;
        board.DisablePlayerHandDragging();

        return result;
    }
}