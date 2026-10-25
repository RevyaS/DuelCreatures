using System;
using System.Collections.Generic;
using MessagePack;

[MessagePackObject]
public class Profile
{
    [Key(0)]
    public string Name { get; set; } = null!;
    [Key(1)]
    public DateTime CreatedAt { get; set; }
    /// <summary>Card id (see <see cref="CardRegistry"/>) to the amount of copies owned.</summary>
    [Key(2)]
    public Dictionary<string, int> OwnedCards { get; set; } = new();
    [Key(3)]
    public List<DeckInfo> Decks { get; set; } = new();
    [Key(4)]
    public ProfileResources Resources { get; set; } = new();

    public void AddCards(string cardId, int count = 1)
    {
        OwnedCards[cardId] = OwnedCards.GetValueOrDefault(cardId) + count;
    }

    public void AddCards(DeckInfo deck)
    {
        AddCards(deck.FirstVanguardId);
        foreach(var (cardId, count) in deck.Cards)
        {
            AddCards(cardId, count);
        }
    }
}
