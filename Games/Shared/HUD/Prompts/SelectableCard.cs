using System;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// One card offered on an input screen, enlarged: the game's card scene (CardScenes.Create) in %CardSlot at
/// <see cref="CardScale"/> (a ratio of the card component's own size, SizeConstants.CardDefaultSize, as every card
/// holder sizes its cards; the slot is that big and the layout around it follows), with its pick number (%Badge, a SelectionBadge), where it is or what it's for (%Tag, a
/// CardTagView), a state drawn over it (%Marker, a CardMarker's scene) and the button picking it (%SelectButton, any
/// BaseButton in toggle mode). A tap on the card is <see cref="Pressed"/> (its details); the button is
/// <see cref="SelectPressed"/>. The card shows what it's told: whoever owns the selection sets <see cref="PickNumber"/>
/// and <see cref="CanSelect"/>, the button never picks by itself. Laid out in SelectableCard.tscn.
/// </summary>
[Tool]
public partial class SelectableCard : Control
{
    float cardScale = 1;
    /// <summary>The card's size, as a ratio of the card component's own size (SelectionGrid sets it for its cards).</summary>
    [Export(PropertyHint.Range, "0.05, 2.0, 0.01, or_greater")]
    public float CardScale
    {
        get => cardScale;
        set
        {
            cardScale = value;
            if(IsNodeReady()) FitCard();
        }
    }

    Control? cardSlot, markerSlot;
    BaseButton? selectButton;
    SelectionBadge? badge;
    CardTagView? tagView;
    Control CardSlot => cardSlot ??= GetNode<Control>("%CardSlot");
    Control MarkerSlot => markerSlot ??= GetNode<Control>("%Marker");
    BaseButton SelectButton => selectButton ??= GetNode<BaseButton>("%SelectButton");
    SelectionBadge Badge => badge ??= GetNode<SelectionBadge>("%Badge");
    CardTagView TagView => tagView ??= GetNode<CardTagView>("%Tag");

    Card? cardNode;
    CardBase? card;
    int? pickNumber;
    CardMarker? marker;

    /// <summary>The select button (picks or drops the card).</summary>
    public event Action<SelectableCard>? SelectPressed;
    /// <summary>A tap on the card: show its details.</summary>
    public event Action<SelectableCard>? Pressed;

    public override void _Ready()
    {
        // Fields reset after a C# rebuild in the editor: look the nodes up again
        cardSlot = null;
        FitCard();
        if(Engine.IsEditorHint()) return;
        var gestures = GetNode<GestureDetector>("%Gestures");
        gestures.Tapped += () => Pressed?.Invoke(this);
        // A drag carries the card (CardDrag) to whatever takes it, e.g. a circle on the enlarged field
        gestures.DragStarted += position =>
        {
            if(draggable && cardNode is not null) CardDrag.Begin(cardNode, position);
        };
        gestures.DragMoved += CardDrag.Move;
        gestures.DragEnded += CardDrag.End;
        gestures.DragCancelled += CardDrag.Cancel;
        SelectButton.ToggleMode = true;
        SelectButton.Pressed += OnSelectButtonPressed;
        Badge.Number = pickNumber;
        Draggable = draggable;
    }

    bool draggable;
    /// <summary>The card can be dragged out (onto drop targets); off, a drag scrolls the list it's in.</summary>
    public bool Draggable
    {
        get => draggable;
        set
        {
            draggable = value;
            if(!IsNodeReady() || Engine.IsEditorHint()) return;
            var gestures = GetNode<GestureDetector>("%Gestures");
            gestures.Drags = value;
            gestures.ScrollsParent = !value;
        }
    }

    /// <summary>The card shown.</summary>
    public CardBase Card => card ?? throw new InvalidOperationException($"{GetPath()} shows no card yet (Show it one)");

    /// <summary>Shows <paramref name="shown"/>, face up (the player is offered it, so they may read it), in its game's card scene.</summary>
    public void Show(CardBase shown)
    {
        cardNode?.QueueFree();
        card = shown;
        cardNode = CardScenes.Create(shown);
        cardNode.IsFront = true;
        CardSlot.AddChild(cardNode);
        // Under the slot's marker, tag and badge
        CardSlot.MoveChild(cardNode, 0);
        // This view takes every press; the card only shows
        cardNode.IgnoreMouse();
        FitCard();
    }

    /// <summary>Picked as number N (the badge shows it, the button is down), or not picked (null: no badge, button up).</summary>
    public int? PickNumber
    {
        get => pickNumber;
        set
        {
            pickNumber = value;
            Badge.Number = value;
            SelectButton.SetPressedNoSignal(value is not null);
        }
    }

    /// <summary>The select button can be pressed (false: shown disabled, e.g. the maximum is picked).</summary>
    public bool CanSelect
    {
        get => !SelectButton.Disabled;
        set => SelectButton.Disabled = !value;
    }

    /// <summary>Shows the select button at all (false where the card is only to read).</summary>
    public bool Selectable
    {
        get => SelectButton.Visible;
        set => SelectButton.Visible = value;
    }

    /// <summary>Where the card is or what it's for (icon and/or text), or none.</summary>
    public CardTag? Tag
    {
        get => TagView.Tag;
        set => TagView.Tag = value;
    }

    /// <summary>A state drawn over the card (its scene instanced into %Marker), or none.</summary>
    public CardMarker? Marker
    {
        get => marker;
        set
        {
            if(ReferenceEquals(marker, value)) return;
            marker = value;
            foreach(var shown in MarkerSlot.GetChildren()) shown.QueueFree();
            if(value is null) return;
            var scene = value.Scene ?? throw new InvalidOperationException($"The card marker {value.ResourcePath} has no {nameof(CardMarker.Scene)}");
            var node = scene.Instantiate();
            MarkerSlot.AddChild(node);
            if(node is Control control) control.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            node.IgnoreMouse();
        }
    }

    // The button only reports the press: the selection decides, and PickNumber puts the button back as it says
    private void OnSelectButtonPressed()
    {
        SelectButton.SetPressedNoSignal(pickNumber is not null);
        // The release of a drag scrolling the list isn't a press
        if(ScrollingList() is { LastGestureWasDrag: true }) return;
        SelectPressed?.Invoke(this);
    }

    private DragScrollContainer? ScrollingList()
    {
        for(var node = GetParent(); node is not null; node = node.GetParent())
        {
            if(node is DragScrollContainer list) return list;
        }
        return null;
    }

    // The slot is a card at CardScale; the card scene is centered on it and scales around its center
    private void FitCard()
    {
        CardSlot.CustomMinimumSize = SizeConstants.CardDefaultSize * CardScale;
        if(cardNode is not null) cardNode.Scale = new Vector2(CardScale, CardScale);
    }
}
