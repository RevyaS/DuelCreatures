using Godot;

public partial class CardVerticalStack : PanelContainer
{
    CardContainer cardContainer = null!;

    public override void _Ready()
    {
        cardContainer = GetNode<CardContainer>($"%{nameof(CardContainer)}");
    }

    public void ClearCard()
    {
        cardContainer.RemoveCardAndFree();
    }
}
