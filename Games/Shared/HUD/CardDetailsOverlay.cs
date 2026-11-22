using System;
using System.Linq;
using Godot;

/// <summary>
/// A game's CardDetailsView shown over the board (e.g. Vanguard, whose board fills the screen): a full-screen layer
/// holding the details scene, shown with a card's details and hidden by a press anywhere outside them. Put the game's
/// details scene (a CardDetailsView) under it, where details go over the board. While an input screen is up, the
/// details are fitted into its DetailsSpace instead, and presses go through to the screen (a tap on another card shows
/// that one's details instead); they close with the screen.
/// </summary>
public partial class CardDetailsOverlay : Control
{
    CardDetailsView details = null!;
    // Where the scene puts the details (anchors, then offsets: left, top, right, bottom)
    float[] homeAnchors = null!;
    float[] homeOffsets = null!;

    /// <summary>The details shown here (e.g. to use their skill text interpreter, or to let a card list leave them clickable).</summary>
    public CardDetailsView Details => details;

    public override void _Ready()
    {
        details = GetChildren().OfType<CardDetailsView>().SingleOrDefault()
            ?? throw new InvalidOperationException($"{GetPath()}: a {nameof(CardDetailsOverlay)} holds one {nameof(CardDetailsView)} (the game's details scene)");
        homeAnchors = [ details.AnchorLeft, details.AnchorTop, details.AnchorRight, details.AnchorBottom ];
        homeOffsets = [ details.OffsetLeft, details.OffsetTop, details.OffsetRight, details.OffsetBottom ];
    }

    public override void _EnterTree() => InputScreen.CurrentChanged += OnScreenChanged;
    public override void _ExitTree() => InputScreen.CurrentChanged -= OnScreenChanged;

    // Details shown on a screen go with it
    private void OnScreenChanged()
    {
        if(onScreen && InputScreen.Current is null) Hide();
    }

    bool onScreen;

    // On a screen, a press outside the details closes them and still reaches the screen (a tap on another card then
    // shows that one's details)
    public override void _Input(InputEvent e)
    {
        if(!onScreen || !Visible || !ZoneInput.IsClick(e)) return;
        var position = e is InputEventScreenTouch touch ? touch.Position : GetGlobalMousePosition();
        if(!details.GetGlobalRect().HasPoint(position)) Hide();
    }

    public void Show(CardDetail detail)
    {
        details.Show(detail);
        Place(DetailsSpace.Showing());
        Show();
        MoveToFront();
    }

    // Into the input screen's details space, or where the scene puts them with no screen up
    private void Place(DetailsSpace? space)
    {
        onScreen = space is not null;
        // Over the board, a press anywhere else closes them; on a screen, presses reach the screen
        MouseFilter = onScreen ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        if(space is null)
        {
            SetEdges(details, homeAnchors, homeOffsets);
            return;
        }
        var toLocal = GetGlobalTransform().AffineInverse();
        var rect = space.GetGlobalRect();
        var topLeft = toLocal * rect.Position;
        var bottomRight = toLocal * rect.End;
        SetEdges(details, [ 0, 0, 0, 0 ], [ topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y ]);
    }

    private static void SetEdges(Control control, float[] anchors, float[] offsets)
    {
        Side[] sides = [ Side.Left, Side.Top, Side.Right, Side.Bottom ];
        for(var i = 0; i < sides.Length; i++)
        {
            control.SetAnchor(sides[i], anchors[i]);
            control.SetOffset(sides[i], offsets[i]);
        }
    }

    // Presses on the details reach them; a press anywhere else closes the overlay
    public override void _GuiInput(InputEvent e)
    {
        if(e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            Hide();
            AcceptEvent();
        }
    }
}
