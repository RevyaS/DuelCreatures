using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;

/// <summary>
/// A node showing one zone of a game. Binding (IBindable of the zone kinds it can show) gives it its zone and shows its
/// content; afterwards it only changes through <see cref="Apply"/>. The zone scenes: CardSlot and CardBox (one card),
/// LineZone, HandZone and StackZone (several).
/// </summary>
public interface IZoneView : ICardLocation
{
    /// <summary>The zone it shows; null until bound.</summary>
    ICardSpace? Space { get; }

    /// <summary>Shows <paramref name="change"/>: animated through <paramref name="presentation"/>, or at once without one.</summary>
    void Apply(ZoneChange change, PresentationContext? presentation);

    /// <summary>What it shows now, for <see cref="ZoneDesync"/>.</summary>
    ZoneSnapshot Displayed { get; }

    /// <summary>Raised once a change was shown, with the content after it (e.g. for a zone's extensions to refresh).</summary>
    event Action<ZoneSnapshot>? Presented;

    /// <summary>
    /// The player clicked a card it shows (the card), or the zone itself (null). Raised for face-down cards too: what may
    /// be seen of them is decided by whoever listens (ArcDriver only inspects cards their viewer may see).
    /// </summary>
    event Action<IZoneView, CardBase?>? Clicked;

    /// <summary>
    /// Shows what the player can click now: the zone itself (<paramref name="zone"/>) and the cards
    /// <paramref name="cards"/> accepts (none when null). Set by input scopes, cleared when they end.
    /// </summary>
    void SetHighlight(bool zone, Func<CardBase, bool>? cards);

    /// <summary>The cards <paramref name="cards"/> accepts can be dragged out of it (none when null).</summary>
    void SetDraggable(Func<CardBase, bool>? cards);

    /// <summary>Takes dropped cards as <paramref name="offer"/> says, split into its choices while one hovers (none when null).</summary>
    void OfferDrop(DropOffer? offer);

    /// <summary>
    /// Whether it reports swipes and drags from it (aims). Only zones holding one card do (CardSlot, CardBox); asking
    /// another kind of zone fails.
    /// </summary>
    void SetGestures(bool swipes, bool aims);

    /// <summary>A card was dropped on it, on <see cref="DropChoice"/>'s region (see <see cref="OfferDrop"/>).</summary>
    event Action<IZoneView, CardBase, DropChoice>? CardDropped;

    /// <summary>It was swiped (see <see cref="SetGestures"/>).</summary>
    event Action<IZoneView, GestureDetector.SwipeDirection>? Swiped;

    /// <summary>The player long pressed a card it shows (the card), or the zone itself (null).</summary>
    event Action<IZoneView, CardBase?>? LongPressed;

    /// <summary>
    /// Shows its zone as it is now, dropping whatever a prompt staged on it to preview an answer (e.g. guards placed
    /// on the guardian circle before they're confirmed). Only while nothing is being presented.
    /// </summary>
    void Resync();

    /// <summary>The sleeve its face-down cards show (its side's, ArcDriver.UseSleeves).</summary>
    void UseSleeve(DuelCreatures.Data.SleeveInfo sleeve);

    /// <summary>
    /// Shows <paramref name="number"/> on it (which pick it was, e.g. the 1st of 2 Xyz materials), or nothing when null.
    /// Zones that are never picked several at a time show nothing.
    /// </summary>
    void SetPickNumber(int? number) {}

    /// <summary>
    /// Shows it's the target a drag would be let go on (e.g. the unit an attack would hit), while that drag lasts. Zones
    /// that are never aimed at show nothing.
    /// </summary>
    void SetTargeted(bool targeted) {}

    /// <summary>A drag from it started, moved, ended or was cancelled, at the global pointer position (see <see cref="SetGestures"/>).</summary>
    event Action<IZoneView, AimStep, Godot.Vector2>? Aiming;
}

/// <summary>
/// Finds zones showing something other than their zone holds. Only meaningful once the game settled and every change
/// was presented (the game runs ahead of what is shown).
/// </summary>
public static class ZoneDesync
{
    public static IReadOnlyList<string> Find(IEnumerable<IZoneView> views) => views
        .Select(view => (view, mismatch: Describe(view)))
        .Where(found => found.mismatch is not null)
        .Select(found => $"{NameOf(found.view)}: {found.mismatch}")
        .ToList();

    /// <summary>How <paramref name="view"/>'s display differs from its zone now, or null when it matches.</summary>
    public static string? Describe(IZoneView view) => view.Space is null ? "not bound" : Describe(view, ZoneSnapshot.Of(view.Space));

    /// <summary>
    /// How <paramref name="view"/>'s display differs from <paramref name="zone"/> (its zone's content at some moment,
    /// e.g. when the game settled), or null when it matches.
    /// </summary>
    public static string? Describe(IZoneView view, ZoneSnapshot zone)
    {
        var expected = view is IShowsPartOfZone partial ? partial.VisiblePart(zone) : zone;
        var shown = view.Displayed;

        if(view is IShowsPartOfZone { ShownTotal: { } total } && total != zone.Count)
        {
            return $"counts {total} card(s), the zone holds {zone.Count}";
        }
        if(shown.Count != expected.Count) return $"shows {shown.Count} card(s), the zone holds {expected.Count}";
        for(int i = 0; i < expected.Count; i++)
        {
            if(!ReferenceEquals(shown.Cards[i].Card, expected.Cards[i].Card)) return $"card {i + 1} is {shown.Cards[i].Card.Name}, the zone has {expected.Cards[i].Card.Name}";
            if(shown.Cards[i].Facing != expected.Cards[i].Facing) return $"{expected.Cards[i].Card.Name} is shown face-{Lower(shown.Cards[i].Facing)}, it is face-{Lower(expected.Cards[i].Facing)}";
        }
        // An empty zone may keep its last orientation; only a card's is shown
        if(expected.Count > 0 && expected.Orientation is { } orientation && shown.Orientation != orientation) return $"shown {Lower(shown.Orientation)}, the zone is {Lower(orientation)}";
        return null;
    }

    public static string NameOf(IZoneView view) => view is Godot.Node node ? node.GetPath() : view.GetType().Name;
    static string Lower<T>(T value) => value?.ToString()?.ToLowerInvariant() ?? "unknown";
}

/// <summary>
/// A zone view that shows only part of its zone, or shows it differently (a pile showing its top card, a hand shown
/// face-down): what it should show of <paramref name="zone"/>, for <see cref="ZoneDesync"/>.
/// </summary>
public interface IShowsPartOfZone
{
    ZoneSnapshot VisiblePart(ZoneSnapshot zone);

    /// <summary>How many cards it says the zone holds (a pile's count), when it shows one; checked against the zone.</summary>
    int? ShownTotal { get; }
}
