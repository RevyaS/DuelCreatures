using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;

public class MainRidePhaseStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy, IRequestMainPhaseAction
{
    CardBase selectedCardForCall = null!;
    RearGuard selectedRearguardForCall = null!;

    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();
        Board.ShowEndPhaseButton();
        Action endPhaseHandler = () => {
            var selected = actions.FirstOf<EndMainPhase>();
            completionSource.SetResult(selected);
        };
        Action<UnitCircleComponent, Card> onCardPlacedToRGHandler = (unitCircle, card) =>
        {
            selectedCardForCall = card.CurrentCard;
            selectedRearguardForCall = (RearGuard)unitCircle.UnitCircle;
            completionSource.SetResult(actions.FirstOf<CallRearguard>());
        };
        Board.EndPhasePressed +=  endPhaseHandler;
        Board.CardDroppedToPlayerRearguard +=  onCardPlacedToRGHandler;


        var result = await completionSource.Task;
        Board.EndPhasePressed -=  endPhaseHandler;
        Board.HideEndPhaseButton();
        return result;
    }
}