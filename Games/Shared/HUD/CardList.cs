using System;
using System.Collections.Generic;
using ArC.CardGames.Components;
using Godot;

public partial class CardList : Control, ICardDropTarget
{
    CardListPanel CardListPanel = null!;
    CardBaseComponent? draggedCard = null;
    
    public bool CanClose { get; set; } = true;

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
        CardListPanel = GetNode<CardListPanel>($"%{nameof(CardListPanel)}");
        CardListPanel.CardPressed += OnCardPressed;
        CardListPanel.CardDragging += OnCardDragging;

        VisibilityChanged += OnVisibilityChanged;
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        draggedCard = component;
    }

    private void OnVisibilityChanged()
    {
        if(!Visible)
        {
            OnClosed?.Invoke();
        }
    }

    private void Render()
    {
        CardListPanel.CardsDraggable = CardsDraggable;
    }

    public override void _EnterTree()
    {
        CardDrag.Register(this);
    }

    public override void _ExitTree()
    {
        CardDrag.Unregister(this);
    }

    // The whole screen behind the list: a card dragged out of the list and let go anywhere
    // else counts (the list's own drop area wins over its panel, being later in the tree)
    public Control DropRegion => this;

    public bool AcceptsCard(Card card) => BaseDroppable && IsVisibleInTree() && ReferenceEquals(draggedCard, card);

    public void DropCard(Card card)
    {
        draggedCard = null;
        CardDroppedOutside?.Invoke(card);
    }

    public void SetCardHovering(bool hovering)
    {
    }

    public override void _GuiInput(InputEvent e)
    {
        // Mouse click or touch
        if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
        {
            if(CanClose)
            {
                Hide();
            }
        }
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    public void Show(string title, IEnumerable<CardBase> cardsToShow)
    {
        CardListPanel.Setup(title, cardsToShow);
        Show();
    }

    public event Action<Card>? CardPressed;
    public event Action<Card>? CardDroppedOutside;
    public event Action? OnClosed;
}