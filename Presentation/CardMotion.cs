using System;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using DuelCreatures.Data;
using Godot;

// The generic "card goes from A to B" animation. A zone only says where the card
// arrives and how to place it; where it came from is looked up (CardSightings).
public static class CardMotion
{
    // `place` puts the real card at `destination`; a copy then flies in from where the
    // card was last seen, or from `fallbackSource` (e.g. the deck for a draw)
    public static void PresentArrival(this PresentationContext? context, CardBase card, CardMove move,
        ICardLocation destination, ICardLocation? fallbackSource, Action place, bool? faceUp = null)
    {
        context.PresentArrival(card, _ => move, destination, fallbackSource, place, faceUp);
    }

    // Same, picking the move (and so its timing) from where the card comes from
    public static void PresentArrival(this PresentationContext? context, CardBase card, Func<ICardLocation?, CardMove> pickMove,
        ICardLocation destination, ICardLocation? fallbackSource, Action place, bool? faceUp = null)
    {
        if(context is null)
        {
            place();
            return;
        }
        context.Present(() => Arrive(context, card, pickMove, destination, fallbackSource, place, faceUp));
    }

    // Call inside a removal step, right before a location removes its node for `card`
    public static void Leaving(this PresentationContext? context, CardBase card, ICardLocation location)
    {
        context?.Presenter.Sightings.Leaving(card, location);
    }

    private static async Task Arrive(PresentationContext context, CardBase card, Func<ICardLocation?, CardMove> pickMove,
        ICardLocation destination, ICardLocation? fallbackSource, Action place, bool? faceUp)
    {
        var presenter = context.Presenter;
        var sightings = presenter.Sightings;
        var lastLocation = sightings.LastLocation(card);

        // A node still showing the card wins (e.g. a guard still in the guard zone),
        // then where it was last seen, then the fallback's slot
        var sourceNode = lastLocation?.FindCardNode(card) ?? fallbackSource?.FindCardNode(card);
        Transform2D? from = sourceNode?.GetGlobalTransform()
            ?? sightings.LastTransform(card)
            ?? fallbackSource?.GetCardTransform(card);
        var droppedByPlayer = sightings.WasDroppedByPlayer(card) && fallbackSource?.FindCardNode(card) is null;
        var source = lastLocation ?? fallbackSource;

        place();
        sightings.Arrived(card, destination);

        var flights = presenter.Flights;
        if(flights is null || from is null || droppedByPlayer || !Presenter.Animates(context.Side)) return;

        var node = destination.FindCardNode(card);
        SetHidden(node, true);
        SetHidden(sourceNode, true);

        var transition = PresentationConfig.Moves[pickMove(source)];
        var flight = Fly(flights, card, from.Value, destination, node, sourceNode, transition, faceUp);

        // Staggered moves overlap: start the next one after the stagger, the queue
        // still waits for every flight before it counts as idle
        if(transition.Stagger > 0)
        {
            presenter.Track(flight);
            await flights.Wait(transition.Stagger);
        }
        else
        {
            await flight;
        }
    }

    private static async Task Fly(CardFlightLayer flights, CardBase card, Transform2D from, ICardLocation destination,
        Card? node, Card? sourceNode, Transition transition, bool? faceUp)
    {
        var isFront = faceUp ?? (IsAlive(node) ? node!.IsFront : true);
        SleeveInfo? sleeve = IsAlive(node) ? node!.SleeveInfo : IsAlive(sourceNode) ? sourceNode!.SleeveInfo : null;

        await flights.Fly(card, from,
            () => IsAlive(node) ? node!.GetGlobalTransform() : destination.GetCardTransform(card),
            transition, isFront, sleeve);

        SetHidden(node, false);
        SetHidden(sourceNode, false);
    }

    private static void SetHidden(Card? node, bool hidden)
    {
        if(IsAlive(node))
        {
            node!.Modulate = hidden ? Colors.Transparent : Colors.White;
        }
    }

    private static bool IsAlive(Card? node) => node is not null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
}
