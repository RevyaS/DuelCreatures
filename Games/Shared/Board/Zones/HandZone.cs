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
    public event Action<IZoneView, CardBase>? CardInspected;

    public override void _Ready()
    {
        base._Ready();
        if(Engine.IsEditorHint()) return;
        CardPressed += node =>
        {
            if(node.IsFront && node.CurrentCard is { } card) CardInspected?.Invoke(this, card);
        };
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
