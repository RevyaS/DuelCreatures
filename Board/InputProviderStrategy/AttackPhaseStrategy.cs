using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.Common.Extensions;

public class AttackPhaseStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy, IRequestAttackPhaseAction
{
    public async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        Board.ShowEndPhaseButton();

        TaskCompletionSource<IAttackPhaseAction> completionSource = new();
        
        Action endPhaseHandler = () =>
        {
            completionSource.SetResult(actions.FirstOf<EndAttackPhase>());
        };
        Board.EndPhasePressed += endPhaseHandler;

        var result = await completionSource.Task;
        Board.EndPhasePressed -= endPhaseHandler;

        return result;
    }
}