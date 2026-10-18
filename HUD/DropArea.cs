using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
[Tool]
public partial class DropArea : Control
{
    bool dragHovered = false;

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        var result = data.Obj is VanguardCardComponent;
        // Called continuously while a drag hovers this area
        SetDragHovered(result);
        return result;
    }

    public override void _Notification(int what)
    {
        if(what == NotificationMouseExit || what == NotificationDragEnd
            || (what == NotificationVisibilityChanged && !Visible))
        {
            SetDragHovered(false);
        }
    }

    private void SetDragHovered(bool hovered)
    {
        if(dragHovered == hovered) return;

        dragHovered = hovered;
        DragHoverChanged?.Invoke(hovered);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        CardDropped?.Invoke((Card)data.Obj!);
    }

    public event Action<Card>? CardDropped;
    // A droppable card is being dragged over this area (true) or left it (false)
    public event Action<bool>? DragHoverChanged;
}
