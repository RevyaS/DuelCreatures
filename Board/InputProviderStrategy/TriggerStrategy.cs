using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;

public class TriggerStrategy(DuelCreaturesBoard Board, VanguardPlayArea PlayArea, SelectFromCardList SelectFromCardList, GameContext GameContext) : BaseStrategy(Board), ISelectOwnUnitCircle, ISelectCardFromDamageZone, ISelectOwnRearguard
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
            return GameBoard.DoFuncWithIndicatorAsync("Select Circle to Provide Power", () => SelectOwnUnitCircle(UnitSelector.ALL_CIRCLES | UnitSelector.NON_EMPTY));
        }
        if(GameContext.GameState is TriggerCriticalState)
        {
            return GameBoard.DoFuncWithIndicatorAsync("Select Circle to Provide Critical", () => SelectOwnUnitCircle(UnitSelector.ALL_CIRCLES | UnitSelector.NON_EMPTY));
        }
        throw new InvalidOperationException();
    }

    public async Task<RearGuard> SelectOwnRearguard(UnitSelector unitSelector)
    {
        GameBoard.EnableSelectOwnUnitCircle(unitSelector);
        TaskCompletionSource<RearGuard> completionSource = new();
        
        Action<UnitCircleComponent> playerCircleSelectedHandler = (uc) =>
        {
            completionSource.SetResult((RearGuard)uc.UnitCircle);
        };
        GameBoard.PlayerCircleSelected += playerCircleSelectedHandler;

        var result = await completionSource.Task;
        GameBoard.DisableSelectOwnUnitCircle();
        GameBoard.PlayerCircleSelected -= playerCircleSelectedHandler;

        return result;
    }
}