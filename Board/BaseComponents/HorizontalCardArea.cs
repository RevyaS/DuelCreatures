using Godot;

public partial class HorizontalCardArea : PanelContainer
{
    CardContainer Container = null!;

    private bool _flippedAppearance = false;
    [Export]
    public bool FlippedAppearance
    {
        get => _flippedAppearance;
        set
        {
            _flippedAppearance = value;
            Render();
        }
    }

    public override void _Ready()
    {
        Container = GetNode<CardContainer>($"%{nameof(Container)}");
        Render();
    }

    private void Render()
    {
        if (IsNodeReady())
        {
            Container.FlippedAppearance = FlippedAppearance;
        }
    }

    public void ClearCard()
    {
        Container.RemoveCardAndFree();
    }

    public void AddCard(Card card)
    {
        Container.AddCard(card);
    }
}
