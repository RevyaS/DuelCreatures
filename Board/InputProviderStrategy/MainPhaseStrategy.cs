using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;

public class MainRidePhaseStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy, IRequestMainPhaseAction, ISelectCardFromHand, ISelectOwnRearguard
{
    CardBase selectedCardForCall = null!;
    RearGuard selectedRearguardForCall = null!;

    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();
        Board.EnablePlayerRearguardDropping();
        Board.EnablePlayerHandDragging();
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
        Board.CardDroppedToPlayerRearguard -=  onCardPlacedToRGHandler;

        Board.HideEndPhaseButton();
        Board.DisablePlayerRearguardDropping();
        Board.DisablePlayerHandDragging();

        return result;
    }

    public Task<CardBase> SelectCardFromHand()
    {
        if(selectedCardForCall is not null)
        {
            var result = selectedCardForCall;
            selectedCardForCall = null!;
            return Task.FromResult(result);
        }
        throw new NotImplementedException();
    }

    public Task<RearGuard> SelectOwnRearguard()
    {
        if(selectedRearguardForCall is not null)
        {
            var result = selectedRearguardForCall;
            selectedRearguardForCall = null!;
            return Task.FromResult(result);
        }
        throw new NotImplementedException();
    }
}