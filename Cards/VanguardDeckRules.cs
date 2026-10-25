using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;

public static class VanguardDeckRules
{
    /// <summary>The first vanguard included.</summary>
    public const int DeckSize = 50;
    public const int MaxCopies = 4;
    public const int TriggerCount = 16;
    public const int MaxHealTriggers = 4;
    public const int StarterGrade = 0;

    /// <summary>Why the deck can't be played, empty when it can. Owned cards are checked when given.</summary>
    public static List<string> Validate(DeckInfo deck, CardCatalog catalog, IReadOnlyDictionary<string, int>? ownedCards = null)
    {
        List<string> problems = [];
        var cards = deck.GetAllCards();
        var data = cards.Keys.Where(catalog.Contains).ToDictionary(id => id, catalog.GetData);

        if(deck.FirstVanguardId is null) problems.Add("No starting vanguard, long press a grade 0 to set it");
        else if(data.TryGetValue(deck.FirstVanguardId, out var starter) && starter.Grade != StarterGrade) problems.Add($"{starter.Name} can't be the starting vanguard, it isn't grade {StarterGrade}");

        int total = cards.Values.Sum();
        if(total != DeckSize) problems.Add($"The deck has {total} cards, it needs {DeckSize}");

        int triggers = cards.Where(card => data.TryGetValue(card.Key, out var cardData) && cardData.Trigger != VanguardTrigger.NONE).Sum(card => card.Value);
        if(triggers != TriggerCount) problems.Add($"The deck has {triggers} triggers, it needs {TriggerCount}");

        int heals = cards.Where(card => data.TryGetValue(card.Key, out var cardData) && cardData.Trigger == VanguardTrigger.HEAL).Sum(card => card.Value);
        if(heals > MaxHealTriggers) problems.Add($"The deck has {heals} heal triggers, at most {MaxHealTriggers} are allowed");

        foreach(var (cardId, count) in cards)
        {
            if(!data.TryGetValue(cardId, out var cardData))
            {
                problems.Add($"{cardId} is not in the card pool");
                continue;
            }
            if(CardRegistry.GetClan(cardId) != deck.Clan) problems.Add($"{cardData.Name} is not a {deck.Clan.DisplayName} card");
            if(count > MaxCopies) problems.Add($"{count} copies of {cardData.Name}, at most {MaxCopies} are allowed");
            if(ownedCards is not null && count > ownedCards.GetValueOrDefault(cardId)) problems.Add($"{count} copies of {cardData.Name}, only {ownedCards.GetValueOrDefault(cardId)} owned");
        }
        return problems;
    }
}
