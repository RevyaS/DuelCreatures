using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
[Tool]
public partial class HoverArea : Control
{    
    private bool Hovered = false;
    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mm)
        {
            OnHover();
        }
    }

    private void OnHover()
    {
        Hovered = true;
        Hovering?.Invoke();
    }

    public override void _Process(double delta)
    {
        // Detect hover exit (no more input events inside control)
        Vector2 pointer = GetLocalMousePosition();
        bool inside = GetRect().HasPoint(pointer);

        if(!inside && Hovered)
        {
            Hovered = false;
            HoverReleased?.Invoke();
        }
    }

    public event Action? Hovering;
    public event Action? HoverReleased;
}
