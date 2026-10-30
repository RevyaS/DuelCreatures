using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Cards;

/// <summary>
/// Every card of a card factory, keyed by "Clan.Property" (eg; RoyalPaladin.BlasterBlade). A factory is a static class
/// whose nested classes are clans holding static <see cref="VanguardCard"/> properties, like <see cref="VanguardCardsFactory"/>.
/// The id stays the same when the card's stats are rebalanced, saved data refers to cards by it.
/// </summary>
public sealed class CardRegistry
{
    static CardRegistry? game;

    /// <summary>ArC's own cards, the reference the card pool validator compares against.</summary>
    public static CardRegistry ArC { get; } = new(typeof(VanguardCardsFactory));

    /// <summary>The cards the game plays with, see <see cref="DataManager.CardFactory"/>.</summary>
    public static CardRegistry Game => game ??= DataManager.CardFactory == ArC.FactoryType ? ArC : new(DataManager.CardFactory);

    readonly Lazy<IReadOnlyDictionary<string, Func<VanguardCard>>> factories;
    readonly Lazy<ILookup<string, string>> idsByName;

    public Type FactoryType { get; }

    public CardRegistry(Type factoryType)
    {
        FactoryType = factoryType;
        factories = new(FindFactories);
        idsByName = new(() => Ids.ToLookup(id => Create(id).Name));
    }

    public IEnumerable<string> Ids => factories.Value.Keys;

    public IReadOnlyList<Clan> Clans => Ids.Select(GetClan).Distinct().ToList();

    public IEnumerable<string> GetIds(Clan clan) => Ids.Where(id => GetClan(id) == clan);

    public static Clan GetClan(string cardId) => new(cardId[..cardId.IndexOf('.')]);

    public bool Contains(string id) => factories.Value.ContainsKey(id);

    /// <summary>A new copy of the card exactly as the factory has it.</summary>
    public VanguardCard Create(string id) => factories.Value.TryGetValue(id, out var create)
        ? create()
        : throw new KeyNotFoundException($"No card with id {id} in {FactoryType.Name}");

    /// <summary>The id of a factory card, cards don't carry it so it is found by name.</summary>
    public string GetId(CardBase card)
    {
        var ids = idsByName.Value[card.Name].ToList();
        return ids.Count switch
        {
            1 => ids[0],
            0 => throw new KeyNotFoundException($"{card.Name} is not in {FactoryType.Name}"),
            _ => throw new InvalidOperationException($"{card.Name} is ambiguous, it is used by {string.Join(", ", ids)}")
        };
    }

    IReadOnlyDictionary<string, Func<VanguardCard>> FindFactories() => FactoryType.GetNestedTypes()
        .SelectMany(clan => clan.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(VanguardCard))
            .Select(property => (Id: $"{clan.Name}.{property.Name}", Create: (Func<VanguardCard>)(() => (VanguardCard)property.GetValue(null)!))))
        .ToDictionary(card => card.Id, card => card.Create);
}
