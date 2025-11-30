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
        Card cardComponent = SceneFactory.CreateVanguardCard(card);
        cardRotationContainer.AddCard(cardComponent);
        cardRotationContainer.FaceUp();
    }
}
