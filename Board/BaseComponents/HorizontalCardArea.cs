using Godot;

public partial class HorizontalCardArea : PanelContainer
{
    CardContainer Container = null!;

    public override void _Ready()
    {
        Container = GetNode<CardContainer>($"%{nameof(Container)}");
    }

    public void ClearCard()
    {
        Container.RemoveCardAndFree();
    }

    public void AddCard(Card card)
    {
        card.RotationDegrees = 90;
        Container.AddCard(card);
    }
}
