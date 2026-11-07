using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// A panel on a card's face around some face texts (e.g. the ATK/DEF bar): shown only while the card is face up and
/// one of the texts in it has a value, so a spell shows no empty bar.
/// </summary>
[Tool]
public partial class CardFacePanel : PanelContainer, ICardFacePart
{
    List<CardFaceText>? texts;

    public void Show(CardBase card) {}

    // The card shows every part first, then turns them: the texts know whether they have a value by now
    public void FaceUp(bool faceUp)
    {
        if(Engine.IsEditorHint()) return;
        Visible = faceUp && Texts.Any(text => text.HasValue);
    }

    private List<CardFaceText> Texts => texts ??= this.Descendants<CardFaceText>().ToList();
}
