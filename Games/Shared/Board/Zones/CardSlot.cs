using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

/// <summary>
/// One card, in a space shaped like a card (e.g. a trigger zone, a field zone, a spell &amp; trap zone): its card is
/// never turned sideways, and being told to is an error naming the zone (show it with a <see cref="CardBox"/>
/// instead). Shows any one-card zone.
/// </summary>
[Tool]
public partial class CardSlot : CardArea, IZoneView, IBindable<ISingleSpace>
{
    public ICardSpace? Space { get; private set; }

    public event Action<ZoneSnapshot>? Presented;
    public event Action<IZoneView, CardBase?>? Clicked;
    public event Action<IZoneView, CardBase?>? LongPressed;
    public event Action<IZoneView, CardBase, DropChoice>? CardDropped;
    public event Action<IZoneView, GestureDetector.SwipeDirection>? Swiped;
    public event Action<IZoneView, AimStep, Vector2>? Aiming;

    [Export] public Color HighlightColor { get; set; } = new(1f, 0.85f, 0.2f);
    [Export] public float HighlightWidth { get; set; } = 4;
    [Export] public Color CardHighlightColor { get; set; } = new(1f, 0.85f, 0.2f);
    /// <summary>The block over the slot while a drag would be let go on it (SetTargeted).</summary>
    [Export] public Color TargetColor { get; set; } = new(1f, 0.85f, 0.2f, 0.45f);
    /// <summary>How far and how fast a flick must be to count as a swipe (GestureDetector).</summary>
    [Export] public float SwipeDistance { get; set; } = 60;
    [Export] public float SwipeMaxTime { get; set; } = 0.35f;

    ReferenceRect? highlight;
    ColorRect? targetBlock;
    Func<CardBase, bool>? highlightedCards;
    GestureDetector? gestures;
    DropChoicesOverlay? drops;
    // Which cards a drag from the slot moves out (SetDraggable), and whether drags aim (SetGestures)
    Func<CardBase, bool>? draggableCards;
    bool aims;
    // The card a drag from the slot is moving, until it ends
    Card? movingCard;

    public override void _Ready()
    {
        base._Ready();
        if(Engine.IsEditorHint()) return;
        highlight = ZoneInput.AddHighlight(this, HighlightColor, HighlightWidth);
        ZoneInput.PassClicksToZone(this);

        // The slot takes every press (its card only shows): a tap, a swipe, or a drag from it
        Container.CardAdded += ZoneInput.IgnoreMouse;
        if(Container.CurrentCard is { } shown) ZoneInput.IgnoreMouse(shown);
        gestures = new GestureDetector
        {
            Name = "Gestures",
            Swipes = false,
            HorizontalSwipes = true,
            VerticalSwipes = false,
            Drags = false,
            ScrollsParent = false,
            SwipeDistance = SwipeDistance,
            SwipeMaxTime = SwipeMaxTime,
        };
        AddChild(gestures);
        gestures.Tapped += () => Clicked?.Invoke(this, Container.CurrentCard?.CurrentCard);
        gestures.LongPressed += () => LongPressed?.Invoke(this, Container.CurrentCard?.CurrentCard);
        gestures.Swiped += direction => Swiped?.Invoke(this, direction);
        gestures.DragStarted += OnDragStarted;
        gestures.DragMoved += position =>
        {
            if(movingCard is not null) CardDrag.Move(position);
            else Aiming?.Invoke(this, AimStep.Move, position);
        };
        gestures.DragEnded += position =>
        {
            if(movingCard is not null) EndMove(() => CardDrag.End(position));
            else Aiming?.Invoke(this, AimStep.End, position);
        };
        gestures.DragCancelled += () =>
        {
            if(movingCard is not null) EndMove(CardDrag.Cancel);
            else Aiming?.Invoke(this, AimStep.Cancel, GetGlobalMousePosition());
        };
    }

    // A drag from the slot moves its card out when it may be dragged (e.g. a unit to another circle), else it aims
    private void OnDragStarted(Vector2 position)
    {
        if(Container.CurrentCard is { CurrentCard: { } card } node && draggableCards?.Invoke(card) == true)
        {
            movingCard = node;
            CardDrag.Begin(node, position);
            return;
        }
        if(aims) Aiming?.Invoke(this, AimStep.Start, position);
        else gestures?.Cancel();
    }

    private void EndMove(Action end)
    {
        movingCard = null;
        end();
    }

    public void SetTargeted(bool targeted) => ShowTargeted(targeted);

    public void UseSleeve(DuelCreatures.Data.SleeveInfo sleeve) => Container.SetSleeveInfo(sleeve);

