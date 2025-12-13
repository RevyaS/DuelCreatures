using System;
using System.Collections.Generic;
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

    public override void AddCard(Card card)
    {
        CardContainer cardContainer = new();
        Container.AddChild(cardContainer);
        cardContainer.AddChild(card);
        card.IsFront = !_hideCards;
        card.CardPressed += OnCardPressed;
        base.AddCard(card);
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    public void RemoveCard(Card card)
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            var containedCard = child.GetChild<Card>(0);
            if(ReferenceEquals(containedCard, card))
            {
                RemoveCardContainer(child);
            }
        });
    }

    public void RemoveCard(CardBase card)
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            var containedCard = child.GetChild<Card>(0);
            if(ReferenceEquals(containedCard.CurrentCard, card))
            {
                RemoveCardContainer(child);
            }
        });
    }

    private void RemoveCardContainer(CardContainer card)
    {
        var containedCard = card.GetChild<Card>(0);
        Container.RemoveChild(card);
        UnsubscribeCardEvents(containedCard);
        card.RemoveChild(containedCard);
        card.QueueFree();
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

    public event Action<Card>? CardPressed;
}
