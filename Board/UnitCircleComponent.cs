using System;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

[Tool]
public partial class UnitCircleComponent : Control, ICardSpaceBindable<UnitCircle>, IEventBusUtilizer
{
    CardRotationContainer cardRotationContainer = null!;
    DropArea dropArea = null!;
    DragArea dragArea = null!;
    HoverArea hoverArea = null!;
    Button SelectButton = null!;
    TextureRect CircleTexture = null!, IconTexture = null!;
    Control TargetIndicators = null!;

    public UnitCircle UnitCircle { get; private set; } = null!;

    private bool isSelected = false;
    public bool IsSelected => isSelected;
    public Card? CurrentCard => cardRotationContainer.CurrentCard;
    public bool HasTargetIndicator => TargetIndicators.Visible;

    private ComponentInputState inputState = ComponentInputState.None;
    [Export]
    public bool Selectable
    {
        get => inputState == ComponentInputState.Selectable;
        set
        {
            SetState(ComponentInputState.Selectable, value);
            Render();
        }
    }

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

    private Texture2D _icon = null!; 
    [Export]
    public Texture2D Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            Render();
        }
    }

    private Texture2D _circleTexture = null!; 
    [Export]
    public Texture2D Circle
    {
        get => _circleTexture;
        set
        {
            _circleTexture = value;
            Render();
        }
    }

    private Color _circleColor = Colors.White; 
    [Export]
    public Color CircleColor
    {
        get => _circleColor;
        set
        {
            _circleColor = value;
            Render();
        }
    }

    private bool _flippedAppearance = false;
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

    public override void _Ready()
    {
        CircleTexture = GetNode<TextureRect>($"%{nameof(CircleTexture)}");
        IconTexture = GetNode<TextureRect>($"%{nameof(IconTexture)}");

        TargetIndicators = GetNode<Control>($"%{nameof(TargetIndicators)}");

        cardRotationContainer = GetNode<CardRotationContainer>($"%{nameof(CardRotationContainer)}");
        cardRotationContainer.CardDragging += OnCardDragging;
        cardRotationContainer.CardDragCancelled += OnCardDragCancelled;
        cardRotationContainer.CardPressed += OnCardPressed;
        cardRotationContainer.CardLongPressed += OnCardLongPressed;

        dropArea = GetNode<DropArea>($"%{nameof(DropArea)}");
        dropArea.CardDropped += OnCardDropped;

        dragArea = GetNode<DragArea>($"%{nameof(DragArea)}");
        dragArea.Dragging += OnDragging;
        dragArea.DragReleased += OnDragReleased;

        hoverArea = GetNode<HoverArea>($"%{nameof(HoverArea)}");
        hoverArea.Hovering += OnHovering;
        hoverArea.HoverReleased += OnHoverReleased;

        SelectButton = GetNode<Button>($"%{nameof(SelectButton)}");
        SelectButton.Pressed += OnSelectButtonPressed;

        Render();
    }

    private void OnSelectButtonPressed()
    {
        isSelected = !isSelected;
        RenderSelectButton();
        if(isSelected)
        {
            Selected?.Invoke(this);
        } else
        {
            Deselected?.Invoke(this);
        }
    }

    private void RenderSelectButton()
    {
        SelectButton.Text = IsSelected ? "Deselect" : "Select";
    }

    private void OnCardLongPressed(Card card)
    {
        LongPressed?.Invoke(this);
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
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
        eventBus.CardRemovedFromUnitCircle += OnCardRemovedFromUnitCircle;
        eventBus.UnitCircleOrientationChanged += OnUnitCircleOrientationChanged;
        eventBus.PowerEffectUpdatedToUnitCircle += OnPowerEffectUpdatedToUnitCircle;
        eventBus.CriticalEffectUpdatedToUnitCircle += OnCriticalEffectUpdatedToUnitCircle;
    }

    private void OnCriticalEffectUpdatedToUnitCircle(UnitCircle circle)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            UpdateStats();
        }
    }

    private Task OnCardRemovedFromUnitCircle(UnitCircle circle)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            if(circle.Card is null)
            {
                cardRotationContainer.RemoveCardAndFree();
            }
        }
        return Task.CompletedTask;
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

    private Task OnUnitCircleOrientationChanged(UnitCircle circle, Orientation orientation)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            cardRotationContainer.ChangeOrientation(orientation);
        }
        return Task.CompletedTask;
    }

    private Task OnCardAssignedToUnitCircle(UnitCircle circle)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            if(circle.Card is null)
            {
                cardRotationContainer.RemoveCardAndFree();
            } else
            {
                SetCard(circle.Card!);
            }
        }
        return Task.CompletedTask;
    }

    public void Bind(UnitCircle unitCircle)
    {
        UnitCircle = unitCircle;
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        dropArea.Visible = Droppable;
        cardRotationContainer.FlippedAppearance = FlippedAppearance;
        cardRotationContainer.Draggable = Draggable;
        cardRotationContainer.CardScale = CardScale;
        dragArea.Visible = ScreenDraggable;
        hoverArea.Visible = Hoverable;
        SelectButton.Visible = Selectable;
        IconTexture.Texture = Icon;
        CircleTexture.Modulate = CircleColor;
        CircleTexture.Texture = Circle;
        IconTexture.FlipV = FlippedAppearance;
        IconTexture.FlipH = FlippedAppearance;
    }

    protected virtual void OnCardDropped(Card card)
    {
        CardDropped?.Invoke(card);
    }

    public void ClearCard()
    {
        cardRotationContainer.RemoveCardAndFree();
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

    public void UpdatePower(int newPower)
    {
        cardRotationContainer.UpdatePower(newPower);
    }

    public void ResetSelection()
    {
        isSelected = false;
        RenderSelectButton();
    }

    public void ShowTargetIndicators()
    {
        TargetIndicators.Show();
    }

    public void HideTargetIndicators()
    {
        TargetIndicators.Hide();
    }

    public event Action<Card>? CardDropped;
    public event Action<UnitCircleComponent, CardBaseComponent>? RearguardCardDragging;
    public event Action<UnitCircleComponent, CardBaseComponent>? RearguardCardDragCancelled;
    public event Action<UnitCircleComponent>? ScreenDragging;
    public event Action<UnitCircleComponent>? ScreenDragRelease;
    public event Action<UnitCircleComponent>? Hovering;
    public event Action<UnitCircleComponent>? HoverReleased;
    public event Action<Card>? CardPressed;
    public event Action<UnitCircleComponent>? LongPressed;
    public event Action<UnitCircleComponent>? Selected;
    public event Action<UnitCircleComponent>? Deselected;
}
