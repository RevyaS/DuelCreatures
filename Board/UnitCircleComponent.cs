using System;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

[Tool]
public partial class UnitCircleComponent : Control, ICardSpaceBindable<UnitCircle>, IEventBusUtilizer, ICardLocation
{
    CardRotationContainer cardRotationContainer = null!;
    // Plain Controls: in the editor they can still be script placeholders, so only the game casts them
    Control dropArea = null!;
    // The circle's only input: tap (card info), long press, and drags (attack aim or moving the card)
    GestureDetector gestures = null!;
    Button SelectButton = null!;
    QuarterTurnContainer ButtonContainer = null!;
    TextureRect CircleTexture = null!, IconTexture = null!;
    Control TargetIndicators = null!;

    public UnitCircle UnitCircle { get; private set; } = null!;
    PresentationContext? presentation = null;

    // Where units called straight from the deck come from (superior call)
    public ICardLocation? DeckLocation { get; set; }

    public Card? FindCardNode(CardBase card)
    {
        var shown = cardRotationContainer.CurrentCard;
        return shown is not null && ReferenceEquals(shown.CurrentCard, card) ? shown : null;
    }

    public Transform2D GetCardTransform(CardBase? card)
    {
        return cardRotationContainer.GetCardGlobalTransform();
    }

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

        dropArea = GetNode<Control>($"%{nameof(DropArea)}");
        if(!Engine.IsEditorHint())
        {
            var drop = (DropArea)dropArea;
            drop.CardDropped += OnCardDropped;
            drop.DragHoverChanged += OnDropAreaDragHoverChanged;

            gestures = new GestureDetector();
            gestures.Tapped += OnTapped;
            gestures.LongPressed += () => LongPressed?.Invoke(this);
            gestures.DragStarted += OnGestureDragStarted;
            gestures.DragMoved += OnGestureDragMoved;
            gestures.DragEnded += OnGestureDragEnded;
            gestures.DragCancelled += OnGestureDragCancelled;
            AddChild(gestures);

            // Cards here only show: the circle takes every press, so nothing on top can block it
            cardRotationContainer.CardAdded += IgnoreMouse;
            if(cardRotationContainer.CurrentCard is not null) IgnoreMouse(cardRotationContainer.CurrentCard);
        }

        SelectButton = GetNode<Button>($"%{nameof(SelectButton)}");
        ButtonContainer = GetNode<QuarterTurnContainer>($"%{nameof(ButtonContainer)}");
        SelectButton.Pressed += OnSelectButtonPressed;

