using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class UnitCircle : Control
{
    CardRotationContainer cardRotationContainer;

    public override void _Ready()
    {
        cardRotationContainer = GetNode<CardRotationContainer>($"%{nameof(CardRotationContainer)}");
    }

    public void ClearCard()
    {
        cardRotationContainer.RemoveCard();
    }

    public void FaceUp()
    {
        cardRotationContainer.FaceUp();
    }

    public void SetCard(VanguardCard card)
    {
        Card cardComponent = SceneFactory.CreateCard();
        cardComponent.Texture = GetCardNameTexture(card.Name);
        cardRotationContainer.AddCard(cardComponent);
        cardRotationContainer.FaceUp();
    }

    private Texture2D GetCardNameTexture(string cardName)
    {
        var cleanedName = cardName.Replace(" ", "").Replace(",", "");
        var path = $"res://Assets/Cards/{cleanedName}.png";
        return ResourceLoader.Load<Texture2D>(path);
    }
}
