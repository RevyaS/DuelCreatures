using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;

public class SoulBlastSkillStrategy(Soul soul, SelectFromCardList SelectFromCardList) : IInputProviderStrategy, ISelectCardsFromSoul
{
    public async Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
    {
        SelectFromCardList.Show("Soul", $"Soul Blast ({amount})", amount, amount, soul.Cards.Cast<VanguardCard>().ToList());

        TaskCompletionSource<List<VanguardCard>> completionSource = new();

        Action<List<Card>> onConfirmHandler = (cards) =>
        {
            completionSource.SetResult(cards.Select(x => x.CurrentCard).Cast<VanguardCard>().ToList());
        };

        SelectFromCardList.ConfirmedCards += onConfirmHandler;

        var result = await completionSource.Task;

        SelectFromCardList.ConfirmedCards -= onConfirmHandler;
        SelectFromCardList.Deactivate();
        
        return result;
    }
}