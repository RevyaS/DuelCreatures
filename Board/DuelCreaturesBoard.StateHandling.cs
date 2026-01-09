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
        PlayerArea.Vanguard.Droppable = true;
    }
    public void DisablePlayerVanguardDropping()
    {
        PlayerArea.Vanguard.Droppable = false;
    }

    public void EnablePlayerRearguardDropping()
    {
        EnablePlayerRearguardDropping(PlayerArea.Rearguards);
    }

    public void EnablePlayerRearguardDropping(List<UnitCircleComponent> rearguards)
    {
        rearguards.ForEach(rg => rg.Droppable = true);
    }

    public void DisablePlayerRearguardDropping()
    {
        PlayerArea.DisableRearguardDropping();
    }

    public void EnablePlayerRearguardDragging()
    {
        PlayerArea.EnableRearguardDropping();
    }
    public void DisablePlayerRearguardDragging()
    {
        PlayerArea.DisableRearguardDragging(PlayerArea.Rearguards);
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
        PlayerArea.EnableUnitCircleScreenDragging();
    }
    public void DisablePlayerUnitCircleScreenDragging()
    {
        PlayerArea.DisableUnitCircleScreenDragging();
    }

    public void EnablePlayerFrontRowUnitCircleHovering()
    {
        PlayerArea.EnableFrontRowUnitCircleHovering();
    }

    public void DisablePlayerFrontRowUnitCircleHovering()
    {
        PlayerArea.DisableFrontRowUnitCircleHovering();
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
        PlayerArea.ShowBoostLine(unitCircleComponent);
    }

    public void HideBoostLines()
    {
        PlayerArea.HideBoostLines();
    }

    public void ShowAttackLine(UnitCircle attacker, UnitCircle target)
    {
        ShowAttackLine(GetUnitCircleComponent(attacker), GetUnitCircleComponent(target));
    }

    public void ShowAttackLine(UnitCircleComponent attacker, UnitCircleComponent target)
    {
        if (ReferenceEquals(PlayerArea.FrontLeft, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerLeftAttackLeftLine.Show();
        if (ReferenceEquals(PlayerArea.FrontLeft, attacker) && ReferenceEquals(OppVanguard, target)) PlayerLeftAttackCenterLine.Show();
        if (ReferenceEquals(PlayerArea.FrontLeft, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerLeftAttackRightLine.Show();
        if (ReferenceEquals(PlayerArea.Vanguard, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerCenterAttackLeftLine.Show();
        if (ReferenceEquals(PlayerArea.Vanguard, attacker) && ReferenceEquals(OppVanguard, target)) PlayerCenterAttackCenterLine.Show();
        if (ReferenceEquals(PlayerArea.Vanguard, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerCenterAttackRightLine.Show();
        if (ReferenceEquals(PlayerArea.FrontRight, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerRightAttackLeftLine.Show();
        if (ReferenceEquals(PlayerArea.FrontRight, attacker) && ReferenceEquals(OppVanguard, target)) PlayerRightAttackCenterLine.Show();
        if (ReferenceEquals(PlayerArea.FrontRight, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerRightAttackRightLine.Show();

        if (ReferenceEquals(OppFrontLeft, attacker) && ReferenceEquals(PlayerArea.FrontLeft, target)) OppLeftAttackLeftLine.Show();
        if (ReferenceEquals(OppFrontLeft, attacker) && ReferenceEquals(PlayerArea.Vanguard, target)) OppLeftAttackCenterLine.Show();
        if (ReferenceEquals(OppFrontLeft, attacker) && ReferenceEquals(PlayerArea.FrontRight, target)) OppLeftAttackRightLine.Show();
        if (ReferenceEquals(OppVanguard, attacker) && ReferenceEquals(PlayerArea.FrontLeft, target)) OppCenterAttackLeftLine.Show();
        if (ReferenceEquals(OppVanguard, attacker) && ReferenceEquals(PlayerArea.Vanguard, target)) OppCenterAttackCenterLine.Show();
        if (ReferenceEquals(OppVanguard, attacker) && ReferenceEquals(PlayerArea.FrontRight, target)) OppCenterAttackRightLine.Show();
        if (ReferenceEquals(OppFrontRight, attacker) && ReferenceEquals(PlayerArea.FrontLeft, target)) OppRightAttackLeftLine.Show();
        if (ReferenceEquals(OppFrontRight, attacker) && ReferenceEquals(PlayerArea.Vanguard, target)) OppRightAttackCenterLine.Show();
        if (ReferenceEquals(OppFrontRight, attacker) && ReferenceEquals(PlayerArea.FrontRight, target)) OppRightAttackRightLine.Show();
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

        OppLeftAttackLeftLine.Hide();
        OppLeftAttackCenterLine.Hide();
        OppLeftAttackRightLine.Hide();
        OppCenterAttackLeftLine.Hide();
        OppCenterAttackCenterLine.Hide();
        OppCenterAttackRightLine.Hide();
        OppRightAttackLeftLine.Hide();
        OppRightAttackCenterLine.Hide();
        OppRightAttackRightLine.Hide();
    }

    public void EnableSelectOppCircle(UnitSelector selector)
    {
        var selectedCircles = _gameSession.Game.Board.Player2Area.GetUnitCirclesBySelector(selector);
        selectedCircles.Select(circle => OpponentCircles.First(pc => ReferenceEquals(pc.UnitCircle, circle))).ToList()
            .ForEach((circle) => circle.Selectable = true);
    }

    public void DisableSelectOppUnitCircle()
    {
        OpponentCircles.ForEach((circle) => circle.Selectable = false);
    }

    public void EnableSelectOwnUnitCircle(UnitSelector selector)
    {
        PlayerArea.EnableSelectUnitCircle(selector);
    }

    public void DisableSelectOwnUnitCircle()
    {
        PlayerArea.DisableSelectUnitCircle();
    }
}
