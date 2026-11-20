using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Marks where card details go on an input screen: while the screen shows it, CardDetailsOverlay fits its details into
/// this rect instead of its own place. Only one screen is up at a time, so two showing at once is an error. Put it in a
/// screen's layout (DetailsSpace.tscn); it shows nothing itself and lets presses through.
/// </summary>
public partial class DetailsSpace : Control
{
    static readonly List<DetailsSpace> inTree = [];

    public override void _EnterTree() => inTree.Add(this);
    public override void _ExitTree() => inTree.Remove(this);

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    /// <summary>The details space of the screen showing now, or null with no screen up.</summary>
    public static DetailsSpace? Showing()
    {
        var showing = inTree.Where(space => space.IsVisibleInTree()).ToList();
        return showing.Count switch
        {
            0 => null,
            1 => showing[0],
            _ => throw new InvalidOperationException(
                $"{showing.Count} details spaces are showing at once ({string.Join(", ", showing.Select(space => space.GetPath()))}): only one input screen may be up"),
        };
    }
}
