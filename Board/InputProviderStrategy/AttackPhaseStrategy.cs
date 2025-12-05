using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Common;
using ArC.Common.Extensions;
using Godot;

public class AttackPhaseStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy, IRequestAttackPhaseAction
{
    public async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        Board.ShowEndPhaseButton();
        Board.EnablePlayerUnitCircleScreenDragging();

        TaskCompletionSource<IAttackPhaseAction> completionSource = new();

        UnitCircleComponent? boostingCircle = null;
        UnitCircleComponent? attackingCircle = null;
        
        Action endPhaseHandler = () =>
        {
            completionSource.SetResult(actions.FirstOf<EndAttackPhase>());
        };
        Board.EndPhasePressed += endPhaseHandler;

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
        Board.PlayerCircleHovering += playerFrontRowHoverHandler;

        Action<UnitCircleComponent> screenDragHandler = (unitCircleComponent) => {
            if(Board.IsBackRow(unitCircleComponent))
            {
                boostingCircle = unitCircleComponent;
                Board.DisablePlayerUnitCircleScreenDragging();
                Board.EnablePlayerFrontRowUnitCircleHovering();
                GD.Print("Backrow dragging detected");
            }
        };
        Board.PlayerCircleScreenDragged += screenDragHandler;

        var result = await completionSource.Task;
        Board.EndPhasePressed -= endPhaseHandler;
        Board.PlayerCircleScreenDragged -= screenDragHandler;
        Board.PlayerCircleHovering -= playerFrontRowHoverHandler;

        return result;
    }
}