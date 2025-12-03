using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;

public class MulliganPhaseStrategy(SelectCardsFromHandComponent selectCardsFromHandComponent) : IInputProviderStrategy
{
    public Task<CardBase?> SelectCardFromHandOrNot(VanguardCard currentVanguard)
    {
        throw new NotImplementedException();
    }

    public async Task<List<CardBase>> SelectCardsFromHand()
    {
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
        return result;
    }
}