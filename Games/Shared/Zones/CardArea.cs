using System;
using ArC.CardGames.Components;
using Godot;

// Holds a single upright card, sized by CardScale. Turn it with a QuarterTurnContainer
// when it needs to lie sideways (e.g. the trigger zone)
[Tool]
public partial class CardArea : PanelContainer, ICardLocation
{
    protected CardContainer Container { get; private set; } = null!;

    private float _cardScale = SizeConstants.DefaultCardScale;
    [Export(PropertyHint.Range, "0.05, 2.0, 0.01, or_greater")]
    public float CardScale
    {
        get => _cardScale;
        set
        {
            _cardScale = value;
            Render();
        }
    }

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
        Container.CardPressed += card => CardPressed?.Invoke(card);
        Render();
    }

    private void Render()
    {
        if (IsNodeReady())
        {
            Container.FlippedAppearance = FlippedAppearance;
            Container.CardScale = CardScale;
        }
        AppearanceChanged();
    }

    /// <summary>The card scale or flipped appearance changed (or the area got ready): for subclasses drawing more around the card.</summary>
    protected virtual void AppearanceChanged() {}

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

    public event Action<Card>? CardPressed;
}
