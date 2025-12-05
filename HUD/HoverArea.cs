using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class HoverArea : Control
{

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mm)
        {
            OnHover();
        }

        if (e is InputEventScreenDrag)
        {
            OnHover();
        }
    }

    private void OnHover()
    {
        GD.Print($"Hovering: true");
        Hovering?.Invoke();
    }

    public event Action? Hovering;
}
