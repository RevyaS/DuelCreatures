using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

[Tool]
public partial class CardLineDynamic : CardLine
{
    private int _separation = 0;
    [Export]
    public int Separation { 
        get => _separation; 
        set
        {
            _separation = value;
            Render();
        } 
    }

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

    private bool _draggable = false;
    [Export]
    public bool Draggable { 
        get => _draggable; 
        set
        {
            _draggable = value;
            Render();
        } 
    }

    protected override void RenderCore()
    {
        Container.RemoveThemeConstantOverride("separation");
        Container.AddThemeConstantOverride("separation", _separation);

        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            if(_hideCards)
            {
                child.FaceDown();
            } else
            {
                child.FaceUp();
            }

            child.Draggable = Draggable;
        });

        base.RenderCore();
    }

    public void AddCard(Card card)
    {
        CardContainer cardContainer = new();
        Container.AddChild(cardContainer);
        cardContainer.AddChild(card);
        card.IsFront = !_hideCards;
        card.CardPressed += OnCardPressed;
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

    public Card? FindCard(Func<Card, bool> predicate)
    {
        return GetCards().FirstOrDefault(predicate);
    }

    public IEnumerable<Card> GetCards()
    {
        return Container.GetChildren<CardContainer>()
            .Select(cont => cont.CurrentCard!);
    }

    public bool HasCard(Func<CardContainer, bool> predicate)
    {
        return ContainerNodeManager.HasChild(predicate);
    }

    public event Action<Card>? CardPressed;
}
