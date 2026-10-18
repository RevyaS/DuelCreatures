using System;
using System.Collections.Generic;
using System.Linq;
using DuelCreatures.Data;
using Godot;

[Tool]
public partial class CardLine : PanelContainer
{
    protected HBoxNodeContainer Container = null!;
    protected IChildManagerComponent ContainerNodeManager => Container;    
    protected DropArea DropArea = null!;
    ColorRect BG = null!;

    public int CardCount => Container.GetChildCount<CardContainer>();

    private ComponentInputState inputState = ComponentInputState.None;
    [Export]
    public bool Droppable
    {
        get => inputState == ComponentInputState.Droppable;
        set
        {
            SetState(ComponentInputState.Droppable, value);
            Render();
        }
    }

    private float _cardScale = SizeConstants.CardScaleFactor;
    [Export(PropertyHint.Range, "0.0, 1.0")]
    public float CardScale
    {
        get => _cardScale;
        set
        {
            _cardScale = value;
            Render();
        }
    }

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

    private bool _shrinks = false;
    [Export]
    public bool Shrinks
    {
        get => _shrinks;
        set
        {
            _shrinks = value;
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

    private BoxContainer.AlignmentMode _alignment = BoxContainer.AlignmentMode.Begin;
    [Export]
    public BoxContainer.AlignmentMode Alignment { 
        get => _alignment; 
        set
        {
            _alignment = value;
            Render();
        } 
    }

    private bool _showBackground = true;
    [Export]
    public bool ShowBackground  { 
        get => _showBackground; 
        set
        {
            _showBackground = value;
            Render();
        } 
    }

    private bool _flippedAppearance = false;
    // Rotates every card 180 degrees
    [Export]
    public bool FlippedAppearance
    {
        get => _flippedAppearance;
        set
        {
            _flippedAppearance = value;
            Render();
        }
    }

    private Predicate<Card>? _canDragCard = null;
    // Optional per-card veto on top of Draggable (e.g. intercepting units in the guard zone)
    public Predicate<Card>? CanDragCard
    {
        get => _canDragCard;
        set
        {
            _canDragCard = value;
            Render();
        }
    }

    private SleeveInfo _sleeveInfo = null!;
    [Export]
    public SleeveInfo SleeveInfo
    {
        get => _sleeveInfo;
        set
        {
            _sleeveInfo = value;
            Render();
        }
    }

    public override void _Ready()
    {
        Container = GetNode<HBoxNodeContainer>($"%{nameof(Container)}");
        Container.ChildEnteredTree += OnContainerChildEntered;
        Container.ChildExitingTree += OnContainerChildExiting;
        ContainerNodeManager.ApplyToChildren<CardContainer>(SubscribeContainer);

        DropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        DropArea.CardDropped += OnCardDropped;
        BG = GetNode<ColorRect>($"%{nameof(BG)}");

        OnComponentsSet();
        Render();
    }

    private void OnCardDropped(Card card)
    {
        CardDropped?.Invoke(card);
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        CardDragging?.Invoke(component);
    }

    // The line only talks to its containers; the containers (re)wire whichever
    // card they currently hold, so cards can move between slots freely
    private void OnContainerChildEntered(Node node)
    {
        if(node is CardContainer container)
        {
            SubscribeContainer(container);
            ConfigureContainer(container);
        }
    }

    private void OnContainerChildExiting(Node node)
    {
        if(node is CardContainer container)
        {
            container.CardAdded -= ApplyCardState;
            container.CardDragging -= OnCardDragging;
            container.CardDragCancelled -= OnCardDragCancelled;
            container.CardPressed -= OnCardPressed;
            container.CardLongPressed -= OnCardLongPressed;
        }
    }

    private void SubscribeContainer(CardContainer container)
    {
        container.CardAdded += ApplyCardState;
        container.CardDragging += OnCardDragging;
        container.CardDragCancelled += OnCardDragCancelled;
        container.CardPressed += OnCardPressed;
        container.CardLongPressed += OnCardLongPressed;
    }

    // Applies the line's properties to a slot; the slot passes them on to its card
    private void ConfigureContainer(CardContainer container)
    {
        container.CardScale = CardScale;
        container.Draggable = Draggable;
        container.FlippedAppearance = FlippedAppearance;
        container.SetSleeveInfo(SleeveInfo);
    }

    // Runs after the slot has applied its properties, both when a card arrives
    // and on every render
    private void ApplyCardState(Card card)
    {
        ConfigureCard(card);
        if(_canDragCard is not null && !_canDragCard(card))
        {
            card.Draggable = false;
        }
    }

    // Line-wide card state that isn't a slot property. Runs when a card arrives
    // and on every render; don't put one-off "on add" rules here
    protected virtual void ConfigureCard(Card card)
    {
    }

    protected virtual void OnComponentsSet()
    {
    }

    protected void Render()
    {
        if(!IsInsideTree() || !IsNodeReady()) return;
        RenderCore();
    }

    protected virtual void RenderCore()
    {
        Container.Alignment = Alignment;
        DropArea.Visible = Droppable;
        BG.Visible = ShowBackground;

        ContainerNodeManager.ApplyToChildren<CardContainer>(container =>
        {
            ConfigureContainer(container);
            if(container.CurrentCard is not null)
            {
                ApplyCardState(container.CurrentCard);
            }
        });

        // After the containers are updated, since separation can depend on their sizes
        Container.RemoveThemeConstantOverride("separation");
        Container.AddThemeConstantOverride("separation", CalculateSeparation());

        if(_shrinks)
        {
            ResetSize();
        }
    }

    protected virtual int CalculateSeparation()
    {
        return _separation;
    }

    private void SetState(ComponentInputState newState, bool value)
    {
        if(!value)
        {
            inputState = ComponentInputState.None;
        } else
        {
            inputState = newState;
        }
    }

    private void OnCardLongPressed(Card card)
    {
        CardLongPressed?.Invoke(card);
    }

    private void OnCardDragCancelled(CardBaseComponent component)
    {
        CardDragCancelled?.Invoke(component);
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
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

    public IEnumerable<Card> GetNonEmptyCards()
    {
        return Container.GetChildren<CardContainer>()
            .Where(cont => cont.HasCard)
            .Select(cont => cont.CurrentCard!);
    }

    public IEnumerable<Card> GetFaceUpCards()
    {
        return GetNonEmptyCards().Where(x => x.IsFront);
    }

    public event Action<CardBaseComponent>? CardDragging;
    public event Action<CardBaseComponent>? CardDragCancelled;
    public event Action<Card>? CardDropped;
    public event Action<Card>? CardPressed;
    public event Action<Card>? CardLongPressed;
}
