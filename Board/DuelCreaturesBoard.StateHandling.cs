using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    public void ShowLeftButton(string caption)
    {
        LeftButton.Text = caption;
        LeftButton.Show();
    }

    public void HideLeftButton()
    {
        LeftButton.Hide();
    }

    public void DisableLeftButton()
    {
        LeftButton.Disabled = true;
    }
    public void EnableLeftButton()
    {
        LeftButton.Disabled = false;
    }
    public void EnableLeftButton(bool shouldEnable)
    {
        LeftButton.Disabled = !shouldEnable;
    }

    public void EnablePlayerVanguardDropping()
    {
        PlayerVanguard.Droppable = true;
    }
    public void DisablePlayerVanguardDropping()
    {
        PlayerVanguard.Droppable = false;
    }

    public void EnablePlayerRearguardDropping()
    {
        EnablePlayerRearguardDropping(PlayerRearguards);
    }

    public void EnablePlayerRearguardDropping(List<UnitCircleComponent> rearguards)
    {
        rearguards.ForEach(rg => rg.Droppable = true);
    }

    public void DisablePlayerRearguardDropping()
    {
        PlayerFrontLeft.Droppable = false;
        PlayerBackLeft.Droppable = false;
        PlayerBackCenter.Droppable = false;
        PlayerFrontRight.Droppable = false;
        PlayerBackRight.Droppable = false;
    }

    public void EnablePlayerRearguardDragging()
    {
        PlayerFrontLeft.Draggable = true;
        PlayerBackLeft.Draggable = true;
        PlayerBackCenter.Draggable = true;
        PlayerFrontRight.Draggable = true;
        PlayerBackRight.Draggable = true;
    }
    public void DisablePlayerRearguardDragging()
    {
        DisablePlayerRearguardDragging(PlayerRearguards);
    }

    public void DisablePlayerRearguardDragging(List<UnitCircleComponent> unitCircleComponents)
    {
        unitCircleComponents.ForEach(rg => rg.Draggable = false);
    }

    public void EnablePlayerHandDragging()
    {
        PlayerHand.Draggable = true;
    }
    public void DisablePlayerHandDragging()
    {
        PlayerHand.Draggable = false;
    }

    public void EnablePlayerHandDropping()
    {
        PlayerHand.Droppable = true;
    }
    public void DisablePlayerHandDropping()
    {
        PlayerHand.Droppable = false;
    }

    public void EnableGuardDragging()
    {
        GuardZone.Draggable = true;
    }
    public void DisableGuardDragging()
    {
        GuardZone.Draggable = false;
    }

    public void EnableGuardDropping()
    {
        GuardZone.Droppable = true;
    }
    public void DisableGuardDropping()
    {
        GuardZone.Droppable = false;
    }

    public void EnablePlayerUnitCircleScreenDragging()
    {
        PlayerVanguard.ScreenDraggable = true;
        PlayerFrontLeft.ScreenDraggable = true;
        PlayerBackLeft.ScreenDraggable = true;
        PlayerBackCenter.ScreenDraggable = true;
        PlayerFrontRight.ScreenDraggable = true;
        PlayerBackRight.ScreenDraggable = true;
    }
    public void DisablePlayerUnitCircleScreenDragging()
    {
        PlayerVanguard.ScreenDraggable = false;
        PlayerFrontLeft.ScreenDraggable = false;
        PlayerBackLeft.ScreenDraggable = false;
        PlayerBackCenter.ScreenDraggable = false;
        PlayerFrontRight.ScreenDraggable = false;
        PlayerBackRight.ScreenDraggable = false;
    }

    public void EnablePlayerFrontRowUnitCircleHovering()
    {
        PlayerFrontRowCircles.ForEach((circle) => circle.Hoverable = true);
    }

    public void DisablePlayerFrontRowUnitCircleHovering()
    {
        PlayerFrontRowCircles.ForEach((circle) => circle.ScreenDraggable = false);
    }

    public void EnableOppFrontRowUnitCircleHovering()
    {
        OppFrontRowCircles.ForEach((circle) => circle.Hoverable = true);
    }

    public void DisableOppFrontRowUnitCircleHovering()
    {
        OppFrontRowCircles.ForEach((circle) => circle.ScreenDraggable = false);
    }
    public void ShowBoostLine(UnitCircleComponent unitCircleComponent)
    {
        if (ReferenceEquals(PlayerFrontLeft, unitCircleComponent)) PlayerLeftBoostLine.Show();
        if (ReferenceEquals(PlayerVanguard, unitCircleComponent)) PlayerCenterBoostLine.Show();
        if (ReferenceEquals(PlayerFrontRight, unitCircleComponent)) PlayerRightBoostLine.Show();
    }

    public void HideBoostLines()
    {
        PlayerLeftBoostLine.Hide();
        PlayerCenterBoostLine.Hide();
        PlayerRightBoostLine.Hide();
    }

    public void ShowAttackLine(UnitCircleComponent attacker, UnitCircleComponent target)
    {
        if (ReferenceEquals(PlayerFrontLeft, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerLeftAttackLeftLine.Show();
        if (ReferenceEquals(PlayerFrontLeft, attacker) && ReferenceEquals(OppVanguard, target)) PlayerLeftAttackCenterLine.Show();
        if (ReferenceEquals(PlayerFrontLeft, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerLeftAttackRightLine.Show();
        if (ReferenceEquals(PlayerVanguard, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerCenterAttackLeftLine.Show();
        if (ReferenceEquals(PlayerVanguard, attacker) && ReferenceEquals(OppVanguard, target)) PlayerCenterAttackCenterLine.Show();
        if (ReferenceEquals(PlayerVanguard, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerCenterAttackRightLine.Show();
        if (ReferenceEquals(PlayerFrontRight, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerRightAttackLeftLine.Show();
        if (ReferenceEquals(PlayerFrontRight, attacker) && ReferenceEquals(OppVanguard, target)) PlayerRightAttackCenterLine.Show();
        if (ReferenceEquals(PlayerFrontRight, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerRightAttackRightLine.Show();
    }

    public void HideAttackLines()
    {
        PlayerLeftAttackLeftLine.Hide();
        PlayerLeftAttackCenterLine.Hide();
        PlayerLeftAttackRightLine.Hide();
        PlayerCenterAttackLeftLine.Hide();
        PlayerCenterAttackCenterLine.Hide();
        PlayerCenterAttackRightLine.Hide();
        PlayerRightAttackLeftLine.Hide();
        PlayerRightAttackCenterLine.Hide();
        PlayerRightAttackRightLine.Hide();
    }

    public void EnableSelectOppCircle(UnitSelector selector)
    {
        List<UnitCircleComponent> selection = [];
        if(selector.HasFlag(UnitSelector.VANGUARD))
        {
            selection.Add(OppVanguard);
        }
        if(selector.HasFlag(UnitSelector.REARGUARD))
        {
            if(selector.HasFlag(UnitSelector.FRONT))
            {
                selection.AddRange(OppFrontRowRearguards);
            }
            if(selector.HasFlag(UnitSelector.BACK))
            {
                selection.AddRange(OppBackRowRearguards);
            }
        }

        Func<UnitCircleComponent, bool> predicate = (uc) => selector.HasFlag(UnitSelector.EMPTY) ? true : !uc.UnitCircle.IsEmpty;

        selection.Where(predicate).ToList().ForEach((circle) => circle.Selectable = true);
    }

    public void DisableSelectOppUnitCircle()
    {
        OpponentCircles.ForEach((circle) => circle.Selectable = false);
    }

    public void EnableSelectOwnUnitCircle(UnitSelector selector)
    {
        List<UnitCircleComponent> selection = [];
        if(selector.HasFlag(UnitSelector.VANGUARD))
        {
            selection.Add(PlayerVanguard);
        }
        if(selector.HasFlag(UnitSelector.FRONT_LEFT))
        {
            selection.Add(PlayerFrontLeft);
        }
        if(selector.HasFlag(UnitSelector.BACK_LEFT))
        {
            selection.Add(PlayerBackLeft);
        }
        if(selector.HasFlag(UnitSelector.BACK_CENTER))
        {
            selection.Add(PlayerBackCenter);
        }
        if(selector.HasFlag(UnitSelector.FRONT_RIGHT))
        {
            selection.Add(PlayerFrontRight);
        }
        if(selector.HasFlag(UnitSelector.BACK_RIGHT))
        {
            selection.Add(PlayerBackRight);
        }

        bool allowEmpty     = selector.HasFlag(UnitSelector.EMPTY);
        bool allowNonEmpty  = selector.HasFlag(UnitSelector.NON_EMPTY);
        bool allowResting   = selector.HasFlag(UnitSelector.RESTING);

        Func<UnitCircleComponent, bool> predicate = uc =>
        {
            bool isEmpty = uc.UnitCircle.IsEmpty;
            bool isStanding = uc.UnitCircle.IsStanding;

            // Empty / Non-empty logic
            bool emptyCheck =
                (!allowEmpty && !allowNonEmpty) ||   // no restriction
                (allowEmpty && isEmpty) ||
                (allowNonEmpty && !isEmpty);

            // Resting logic
            bool restingCheck =
                !allowResting || !isStanding;

            return emptyCheck && restingCheck;
        };

        selection.Where(predicate).ToList().ForEach((circle) => circle.Selectable = true);
    }

    public void DisableSelectOwnUnitCircle()
    {
        PlayerCircles.ForEach((circle) => { 
            circle.ResetSelection();
            circle.Selectable = false;
        });
    }
}