    /// <summary>How the slot shows it's targeted: a yellow block over it. A zone with its own target marks overrides this.</summary>
    protected virtual void ShowTargeted(bool targeted)
    {
        if(targetBlock is null)
        {
            if(!targeted) return;
            targetBlock = new ColorRect { Name = "Target", Color = TargetColor, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(targetBlock);
            targetBlock.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        }
        targetBlock.Visible = targeted;
    }

    public void SetGestures(bool swipes, bool aims)
    {
        this.aims = aims;
        if(gestures is null) return;
        gestures.Swipes = swipes;
        UpdateDrags();
    }

    /// <summary>
    /// A drag from the slot moves its card out when <paramref name="cards"/> accepts it (e.g. a rear-guard to the
    /// opposite circle), shown as the card following the pointer; dropping it is the target's CardDropped. Let go
    /// anywhere else, it stays. Takes precedence over aiming for the cards it accepts.
    /// </summary>
    public void SetDraggable(Func<CardBase, bool>? cards)
    {
        draggableCards = cards;
        UpdateDrags();
    }

    private void UpdateDrags()
    {
        if(gestures is not null) gestures.Drags = aims || draggableCards is not null;
    }

    public void Resync()
    {
        if(Space is ISingleSpace space) Bind(space);
    }

    public void OfferDrop(DropOffer? offer)
    {
        if(offer is null && drops is null) return;
        if(drops is null)
        {
            drops = DropChoicesOverlay.For(this);
            drops.Dropped += (card, choice) => CardDropped?.Invoke(this, card, choice);
        }
        drops.Offer(offer);
    }

    public void SetHighlight(bool zone, Func<CardBase, bool>? cards)
    {
        if(highlight is not null) highlight.Visible = zone;
        highlightedCards = cards;
        OutlineCard();
    }

    private void OutlineCard()
    {
        if(Container.CurrentCard is { CurrentCard: { } card } node)
        {
            node.Outline = highlightedCards?.Invoke(card) == true ? CardHighlightColor : null;
        }
    }

    /// <summary>Whether a card here may lie sideways. A <see cref="CardBox"/>'s square fits one; a card-shaped slot doesn't.</summary>
    protected virtual bool AllowsSideways => false;

    public void Bind(ISingleSpace space)
    {
        Space = space;
        var snapshot = ZoneSnapshot.Of(space);
        EnsureFits(snapshot);
        Render(snapshot);
    }

    public ZoneSnapshot Displayed => Container.CurrentCard is { CurrentCard: { } card } node
        ? new([ new(card, node.IsFront ? Facing.UP : Facing.DOWN) ], Container.CardOrientation)
        : new([], Container.CardOrientation);

    public void Apply(ZoneChange change, PresentationContext? presentation)
    {
        EnsureFits(change.After);
        switch(change)
        {
            case ZoneCardsArrived arrived:
                if(arrived.Cards.Count != 1) throw new InvalidOperationException($"{Name} shows one card, {arrived.Cards.Count} arrived");
                var card = ZonePresentation.Find(change.After, arrived.Cards[0]);
                presentation.PresentArrival(card.Card, arrived.Move, this, arrived.From, () => Render(change.After), card.IsFaceUp);
                break;

            case ZoneCardsLeft left:
                presentation.Present(() =>
                {
                    foreach(var leaving in left.Cards) presentation.Leaving(leaving, this);
                    Render(change.After);
                });
                break;

            case ZoneCardTurned turned:
                var after = ZonePresentation.Find(change.After, turned.Card);
                presentation.Present(async () =>
                {
                    if(FindCardNode(turned.Card) is { } node)
                    {
                        await ZonePresentation.Flip(presentation, node, after.IsFaceUp);
                    }
                    Render(change.After);
                });
                break;

            default:
                throw new NotSupportedException($"{Name} can't show a {change.GetType().Name}");
        }
        presentation.Present(() => Presented?.Invoke(change.After));
    }

    // Shows the snapshot's card as it is, keeping the node already showing it
    private void Render(ZoneSnapshot snapshot)
    {
        if(snapshot.Top is not { } top)
        {
            ClearCard();
            return;
        }
        if(FindCardNode(top.Card) is { } node)
        {
            node.IsFront = top.IsFaceUp;
        }
        else
        {
            AddCard(ZonePresentation.CreateNode(top));
        }
        Container.ChangeOrientation(snapshot.Orientation ?? Orientation.VERTICAL);
        OutlineCard();
    }

    private void EnsureFits(ZoneSnapshot snapshot)
    {
        if(AllowsSideways || snapshot.Count == 0 || snapshot.Orientation != Orientation.HORIZONTAL) return;
        throw new InvalidOperationException(
            $"{GetPath()} is a {nameof(CardSlot)}, which can't show a card sideways ({snapshot.Cards.First().Card.Name}): show this zone with a {nameof(CardBox)}");
    }
}
