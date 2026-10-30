using System;
using Godot;

[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
[Tool]
// Where a dragged card can be dropped while it's shown (e.g. a line's or circle's Droppable).
// Found by position (CardDrag), so it never catches the mouse and can't block clicks.
public partial class DropArea : Control, ICardDropTarget
{
    public override void _Ready()
    {
        if(Engine.IsEditorHint()) return;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _EnterTree()
    {
        if(Engine.IsEditorHint()) return;
        CardDrag.Register(this);
    }

    public override void _ExitTree()
    {
        CardDrag.Unregister(this);
    }

    public Control DropRegion => this;

    public bool AcceptsCard(Card card) => IsVisibleInTree();

    public void DropCard(Card card) => CardDropped?.Invoke(card);

    public void SetCardHovering(bool hovering) => DragHoverChanged?.Invoke(hovering);

    public event Action<Card>? CardDropped;
    // A droppable card is being dragged over this area (true) or left it (false)
    public event Action<bool>? DragHoverChanged;
}
