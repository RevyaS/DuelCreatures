using System;
using System.Collections.Generic;
using System.Linq;
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

    private bool _baseDroppable = false;
    /// <summary>
    /// A card dragged out of the list and let go outside it is <see cref="CardDroppedOutside"/>; let go back over the
    /// list's panel, it returns to the list (a drag out started by accident can be undone).
    /// </summary>
    public bool BaseDroppable
    {
        get => _baseDroppable;
        set
        {
            _baseDroppable = value;
            if(IsNodeReady()) CardListPanel.Droppable = value;
        }
    }

    public override void _Ready()
    {
        CardListPanel = GetNode<CardListPanel>($"%{nameof(CardListPanel)}");
        CardListPanel.CardPressed += OnCardPressed;
        CardListPanel.CardDragging += OnCardDragging;
        CardListPanel.CardDropped += OnCardReturned;

        VisibilityChanged += OnVisibilityChanged;
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        draggedCard = component;
    }

    // Let go back over the panel (its drop area wins over the list's, being later in the tree): nothing is picked
    private void OnCardReturned(Card card)
    {
        draggedCard = null;
        card.CurrentlyDragged = false;
        card.NotifyDragCancelled();
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
    // else counts; over the panel, the panel's own drop area takes it back (OnCardReturned)
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

    readonly List<Control> letThrough = [];

    /// <summary>
    /// Clicks on <paramref name="control"/> reach it while the list is open (e.g. the card details, to read and use
    /// them), and don't count as a click outside the list (which closes it).
    /// </summary>
    public void LetThrough(Control control)
    {
        if(!letThrough.Contains(control)) letThrough.Add(control);
    }

    // The list covers the screen to catch clicks outside it, except over the controls it lets through
    public override bool _HasPoint(Vector2 point)
    {
        var global = GetGlobalTransform() * point;
        if(letThrough.Any(control => IsInstanceValid(control) && UnderPointer.Contains(control, global))) return false;
        return new Rect2(Vector2.Zero, Size).HasPoint(point);
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

    /// <summary>The card nodes shown, in the order they were given to <see cref="Show"/>.</summary>
    public IReadOnlyList<Card> ShownCards => CardListPanel.ShownCards;

    public event Action<Card>? CardPressed;
    public event Action<Card>? CardDroppedOutside;
    public event Action? OnClosed;
}