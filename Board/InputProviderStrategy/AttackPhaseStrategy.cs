using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;
using Godot;

public class AttackPhaseStrategy(DuelCreaturesBoard Board, GameContext GameContext) : BaseStrategy(Board), IRequestAttackPhaseAction, ISelectOpponentCircle, ISelectOwnUnitCircle
{
    UnitCircleComponent? boostingCircle = null;
    UnitCircleComponent? attackingCircle = null;
    UnitCircleComponent? targetCircle = null;

    public async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        GameBoard.ShowLeftButton(TextConstants.EndPhase);
        GameBoard.DisablePlayerFrontRowUnitCircleHovering();
        GameBoard.EnablePlayerUnitCircleScreenDragging();

        TaskCompletionSource<IAttackPhaseAction> completionSource = new();

        boostingCircle = null;
        attackingCircle = null;
        targetCircle = null;
        
        Action endPhaseHandler = () =>
        {
            Board.PlayPhaseChangeSfx();
            completionSource.SetResult(actions.FirstOf<EndAttackPhase>());
        };

        Action<UnitCircleComponent> oppHoverReleaseHandler = (unitCircleComponent) =>
        {
            if(ReferenceEquals(unitCircleComponent, targetCircle))
            {
                GD.Print("Release targetCircle ", GameBoard.OppArea.GetUnitCircleComponentName(unitCircleComponent));
                // Release
                targetCircle = null;
                GameBoard.HideAttackLines();
            }
        };

        Action<UnitCircleComponent> dragReleaseHandler = (unitCircleComponent) =>
        {
            GameBoard.OppArea.HideTargetIndicators();
            if(targetCircle is not null)
            {
                if (boostingCircle is not null)
                {
                    completionSource.SetResult(actions.FirstOf<BoostedAttackAction>());
                }
                else
                {
                    completionSource.SetResult(actions.FirstOf<AttackAction>());
                }
            } else
            {
                //RESET
                GD.Print("Resetting");
                boostingCircle = null;
                attackingCircle = null;
                targetCircle = null;
                GameBoard.EnablePlayerUnitCircleScreenDragging();
                GameBoard.DisableOppFrontRowUnitCircleHovering();
                GameBoard.HideBoostLines();
                GameBoard.HideAttackLines();
            }
        };

        Action<UnitCircleComponent> oppFrontRowHoverHandler = (unitCircleComponent) =>
        {
            if(attackingCircle is not null && VanguardGameRules.TargetCircleCanBeTargetted(GameBoard.OppArea.PlayArea, unitCircleComponent.UnitCircle))
            {
                targetCircle = unitCircleComponent;

                if(!targetCircle.HasTargetIndicator)
                {
                    GameBoard.OppArea.HideTargetIndicators();
                    targetCircle.ShowTargetIndicators();
                    GameBoard.PlayTargetSfx();
                }

                GameBoard.HideAttackLines();
                GameBoard.ShowAttackLine(attackingCircle, targetCircle);
            }
        };

        Action<UnitCircleComponent> playerFrontRowHoverHandler = (unitCircleComponent) =>
        {
            if(boostingCircle is not null && ReferenceEquals(GameBoard.GetPlayerOppositeCircle(unitCircleComponent), boostingCircle))
            {
                attackingCircle = unitCircleComponent;
                GameBoard.ShowBoostLine(unitCircleComponent);
                GameBoard.DisablePlayerFrontRowUnitCircleHovering();
                GameBoard.EnableOppFrontRowUnitCircleHovering();
            }
        };

        Action<UnitCircleComponent> screenDragHandler = (unitCircleComponent) => {
            // Catalyst for Boosted attack
            if(GameBoard.IsBackRow(unitCircleComponent))
            {
                boostingCircle = unitCircleComponent;
                GameBoard.DisablePlayerUnitCircleScreenDragging();
                GameBoard.EnablePlayerFrontRowUnitCircleHovering();
            }
            if(GameBoard.IsFrontRow(unitCircleComponent))
            {
                GD.Print("Front row dragging");
                attackingCircle = unitCircleComponent;
                GameBoard.DisablePlayerUnitCircleScreenDragging();
                GameBoard.EnableOppFrontRowUnitCircleHovering();
            }
        };

        GameBoard.LeftButtonPressed += endPhaseHandler;
        GameBoard.PlayerCircleHovering += playerFrontRowHoverHandler;
        GameBoard.PlayerCircleScreenDragged += screenDragHandler;
        GameBoard.OppCircleHovering += oppFrontRowHoverHandler;
        GameBoard.OppCircleHoverReleased += oppHoverReleaseHandler;
        GameBoard.PlayerCircleScreenDragReleased += dragReleaseHandler;

        var result = await completionSource.Task;
        GameBoard.LeftButtonPressed -= endPhaseHandler;
        GameBoard.PlayerCircleScreenDragged -= screenDragHandler;
        GameBoard.PlayerCircleHovering -= playerFrontRowHoverHandler;
        GameBoard.OppCircleHovering -= oppFrontRowHoverHandler;
        GameBoard.PlayerCircleScreenDragReleased -= dragReleaseHandler;
        GameBoard.OppCircleHoverReleased -= oppHoverReleaseHandler;

        GD.Print("Attack phase action selected: ", result.GetType().Name);
        return result;
    }

    public Task<UnitCircle> SelectOpponentCircle(UnitSelector selector)
    {
        if(targetCircle is null) throw new InvalidOperationException();
        return Task.FromResult(targetCircle.UnitCircle);
    }

    public Task<UnitCircle> SelectOwnUnitCircle(UnitSelector unitSelector)
    {
        if(GameContext.GameState is BoostedAttackState || GameContext.GameState is AttackState)
        {
            if (attackingCircle is null) throw new InvalidOperationException();
            return Task.FromResult(attackingCircle.UnitCircle);
        }
        throw new InvalidOperationException();
    }
}