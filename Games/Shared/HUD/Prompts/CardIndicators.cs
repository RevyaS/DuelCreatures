using System;
using System.Linq;
using Godot;

/// <summary>
/// A game's set of card tags or markers, one exported property per place or role (e.g. VanguardCardTags.Soul), so a
/// missing one is a missing property rather than a mistyped key. <see cref="EnsureComplete"/> fails naming the resource
/// and every property left unset.
/// </summary>
public abstract partial class CardIndicators : Resource
{
    bool checkedComplete;

    /// <summary>Throws if any CardTag or CardMarker property of this resource is unset.</summary>
    public void EnsureComplete()
    {
        if(checkedComplete) return;
        var missing = GetType().GetProperties()
            .Where(property => property.CanRead && (property.PropertyType == typeof(CardTag) || property.PropertyType == typeof(CardMarker)))
            .Where(property => property.GetValue(this) is null)
            .Select(property => property.Name)
            .ToList();
        if(missing.Count > 0)
        {
            throw new InvalidOperationException($"{GetType().Name} {ResourcePath} has no {string.Join(", ", missing)}: set them in the resource");
        }
        checkedComplete = true;
    }
}
