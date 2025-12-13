using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;

public class MainPhaseStrategy(DuelCreaturesBoard Board, VanguardPlayArea playArea, GameContext gameContext) : IInputProviderStrategy, IRequestMainPhaseAction, ISelectCardFromHand, ISelectOwnRearguard
{
    CardBase selectedCardForCall = null!;
    RearGuard selectedRearguardForCall = null!;
    RearGuard selectedRearguardForSwap = null!;

    bool rearguardDragging = false;
    UnitCircleComponent rearguardDraggedFrom = null!;

    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();
        Board.EnablePlayerRearguardDragging();
        Board.DisablePlayerRearguardDragging([Board.PlayerBackCenter]);
        Board.EnablePlayerHandDragging();
        Board.ShowLeftButton(TextConstants.EndPhase);

        rearguardDragging = false;
        rearguardDraggedFrom = null!;

        Action endPhaseHandler = () => {
            var selected = actions.FirstOf<EndMainPhase>();
            completionSource.SetResult(selected);
        };
        Board.LeftButtonPressed +=  endPhaseHandler;

        Action<UnitCircleComponent, CardBaseComponent> onCardDragCancelledHandler = (unitCircle, card) =>
        {
            Board.EnablePlayerRearguardDragging();
        };
        Board.PlayerRearGuardCardDragCancelled += onCardDragCancelledHandler;

        // If card is dropped to RG then we execute Call Rearguard
        Action<UnitCircleComponent, Card> onCardPlacedToRGHandler = (unitCircle, card) =>
        {
            Board.DisablePlayerRearguardDropping();
            if(rearguardDragging)
            {
                var selectedRearguard = (RearGuard)rearguardDraggedFrom.UnitCircle;
                selectedRearguardForSwap = selectedRearguard;
                completionSource.SetResult(actions.FirstOf<SwapRearguard>());
            } else
            {
                selectedRearguardForCall = (RearGuard)unitCircle.UnitCircle;
                selectedCardForCall = card.CurrentCard;
                completionSource.SetResult(actions.FirstOf<CallRearguard>());
            }
        };
        Board.CardDroppedToPlayerRearguard +=  onCardPlacedToRGHandler;

        // Catalyst for rearguard calling
        Action<CardBaseComponent> onHandCardDragging = (card) =>
        {
            Board.EnablePlayerRearguardDropping();
        };
        Board.PlayerHand.CardDragging += onHandCardDragging;

        // Catalyst for rearguard swapping
        Action<UnitCircleComponent, CardBaseComponent> onRearguardDraggedHandler = (rearguard, card) =>
        {
            Board.EnablePlayerRearguardDropping([rearguard, Board.GetPlayerOppositeCircle(rearguard)]);
            rearguardDraggedFrom = rearguard;
            rearguardDragging = true;
        };
        Board.PlayerRearGuardDragged += onRearguardDraggedHandler;

        var result = await completionSource.Task;

        Board.LeftButtonPressed -=  endPhaseHandler;
        Board.CardDroppedToPlayerRearguard -=  onCardPlacedToRGHandler;
        Board.PlayerRearGuardCardDragCancelled -= onCardDragCancelledHandler;
        Board.PlayerHand.CardDragging -= onHandCardDragging;
        Board.PlayerRearGuardDragged -= onRearguardDraggedHandler;

        Board.HideLeftButton();
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
        if(gameContext.GameState is CallRearguardState)
        {
            if(selectedRearguardForCall is null) throw new InvalidOperationException();
            var result = selectedRearguardForCall;
            selectedRearguardForCall = null!;
            return Task.FromResult(result);
        }
        if(gameContext.GameState is SwapRearguardState && selectedRearguardForSwap is not null)
        {
            var result = selectedRearguardForSwap;
            selectedRearguardForSwap = null!;
            return Task.FromResult(result);
        }
        throw new NotImplementedException();
    }
}