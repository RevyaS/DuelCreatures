using System;
using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class CardList : Control
{
    CardListPanel CardListPanel = null!;
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

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    public void Show(string title, List<VanguardCard> cardsToShow)
    {
        CardListPanel.Setup(title, cardsToShow);
        Show();
    }

    public event Action<Card>? CardPressed;
    public event Action? CardDroppedOutside;
    public event Action? OnClosed;
}