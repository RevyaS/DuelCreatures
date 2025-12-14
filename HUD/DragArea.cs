using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
[Tool]
public partial class DragArea : Control
{
    private bool IsDragging = false;

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
        IsDragging = true;
        Dragging?.Invoke();
    }

    public override void _Input(InputEvent @event)
    {
        if(!IsDragging) return;

        if(@event is InputEventMouseButton mb && !mb.Pressed)
        {
            OnDragEnd();
        }

        if(@event is InputEventScreenTouch st && !st.Pressed)
        {
            OnDragEnd();
        }
    }

    private void OnDragEnd()
    {
        GD.Print($"Dragging: false");
        IsDragging = false;
        DragReleased?.Invoke();
    }

    public event Action? Dragging;
    public event Action? DragReleased;
}
