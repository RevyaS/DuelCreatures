using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Vanguard;

/// <summary>The factory cards with the stats of a <see cref="CardPool"/> applied.</summary>
public class CardCatalog
{
    readonly Dictionary<string, CardData> cards;

    public CardPoolType PoolType { get; }
    public IReadOnlyCollection<CardData> Cards => cards.Values;

    public CardCatalog(CardPoolType poolType, CardPool pool)
    {
        PoolType = poolType;
        cards = pool.Cards.ToDictionary(card => card.Id);
    }

    public CardData GetData(string cardId) => cards.TryGetValue(cardId, out var data)
        ? data
        : throw new KeyNotFoundException($"No card with id {cardId} in the {PoolType} card pool, run the DataGenerator");

    public bool Contains(string cardId) => cards.ContainsKey(cardId);

    public IEnumerable<CardData> GetCards(Clan clan) => cards.Values.Where(card => CardRegistry.GetClan(card.Id) == clan);

    public VanguardCard Create(string cardId) => GetData(cardId).ApplyTo(CardRegistry.Game.Create(cardId));

    public VanguardDeckBuilder BuildDeck(DeckInfo deck)
    {
        string firstVanguardId = deck.FirstVanguardId ?? throw new InvalidOperationException($"{deck.Name} has no starting vanguard");
        var builder = VanguardDeckBuilder.Create().SetStartingVanguard(Create(firstVanguardId));
        foreach(var (cardId, count) in deck.Cards)
        {
            builder.AddCards(Create(cardId), count);
        }
        return builder;
    }
}
