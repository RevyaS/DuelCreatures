using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;

public class MainPhaseStrategy(DuelCreaturesBoard Board, CardInfo CardInfo, GameContext gameContext) : IInputProviderStrategy, IRequestMainPhaseAction, ISelectCardFromHand, ISelectOwnUnitCircle, ISelectSkillToActivate
{
    CardBase selectedCardForCall = null!;
    CardBase selectedCardForHandSkillActivation = null!;
    UnitCircle selectedRearguardForCall = null!;
    UnitCircle selectedRearguardForSwap = null!;
    UnitCircle selectedUnitCircleActivation = null!;
    VanguardActivationSkill selectedSkillForActivation = null!;

    bool rearguardDragging = false;
    UnitCircleComponent rearguardDraggedFrom = null!;

    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        TaskCompletionSource<IMainPhaseAction> completionSource = new();
        Board.EnablePlayerRearguardDragging();
        Board.PlayerArea.DisableRearguardDragging([Board.PlayerArea.BackCenter]);
        Board.EnablePlayerHandDragging();
        Board.ShowLeftButton(TextConstants.EndPhase);
        CardInfo.ShowActivateButton = true;

        rearguardDragging = false;
        rearguardDraggedFrom = null!;

        Action<VanguardCard> activatedHandler = (card) =>
        {
            CardInfo.Hide();
            if(Board.IsCardInPlayerUnitCircle(card))
            {
                selectedUnitCircleActivation = Board.GetPlayerUnitCircleComponent(card).UnitCircle;
                selectedSkillForActivation = card.Skills.FirstOf<VanguardActivationSkill>(x => x.Location.HasFlag(VanguardSkillCardLocation.VANGUARD) || x.Location.HasFlag(VanguardSkillCardLocation.REARGUARD));
                var selected = actions.FirstOf<ActivateSkillFromUnitCircle>();
                completionSource.SetResult(selected);
            }
            else
            {
                selectedCardForHandSkillActivation = card;
                selectedSkillForActivation = card.Skills.FirstOf<VanguardActivationSkill>(x => x.Location.HasFlag(VanguardSkillCardLocation.HAND));
                var selected = actions.FirstOf<ActivateSkillFromHand>();
                completionSource.SetResult(selected);
            }
        };
        CardInfo.ActivationPressed += activatedHandler;

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

        CardInfo.ActivationPressed -= activatedHandler;
        Board.LeftButtonPressed -=  endPhaseHandler;
        Board.CardDroppedToPlayerRearguard -=  onCardPlacedToRGHandler;
        Board.PlayerRearGuardCardDragCancelled -= onCardDragCancelledHandler;
        Board.PlayerHand.CardDragging -= onHandCardDragging;
        Board.PlayerRearGuardDragged -= onRearguardDraggedHandler;

        Board.HideLeftButton();
        Board.DisablePlayerRearguardDropping();
        Board.DisablePlayerHandDragging();
        CardInfo.ShowActivateButton = false;

        return result;
    }

    public Task<CardBase> SelectCardFromHand()
    {
        if(gameContext.GameState is CallRearguardState)
        {
            var callResult = selectedCardForCall;
            selectedCardForCall = null!;
            return Task.FromResult(callResult);
        }
        if(gameContext.GameState is ActivateHandSkillState)
        {
            var handResult = selectedCardForHandSkillActivation;
            selectedCardForHandSkillActivation = null!;
            return Task.FromResult(handResult);
        }
        throw new NotImplementedException();
    }

    public Task<UnitCircle> SelectOwnUnitCircle(UnitSelector unitSelector)
    {
        if (gameContext.GameState is CallRearguardState)
        {
            if (selectedRearguardForCall is null) throw new InvalidOperationException();
            var result = selectedRearguardForCall;
            selectedRearguardForCall = null!;
            return Task.FromResult(result);
        }
        if (gameContext.GameState is SwapRearguardState && selectedRearguardForSwap is not null)
        {
            var result = selectedRearguardForSwap;
            selectedRearguardForSwap = null!;
            return Task.FromResult(result);
        }
        if (gameContext.GameState is ActivateUnitCircleSkillState && selectedUnitCircleActivation is not null)
        {
            var result = selectedUnitCircleActivation;
            selectedUnitCircleActivation = null!;
            return Task.FromResult(result);
        }
        throw new NotImplementedException();
    }

    public Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills)
    {
        if((gameContext.GameState is ActivateUnitCircleSkillState || gameContext.GameState is ActivateHandSkillState) && selectedSkillForActivation is not null)
        {
            var result = selectedSkillForActivation;
            selectedSkillForActivation = null!;
            return Task.FromResult(result);
        }
        throw new NotImplementedException();
    }
}