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
        BG.Visible = ShowBackground;

        ContainerNodeManager.ApplyToChildren<CardContainer>(container =>
        {
            container.CardScale = CardScale;
            container.Draggable = Draggable;
            container.SetSleeveInfo(SleeveInfo);
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
        card.CardLongPressed += OnCardLongPressed;
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
