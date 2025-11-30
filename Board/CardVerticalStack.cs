using Godot;

public partial class CardVerticalStack : PanelContainer
{
    CardContainer cardContainer;

    public override void _Ready()
    {
        cardContainer = GetNode<CardContainer>($"%{nameof(CardContainer)}");
    }

    public void ClearCard()
    {
        cardContainer.RemoveCard();
    }
}
