using System;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// One card offered on an input screen, enlarged: the game's card scene (CardScenes.Create) in %CardSlot at
/// <see cref="CardScale"/> (a ratio of the card component's own size, SizeConstants.CardDefaultSize, as every card
/// holder sizes its cards; the slot is that big and the layout around it follows). Over the card, %Overlays is a layer
/// the card component's own size (300x420), scaled with the card, so what's in it is designed as on a full-size card:
/// its pick number (%Badge, a SelectionBadge), where it is or what it's for (%Tag, a CardTagView) and how it will face
/// (%Facing, a FacingIndicator).
/// <para>
/// A tap is <see cref="Pressed"/> (show its details) and, while it may be picked (<see cref="Selectable"/> and
/// <see cref="CanSelect"/>), <see cref="SelectPressed"/> too. The card shows what it's told: whoever owns the selection
/// sets <see cref="PickNumber"/> (picked cards are outlined) and <see cref="CanSelect"/> (cards that can't be picked
/// now are dimmed). A drag carries the card out (<see cref="Draggable"/>). Laid out in SelectableCard.tscn.
/// </para>
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

    /// <summary>The outline of a picked card.</summary>
    [Export] public Color PickedOutline { get; set; } = new(1f, 0.85f, 0.2f);
    /// <summary>How a card that can't be picked now is tinted (the most are picked already).</summary>
    [Export] public Color DimmedTint { get; set; } = new(0.45f, 0.45f, 0.45f);

    Control? cardSlot, overlays;
    SelectionBadge? badge;
    CardTagView? tagView;
    FacingIndicator? facingView;
    Control CardSlot => cardSlot ??= GetNode<Control>("%CardSlot");
    Control Overlays => overlays ??= GetNode<Control>("%Overlays");
    FacingIndicator FacingView => facingView ??= GetNode<FacingIndicator>("%Facing");
    SelectionBadge Badge => badge ??= GetNode<SelectionBadge>("%Badge");
    CardTagView TagView => tagView ??= GetNode<CardTagView>("%Tag");

    Card? cardNode;
    CardBase? card;
    int? pickNumber;
    bool selectable;
    bool canSelect = true;
    bool draggable;

    /// <summary>A tap while the card may be picked: pick or drop it.</summary>
    public event Action<SelectableCard>? SelectPressed;
    /// <summary>A tap on the card: show its details.</summary>
    public event Action<SelectableCard>? Pressed;

    public override void _Ready()
    {
        // Fields reset after a C# rebuild in the editor: look the nodes up again
        cardSlot = null;
        overlays = null;
        FitCard();
        if(Engine.IsEditorHint()) return;
        var gestures = GetNode<GestureDetector>("%Gestures");
        gestures.Tapped += () =>
        {
            if(selectable && canSelect) SelectPressed?.Invoke(this);
            Pressed?.Invoke(this);
        };
        // A drag carries the card (CardDrag) to whatever takes it, e.g. a circle on the enlarged field
        gestures.DragStarted += position =>
        {
            if(draggable && cardNode is not null) CardDrag.Begin(cardNode, position);
        };
        gestures.DragMoved += CardDrag.Move;
        gestures.DragEnded += CardDrag.End;
        gestures.DragCancelled += CardDrag.Cancel;
        Badge.Number = pickNumber;
        Draggable = draggable;
    }

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

    /// <summary>The game's card node shown (e.g. to switch a Vanguard card to show its shield), once shown.</summary>
    public Card? CardNode => cardNode;

    /// <summary>Whether <paramref name="node"/> is this card's node (e.g. the one a drag carried).</summary>
    public bool Shows(Card node) => ReferenceEquals(cardNode, node);

    /// <summary>Shows <paramref name="shown"/>, face up (the player is offered it, so they may read it), in its game's card scene.</summary>
    public void Show(CardBase shown)
    {
        cardNode?.QueueFree();
        card = shown;
        cardNode = CardScenes.Create(shown);
        cardNode.IsFront = true;
        CardSlot.AddChild(cardNode);
        // Under the overlays
        CardSlot.MoveChild(cardNode, 0);
        // This view takes every press; the card only shows
        cardNode.IgnoreMouse();
        FitCard();
        ShowState();
    }

    /// <summary>Picked as number N (the badge shows it, the card is outlined), or not picked (null).</summary>
    public int? PickNumber
    {
        get => pickNumber;
        set
        {
            pickNumber = value;
            Badge.Number = value;
            ShowState();
        }
    }

    /// <summary>A tap may pick or drop the card now (false: dimmed, e.g. the most are picked already).</summary>
    public bool CanSelect
    {
        get => canSelect;
        set
        {
            canSelect = value;
            ShowState();
        }
    }

    /// <summary>Taps pick the card at all (false where the card is only to read or to drag).</summary>
    public bool Selectable
    {
        get => selectable;
        set
        {
            selectable = value;
            ShowState();
        }
    }

    /// <summary>Where the card is or what it's for (icon and/or text), or none.</summary>
    public CardTag? Tag
    {
        get => TagView.Tag;
        set => TagView.Tag = value;
    }

    /// <summary>How the card will face once answered (counter blast / charge), or none.</summary>
    public FacingMark Facing
    {
        get => FacingView.Mark;
        set => FacingView.Mark = value;
    }

    // Picked: outlined; can't be picked now: dimmed
    private void ShowState()
    {
        if(cardNode is null) return;
        cardNode.Outline = pickNumber is not null ? PickedOutline : null;
        cardNode.Modulate = selectable && !canSelect && pickNumber is null ? DimmedTint : Colors.White;
    }

    // The slot is a card at CardScale; the card scene and the overlays are centered on it and scale around their center
    private void FitCard()
    {
        var scale = new Vector2(CardScale, CardScale);
        CardSlot.CustomMinimumSize = SizeConstants.CardDefaultSize * CardScale;
        Overlays.Scale = scale;
        if(cardNode is not null) cardNode.Scale = scale;
    }
}
