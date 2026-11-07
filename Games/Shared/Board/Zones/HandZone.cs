using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using Godot;

/// <summary>
/// A hand: as many cards as it holds, re-spaced as they come and go. With HideCards (an opponent's hand) every card
/// shows its back; otherwise each shows its own facing.
/// </summary>
[Tool]
public partial class HandZone : CardLineDynamic, IZoneView, IBindable<GroupSpace>, IShowsPartOfZone
{
    // The facing of each card shown, as of the last change presented; HideCards hides them all regardless
    readonly Dictionary<CardBase, Facing> facings = new(ReferenceEqualityComparer.Instance);

    public ICardSpace? Space { get; private set; }

    public event Action<ZoneSnapshot>? Presented;
    public event Action<IZoneView, CardBase?>? Clicked;
    public event Action<IZoneView, CardBase?>? LongPressed;

    [Export] public Color HighlightColor { get; set; } = new(1f, 0.85f, 0.2f);
    [Export] public float HighlightWidth { get; set; } = 4;
    [Export] public Color CardHighlightColor { get; set; } = new(1f, 0.85f, 0.2f);

    ReferenceRect? highlight;

    public override void _Ready()
    {
        base._Ready();
        if(Engine.IsEditorHint()) return;
        highlight = ZoneInput.AddHighlight(this, HighlightColor, HighlightWidth);
        CardPressed += node =>
        {
            if(node.CurrentCard is { } card) Clicked?.Invoke(this, card);
        };
        CardLongPressed += node =>
        {
            if(node.CurrentCard is { } card) LongPressed?.Invoke(this, card);
        };
    }

    public event Action<IZoneView, CardBase, DropChoice>? CardDropped;
    public event Action<IZoneView, GestureDetector.SwipeDirection>? Swiped;
    public event Action<IZoneView, AimStep, Vector2>? Aiming;

    DropChoicesOverlay? drops;

    public void SetDraggable(Func<CardBase, bool>? cards)
    {
        CanDragCard = cards is null ? null : node => node.CurrentCard is { } card && cards(card);
        Draggable = cards is not null;
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

    // Only a zone of one card swipes or is aimed from
    public void SetGestures(bool swipes, bool aims)
    {
        if(swipes || aims) throw new NotSupportedException($"{GetPath()} ({GetType().Name}) doesn't take swipes or aims; only CardSlot and CardBox do");
    }

    public void SetHighlight(bool zone, Func<CardBase, bool>? cards)
    {
        if(highlight is not null) highlight.Visible = zone;
        OutlineCard = cards is null ? null : node => node.CurrentCard is { } card && cards(card) ? CardHighlightColor : null;
        Refresh();
    }

    public void UseSleeve(DuelCreatures.Data.SleeveInfo sleeve) => SleeveInfo = sleeve;

    public void Resync()
    {
        if(Space is GroupSpace space) Bind(space);
    }

    public void Bind(GroupSpace space)
    {
        Space = space;
        ClearCards();
        facings.Clear();
        foreach(var card in ZoneSnapshot.Of(space).Cards)
        {
            Add(card);
        }
    }

    public ZoneSnapshot Displayed => new(GetNonEmptyCards()
        .Select(node => new CardSnapshot(node.CurrentCard, node.IsFront ? Facing.UP : Facing.DOWN))
        .ToList(), null);

    public ZoneSnapshot VisiblePart(ZoneSnapshot zone) => HideCards
        ? zone with { Cards = zone.Cards.Select(card => card with { Facing = Facing.DOWN }).ToList() }
        : zone;

    public int? ShownTotal => null;

    public void Apply(ZoneChange change, PresentationContext? presentation)
    {
        switch(change)
        {
            case ZoneCardsArrived arrived:
                foreach(var arriving in arrived.Cards)
                {
                    var card = ZonePresentation.Find(change.After, arriving);
                    presentation.PresentArrival(card.Card,
                        source => source is IZoneView { Space: Deck } ? CardMoves.DeckToHand : arrived.Move,
                        this, arrived.From, () => Add(card), !HideCards && card.IsFaceUp);
                }
                break;

            case ZoneCardsLeft left:
                presentation.Present(() =>
                {
                    foreach(var leaving in left.Cards)
                    {
                        presentation.Leaving(leaving, this);
                        RemoveCard(leaving);
                        facings.Remove(leaving);
                    }
                });
                break;

            case ZoneCardTurned turned:
                var after = ZonePresentation.Find(change.After, turned.Card);
                presentation.Present(async () =>
                {
                    facings[turned.Card] = after.Facing;
                    if(HideCards) return;
                    var node = FindCardNode(turned.Card) ?? throw new InvalidOperationException($"{GetPath()} doesn't show {turned.Card.Name}, which was turned");
                    await ZonePresentation.Flip(presentation, node, after.IsFaceUp);
                });
                break;

            default:
                throw new NotSupportedException($"{Name} can't show a {change.GetType().Name}");
        }
        presentation.Present(() => Presented?.Invoke(change.After));
    }

    // The height it keeps when empty: a card at CardScale, plus this (the margins around the card row)
    private float _emptyHeightPadding = 4;
    [Export]
    public float EmptyHeightPadding
    {
        get => _emptyHeightPadding;
        set
        {
            _emptyHeightPadding = value;
            Render();
        }
    }

    protected override void RenderCore()
    {
        base.RenderCore();
        ZoneLayout.KeepCardHeight(this, CardScale, EmptyHeightPadding);
    }

    // Runs whenever the line renders: keeps each card's facing (the base shows every card face up unless HideCards)
    protected override void ConfigureCard(Card card)
    {
        card.IsFront = !HideCards && (card.CurrentCard is null || !facings.TryGetValue(card.CurrentCard, out var facing) || facing == Facing.UP);
    }

    private void Add(CardSnapshot card)
    {
        facings[card.Card] = card.Facing;
        AddCard(ZonePresentation.CreateNode(card));
    }
}
