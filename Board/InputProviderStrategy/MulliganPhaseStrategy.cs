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
        
        Action<List<Card>> handler = (cards) =>
        {
            selectCardsFromHandComponent.Deactivate();
            completionSource.SetResult(cards.Select(x => x.CurrentCard).ToList());
        };
        selectCardsFromHandComponent.ConfirmedCards += handler;
        
        var result = await completionSource.Task;

        selectCardsFromHandComponent.ConfirmedCards -= handler;
        board.DisablePlayerHandDragging();

        return result;
    }
}