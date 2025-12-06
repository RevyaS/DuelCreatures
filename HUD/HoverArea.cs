using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class HoverArea : Control
{    
    private bool Hovered = false;
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
        Hovered = true;
        Hovering?.Invoke();
    }

    public override void _Process(double delta)
    {
        // Detect hover exit (no more input events inside control)
        Vector2 pointer = GetViewport().GetMousePosition();
        bool inside = GetGlobalRect().HasPoint(pointer);

        if(!inside && Hovered)
        {
            GD.Print($"Hovering: false");
            Hovered = false;
            HoverReleased?.Invoke();
        }
    }

    public event Action? Hovering;
    public event Action? HoverReleased;
}
