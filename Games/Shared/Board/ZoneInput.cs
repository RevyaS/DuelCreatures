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

    /// <summary>
    /// Shows <paramref name="number"/> in a round badge at the top of <paramref name="zone"/> (which pick it was), or hides
    /// it when null, creating it the first time; it never takes clicks.
    /// </summary>
    public static void ShowPickNumber(Control zone, ref PanelContainer? badge, int? number, Color color, Color fontColor, int fontSize)
    {
        if(badge is null)
        {
            if(number is null) return;
            badge = new PanelContainer { Name = "PickNumber", MouseFilter = Control.MouseFilterEnum.Ignore };
            badge.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = color,
                CornerRadiusTopLeft = 100, CornerRadiusTopRight = 100, CornerRadiusBottomLeft = 100, CornerRadiusBottomRight = 100,
                ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 2, ContentMarginBottom = 2,
            });
            var label = new Label { Name = "Number", HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
            label.AddThemeColorOverride("font_color", fontColor);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            badge.AddChild(label);
            zone.AddChild(badge);
            badge.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterTop, Control.LayoutPresetMode.Minsize);
            badge.GrowHorizontal = Control.GrowDirection.Both;
        }
        badge.GetNode<Label>("Number").Text = number?.ToString() ?? string.Empty;
        badge.Visible = number is not null;
    }

    public static bool IsClick(InputEvent input)
        => input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } or InputEventScreenTouch { Pressed: true };
}
