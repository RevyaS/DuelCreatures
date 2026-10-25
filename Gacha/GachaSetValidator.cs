using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Checks set json files against the card pool, the rarities and box rules of <see cref="GachaConfig"/>, and the images.</summary>
public static class GachaSetValidator
{
    /// <summary>Problems per file path, files without problems are left out.</summary>
    public static Dictionary<string, List<string>> ValidateAll(IEnumerable<GachaSetFile> files, Func<string, bool> cardExists, Func<string, bool> resourceExists)
    {
        var fileList = files.ToList();
        var problems = new Dictionary<string, List<string>>();

        foreach(var file in fileList)
        {
            var fileProblems = file.Set is null ? [$"Can't read the file: {file.Error}"] : Validate(file.Set, cardExists, resourceExists);
            if(fileProblems.Count > 0) problems[file.Path] = fileProblems;
        }

        foreach(var duplicate in fileList.Where(file => file.Set is not null).GroupBy(file => file.Set!.Id).Where(group => group.Count() > 1))
        {
            foreach(var file in duplicate)
            {
                problems.TryAdd(file.Path, []);
                problems[file.Path].Add($"Set id {duplicate.Key} is also used by {string.Join(", ", duplicate.Where(other => other != file).Select(other => other.Path))}");
            }
        }
        return problems;
    }

    public static List<string> Validate(GachaSet set, Func<string, bool> cardExists, Func<string, bool> resourceExists)
    {
        List<string> problems = [];

        if(string.IsNullOrWhiteSpace(set.Id)) problems.Add("No Id");
        if(string.IsNullOrWhiteSpace(set.Name)) problems.Add("No Name");
        if(set.TicketCost < 0) problems.Add($"TicketCost {set.TicketCost} is negative");
        if(set.PacksPerBox < 1) problems.Add($"PacksPerBox {set.PacksPerBox} must be at least 1");
        if(set.CardsPerPack < 1) problems.Add($"CardsPerPack {set.CardsPerPack} must be at least 1");

        // Images
        if(string.IsNullOrWhiteSpace(set.BoxImage)) problems.Add("No BoxImage");
        else if(!resourceExists(set.BoxImage)) problems.Add($"BoxImage {set.BoxImage} not found");
        if(set.PackImages.Length == 0) problems.Add("No PackImages");
        foreach(var image in set.PackImages.Where(image => !resourceExists(image)))
        {
            problems.Add($"Pack image {image} not found");
        }

        // Cards
        foreach(var (cardId, rarityId) in set.Cards)
        {
            if(!cardExists(cardId)) problems.Add($"Card {cardId} is not in the card pool");
            if(GachaConfig.FindRarity(rarityId) is null) problems.Add($"Card {cardId} has unknown rarity {rarityId}");
        }

        // Box rules
        int guaranteed = 0;
        foreach(var (rarityId, packs) in set.BoxHits)
        {
            if(GachaConfig.FindRarity(rarityId) is null) problems.Add($"Hits has unknown rarity {rarityId}");
            if(packs < 0) problems.Add($"Hits {rarityId} is negative");
            guaranteed += Math.Max(0, packs);
        }
        if(guaranteed > set.PacksPerBox) problems.Add($"Hits guarantee {guaranteed} packs but a box has {set.PacksPerBox}");

        foreach(var rarity in GetNeededRarities(set).Where(rarity => set.GetCards(rarity).Count == 0))
        {
            problems.Add($"A box needs {rarity.Name} ({rarity.Id}) cards but the set has none");
        }
        return problems;
    }

    /// <summary>The rarities a box of the set can hold.</summary>
    public static IEnumerable<Rarity> GetNeededRarities(GachaSet set)
    {
        HashSet<Rarity> needed = [];
        if(set.CardsPerPack > 1)
        {
            needed.Add(GachaConfig.FillerRarity);
            if(GachaConfig.LateHitChance > 0 && GachaConfig.Upgrade(GachaConfig.FillerRarity) is Rarity lateHit) needed.Add(lateHit);
        }
        int guaranteed = set.BoxHits.Values.Sum(packs => Math.Max(0, packs));
        if(guaranteed < set.PacksPerBox) needed.Add(GachaConfig.DefaultHitRarity);
        foreach(var (rarityId, packs) in set.BoxHits)
        {
            if(packs > 0 && GachaConfig.FindRarity(rarityId) is Rarity rarity) needed.Add(rarity);
        }
        return needed;
    }
}
