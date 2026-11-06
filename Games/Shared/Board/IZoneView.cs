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

    /// <summary>How <paramref name="view"/>'s display differs from its zone, or null when it matches.</summary>
    public static string? Describe(IZoneView view)
    {
        if(view.Space is null) return "not bound";
        var expected = view is IShowsPartOfZone partial ? partial.Visible(ZoneSnapshot.Of(view.Space)) : ZoneSnapshot.Of(view.Space);
        var shown = view.Displayed;

        if(view is IShowsPartOfZone { ShownTotal: { } total } && total != ZoneSnapshot.Of(view.Space).Count)
        {
            return $"counts {total} card(s), the zone holds {ZoneSnapshot.Of(view.Space).Count}";
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

    static string NameOf(IZoneView view) => view is Godot.Node node ? node.GetPath() : view.GetType().Name;
    static string Lower<T>(T value) => value?.ToString()?.ToLowerInvariant() ?? "unknown";
}

/// <summary>
/// A zone view that shows only part of its zone, or shows it differently (a pile showing its top card, a hand shown
/// face-down): what it should show of <paramref name="zone"/>, for <see cref="ZoneDesync"/>.
/// </summary>
public interface IShowsPartOfZone
{
    ZoneSnapshot Visible(ZoneSnapshot zone);

    /// <summary>How many cards it says the zone holds (a pile's count), when it shows one; checked against the zone.</summary>
    int? ShownTotal { get; }
}
