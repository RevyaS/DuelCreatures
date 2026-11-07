using Godot;

/// <summary>
/// Where the line of a drag from a zone is drawn (e.g. an attack): an <see cref="IndicatorScene"/> (an AttackIndicator)
/// stretched from the zone to the pointer, or to the target it would be let go on. Put it over the board in the HUD,
/// full size; it never takes the mouse.
/// </summary>
public partial class AimIndicatorLayer : Control
{
    /// <summary>The line drawn: an AttackIndicator scene (a Path2D whose curve is set from start to end).</summary>
    [Export] public PackedScene? IndicatorScene { get; set; }

    AttackIndicator? line;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void ShowLine(Vector2 fromGlobal, Vector2 toGlobal)
    {
        if(IndicatorScene is null) return;
        if(line is null)
        {
            line = IndicatorScene.Instantiate<AttackIndicator>();
            AddChild(line);
            // Its curve is in this layer's coordinates
            line.Position = Vector2.Zero;
            // Its own curve, not the scene's shared one
            line.Curve = (Curve2D)line.Curve.Duplicate();
        }
        var toLocal = GetGlobalTransform().AffineInverse();
        line.Curve.ClearPoints();
        line.Curve.AddPoint(toLocal * fromGlobal);
        line.Curve.AddPoint(toLocal * toGlobal);
        line.Show();
    }

    public void HideLine() => line?.Hide();
}
