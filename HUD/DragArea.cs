using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class DragArea : Control
{

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mm && mm.ButtonMask == MouseButtonMask.Left)
        {
            OnDrag();
        }

        if (e is InputEventScreenDrag)
        {
            OnDrag();
        }
    }

    private void OnDrag()
    {
        GD.Print($"Dragging: true");
        Dragging?.Invoke();
    }

    public event Action? Dragging;
}
