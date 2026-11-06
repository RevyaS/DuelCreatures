using Godot;

/// <summary>The card's name.</summary>
[Tool]
public partial class CardNamePart : Label, ICardDetailPart
{
    public void Show(CardDetail detail) => Text = detail.Card.Name;
}
