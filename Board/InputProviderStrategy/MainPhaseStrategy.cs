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

    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();
        Board.EnablePlayerRearguardDragging();
        Board.EnablePlayerHandDragging();
        Board.ShowEndPhaseButton();

        bool rearguardDragging = false;
        RearGuard rearguardDraggedFrom = null!;

        Action endPhaseHandler = () => {
            var selected = actions.FirstOf<EndMainPhase>();
            completionSource.SetResult(selected);
        };
        Board.EndPhasePressed +=  endPhaseHandler;

        // If card is dropped to RG then we execute Call Rearguard
        Action<UnitCircleComponent, Card> onCardPlacedToRGHandler = (unitCircle, card) =>
        {
            if(rearguardDragging)
            {
                var exception = VanguardValidator.CanSwap(playArea, rearguardDraggedFrom, (RearGuard)unitCircle.UnitCircle);
                if(exception is null)
                {
                    selectedRearguardForSwap = rearguardDraggedFrom;
                    Board.DisablePlayerRearguardDropping();
                    completionSource.SetResult(actions.FirstOf<SwapRearguard>());
                } else
                {
                    card.CurrentlyDragged = false;
                }
            } else
            {
                selectedCardForCall = card.CurrentCard;
                selectedRearguardForCall = (RearGuard)unitCircle.UnitCircle;
                Board.DisablePlayerRearguardDropping();
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
            Board.EnablePlayerRearguardDropping();
            Board.PlayerBackCenter.Droppable = false;
            rearguardDragging = true;
            rearguard.Droppable = false;
            rearguardDraggedFrom = (RearGuard)rearguard.UnitCircle;
        };
        Board.PlayerRearGuardDragged += onRearguardDraggedHandler;

        var result = await completionSource.Task;
        Board.EndPhasePressed -=  endPhaseHandler;
        Board.CardDroppedToPlayerRearguard -=  onCardPlacedToRGHandler;
        Board.PlayerHand.CardDragging -= onHandCardDragging;
        Board.PlayerRearGuardDragged -= onRearguardDraggedHandler;

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
        if(gameContext.GameState is CallRearguardState && selectedRearguardForCall is not null)
        {
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