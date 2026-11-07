using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// Several cards side by side in a fixed number of slots (MaxCards), e.g. a damage zone or a banish row. Cards keep
/// their own facing, so one can be flipped face-down in place. More cards than slots is an error.
/// </summary>
[Tool]
public partial class LineZone : CardLineStatic, IZoneView, IBindable<GroupSpace>
{
    public ICardSpace? Space { get; private set; }

    /// <summary>Pressing a card asks to show the whole zone (<see cref="InspectRequested"/>).</summary>
    [Export] public bool Inspectable { get; set; }

    public event Action<ZoneSnapshot>? Presented;
    public event Action<IZoneView>? InspectRequested;
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
        // Inspectable: a press also asks to show the whole zone
        CardPressed += node =>
        {
            if(node.CurrentCard is { } card) Clicked?.Invoke(this, card);
            if(Inspectable && Space is not null) InspectRequested?.Invoke(this);
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
        foreach(var card in ZoneSnapshot.Of(space).Cards)
        {
            Add(card);
        }
    }

    public ZoneSnapshot Displayed => new(GetNonEmptyCards()
        .Select(node => new CardSnapshot(node.CurrentCard, node.IsFront ? Facing.UP : Facing.DOWN))
        .ToList(), null);

    public void Apply(ZoneChange change, PresentationContext? presentation)
    {
        switch(change)
        {
            case ZoneCardsArrived arrived:
                foreach(var arriving in arrived.Cards)
                {
                    var card = ZonePresentation.Find(change.After, arriving);
                    presentation.PresentArrival(card.Card, arrived.Move, this, arrived.From, () => Add(card), card.IsFaceUp);
                }
                break;

            case ZoneCardsLeft left:
                presentation.Present(() =>
                {
                    foreach(var leaving in left.Cards)
                    {
                        presentation.Leaving(leaving, this);
                        RemoveCard(leaving);
                    }
                });
                break;

            case ZoneCardTurned turned:
                var after = ZonePresentation.Find(change.After, turned.Card);
                presentation.Present(async () =>
                {
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

    private void Add(CardSnapshot card)
    {
        var node = ZonePresentation.CreateNode(card);
        AddCard(node);
        // AddCard shows new cards face up; this zone shows the card's own facing
        node.IsFront = card.IsFaceUp;
    }
}
