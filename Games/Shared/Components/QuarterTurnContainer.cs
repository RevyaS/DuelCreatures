using System.Collections.Generic;
using Godot;

// Rotates its children in 90 degree steps (and scales them uniformly) while
// reporting the rotated/scaled size as its minimum size, so HBox/VBox parents
// lay it out correctly. Regular containers reset child rotation and scale when
// fitting them, this one applies them after fitting. With FacesOwner, inside a
// play area scene (PlayAreaView), it also turns half around on the far side of
// the board, so what it holds reads upright for the player across the table.
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

    private bool _facesOwner;
    /// <summary>Inside a play area scene: turns half around more on the far side of the board, so it reads upright from there.</summary>
    [Export]
    public bool FacesOwner
    {
        get => _facesOwner;
        set
        {
            _facesOwner = value;
            Refresh();
        }
    }

    private bool _onFarSide;
    /// <summary>Set by the play area (PlayAreaView.FlippedAppearance): it's on the far side of the board.</summary>
    public bool OnFarSide
    {
        get => _onFarSide;
        set
        {
            _onFarSide = value;
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

    // The turns it's set to, plus half a turn when it faces its owner across the board
    QuarterTurn EffectiveTurns => _facesOwner && _onFarSide ? (QuarterTurn)(((int)_turns + 2) % 4) : _turns;
    bool IsSideways => EffectiveTurns is QuarterTurn.Deg90 or QuarterTurn.Deg270;
    float TurnRadians => Mathf.DegToRad((int)EffectiveTurns * 90);

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
