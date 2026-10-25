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
    /// <summary>Set id to the box being pulled from.</summary>
    [Key(5)]
    public Dictionary<string, GachaBox> GachaBoxes { get; set; } = new();

    public void AddCards(string cardId, int count = 1)
    {
        OwnedCards[cardId] = OwnedCards.GetValueOrDefault(cardId) + count;
    }

    public void AddCards(DeckInfo deck)
    {
        foreach(var (cardId, count) in deck.GetAllCards())
        {
            AddCards(cardId, count);
        }
    }
}
