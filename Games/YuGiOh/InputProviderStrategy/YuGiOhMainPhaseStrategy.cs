using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;
using ArC.Common.Extensions;

public class YuGiOhMainPhaseStrategy(YuGiOhBoard Board) : IInputProviderStrategy, IRequestMainPhaseAction
{
    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();

        var result = await completionSource.Task;

        return result;
    }
}