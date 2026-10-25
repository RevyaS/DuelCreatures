using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;
using MessagePack;

/// <summary>A deck as card ids (see <see cref="CardRegistry"/>), the first vanguard is not part of <see cref="Cards"/>.</summary>
[MessagePackObject]
public class DeckInfo
{
    [Key(0)]
    public string Name { get; set; } = null!;
    [Key(1)]
    public string FirstVanguardId { get; set; } = null!;
    [Key(2)]
    public Dictionary<string, int> Cards { get; set; } = new();

    [IgnoreMember]
    public int CardCount => Cards.Values.Sum() + 1;

    public static DeckInfo From(string name, VanguardDeckBuilder deck) => new()
    {
        Name = name,
        FirstVanguardId = CardRegistry.GetId(deck.StartingVanguard),
        Cards = deck.GetCards()
            .GroupBy(CardRegistry.GetId)
            .ToDictionary(group => group.Key, group => group.Count())
    };
}
