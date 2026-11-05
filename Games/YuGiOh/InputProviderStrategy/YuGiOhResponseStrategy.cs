using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;
using ArC.CardGames.YuGiOh;
using ArC.Common.Extensions;

public class YuGiOhResponseStrategy(YuGiOhBoard Board) : IInputProviderStrategy, ISelectChainResponse
{
    public async Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain)
    {
        TaskCompletionSource<YuGiOhChainResponse> completionSource = new();

        var result = await completionSource.Task;

        return result;
    }
}