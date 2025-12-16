using System;
using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardListPanel : PanelContainer
{
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

    public override void _Ready()
    {
        Title = GetNode<Label>($"%{nameof(Title)}");
        Amount = GetNode<Label>($"%{nameof(Amount)}");
        CardContainerList = GetNode<HFlowNodeContainer>($"%{nameof(CardContainerList)}");
        CardContainerManager = CardContainerList;
    }


    private void Render()
    {
        CardContainerManager.ApplyToChildren<CardContainer>(container =>
        {
            container.Draggable = CardsDraggable;
        });
    }

    public void Setup(string title, List<VanguardCard> cardsToShow)
    {
        Title.Text = title;
        Amount.Text = $"({cardsToShow.Count})";
        CardContainerList.ClearChildren();
        foreach(var card in cardsToShow)
        {
            CardContainer container = new();
            container.AddCard(SceneFactory.CreateVanguardCard(card));
            container.CardPressed += OnCardPressed;
            container.CardDragging += OnCardDragging;
            CardContainerList.AddChild(container);
        }
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
    public event Action<CardBaseComponent>? CardDragging;
}
