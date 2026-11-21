using Godot;

/// <summary>
/// The facing indicator: over a card offered to pick, %FaceDown or %FaceUp shows (see <see cref="FacingMark"/>), or
/// nothing. The scene's root covers the card at the card component's own size (300x420, scaled with the card); each
/// state is a node of its own to design freely (a shade and a label to start with: swap in a graphic). Laid out in
/// FacingIndicator.tscn; the code only picks which state shows.
/// </summary>
public partial class FacingIndicator : Control
{
    Control? faceDown, faceUp;
    Control FaceDown => faceDown ??= GetNode<Control>("%FaceDown");
    Control FaceUp => faceUp ??= GetNode<Control>("%FaceUp");

    FacingMark mark;

    public override void _Ready() => Mark = mark;

    public FacingMark Mark
    {
        get => mark;
        set
        {
            mark = value;
            Visible = value != FacingMark.None;
            if(!IsNodeReady()) return;
            FaceDown.Visible = value == FacingMark.FaceDown;
            FaceUp.Visible = value == FacingMark.FaceUp;
        }
    }
}
