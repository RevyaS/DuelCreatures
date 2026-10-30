using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

[Tool]
public partial class CardLineDynamic : CardLine
{
    private bool _hideCards = false;
    [Export]
    public bool HideCards { 
        get => _hideCards; 
        set
        {
            _hideCards = value;
            Render();
        } 
    }

    public override void _Notification(int what)
    {
        if(what == NotificationResized)
        {
            Render();
        }
    }

    // Overlaps cards when they don't fit, so the first card sits at the left edge
    // and the last card at the right edge of this line's width
    protected override int CalculateSeparation()
    {
        var baseSeparation = base.CalculateSeparation();

        var containers = Container.GetChildren<CardContainer>().ToList();
        if(containers.Count < 2) return baseSeparation;

        var availableWidth = GetAvailableCardWidth();
        if(availableWidth <= 0) return baseSeparation;

        var cardWidths = containers.Select(container => container.GetCombinedMinimumSize().X).ToList();
        var totalCardWidth = cardWidths.Sum();
        var gaps = containers.Count - 1;

        if(totalCardWidth + baseSeparation * gaps <= availableWidth) return baseSeparation;

        // Spread the overlap evenly across every gap
        var separation = Mathf.FloorToInt((availableWidth - totalCardWidth) / gaps);

        // Keep at least a sliver of each card visible
        var minSeparation = 1 - Mathf.FloorToInt(cardWidths.Min());
        return Mathf.Max(separation, minSeparation);
    }

    private float GetAvailableCardWidth()
    {
        // Panel and margin padding around the card row
        var padding = GetCombinedMinimumSize().X - Container.GetCombinedMinimumSize().X;
        return Size.X - padding;
    }

    protected override void ConfigureCard(Card card)
    {
        card.IsFront = !_hideCards;
    }

    public void AddCard(Card card)
    {
        CardContainer cardContainer = new();
        Container.AddChild(cardContainer);
        cardContainer.AddCard(card);
        Render();
    }


    public void RemoveCard(Card card)
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            if(ReferenceEquals(child.CurrentCard, card))
            {
                RemoveCardContainer(child);
            }
        });

        Render();
    }

    public void RemoveCard(CardBase card)
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            if(ReferenceEquals(child.CurrentCard?.CurrentCard, card))
            {
                RemoveCardContainer(child);
            }
        });

        Render();
    }

    // Detaches the card (still alive, so it can be moved elsewhere) and frees its slot
    private void RemoveCardContainer(CardContainer container)
    {
        container.RemoveCard();
        Container.RemoveChild(container);
        container.QueueFree();
    }

    public void ClearCards()
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            Container.RemoveChild(child);
            child.QueueFree();
        });
        
        Render();
    }

    public bool HasCardContainer(Func<Card?, bool> predicate)
    {
        return Container.GetChildren<CardContainer>()
            .Select(cont => cont.CurrentCard)
            .Any(predicate);
    }

    public bool HasCard(Func<CardContainer, bool> predicate)
    {
        return ContainerNodeManager.HasChild(predicate);
    }
}
