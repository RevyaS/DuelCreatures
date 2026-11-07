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
    public event Action<IZoneView, CardBase, DropChoice>? CardDropped;
    public event Action<IZoneView, GestureDetector.SwipeDirection>? Swiped;
    public event Action<IZoneView, AimStep, Vector2>? Aiming;

    [Export] public Color HighlightColor { get; set; } = new(1f, 0.85f, 0.2f);
    [Export] public float HighlightWidth { get; set; } = 4;
    [Export] public Color CardHighlightColor { get; set; } = new(1f, 0.85f, 0.2f);
    /// <summary>How far and how fast a flick must be to count as a swipe (GestureDetector).</summary>
    [Export] public float SwipeDistance { get; set; } = 60;
    [Export] public float SwipeMaxTime { get; set; } = 0.35f;

    ReferenceRect? highlight;
    Func<CardBase, bool>? highlightedCards;
    GestureDetector? gestures;
    DropChoicesOverlay? drops;

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
        gestures.Swiped += direction => Swiped?.Invoke(this, direction);
        gestures.DragStarted += position => Aiming?.Invoke(this, AimStep.Start, position);
        gestures.DragMoved += position => Aiming?.Invoke(this, AimStep.Move, position);
        gestures.DragEnded += position => Aiming?.Invoke(this, AimStep.End, position);
        gestures.DragCancelled += () => Aiming?.Invoke(this, AimStep.Cancel, GetGlobalMousePosition());
    }

    public void SetGestures(bool swipes, bool aims)
    {
        if(gestures is null) return;
        gestures.Swipes = swipes;
        gestures.Drags = aims;
    }

    public void SetDraggable(Func<CardBase, bool>? cards)
    {
        // Its card is moved by the game, not dragged out: a drag from it is an aim (SetGestures)
        if(cards is not null) throw new NotSupportedException($"{GetPath()} ({GetType().Name}) can't have its card dragged out; use SetGestures to aim from it");
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
