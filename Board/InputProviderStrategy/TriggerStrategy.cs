using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;

public class TriggerStrategy(DuelCreaturesBoard Board, VanguardPlayArea PlayArea, SelectFromCardList SelectFromCardList, GameContext GameContext) : BaseStrategy(Board), ISelectOwnUnitCircle, ISelectCardFromDamageZone
{
    public async Task<VanguardCard> SelectCardFromDamageZone()
    {
        SelectFromCardList.Show("Damage Zone", $"Select Card to Heal", 1, 1, PlayArea.DamageZone.Cards.Cast<VanguardCard>().ToList());

        TaskCompletionSource<VanguardCard> completionSource = new();

        Action<List<Card>> onConfirmHandler = (cards) =>
        {
            completionSource.SetResult((VanguardCard)cards.First().CurrentCard);
        };

        SelectFromCardList.ConfirmedCards += onConfirmHandler;

        var result = await completionSource.Task;

        SelectFromCardList.ConfirmedCards -= onConfirmHandler;
        SelectFromCardList.Deactivate();

        return result;
    }

    public Task<UnitCircle> SelectOwnUnitCircle()
    {
        if(GameContext.GameState is TriggerPowerState)
        {
            return GameBoard.DoFuncWithIndicatorAsync("Select Circle to Provide Power", () => SelectOwnUnitCircle(UnitSelector.ALL_CIRCLES));
        }
        if(GameContext.GameState is TriggerCriticalState)
        {
            return GameBoard.DoFuncWithIndicatorAsync("Select Circle to Provide Critical", () => SelectOwnUnitCircle(UnitSelector.ALL_CIRCLES));
        }
        throw new InvalidOperationException();
    }
}