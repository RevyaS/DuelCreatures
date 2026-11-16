using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Lists of cards and zones compared by reference: ArC's cards and zones are records, so two copies of a card (or two
/// empty circles) are Equal, but are never the same one.
/// </summary>
public static class Refs
{
    public static bool Has<T>(IEnumerable<T> items, T item) where T : class => items.Any(candidate => ReferenceEquals(candidate, item));

    public static bool Remove<T>(List<T> items, T item) where T : class
    {
        var index = items.FindIndex(candidate => ReferenceEquals(candidate, item));
        if(index < 0) return false;
        items.RemoveAt(index);
        return true;
    }
}
