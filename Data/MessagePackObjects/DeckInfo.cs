using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Vanguard;
using MessagePack;

/// <summary>A deck as card ids (see <see cref="CardRegistry"/>), the first vanguard is not part of <see cref="Cards"/>.</summary>
[MessagePackObject]
public class DeckInfo
{
    [Key(0)]
    public string Name { get; set; } = null!;
    /// <summary>Null while a new deck has no starter yet.</summary>
    [Key(1)]
    public string? FirstVanguardId { get; set; }
    [Key(2)]
    public Dictionary<string, int> Cards { get; set; } = new();
    [Key(3)]
    public string ClanId { get; set; } = null!;

    [IgnoreMember]
    public Clan Clan
    {
        get => new(ClanId);
        set => ClanId = value.Id;
    }

    [IgnoreMember]
    public int CardCount => Cards.Values.Sum() + (FirstVanguardId is null ? 0 : 1);

    /// <summary>The copies of every card, the first vanguard included.</summary>
    public Dictionary<string, int> GetAllCards()
    {
        var all = new Dictionary<string, int>(Cards);
        if(FirstVanguardId is not null) all[FirstVanguardId] = all.GetValueOrDefault(FirstVanguardId) + 1;
        return all;
    }

    public int GetCount(string cardId) => Cards.GetValueOrDefault(cardId) + (FirstVanguardId == cardId ? 1 : 0);

    public DeckInfo Clone() => new()
    {
        Name = Name,
        FirstVanguardId = FirstVanguardId,
        Cards = new(Cards),
        ClanId = ClanId
    };

    public static DeckInfo From(string name, VanguardDeckBuilder deck)
    {
        string firstVanguardId = CardRegistry.Game.GetId(deck.StartingVanguard);
        return new()
        {
            Name = name,
            FirstVanguardId = firstVanguardId,
            ClanId = CardRegistry.GetClan(firstVanguardId).Id,
            Cards = deck.GetCards()
                .GroupBy(CardRegistry.Game.GetId)
                .ToDictionary(group => group.Key, group => group.Count())
        };
    }
}
