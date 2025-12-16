using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;

public class MulliganPhaseStrategy(DuelCreaturesBoard board, SelectCardsComponent selectCardsComponent) : IInputProviderStrategy, ISelectCardsFromHand
{
    public async Task<List<CardBase>> SelectCardsFromHand()
    {
        board.EnablePlayerHandDragging();

        TaskCompletionSource<List<CardBase>> completionSource = new();
        selectCardsComponent.Activate(0, 5);
        
        Action<CardBaseComponent> cardDraggingHandler = (card) =>
        {
            selectCardsComponent.Droppable = true;
        };
        board.PlayerHand.CardDragging += cardDraggingHandler;

        Action<Card> cardSelectedHandler = (card) => { 
            selectCardsComponent.Droppable = false;
            board.PlayerHand.RemoveCard(card);
        };
        selectCardsComponent.CardSelected += cardSelectedHandler;

        Action<Card> cardReturnedHandler = (card) =>
        {
            board.PlayerHand.AddCard(card);
            card.CurrentlyDragged = false;
        };
        selectCardsComponent.CardReturned += cardReturnedHandler;

        Action<List<Card>> confirmedCardHandler = (cards) =>
        {
            selectCardsComponent.Deactivate();
            completionSource.SetResult(cards.Select(x => x.CurrentCard).ToList());
        };
        selectCardsComponent.ConfirmedCards += confirmedCardHandler;
        
        var result = await completionSource.Task;

        selectCardsComponent.ConfirmedCards -= confirmedCardHandler;
        board.PlayerHand.CardDragging -= cardDraggingHandler;
        selectCardsComponent.CardSelected -= cardSelectedHandler;
        selectCardsComponent.CardReturned -= cardReturnedHandler;
        board.DisablePlayerHandDragging();

        return result;
    }
}