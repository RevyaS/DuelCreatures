using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// A booster set, read from a json file in <see cref="GachaConfig.SetsDirectory"/>. Every set is one box:
/// <see cref="PacksPerBox"/> packs of <see cref="CardsPerPack"/> cards, with the box's hits spread over its packs.
/// </summary>
public sealed class GachaSet
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int TicketCost { get; init; }
    public int PacksPerBox { get; init; }
    public int CardsPerPack { get; init; }
    public string BoxImage { get; init; } = string.Empty;
    /// <summary>The packs in the box use these in turn.</summary>
    public string[] PackImages { get; init; } = [];
    /// <summary>Rarity id -> guaranteed packs per box, <see cref="GachaConfig.DefaultBoxHits"/> when left out.</summary>
    public Dictionary<string, int>? Hits { get; init; }
    /// <summary>Card id (see <see cref="CardRegistry"/>) -> rarity id.</summary>
    public Dictionary<string, string> Cards { get; init; } = new();

    public IReadOnlyDictionary<string, int> BoxHits => Hits ?? GachaConfig.DefaultBoxHits;

    public List<string> GetCards(Rarity rarity) => Cards.Where(card => card.Value == rarity.Id).Select(card => card.Key).ToList();

    public string GetPackImage(int packIndex) => PackImages.Length == 0 ? string.Empty : PackImages[packIndex % PackImages.Length];
}

/// <summary>A set json file, <see cref="Set"/> is null when it couldn't be read.</summary>
public sealed record GachaSetFile(string Path, GachaSet? Set, string? Error);

public static class GachaSets
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static List<GachaSetFile> LoadAll(string directory = GachaConfig.SetsDirectory)
    {
        if(!DirAccess.DirExistsAbsolute(directory)) return [];

        return DirAccess.GetFilesAt(directory)
            .Where(file => file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Order()
            .Select(file => Load(directory.PathJoin(file)))
            .ToList();
    }

    public static GachaSetFile Load(string path)
    {
        try
        {
            var json = Godot.FileAccess.GetFileAsString(path);
            var set = JsonSerializer.Deserialize<GachaSet>(json, JsonOptions);
            return set is null ? new(path, null, "The file is empty") : new(path, set, null);
        }
        catch(JsonException exception)
        {
            return new(path, null, exception.Message);
        }
    }
}
