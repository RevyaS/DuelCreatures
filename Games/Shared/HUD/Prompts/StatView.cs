using Godot;

/// <summary>
/// One number with its picture (a power, a critical, a shield): %Icon and %Value. A would-be number
/// (<see cref="Preview"/>) shows instead of the number in <see cref="PreviewColor"/>. Laid out in StatView.tscn; give
/// each instance its icon.
/// </summary>
public partial class StatView : Control
{
    [Export] public Texture2D? Icon { get; set; }
    /// <summary>How the number reads: {0} is the number (e.g. "{0}" or "x{0}").</summary>
    [Export] public string ValueFormat { get; set; } = "{0}";
    [Export] public Color ValueColor { get; set; } = Colors.White;
    /// <summary>The colour of a would-be number.</summary>
    [Export] public Color PreviewColor { get; set; } = new(1f, 0.85f, 0.2f);

    TextureRect? icon;
    Label? value;
    TextureRect IconRect => icon ??= GetNode<TextureRect>("%Icon");
    Label ValueLabel => value ??= GetNode<Label>("%Value");

    int shown;
    int? preview;

    public override void _Ready()
    {
        IconRect.Texture = Icon;
        IconRect.Visible = Icon is not null;
        Render();
    }

    /// <summary>The number shown.</summary>
    public int Value
    {
        get => shown;
        set
        {
            shown = value;
            Render();
        }
    }

    /// <summary>A would-be number shown instead, in PreviewColor; null shows Value again.</summary>
    public int? Preview
    {
        get => preview;
        set
        {
            preview = value;
            Render();
        }
    }

    private void Render()
    {
        if(!IsNodeReady()) return;
        ValueLabel.Text = string.Format(ValueFormat, preview ?? shown);
        ValueLabel.AddThemeColorOverride("font_color", preview is null ? ValueColor : PreviewColor);
    }
}
