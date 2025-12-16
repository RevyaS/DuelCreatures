using System;
using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardList : Control
{
    Label Title = null!, Amount = null!;
    HFlowNodeContainer CardContainerList = null!;
    IChildManagerComponent CardContainerManager = null!;
    CardBaseComponent? draggedCard = null;

    private bool _cardDraggable = false;
    public bool CardsDraggable { 
        get => _cardDraggable;
        set
        {
            _cardDraggable = value;
            Render();
        }
    }

    public bool BaseDroppable { get; set; }

    public override void _Ready()
    {
        Title = GetNode<Label>($"%{nameof(Title)}");
        Amount = GetNode<Label>($"%{nameof(Amount)}");
        CardContainerList = GetNode<HFlowNodeContainer>($"%{nameof(CardContainerList)}");
        CardContainerManager = CardContainerList;
        VisibilityChanged += OnVisibilityChanged;
    }

    private void OnVisibilityChanged()
    {
        if(!Visible)
        {
            OnClosed?.Invoke();
        }
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        draggedCard = component;
    }

    private void Render()
    {
        CardContainerManager.ApplyToChildren<CardContainer>(container =>
        {
            container.Draggable = CardsDraggable;
        });
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return BaseDroppable && data.Obj is CardBaseComponent component && ReferenceEquals(draggedCard, component);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        draggedCard = null;
        CardDroppedOutside?.Invoke();
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            Hide();
        }
    }

    public void Show(string title, List<VanguardCard> cardsToShow)
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

        Show();
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    public event Action<Card>? CardPressed;
    public event Action? CardDroppedOutside;
    public event Action? OnClosed;
}