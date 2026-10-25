using System.Collections.Generic;
using System.Linq;

/// <summary>What a card pool misses, adds, or changes compared to a reference pool.</summary>
public sealed record CardPoolComparison(IReadOnlyList<string> Missing, IReadOnlyList<string> Added, IReadOnlyList<string> Changed)
{
    public bool IsSame => Missing.Count == 0 && Added.Count == 0 && Changed.Count == 0;
}

public static class CardPoolValidator
{
    public static CardPoolComparison Compare(CardRegistry reference, CardRegistry pool) =>
        Compare(ToPool(reference), ToPool(pool));

    public static CardPoolComparison Compare(CardPool reference, CardPool pool)
    {
        var referenceCards = reference.Cards.ToDictionary(card => card.Id);
        var poolCards = pool.Cards.ToDictionary(card => card.Id);

        return new(
            Missing: referenceCards.Keys.Where(id => !poolCards.ContainsKey(id)).ToList(),
            Added: poolCards.Keys.Where(id => !referenceCards.ContainsKey(id)).ToList(),
            Changed: poolCards.Values
                .Where(card => referenceCards.TryGetValue(card.Id, out var referenceCard) && !card.HasSameStats(referenceCard))
                .Select(card => card.Id)
                .ToList());
    }

    public static CardPool ToPool(CardRegistry registry) => new()
    {
        Cards = registry.Ids.Select(id => CardData.From(id, registry.Create(id))).ToList()
    };
}
