using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Extensions;
using Godot;

public class AttackPhaseStrategy(DuelCreaturesBoard Board, GameContext GameContext) : IInputProviderStrategy, IRequestAttackPhaseAction, ISelectOwnUnitCircle, ISelectOpponentFrontRow
{
    UnitCircleComponent? boostingCircle = null;
    UnitCircleComponent? attackingCircle = null;
    UnitCircleComponent? targetCircle = null;

    public async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        Board.ShowEndPhaseButton();
        Board.DisablePlayerFrontRowUnitCircleHovering();
        Board.EnablePlayerUnitCircleScreenDragging();

        TaskCompletionSource<IAttackPhaseAction> completionSource = new();

        boostingCircle = null;
        attackingCircle = null;
        targetCircle = null;
        
        Action endPhaseHandler = () =>
        {
            completionSource.SetResult(actions.FirstOf<EndAttackPhase>());
        };

        Action<UnitCircleComponent> oppHoverReleaseHandler = (unitCircleComponent) =>
        {
            if(ReferenceEquals(unitCircleComponent, targetCircle))
            {
                GD.Print("Release targetCircle");
                // Release
                targetCircle = null;
                Board.HideAttackLines();
            }
        };

        Action<UnitCircleComponent> dragReleaseHandler = (unitCircleComponent) =>
        {
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
                Board.EnablePlayerUnitCircleScreenDragging();
                Board.DisableOppFrontRowUnitCircleHovering();
                Board.HideBoostLines();
                Board.HideAttackLines();
            }
        };

        Action<UnitCircleComponent> oppFrontRowHoverHandler = (unitCircleComponent) =>
        {
            if(attackingCircle is not null)
            {
                targetCircle = unitCircleComponent;
                Board.HideAttackLines();
                Board.ShowAttackLine(attackingCircle, targetCircle);
            }
        };

        Action<UnitCircleComponent> playerFrontRowHoverHandler = (unitCircleComponent) =>
        {
            if(boostingCircle is not null && ReferenceEquals(Board.GetPlayerOppositeCircle(unitCircleComponent), boostingCircle))
            {
                attackingCircle = unitCircleComponent;
                Board.ShowBoostLine(unitCircleComponent);
                Board.DisablePlayerFrontRowUnitCircleHovering();
                Board.EnableOppFrontRowUnitCircleHovering();
            }
        };

        Action<UnitCircleComponent> screenDragHandler = (unitCircleComponent) => {
            // Catalyst for Boosted attack
            if(Board.IsBackRow(unitCircleComponent))
            {
                boostingCircle = unitCircleComponent;
                Board.DisablePlayerUnitCircleScreenDragging();
                Board.EnablePlayerFrontRowUnitCircleHovering();
            }
            if(Board.IsFrontRow(unitCircleComponent))
            {
                GD.Print("Front row dragging");
                attackingCircle = unitCircleComponent;
                Board.DisablePlayerUnitCircleScreenDragging();
                Board.EnableOppFrontRowUnitCircleHovering();
            }
        };

        Board.EndPhasePressed += endPhaseHandler;
        Board.PlayerCircleHovering += playerFrontRowHoverHandler;
        Board.PlayerCircleScreenDragged += screenDragHandler;
        Board.OppCircleHovering += oppFrontRowHoverHandler;
        Board.OppCircleHoverReleased += oppHoverReleaseHandler;
        Board.PlayerCircleScreenDragReleased += dragReleaseHandler;

        var result = await completionSource.Task;
        Board.EndPhasePressed -= endPhaseHandler;
        Board.PlayerCircleScreenDragged -= screenDragHandler;
        Board.PlayerCircleHovering -= playerFrontRowHoverHandler;
        Board.OppCircleHovering -= oppFrontRowHoverHandler;
        Board.PlayerCircleScreenDragReleased -= dragReleaseHandler;
        Board.OppCircleHoverReleased -= oppHoverReleaseHandler;

        return result;
    }

    public Task<UnitCircle> SelectOpponentFrontRow(UnitSelector selector)
    {
        if(targetCircle is null) throw new InvalidOperationException();
        return Task.FromResult(targetCircle.UnitCircle);
    }

    public Task<UnitCircle> SelectOwnUnitCircle()
    {
        if(attackingCircle is null) throw new InvalidOperationException();
        return Task.FromResult(attackingCircle.UnitCircle);
    }
}