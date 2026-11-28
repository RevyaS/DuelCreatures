/// <summary>
/// Something printed on a card in a game's card scene (a <see cref="CardFaceText"/>, a <see cref="CardFacePanel"/>
/// around some): it shows only while the card is face up. Place it anywhere under the card; the card turns every part
/// it finds, and the game's card script fills in the texts.
/// </summary>
public interface ICardFacePart
{
    /// <summary>The card turned face up or down: the part shows only face up.</summary>
    void FaceUp(bool faceUp);
}
