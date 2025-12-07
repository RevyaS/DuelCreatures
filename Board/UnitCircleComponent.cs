using System;
using System.Data;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

[Tool]
public partial class UnitCircleComponent : Control, IEventBusUtilizer
{
    CardRotationContainer cardRotationContainer = null!;
    DropArea dropArea = null!;
    DragArea dragArea = null!;
    HoverArea hoverArea = null!;

    public UnitCircle UnitCircle { get; private set; } = null!;

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

    [Export]
    public bool Draggable
    {
        get => inputState == ComponentInputState.Draggable;
        set
        {
            SetState(ComponentInputState.Draggable, value);
            Render();
        }
    }

    [Export]
    public bool ScreenDraggable
    {
        get => inputState == ComponentInputState.ScreenDraggable;
        set
        {
            SetState(ComponentInputState.ScreenDraggable, value);
            Render();
        }
    }

    [Export]
    public bool Hoverable
    {
        get => inputState == ComponentInputState.Hoverable;
        set
        {
            SetState(ComponentInputState.Hoverable, value);
            Render();
        }
    }

    public override void _Ready()
    {
        cardRotationContainer = GetNode<CardRotationContainer>($"%{nameof(CardRotationContainer)}");
        cardRotationContainer.CardDragging += OnCardDragging;
        cardRotationContainer.CardDragCancelled += OnCardDragCancelled;

        dropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        dropArea.CardDropped += OnCardDropped;

        dragArea = GetNode<DragArea>($"%{nameof(DragArea)}");
        dragArea.Dragging += OnDragging;
        dragArea.DragReleased += OnDragReleased;

        hoverArea = GetNode<HoverArea>($"%{nameof(HoverArea)}");
        hoverArea.Hovering += OnHovering;
        hoverArea.HoverReleased += OnHoverReleased;

        Render();
    }

    private void OnHoverReleased()
    {
        HoverReleased?.Invoke(this);
    }

    private void OnDragReleased()
    {
        ScreenDragRelease?.Invoke(this);
    }

    private void OnHovering()
    {
        Hovering?.Invoke(this);
    }

    private void OnCardDragCancelled(CardBaseComponent component)
    {
        RearguardCardDragCancelled?.Invoke(this, component);
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

    private void OnDragging()
    {
        ScreenDragging?.Invoke(this);
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        RearguardCardDragging?.Invoke(this, component);
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.CardAssignedToUnitCircle += OnCardAssignedToUnitCircle;
        eventBus.UnitCircleOrientationChanged += OnUnitCircleOrientationChanged;
        eventBus.PowerEffectUpdatedToUnitCircle += OnPowerEffectUpdatedToUnitCircle;
    }

    private void OnPowerEffectUpdatedToUnitCircle(UnitCircle unitCircle)
    {
        if(ReferenceEquals(unitCircle, UnitCircle))
        {
            UpdateStats();
        }
    }

    public void UpdateStats()
    {
        if(cardRotationContainer.HasCard && !UnitCircle.IsEmpty)
        {
            cardRotationContainer.UpdatePower(UnitCircle.GetOverallPower());
            cardRotationContainer.UpdateCrit(UnitCircle.GetOverallCritical());
        }
    }

    private async Task OnUnitCircleOrientationChanged(UnitCircle circle, Orientation orientation)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            cardRotationContainer.ChangeOrientation(orientation);
        }
    }

    private Task OnCardAssignedToUnitCircle(UnitCircle circle)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            if(circle.Card is null)
            {
                cardRotationContainer.RemoveCard();
            } else
            {
                SetCard(circle.Card!);
            }
        }
        return Task.CompletedTask;
    }

    public void BindUnitCircle(UnitCircle unitCircle)
    {
        UnitCircle = unitCircle;
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        dropArea.Visible = Droppable;
        cardRotationContainer.Draggable = Draggable;
        dragArea.Visible = ScreenDraggable;
        hoverArea.Visible = Hoverable;
    }

    protected virtual void OnCardDropped(Card card)
    {
        CardDropped?.Invoke(card);
    }

    public void ClearCard()
    {
        cardRotationContainer.RemoveCard();
    }

    public void FaceUp()
    {
        cardRotationContainer.FaceUp();
    }

    public void SetCard(VanguardCard card)
    {
        Card cardComponent = SceneFactory.CreateVanguardCard(card);
        cardRotationContainer.AddCard(cardComponent);
        cardRotationContainer.FaceUp();
    }
    public event Action<Card>? CardDropped;
    public event Action<UnitCircleComponent, CardBaseComponent>? RearguardCardDragging;
    public event Action<UnitCircleComponent, CardBaseComponent>? RearguardCardDragCancelled;
    public event Action<UnitCircleComponent>? ScreenDragging;
    public event Action<UnitCircleComponent>? ScreenDragRelease;
    public event Action<UnitCircleComponent>? Hovering;
    public event Action<UnitCircleComponent>? HoverReleased;
}
