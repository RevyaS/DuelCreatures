using System;
using System.Collections.Generic;
using Godot;

/// <summary>Somewhere a dragged card can be dropped. Found by position, so it never has to catch the mouse.</summary>
public interface ICardDropTarget
{
    /// <summary>The area a drop lands in (hit-tested, rotation and scale included).</summary>
    Control DropRegion { get; }
    bool AcceptsCard(Card card);
    void DropCard(Card card);
    /// <summary>A card it accepts is being dragged over it (true) or left it (false).</summary>
    void SetCardHovering(bool hovering);
}

/// <summary>
/// The card being dragged. A copy follows the pointer above everything, and the drop goes to the topmost
/// registered <see cref="ICardDropTarget"/> under the pointer that accepts it. Drags are started by the
/// control that recognized the gesture (the card itself, or e.g. the unit circle holding it).
/// </summary>
public static class CardDrag
{
    static readonly List<ICardDropTarget> targets = [];
    static Card? dragged;
    static Control? preview;
    static ICardDropTarget? hovered;

    public static bool IsDragging => dragged is not null;

    /// <summary>A drag finished: the card, and whether a target took it.</summary>
    public static event Action<Card, bool>? Ended;

    public static void Register(ICardDropTarget target)
    {
        if(!targets.Contains(target)) targets.Add(target);
    }

    public static void Unregister(ICardDropTarget target)
    {
        targets.Remove(target);
        if(ReferenceEquals(hovered, target)) hovered = null;
    }

    public static void Begin(Card card, Vector2 pointer)
    {
        if(dragged is not null) Cancel();

        dragged = card;
        preview = CreatePreview(card);
        card.CurrentlyDragged = true;
        card.NotifyDragStarted();
        Move(pointer);
    }

    public static void Move(Vector2 pointer)
    {
        if(dragged is null || preview is null) return;

        var size = SizeConstants.CardDefaultSize * preview.Scale;
        preview.Position = pointer - size / 2;
        SetHovered(FindTarget(dragged, pointer));
    }

    public static void End(Vector2 pointer)
    {
        if(dragged is null) return;

        var card = dragged;
        var target = FindTarget(card, pointer);
        Finish();

        if(target is not null)
        {
            target.DropCard(card);
        }
        else
        {
            card.CurrentlyDragged = false;
            card.NotifyDragCancelled();
        }
        Ended?.Invoke(card, target is not null);
    }

    public static void Cancel()
    {
        if(dragged is null) return;

        var card = dragged;
        Finish();
        if(GodotObject.IsInstanceValid(card))
        {
            card.CurrentlyDragged = false;
            card.NotifyDragCancelled();
        }
        Ended?.Invoke(card, false);
    }

    // The topmost (latest in the tree) visible target under the pointer that takes the card
    private static ICardDropTarget? FindTarget(Card card, Vector2 pointer)
    {
        ICardDropTarget? found = null;
        foreach(var target in targets)
        {
            var region = target.DropRegion;
            if(!GodotObject.IsInstanceValid(region) || !UnderPointer.Contains(region, pointer)) continue;
            if(!target.AcceptsCard(card)) continue;
            if(found is null || region.IsGreaterThan(found.DropRegion)) found = target;
        }
        return found;
    }

    private static void SetHovered(ICardDropTarget? target)
    {
        if(ReferenceEquals(hovered, target)) return;
        hovered?.SetCardHovering(false);
        hovered = target;
        hovered?.SetCardHovering(true);
    }

    private static Control CreatePreview(Card card)
    {
        var copy = card.CreateClone();
        // Above everything, at the size the card is shown at
        card.GetTree().Root.AddChild(copy);
        copy.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        copy.PivotOffset = Vector2.Zero;
        copy.Scale = card.GetGlobalTransform().Scale;
        copy.ZIndex = 100;
        IgnoreMouse(copy);
        return copy;
    }

    private static void Finish()
    {
        SetHovered(null);
        if(preview is not null && GodotObject.IsInstanceValid(preview)) preview.QueueFree();
        preview = null;
        dragged = null;
    }

    private static void IgnoreMouse(Node node)
    {
        if(node is Control control) control.MouseFilter = Control.MouseFilterEnum.Ignore;
        foreach(var child in node.GetChildren())
        {
            IgnoreMouse(child);
        }
    }
}
