using ArC.CardGames.Components;

/// <summary>
/// Something printed on a card in a game's card scene (e.g. a YuGiOh monster's Level, its ATK): the card fills it in
/// when it's loaded, and it only shows while the card is face up. Place it anywhere under the card.
/// </summary>
public interface ICardFacePart
{
    /// <summary>Shows what <paramref name="card"/> has to show here (or hides, when it has nothing, e.g. ATK on a spell).</summary>
    void Show(CardBase card);

    /// <summary>The card turned face up or down: the part shows only face up.</summary>
    void FaceUp(bool faceUp);
}
