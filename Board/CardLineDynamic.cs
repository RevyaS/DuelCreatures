using System;
using Godot;

[Tool]
public partial class CardLineDynamic : PanelContainer
{
    HBoxNodeContainer Container;

    IChildManagerComponent ContainerNodeManager => Container;    

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

    private bool _hidden = false;
    [Export]
    public bool Hidden { 
        get => _hidden; 
        set
        {
            _hidden = value;
            Render();
        } 
    }

    public override void _Ready()
    {
        Container = GetNode<HBoxNodeContainer>($"%{nameof(Container)}");
        Render();
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        Container.RemoveThemeConstantOverride("separation");
        Container.AddThemeConstantOverride("separation", _separation);

        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            if(_hidden)
            {
                child.FaceDown();
            } else
            {
                child.FaceUp();
            }
        });
    }

    public void AddCard(Card card)
    {
        CardContainer cardContainer = new();
        Container.AddChild(cardContainer);
        cardContainer.AddChild(card);
        card.IsFront = !_hidden;
        card.CardPressed += OnCardPressed;
    }

    private void OnCardPressed(Card card)
    {
        CardPressed(card);
    }

    public void RemoveCard(Card card)
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            var containedCard = child.GetChild<Card>(0);
            if(ReferenceEquals(containedCard, card))
            {
                child.QueueFree();
            }
        });
    }

    public void ClearCards()
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            child.QueueFree();
        });
    }

    public event Action<Card> CardPressed;
}
