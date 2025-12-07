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
        Container.RemoveCard();
    }

    public void AddCard(Card card)
    {
        Container.AddCard(card);
    }
}
