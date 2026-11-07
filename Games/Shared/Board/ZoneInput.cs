using Godot;

// What every zone view does the same way for input: its highlight, and letting clicks on its own controls reach it
public static class ZoneInput
{
    /// <summary>A border drawn above the zone's content while it's highlighted (hidden until then); it never takes clicks.</summary>
    public static ReferenceRect AddHighlight(Control zone, Color color, float width)
    {
        var overlay = new ReferenceRect
        {
            Name = "Highlight",
            EditorOnly = false,
            BorderColor = color,
            BorderWidth = width,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
        };
        zone.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return overlay;
    }

    /// <summary>
    /// The zone's own controls (background, margins, card slot) pass clicks on to it instead of swallowing them, so the
    /// zone's _GuiInput sees clicks on its empty space. Cards keep theirs: a card click is the card's.
    /// </summary>
    public static void PassClicksToZone(Node zone)
    {
        foreach(var child in zone.GetChildren())
        {
            if(child is CardBaseComponent) continue;
            if(child is Control { MouseFilter: Control.MouseFilterEnum.Stop } control) control.MouseFilter = Control.MouseFilterEnum.Pass;
            PassClicksToZone(child);
        }
    }

    /// <summary>The node and everything in it let the mouse through (e.g. a card in a zone that takes every press itself).</summary>
    public static void IgnoreMouse(Node node)
    {
        if(node is Control control) control.MouseFilter = Control.MouseFilterEnum.Ignore;
        foreach(var child in node.GetChildren()) IgnoreMouse(child);
    }

    public static bool IsClick(InputEvent input)
        => input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } or InputEventScreenTouch { Pressed: true };
}
