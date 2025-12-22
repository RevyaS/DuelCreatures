using System;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;

public class BaseStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy
{
    protected DuelCreaturesBoard GameBoard => Board;
    protected async Task<UnitCircle> SelectOwnUnitCircle(UnitSelector selector)
    {
        GameBoard.EnableSelectOwnUnitCircle(selector);
        TaskCompletionSource<UnitCircle> completionSource = new();
        
        Action<UnitCircleComponent> playerCircleSelectedHandler = (uc) =>
        {
            completionSource.SetResult(uc.UnitCircle);
        };
        GameBoard.PlayerCircleSelected += playerCircleSelectedHandler;

        var result = await completionSource.Task;
        GameBoard.DisableSelectOwnUnitCircle();
        GameBoard.PlayerCircleSelected -= playerCircleSelectedHandler;

        return result;
    }
}