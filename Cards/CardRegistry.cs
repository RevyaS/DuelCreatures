using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Cards;

/// <summary>
/// Every card of <see cref="VanguardCardsFactory"/>, keyed by "Clan.Property" (eg; RoyalPaladin.BlasterBlade).
/// The id stays the same when the card's stats are rebalanced, saved data refers to cards by it.
/// </summary>
public static class CardRegistry
{
    static readonly Lazy<IReadOnlyDictionary<string, Func<VanguardCard>>> factories = new(FindFactories);
    static readonly Lazy<ILookup<string, string>> idsByName = new(() => Ids.ToLookup(id => Create(id).Name));

    public static IEnumerable<string> Ids => factories.Value.Keys;

    public static bool Contains(string id) => factories.Value.ContainsKey(id);

    /// <summary>A new copy of the card exactly as the factory has it.</summary>
    public static VanguardCard Create(string id) => factories.Value.TryGetValue(id, out var create)
        ? create()
        : throw new KeyNotFoundException($"No card with id {id} in {nameof(VanguardCardsFactory)}");

    /// <summary>The id of a factory card, cards don't carry it so it is found by name.</summary>
    public static string GetId(CardBase card)
    {
        var ids = idsByName.Value[card.Name].ToList();
        return ids.Count switch
        {
            1 => ids[0],
            0 => throw new KeyNotFoundException($"{card.Name} is not in {nameof(VanguardCardsFactory)}"),
            _ => throw new InvalidOperationException($"{card.Name} is ambiguous, it is used by {string.Join(", ", ids)}")
        };
    }

    static IReadOnlyDictionary<string, Func<VanguardCard>> FindFactories() => typeof(VanguardCardsFactory).GetNestedTypes()
        .SelectMany(clan => clan.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(VanguardCard))
            .Select(property => (Id: $"{clan.Name}.{property.Name}", Create: (Func<VanguardCard>)(() => (VanguardCard)property.GetValue(null)!))))
        .ToDictionary(card => card.Id, card => card.Create);
}
