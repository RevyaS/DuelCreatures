using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;

// What a zone view is told, instead of listening to a game's event bus. A driver turns the game's events into these
// (ArcDriver, from ArC's CardsMoved and CardTurned); a sandbox could make them from free moves. Only cards, no game
// types, so every zone scene works for every game.

/// <summary>A card as a zone shows it.</summary>
public readonly record struct CardSnapshot(CardBase Card, Facing Facing)
{
    public bool IsFaceUp => Facing == Facing.UP;
}

/// <summary>
/// A zone's content when a change happened: its cards, bottom to top, and its orientation (null for a zone of several
/// cards). The game runs ahead of what is shown, so views draw from this, never from the live zone.
/// </summary>
public sealed record ZoneSnapshot(IReadOnlyList<CardSnapshot> Cards, Orientation? Orientation)
{
    public static ZoneSnapshot Empty { get; } = new([], null);

    public int Count => Cards.Count;
    public CardSnapshot? Top => Cards.Count == 0 ? null : Cards[^1];

    /// <summary>The zone's content now. Fails for a kind of zone it can't read.</summary>
    public static ZoneSnapshot Of(ICardSpace space) => space switch
    {
        ISingleSpace single => new(single.Card is { } card ? [ new(card, card.Facing) ] : [], single.Orientation),
        GroupSpace group => new(group.Cards.Select(card => new CardSnapshot(card, card.Facing)).ToList(), null),
        _ => throw new NotSupportedException($"{space.GetType().Name} is neither a single space nor a group space: add it to {nameof(ZoneSnapshot)}.{nameof(Of)}"),
    };
}

/// <summary>A change to show in a zone, with the zone's content right after it.</summary>
public abstract record ZoneChange(ZoneSnapshot After);

/// <summary><see cref="Cards"/> arrived, from <see cref="From"/> when it is a shown zone (null: from nowhere shown, e.g. a token).</summary>
public sealed record ZoneCardsArrived(IReadOnlyList<CardBase> Cards, IZoneView? From, CardMove Move, ZoneSnapshot After) : ZoneChange(After);

/// <summary><see cref="Cards"/> left, to <see cref="To"/> when it is a shown zone.</summary>
public sealed record ZoneCardsLeft(IReadOnlyList<CardBase> Cards, IZoneView? To, ZoneSnapshot After) : ZoneChange(After);

/// <summary><see cref="Card"/> was flipped, or its zone turned, without moving.</summary>
public sealed record ZoneCardTurned(CardBase Card, ZoneSnapshot After) : ZoneChange(After);
