using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardVerticalStack : PanelContainer
{
    CardContainer cardContainer = null!;
    PressArea PressArea = null!;

    public override void _Ready()
    {
        cardContainer = GetNode<CardContainer>($"%{nameof(CardContainer)}");
        PressArea = GetNode<PressArea>($"%{nameof(PressArea)}");
        PressArea.Pressed += OnPressed;
    }

    protected virtual void OnPressed()
    {
    }

    public void ClearCard()
    {
        cardContainer.RemoveCardAndFree();
    }
    public void SetCard(VanguardCard card)
    {
        var cardComponent = SceneFactory.CreateVanguardCard(card);
        cardContainer.AddCard(cardComponent);
    }
}
