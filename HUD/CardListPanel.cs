using System;
using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardListPanel : PanelContainer
{
    DropArea DropArea = null!;
    Label Title = null!, Amount = null!;
    HFlowNodeContainer CardContainerList = null!;
    IChildManagerComponent CardContainerManager = null!;

    private bool _cardDraggable = false;
    public bool CardsDraggable { 
        get => _cardDraggable;
        set
        {
            _cardDraggable = value;
            Render();
        }
    }

    private bool _Droppable = false;
    public bool Droppable { 
        get => _Droppable;
        set
        {
            _Droppable = value;
            Render();
        }
    }

    public override void _Ready()
    {
        DropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        Title = GetNode<Label>($"%{nameof(Title)}");
        Amount = GetNode<Label>($"%{nameof(Amount)}");
        CardContainerList = GetNode<HFlowNodeContainer>($"%{nameof(CardContainerList)}");
        CardContainerManager = CardContainerList;

        DropArea.CardDropped += OnCardDropped;
    }

    private void OnCardDropped(Card card)
    {
        CardDropped?.Invoke(card);
    }

    private void Render()
    {
        CardContainerManager.ApplyToChildren<CardContainer>(container =>
        {
            container.Draggable = CardsDraggable;
        });
        DropArea.Visible = Droppable;
    }

    public void RemoveCard(Card card)
    {
        var targetContainer = CardContainerList.FirstChild<CardContainer>(x => ReferenceEquals(x.CurrentCard, card));
        targetContainer.RemoveCard();
        CardContainerList.RemoveChild(targetContainer);
    }

    public void Setup(string title, List<VanguardCard> cardsToShow)
    {
        Title.Text = title;
        Amount.Text = $"({cardsToShow.Count})";
        CardContainerList.ClearChildren();
        foreach(var card in cardsToShow)
        {
            AddCard(SceneFactory.CreateVanguardCard(card));
        }
    }

    public void AddCard(Card card)
    {
        CardContainer container = new();
        container.AddCard(card);
        container.CardPressed += OnCardPressed;
        container.CardDragging += OnCardDragging;
        CardContainerList.AddChild(container);
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        CardDragging?.Invoke(component);
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    public event Action<Card>? CardPressed;
    public event Action<Card>? CardDropped;
    public event Action<CardBaseComponent>? CardDragging;
}
