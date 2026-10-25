using System.Collections.Generic;
using System.Linq;
using MessagePack;

/// <summary>A set's box, rolled whole when it is opened so its guarantees hold. Packs stay in place once opened.</summary>
[MessagePackObject]
public class GachaBox
{
    [Key(0)]
    public string SetId { get; set; } = null!;
    [Key(1)]
    public List<GachaPack> Packs { get; set; } = new();

    [IgnoreMember]
    public int PacksLeft => Packs.Count(pack => !pack.Opened);
}

[MessagePackObject]
public class GachaPack
{
    [Key(0)]
    public List<PulledCard> Cards { get; set; } = new();
    [Key(1)]
    public bool Opened { get; set; }
}

[MessagePackObject]
public class PulledCard
{
    [Key(0)]
    public string CardId { get; set; } = null!;
    [Key(1)]
    public string RarityId { get; set; } = null!;
    /// <summary>The rarity the card showed before its late hit, null when it had none.</summary>
    [Key(2)]
    public string? LateHitFromRarityId { get; set; }
}
