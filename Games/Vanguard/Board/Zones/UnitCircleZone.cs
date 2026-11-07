using Godot;

/// <summary>
/// A Vanguard unit circle: a <see cref="CardBox"/> (its unit rests sideways) drawn over the circle's art. Shows one
/// UnitCircle (the vanguard or a rear-guard), bound by name like any zone.
/// </summary>
[Tool]
public partial class UnitCircleZone : CardBox
{
    TextureRect? circleTexture, iconTexture;
    Control? targetArrows;

    private Texture2D? _circle;
    [Export]
    public Texture2D? Circle
    {
        get => _circle;
        set
        {
            _circle = value;
            AppearanceChanged();
        }
    }

    private Texture2D? _icon;
    [Export]
    public Texture2D? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            AppearanceChanged();
        }
    }

    private Color _circleColor = Colors.White;
    [Export]
    public Color CircleColor
    {
        get => _circleColor;
        set
        {
            _circleColor = value;
            AppearanceChanged();
        }
    }

    public override void _Ready()
    {
        circleTexture = GetNode<TextureRect>("%CircleTexture");
        iconTexture = GetNode<TextureRect>("%IconTexture");
        targetArrows = GetNode<Control>("%TargetArrows");
        base._Ready();
    }

    // Three arrows pointing at the unit: the attack would hit it
    protected override void ShowTargeted(bool targeted)
    {
        if(targetArrows is not null) targetArrows.Visible = targeted;
    }

    protected override void AppearanceChanged()
    {
        if(circleTexture is null || iconTexture is null) return;
        circleTexture.Texture = Circle;
        circleTexture.Modulate = CircleColor;
        iconTexture.Texture = Icon;
        // The icon reads upright from its own side
        iconTexture.FlipH = FlippedAppearance;
        iconTexture.FlipV = FlippedAppearance;
    }
}
