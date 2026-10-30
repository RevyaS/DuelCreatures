using System.Collections.Generic;
using Godot;

// Rotates its children in 90 degree steps (and scales them uniformly) while
// reporting the rotated/scaled size as its minimum size, so HBox/VBox parents
// lay it out correctly. Regular containers reset child rotation and scale when
// fitting them, this one applies them after fitting.
[Tool]
[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class QuarterTurnContainer : Container
{
    public enum QuarterTurn
    {
        Deg0, Deg90, Deg180, Deg270
    }

    private QuarterTurn _turns = QuarterTurn.Deg0;
    [Export]
    public QuarterTurn Turns
    {
        get => _turns;
        set
        {
            _turns = value;
            Refresh();
        }
    }

    private float _contentScale = 1;
    [Export(PropertyHint.Range, "0.01, 4.0, 0.01")]
    public float ContentScale
    {
        get => _contentScale;
        set
        {
            _contentScale = Mathf.Max(value, 0.01f);
            Refresh();
        }
    }

    bool IsSideways => _turns is QuarterTurn.Deg90 or QuarterTurn.Deg270;
    float TurnRadians => Mathf.DegToRad((int)_turns * 90);

    private void Refresh()
    {
        UpdateMinimumSize();
        QueueSort();
    }

    public override Vector2 _GetMinimumSize()
    {
        var size = Vector2.Zero;
        foreach(var control in GetLayoutChildren())
        {
            var childMinimum = control.GetCombinedMinimumSize();
            size = new Vector2(Mathf.Max(size.X, childMinimum.X), Mathf.Max(size.Y, childMinimum.Y));
        }

        size *= _contentScale;
        return IsSideways ? new Vector2(size.Y, size.X) : size;
    }

    public override void _Notification(int what)
    {
        if(what == NotificationSortChildren)
        {
            SortChildren();
        }
    }

    private void SortChildren()
    {
        // The area children get in their own (unrotated, unscaled) space
        var available = IsSideways ? new Vector2(Size.Y, Size.X) : Size;
        var childArea = available / _contentScale;
        var center = Size / 2;

        foreach(var control in GetLayoutChildren())
        {
            FitChildInRect(control, new Rect2(center - childArea / 2, childArea));

            // FitChildInRect resets these, so apply them afterwards. Pivoting on the
            // container's center keeps the turned child inside the container.
            control.PivotOffset = center - control.Position;
            control.Rotation = TurnRadians;
            control.Scale = new Vector2(_contentScale, _contentScale);
        }
    }

    private IEnumerable<Control> GetLayoutChildren()
    {
        foreach(var child in GetChildren())
        {
            if(child is Control control && control.Visible && !control.IsSetAsTopLevel())
            {
                yield return control;
            }
        }
    }
}
