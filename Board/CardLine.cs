using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class CardLine : PanelContainer
{
    protected HBoxNodeContainer Container = null!;
    protected IChildManagerComponent ContainerNodeManager => Container;    
    protected DropArea DropArea = null!;

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

    public override void _Ready()
    {
        Container = GetNode<HBoxNodeContainer>($"%{nameof(Container)}");
        DropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        DropArea.CardDropped += OnCardDropped;

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

    protected virtual void OnComponentsSet()
    {
    }

    protected void Render()
    {
        if(!IsInsideTree()) return;
        RenderCore();
    }

    protected virtual void RenderCore()
    {
        Container.RemoveThemeConstantOverride("separation");
        Container.AddThemeConstantOverride("separation", _separation);
        Container.Alignment = Alignment;
        DropArea.Visible = Droppable;

        ContainerNodeManager.ApplyToChildren<CardContainer>(container =>
        {
            container.CardScale = CardScale;
            container.Draggable = Draggable;
        });

        if(_shrinks)
        {
            ResetSize();
        }
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

    public virtual void AddCard(Card card)
    {
        card.CardDragging += OnCardDragging;
        card.CardDragCancelled += OnCardDragCancelled;
        card.CardPressed += OnCardPressed;
    }

    private void OnCardDragCancelled(CardBaseComponent component)
    {
        CardDragCancelled?.Invoke(component);
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    public void UnsubscribeCardEvents(Card card)
    {
        card.CardDragging -= OnCardDragging;
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

    public event Action<CardBaseComponent>? CardDragging;
    public event Action<CardBaseComponent>? CardDragCancelled;
    public event Action<Card>? CardDropped;
    public event Action<Card>? CardPressed;
}
