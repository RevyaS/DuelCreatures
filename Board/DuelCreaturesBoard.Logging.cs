using Godot;


public partial class DuelCreaturesBoard : Control
{
    public void PrintRearguardCardDraggableStates()
    {
        GD.Print($"{nameof(PlayerFrontLeft)}", PlayerFrontLeft.Draggable);
        GD.Print($"{nameof(PlayerBackLeft)}", PlayerBackLeft.Draggable);
        GD.Print($"{nameof(PlayerBackCenter)}", PlayerBackCenter.Draggable);
        GD.Print($"{nameof(PlayerFrontRight)}", PlayerFrontRight.Draggable);
        GD.Print($"{nameof(PlayerBackRight)}", PlayerBackRight.Draggable);
    }
}
