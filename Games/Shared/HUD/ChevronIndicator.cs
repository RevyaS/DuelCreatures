using Godot;

/// <summary>
/// A few chevrons drawn one above the other ("v v", or "^ ^"), e.g. the handle of a collapsed prompt layer. Drawn, so it
/// needs no font with the glyph. Colour, size and direction are exported.
/// </summary>
[Tool]
[GlobalClass]
public partial class ChevronIndicator : Control
{
    private int _count = 2;
    [Export(PropertyHint.Range, "1, 5, 1")]
    public int Count { get => _count; set { _count = value; QueueRedraw(); } }

    private bool _pointsDown = true;
    [Export]
    public bool PointsDown { get => _pointsDown; set { _pointsDown = value; QueueRedraw(); } }

    private Color _color = new(1f, 1f, 1f, 0.85f);
    [Export]
    public Color Color { get => _color; set { _color = value; QueueRedraw(); } }

    private float _thickness = 6;
    [Export]
    public float Thickness { get => _thickness; set { _thickness = value; QueueRedraw(); } }

    /// <summary>Width and height of one chevron.</summary>
    private Vector2 _chevronSize = new(60, 18);
    [Export]
    public Vector2 ChevronSize { get => _chevronSize; set { _chevronSize = value; UpdateMinimumSize(); QueueRedraw(); } }

    private float _spacing = 6;
    [Export]
    public float Spacing { get => _spacing; set { _spacing = value; UpdateMinimumSize(); QueueRedraw(); } }

    public override Vector2 _GetMinimumSize() => new(ChevronSize.X, Count * ChevronSize.Y + (Count - 1) * Spacing);

    public override void _Draw()
    {
        var total = _GetMinimumSize();
        var origin = (Size - total) / 2;
        for(int i = 0; i < Count; i++)
        {
            var top = origin.Y + i * (ChevronSize.Y + Spacing);
            float tipY = PointsDown ? top + ChevronSize.Y : top;
            float endsY = PointsDown ? top : top + ChevronSize.Y;
            DrawPolyline([
                new(origin.X, endsY),
                new(origin.X + ChevronSize.X / 2, tipY),
                new(origin.X + ChevronSize.X, endsY),
            ], Color, Thickness, antialiased: true);
        }
    }
}
