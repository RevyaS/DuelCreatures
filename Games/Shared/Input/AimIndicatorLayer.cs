using Godot;

/// <summary>
/// Where the line of a drag from a zone is drawn (e.g. an attack): an <see cref="IndicatorScene"/> (an AttackIndicator)
/// from the zone to the target it would be let go on, once there is one. Put it over the board in the HUD, full size;
/// it never takes the mouse.
/// </summary>
public partial class AimIndicatorLayer : Control
{
    /// <summary>The line drawn: an AttackIndicator scene (a Path2D whose curve is set from start to end).</summary>
    [Export] public PackedScene? IndicatorScene { get; set; }
    /// <summary>The size of the line's arrows (the scene's own is too big to see the cards under it).</summary>
    [Export] public float TextureScale { get; set; } = 0.4f;
    /// <summary>How fast the line's arrows move along it (progress per second).</summary>
    [Export] public float Speed { get; set; } = 1.6f;

    AttackIndicator? line;
    AttackIndicator? secondLine;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void ShowLine(Vector2 fromGlobal, Vector2 toGlobal)
    {
        Place(ref line, fromGlobal, toGlobal);
        secondLine?.Hide();
    }

    /// <summary>Two lines, one after the other (e.g. from a booster to the unit it boosts, then on to the target).</summary>
    public void ShowLines((Vector2 From, Vector2 To) first, (Vector2 From, Vector2 To) second)
    {
        Place(ref line, first.From, first.To);
        Place(ref secondLine, second.From, second.To);
    }

    public void HideLine()
    {
        line?.Hide();
        secondLine?.Hide();
    }

    private void Place(ref AttackIndicator? indicator, Vector2 fromGlobal, Vector2 toGlobal)
    {
        if(IndicatorScene is null) return;
        if(indicator is null)
        {
            indicator = IndicatorScene.Instantiate<AttackIndicator>();
            AddChild(indicator);
            // Its curve is in this layer's coordinates
            indicator.Position = Vector2.Zero;
            // Its own curve, not the scene's shared one
            indicator.Curve = (Curve2D)indicator.Curve.Duplicate();
            indicator.TextureScale = TextureScale;
            indicator.Speed = Speed;
        }
        var toLocal = GetGlobalTransform().AffineInverse();
        indicator.Curve.ClearPoints();
        indicator.Curve.AddPoint(toLocal * fromGlobal);
        indicator.Curve.AddPoint(toLocal * toGlobal);
        indicator.Show();
    }
}
