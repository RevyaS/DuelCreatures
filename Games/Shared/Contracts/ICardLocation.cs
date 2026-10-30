using ArC.CardGames.Components;
using Godot;

// A place on the board that shows cards. Implementing this is all a new zone needs
// for cards to animate in and out of it (see CardMotion.PresentArrival).
public interface ICardLocation
{
    // The card node showing `card` here right now, if any
    Card? FindCardNode(CardBase card);

    // Global transform of `card` here, or of where a card would sit when it isn't
    // (the next free slot, the top of a stack, ...)
    Transform2D GetCardTransform(CardBase? card);
}
