using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class DragArea : Control
{

public override void _GuiInput(InputEvent e)
{
    if (e is InputEventScreenDrag)
    {
        GD.Print($"Dragging: true");
    }
}

    public event Action? Dragging;
}
