using System;
using System.Linq;
using Godot;

/// <summary>
/// A game's CardDetailsView shown over the board (e.g. Vanguard, whose board fills the screen): a full-screen layer
/// holding the details scene, shown with a card's details and hidden by a press anywhere outside them. Put the game's
/// details scene (a CardDetailsView) under it.
/// </summary>
public partial class CardDetailsOverlay : Control
{
    CardDetailsView details = null!;

    /// <summary>The details shown here (e.g. to use their skill text interpreter, or to let a card list leave them clickable).</summary>
    public CardDetailsView Details => details;

    public override void _Ready()
    {
        details = GetChildren().OfType<CardDetailsView>().SingleOrDefault()
            ?? throw new InvalidOperationException($"{GetPath()}: a {nameof(CardDetailsOverlay)} holds one {nameof(CardDetailsView)} (the game's details scene)");
    }

    public void Show(CardDetail detail)
    {
        details.Show(detail);
        Show();
        MoveToFront();
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
