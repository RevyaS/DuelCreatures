using System;
using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;

public class TriggerStrategy(DuelCreaturesBoard Board, GameContext GameContext) : BaseStrategy(Board), ISelectOwnUnitCircle
{
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