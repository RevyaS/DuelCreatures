using System.Collections.Generic;
using Godot;

/// <summary>
/// Lines drawn between zones on the board (e.g. an attack and its boost), each an <see cref="IndicatorScene"/>
/// (an AttackIndicator) from the centre of one control to the centre of another. Put it over the board, full size;
/// it never takes the mouse. A game's component decides when to draw and clear them.
/// </summary>
public partial class ZoneLinesLayer : Control
{
    /// <summary>The line drawn: an AttackIndicator scene (a Path2D whose curve is set from start to end).</summary>
    [Export] public PackedScene? IndicatorScene { get; set; }

    readonly List<AttackIndicator> lines = [];

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Draws a line from <paramref name="from"/>'s centre to <paramref name="to"/>'s, moving at <paramref name="speed"/>, its arrows scaled by <paramref name="textureScale"/>.</summary>
    public void Draw(Control from, Control to, float speed, float textureScale)
    {
        if(IndicatorScene is null) return;
        var line = IndicatorScene.Instantiate<AttackIndicator>();
        AddChild(line);
        // Its curve is in this layer's coordinates, and its own (not the scene's shared one)
        line.Position = Vector2.Zero;
        line.Curve = new Curve2D();
        var toLocal = GetGlobalTransform().AffineInverse();
        line.Curve.AddPoint(toLocal * CentreOf(from));
        line.Curve.AddPoint(toLocal * CentreOf(to));
        line.Speed = speed;
        line.TextureScale = textureScale;
        line.Show();
        line.Active = true;
        lines.Add(line);
    }

    /// <summary>Removes every line drawn.</summary>
    public void Clear()
    {
        foreach(var line in lines)
        {
            if(IsInstanceValid(line)) line.QueueFree();
        }
        lines.Clear();
    }

    private static Vector2 CentreOf(Control control) => control.GetGlobalTransform() * (control.Size / 2);
}
