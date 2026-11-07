using System.Linq;
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
        foreach(var control in zone.Descendants(node => node is not CardBaseComponent).OfType<Control>())
        {
            if(control.MouseFilter == Control.MouseFilterEnum.Stop) control.MouseFilter = Control.MouseFilterEnum.Pass;
        }
    }

    /// <summary>
    /// Shows or hides the block over <paramref name="zone"/> marking it as targeted (e.g. the monster an attack would
    /// hit), creating it the first time it's shown; it never takes clicks.
    /// </summary>
    public static void ShowTargetBlock(Control zone, ref ColorRect? block, Color color, bool targeted)
    {
        if(block is null)
        {
            if(!targeted) return;
            block = new ColorRect { Name = "Target", Color = color, MouseFilter = Control.MouseFilterEnum.Ignore };
            zone.AddChild(block);
            block.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        block.Visible = targeted;
    }

    public static bool IsClick(InputEvent input)
        => input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } or InputEventScreenTouch { Pressed: true };
}
