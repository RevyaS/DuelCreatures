using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;
using ArC.Common.Extensions;
using Godot;

public class AttackPhaseStrategy(VanguardBoard Board, GameContext GameContext) : BaseStrategy(Board), IRequestAttackPhaseAction, ISelectOpponentCircle, ISelectOwnUnitCircle
{
    UnitCircleComponent? boostingCircle = null;
    UnitCircleComponent? attackingCircle = null;
    UnitCircleComponent? targetCircle = null;

    public async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        GameBoard.ShowLeftButton(TextConstants.EndPhase);
        GameBoard.EnablePlayerUnitCircleScreenDragging();

        TaskCompletionSource<IAttackPhaseAction> completionSource = new();

        boostingCircle = null;
        attackingCircle = null;
        targetCircle = null;
        List<UnitCircleComponent> targets = [];

        Action endPhaseHandler = () =>
        {
            Board.Sfx.Play(SfxCue.PhaseChange, BoardSide.Player);
            completionSource.SetResult(actions.FirstOf<EndAttackPhase>());
        };

        void Aim(UnitCircleComponent attacker)
        {
            attackingCircle = attacker;
            targets = GameBoard.GetAttackTargets(attacker);
        }

        // Aiming starts by dragging out of one of the player's circles: from the back row it's a
        // boost (then over the unit in front of it to attack with that unit), from the front row an attack
        Action<UnitCircleComponent> dragStartedHandler = (circle) =>
        {
            boostingCircle = null;
            attackingCircle = null;
            targetCircle = null;
            targets = [];
            if(GameBoard.IsBackRow(circle))
            {
                boostingCircle = circle;
            }
            else if(GameBoard.IsFrontRow(circle))
            {
                Aim(circle);
            }
        };

        // What's under the pointer is found by position, so no overlay has to sit on the cards
        Action<UnitCircleComponent, Vector2> dragMovedHandler = (circle, position) =>
        {
            if(boostingCircle is not null && attackingCircle is null)
            {
                var boosted = GameBoard.GetPlayerOppositeCircle(boostingCircle);
                if(PointerHitTest.Contains(boosted, position))
                {
                    Aim(boosted);
                    GameBoard.ShowBoostLine(boosted);
                }
                return;
            }
            if(attackingCircle is null) return;

            var target = PointerHitTest.FindAt(targets, position);
            if(ReferenceEquals(target, targetCircle)) return;

            targetCircle = target;
            GameBoard.OppArea.HideTargetIndicators();
            GameBoard.HideAttackLines();
            if(targetCircle is null) return;

            targetCircle.ShowTargetIndicators();
            GameBoard.Sfx.Play(SfxCue.Target, BoardSide.Player);
            GameBoard.ShowAttackLine(attackingCircle, targetCircle);
        };

        Action<UnitCircleComponent> dragReleaseHandler = (circle) =>
        {
            GameBoard.OppArea.HideTargetIndicators();
            if(attackingCircle is not null && targetCircle is not null)
            {
                completionSource.SetResult(boostingCircle is not null
                    ? actions.FirstOf<BoostedAttackAction>()
                    : actions.FirstOf<AttackAction>());
                return;
            }

            // Let go anywhere else: start over
            boostingCircle = null;
            attackingCircle = null;
            targetCircle = null;
            targets = [];
            GameBoard.HideBoostLines();
            GameBoard.HideAttackLines();
        };

        GameBoard.LeftButtonPressed += endPhaseHandler;
        GameBoard.PlayerCircleScreenDragged += dragStartedHandler;
        GameBoard.PlayerCircleScreenDragMoved += dragMovedHandler;
        GameBoard.PlayerCircleScreenDragReleased += dragReleaseHandler;

        var result = await completionSource.Task;
        GameBoard.LeftButtonPressed -= endPhaseHandler;
        GameBoard.PlayerCircleScreenDragged -= dragStartedHandler;
        GameBoard.PlayerCircleScreenDragMoved -= dragMovedHandler;
        GameBoard.PlayerCircleScreenDragReleased -= dragReleaseHandler;
        GameBoard.DisablePlayerUnitCircleScreenDragging();

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