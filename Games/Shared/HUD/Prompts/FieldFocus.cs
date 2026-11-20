using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Marks the rect of an input screen the field is enlarged into. <see cref="Focus"/> scales and moves the real board
/// view so the given play areas fill this rect (centered, keeping their shape), and <see cref="Release"/> puts it back.
/// The board itself is transformed, not a copy and not a play area inside it: its zones, highlights, badges, drops and
/// taps stay the ones the input layer already uses, and whatever works from global positions (aim lines, card flights,
/// drops) follows. Containers lay their children out again at will, so the board's root is moved, which no container
/// holds. Put it on a Control of the screen's layout that lets presses through; the screen covers the rest of the board.
/// </summary>
public partial class FieldFocus : Control
{
    Control? board;
    Vector2 savedPosition, savedScale, savedPivot;

    public bool IsFocused => board is not null;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    /// <summary>Enlarges <paramref name="boardRoot"/> so that <paramref name="areas"/> (nodes inside it) fill this rect.</summary>
    public void Focus(Control boardRoot, IReadOnlyCollection<Control> areas)
    {
        if(areas.Count == 0) throw new ArgumentException($"{GetPath()}: nothing to focus on", nameof(areas));
        Release();

        // The areas' bounds in the board's own coordinates (rotations included, e.g. the far side's play area)
        var toBoard = boardRoot.GetGlobalTransform().AffineInverse();
        var bounds = BoundsOf(areas.SelectMany(area => Corners(area).Select(corner => toBoard * corner)));
        if(bounds.Size.X <= 0 || bounds.Size.Y <= 0) return;

        // This rect in the coordinates the board is placed in
        var parent = boardRoot.GetParent() as CanvasItem
            ?? throw new InvalidOperationException($"{boardRoot.GetPath()} has no parent to be placed in");
        var toParent = parent.GetGlobalTransform().AffineInverse();
        var target = BoundsOf(Corners(this).Select(corner => toParent * corner));

        board = boardRoot;
        savedPosition = boardRoot.Position;
        savedScale = boardRoot.Scale;
        savedPivot = boardRoot.PivotOffset;

        var scale = Mathf.Min(target.Size.X / bounds.Size.X, target.Size.Y / bounds.Size.Y);
        boardRoot.PivotOffset = Vector2.Zero;
        boardRoot.Scale = new Vector2(scale, scale);
        boardRoot.Position = target.GetCenter() - bounds.GetCenter() * scale;
    }

    /// <summary>Puts the board back as it was.</summary>
    public void Release()
    {
        if(board is null) return;
        if(IsInstanceValid(board))
        {
            board.PivotOffset = savedPivot;
            board.Scale = savedScale;
            board.Position = savedPosition;
        }
        board = null;
    }

    public override void _ExitTree() => Release();

    private static IEnumerable<Vector2> Corners(Control control)
    {
        var transform = control.GetGlobalTransform();
        var size = control.Size;
        return [ transform * Vector2.Zero, transform * new Vector2(size.X, 0), transform * size, transform * new Vector2(0, size.Y) ];
    }

    private static Rect2 BoundsOf(IEnumerable<Vector2> points)
    {
        var list = points.ToList();
        var rect = new Rect2(list[0], Vector2.Zero);
        foreach(var point in list.Skip(1)) rect = rect.Expand(point);
        return rect;
    }
}
