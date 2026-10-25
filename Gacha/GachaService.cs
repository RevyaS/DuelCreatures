using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>A card out of an opened pack, with what owning it changed.</summary>
public sealed record PackCard(string CardId, Rarity Rarity, Rarity? LateHitFrom, bool IsNew, int DustedCopies, int DustTickets)
{
    /// <summary>The rarity the card shows until its late hit.</summary>
    public Rarity ShownRarity => LateHitFrom ?? Rarity;
}

public sealed record PackResult(GachaSet Set, int PackIndex, IReadOnlyList<PackCard> Cards, int TicketsSpent)
{
    public int DustTickets => Cards.Sum(card => card.DustTickets);

    /// <summary>The best rarity the anticipation may hint, late hits stay a surprise.</summary>
    public Rarity HintedRarity => Cards.Select(card => card.ShownRarity).MaxBy(rarity => rarity.Tier)!;
}

/// <summary>Booster box simulation: rolls boxes, opens packs into a profile, dusts the copies past <see cref="GachaConfig.MaxCopies"/>.</summary>
public static class GachaService
{
    /// <summary>The set's box in the profile, a new one when there is none or every pack was opened.</summary>
    public static GachaBox GetBox(Profile profile, GachaSet set, Random random)
    {
        if(profile.GachaBoxes.TryGetValue(set.Id, out var box) && box.PacksLeft > 0 && box.Packs.Count == set.PacksPerBox) return box;

        box = CreateBox(set, random);
        profile.GachaBoxes[set.Id] = box;
        return box;
    }

    public static GachaBox CreateBox(GachaSet set, Random random)
    {
        // One hit per pack: the guarantees, then the default hit rarity for the rest
        var hits = set.BoxHits
            .SelectMany(hit => Enumerable.Repeat(GachaConfig.GetRarity(hit.Key), Math.Max(0, hit.Value)))
            .Concat(Enumerable.Repeat(GachaConfig.DefaultHitRarity, set.PacksPerBox))
            .Take(set.PacksPerBox)
            .ToArray();
        random.Shuffle(hits);

        return new GachaBox
        {
            SetId = set.Id,
            Packs = hits.Select(hit => CreatePack(set, hit, random)).ToList()
        };
    }

    private static GachaPack CreatePack(GachaSet set, Rarity hit, Random random)
    {
        var slots = Enumerable.Repeat(GachaConfig.FillerRarity, set.CardsPerPack - 1).Append(hit).ToList();
        var cards = PickCards(set, slots, random);

        int fillers = set.CardsPerPack - 1;
        if(fillers > 0 && GachaConfig.Upgrade(GachaConfig.FillerRarity) is Rarity upgraded && random.NextDouble() < GachaConfig.LateHitChance)
        {
            int slot = random.Next(fillers);
            var taken = cards.Select(card => card.CardId).ToHashSet();
            cards[slot] = new PulledCard
            {
                CardId = PickCard(set, upgraded, taken, random),
                RarityId = upgraded.Id,
                LateHitFromRarityId = GachaConfig.FillerRarity.Id
            };
        }
        return new GachaPack { Cards = cards };
    }

    private static List<PulledCard> PickCards(GachaSet set, IEnumerable<Rarity> slots, Random random)
    {
        HashSet<string> taken = [];
        return slots.Select(rarity =>
        {
            var cardId = PickCard(set, rarity, taken, random);
            taken.Add(cardId);
            return new PulledCard { CardId = cardId, RarityId = rarity.Id };
        }).ToList();
    }

    /// <summary>A random card of the rarity, one not already in the pack while there are any.</summary>
    private static string PickCard(GachaSet set, Rarity rarity, IReadOnlySet<string> taken, Random random)
    {
        var cards = set.GetCards(rarity);
        if(cards.Count == 0) throw new InvalidOperationException($"Set {set.Id} has no {rarity.Name} cards, run the gacha set validator");

        var fresh = cards.Where(card => !taken.Contains(card)).ToList();
        var pool = fresh.Count > 0 ? fresh : cards;
        return pool[random.Next(pool.Count)];
    }

    public static string? CanOpen(Profile profile, GachaSet set, GachaBox box, int packIndex)
    {
        if(packIndex < 0 || packIndex >= box.Packs.Count) return "No such pack";
        if(box.Packs[packIndex].Opened) return "This pack is already opened";
        if(profile.Resources.Tickets < set.TicketCost) return $"Not enough tickets, a pack costs {set.TicketCost}";
        return null;
    }

    /// <summary>Pays for the pack and adds its cards to the profile. The caller saves the profile.</summary>
    public static PackResult OpenPack(Profile profile, GachaSet set, int packIndex)
    {
        var box = profile.GachaBoxes.GetValueOrDefault(set.Id) ?? throw new InvalidOperationException($"No box of set {set.Id}");
        if(CanOpen(profile, set, box, packIndex) is string problem) throw new InvalidOperationException(problem);

        var pack = box.Packs[packIndex];
        pack.Opened = true;
        profile.Resources.Tickets -= set.TicketCost;

        var cards = pack.Cards.Select(card => Own(profile, card)).ToList();
        return new PackResult(set, packIndex, cards, set.TicketCost);
    }

    private static PackCard Own(Profile profile, PulledCard pulled)
    {
        var rarity = GachaConfig.GetRarity(pulled.RarityId);
        int owned = profile.OwnedCards.GetValueOrDefault(pulled.CardId);
        int dusted = 0;

        if(owned < GachaConfig.MaxCopies) profile.AddCards(pulled.CardId);
        else dusted = 1;

        int dustTickets = dusted * rarity.DustTickets;
        profile.Resources.Tickets += dustTickets;

        var lateHitFrom = pulled.LateHitFromRarityId is null ? null : GachaConfig.GetRarity(pulled.LateHitFromRarityId);
        return new PackCard(pulled.CardId, rarity, lateHitFrom, IsNew: owned == 0, dusted, dustTickets);
    }
}
