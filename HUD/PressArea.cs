using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
[Tool]
public partial class PressArea : Control
{
    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            OnPressed();
        }

        if (e is InputEventScreenTouch st && st.Pressed)
        {
            OnPressed();
        }
    }

    private void OnPressed()
    {
        Pressed?.Invoke();
    }

    public event Action? Pressed;
}
