using System.Collections.Generic;
using ArC.CardGames.Components;
using Godot;

// Remembers where each card was last shown, so its next arrival can fly in from there.
// Keyed by reference: deck copies are records, so two copies of a card are "equal".
public class CardSightings
{
    record Sighting(ICardLocation Location, Transform2D? LeftAt, bool LeftWhileDragged);

    readonly Dictionary<CardBase, Sighting> seen = new(ReferenceEqualityComparer.Instance);

    public void Arrived(CardBase card, ICardLocation location)
    {
        seen[card] = new(location, null, false);
    }

    // Call right before a location removes its node for `card`
    public void Leaving(CardBase card, ICardLocation location)
    {
        var node = location.FindCardNode(card);
        seen[card] = new(location,
            node?.GetGlobalTransform() ?? location.GetCardTransform(card),
            node?.CurrentlyDragged ?? false);
    }

    public ICardLocation? LastLocation(CardBase card)
    {
        return seen.TryGetValue(card, out var sighting) ? sighting.Location : null;
    }

    public Transform2D? LastTransform(CardBase card)
    {
        if(!seen.TryGetValue(card, out var sighting)) return null;
        if(sighting.LeftAt is not null) return sighting.LeftAt;

        return sighting.Location.FindCardNode(card)?.GetGlobalTransform()
            ?? sighting.Location.GetCardTransform(card);
    }

    // The player dragged it there themselves, so there's nothing to animate
    public bool WasDroppedByPlayer(CardBase card)
    {
        if(!seen.TryGetValue(card, out var sighting)) return false;
        if(sighting.LeftAt is not null) return sighting.LeftWhileDragged;

        return sighting.Location.FindCardNode(card)?.CurrentlyDragged ?? false;
    }
}