        Render();
    }

    bool showingDropIndicator = false;

    // Point at the circle a dragged card would be placed on
    private void OnDropAreaDragHoverChanged(bool hovered)
    {
        if(hovered && Droppable)
        {
            showingDropIndicator = true;
            ShowTargetIndicators();
        }
        else if(!hovered && showingDropIndicator)
        {
            showingDropIndicator = false;
            HideTargetIndicators();
        }
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

    private void OnTapped()
    {
        if(cardRotationContainer.CurrentCard is { } card)
        {
            CardPressed?.Invoke(card);
        }
    }

    bool screenDragging = false;
    bool movingCard = false;

    private void OnGestureDragStarted(Vector2 position)
    {
        if(ScreenDraggable)
        {
            // Aiming (e.g. an attack): the strategy finds targets under the pointer
            screenDragging = true;
            ScreenDragging?.Invoke(this);
            return;
        }

        // Moving the unit (e.g. a rearguard swap)
        if(Draggable && cardRotationContainer.CurrentCard is { Draggable: true } card)
        {
            movingCard = true;
            CardDrag.Begin(card, position);
            return;
        }
        gestures.Cancel();
    }

    private void OnGestureDragMoved(Vector2 position)
    {
        if(screenDragging) ScreenDragMoved?.Invoke(this, position);
        else if(movingCard) CardDrag.Move(position);
    }

    private void OnGestureDragEnded(Vector2 position)
    {
        if(movingCard)
        {
            movingCard = false;
            CardDrag.End(position);
        }
        FinishScreenDrag();
    }

    private void OnGestureDragCancelled()
    {
        if(movingCard)
        {
            movingCard = false;
            CardDrag.Cancel();
        }
        FinishScreenDrag();
    }

    private void FinishScreenDrag()
    {
        if(!screenDragging) return;
        screenDragging = false;
        ScreenDragRelease?.Invoke(this);
    }

    private static void IgnoreMouse(Node node)
    {
        if(node is Control control) control.MouseFilter = MouseFilterEnum.Ignore;
        foreach(var child in node.GetChildren())
        {
            IgnoreMouse(child);
        }
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

    private void OnCardDragging(CardBaseComponent component)
    {
        RearguardCardDragging?.Invoke(this, component);
    }

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
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
                presentation.Present(RemoveShownCard);
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
        if(UnitCircle is null || UnitCircle.IsEmpty) return;

        var power = UnitCircle.GetOverallPower();
        var critical = UnitCircle.GetOverallCritical();
        presentation.Present(() => ShowStats(power, critical));
    }

    private async Task ShowStats(int power, int critical)
    {
        if(cardRotationContainer.CurrentCard is not VanguardCardComponent card || card.CurrentCard is null) return;

        var oldPower = card.Power;
        var oldCritical = card.Critical;
        if(power > oldPower) presentation.PlaySfx(SfxCue.PowerUp);
        if(critical > oldCritical) presentation.PlaySfx(SfxCue.CriticalUp);

        var flights = presentation?.Presenter.Flights;
        var changed = oldPower != power || oldCritical != critical;
        if(flights is not null && presentation is not null && changed && Presenter.Animates(presentation.Side))
        {
            var panel = card.StatsPanel;
            panel.PivotOffset = panel.Size / 2;
            await Task.WhenAll(
                flights.PlayKeyframes(panel, PresentationConfig.StatChangePulse),
                flights.Animate(PresentationConfig.StatChange, weight =>
                {
                    if(!IsInstanceValid(card)) return;
                    card.Power = Mathf.RoundToInt(Mathf.Lerp(oldPower, power, weight));
                    card.Critical = Mathf.RoundToInt(Mathf.Lerp(oldCritical, critical, weight));
                }));
        }

        if(IsInstanceValid(card))
        {
            card.Power = power;
            card.Critical = critical;
        }
    }

    private Task OnUnitCircleOrientationChanged(UnitCircle circle, Orientation orientation)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            presentation.Present(() => cardRotationContainer.ChangeOrientation(orientation));
        }
        return Task.CompletedTask;
    }

    private Task OnCardAssignedToUnitCircle(UnitCircle circle)
    {
        if(ReferenceEquals(circle, UnitCircle))
        {
            var card = circle.Card;
            var orientation = circle.Orientation;
            if(card is null)
            {
                presentation.Present(RemoveShownCard);
                return Task.CompletedTask;
            }

            presentation.PresentArrival(card, CardMove.ToUnitCircle, this, DeckLocation, () =>
            {
                // The unit being replaced (ride, overcall) leaves from here
                var replaced = cardRotationContainer.CurrentCard?.CurrentCard;
                if(replaced is not null && !ReferenceEquals(replaced, card))
                {
                    presentation.Leaving(replaced, this);
                }

                SetCard(card, orientation);
                presentation.PlaySfx(circle is RearGuard ? SfxCue.Call : SfxCue.Ride);
            });
        }
        return Task.CompletedTask;
    }

    private void RemoveShownCard()
    {
        var shown = cardRotationContainer.CurrentCard?.CurrentCard;
        if(shown is not null)
        {
            presentation.Leaving(shown, this);
        }
        cardRotationContainer.RemoveCardAndFree();
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
        if(gestures is not null)
        {
            gestures.Drags = ScreenDraggable || Draggable;
        }
        SelectButton.Visible = Selectable;
        IconTexture.Texture = Icon;
        CircleTexture.Modulate = CircleColor;
        CircleTexture.Texture = Circle;
        IconTexture.FlipV = FlippedAppearance;
        IconTexture.FlipH = FlippedAppearance;
        // Keep the button readable on the flipped (opponent's) side
        ButtonContainer.Turns = FlippedAppearance ? QuarterTurnContainer.QuarterTurn.Deg180 : QuarterTurnContainer.QuarterTurn.Deg0;
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

    public void SetCard(VanguardCard card, Orientation? orientation = null)
    {
        Card cardComponent = SceneFactory.CreateVanguardCard(card);
        cardRotationContainer.AddCard(cardComponent);
        cardRotationContainer.FaceUp();

        // The circle (not the card) is what's standing or resting, and it keeps that
        // across card changes, e.g. an intercepting unit returning from the guard zone
        var circleOrientation = orientation ?? UnitCircle?.Orientation;
        if(circleOrientation is not null)
        {
            cardRotationContainer.ChangeOrientation(circleOrientation.Value);
        }
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
    // Global pointer position while aiming from this circle
    public event Action<UnitCircleComponent, Vector2>? ScreenDragMoved;
    public event Action<Card>? CardPressed;
    public event Action<UnitCircleComponent>? LongPressed;
    public event Action<UnitCircleComponent>? Selected;
    public event Action<UnitCircleComponent>? Deselected;
}
