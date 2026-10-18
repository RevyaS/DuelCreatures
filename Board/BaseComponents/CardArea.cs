using ArC.CardGames.Components;
using Godot;

// Holds a single upright card. Turn it with a QuarterTurnContainer
// when it needs to lie sideways (e.g. the trigger zone)
public partial class CardArea : PanelContainer, ICardLocation
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

    public Card? FindCardNode(CardBase card)
    {
        var shown = Container.CurrentCard;
        return shown is not null && ReferenceEquals(shown.CurrentCard, card) ? shown : null;
    }

    public Transform2D GetCardTransform(CardBase? card)
    {
        return Container.GetCardGlobalTransform();
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
