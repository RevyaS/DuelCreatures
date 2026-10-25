using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum GachaCue
{
    AnticipationStep,
    PackBurst,
    CardDealt,
    LateHit,
    Flip,
    BigReveal,
}

// One stop shop for this game's gacha: rarities, box rules, dusting, timings, Sfx and effects.
// Another game keeps the gacha code and swaps this file.
public static class GachaConfig
{
    public const string SetsDirectory = "res://Data/GachaSets";

    #region Rarities
    // Lowest tier first. Ids are what the set json files use
    public static readonly Rarity Common = new("C", "Common", 0, [new("#a8743f")], DustTickets: 5);
    public static readonly Rarity Rare = new("R", "Rare", 1, [new("#c9d1d9")], DustTickets: 15);
    public static readonly Rarity DoubleRare = new("RR", "Double Rare", 2, [new("#f2c14e")], DustTickets: 50);
    public static readonly Rarity TripleRare = new("RRR", "Triple Rare", 3, [new("#4fa3ff"), new("#6f6bff"), new("#a35cff"), new("#d46bff"), new("#4fd1ff")], DustTickets: 150);

    public static readonly IReadOnlyList<Rarity> Rarities = [Common, Rare, DoubleRare, TripleRare];

    public static Rarity? FindRarity(string id) => Rarities.FirstOrDefault(rarity => rarity.Id == id);

    public static Rarity GetRarity(string id) => FindRarity(id) ?? throw new KeyNotFoundException($"No rarity {id} in {nameof(GachaConfig)}");

    /// <summary>The next tier up, null for the highest.</summary>
    public static Rarity? Upgrade(Rarity rarity) => Rarities.FirstOrDefault(other => other.Tier == rarity.Tier + 1);
    #endregion

    #region Box simulation
    // Every pack has one hit slot (its last card), the other cards are FillerRarity
    public static readonly Rarity FillerRarity = Common;

    // The rarity of hit slots no guarantee took
    public static readonly Rarity DefaultHitRarity = Rare;

    // Guaranteed hits per box (rarity id -> packs), a set's json can override them with "Hits"
    public static readonly IReadOnlyDictionary<string, int> DefaultBoxHits = new Dictionary<string, int>
    {
        [TripleRare.Id] = 1,
        [DoubleRare.Id] = 2,
    };

    // Chance per pack that one filler card is upgraded a tier, rolled with the box and revealed late in the arc
    public const float LateHitChance = 0.15f;
    #endregion

    #region Dusting
    // Copies kept per card, every copy past it turns into its rarity's DustTickets
    public const int MaxCopies = 4;
    #endregion

    #region Presentation
    // From this rarity up a card gets the big flip
    public static readonly Rarity BigRevealFrom = DoubleRare;

    // One step of the aura climbing a tier, then the pack bursts
    public static readonly Transition AnticipationStep = new(0.6f, Easing.EaseInOut);
    public static float AnticipationShake = 6;          // pixels per tier
    public static readonly Transition PackBurst = new(0.35f, Easing.EaseIn);

    // Cards fanned out face down in an arc
    public static readonly Transition ArcDeal = new(0.35f, Easing.BackOut, Stagger: 0.08f);
    public static float ArcRadius = 1600;
    public static float ArcSpanDeg = 36;
    public static float ArcWidth = 1000;                // the cards are scaled to fit it
    public static float MaxArcCardScale = 0.8f;

    // A filler card jolting into its upgraded rarity
    public static readonly Transition LateHit = new(0.9f, Easing.EaseOut, Delay: 0.4f);

    public static readonly Transition Flip = new(0.3f, Easing.EaseInOut);
    public static readonly Transition BigReveal = new(0.6f, Easing.EaseOut);
    public static float BigRevealScale = 2.4f;
    public static float BigRevealHold = 1.2f;

    // Sfx: (cue, rarity id) -> stream, a null rarity id is the fallback for every rarity.
    // Cues without a stream are silent
    public static readonly Dictionary<(GachaCue Cue, string? RarityId), string> SfxStreams = new()
    {
        // [(GachaCue.AnticipationStep, null)] = "res://Assets/Sfx/Gacha/anticipation.ogg",
        // [(GachaCue.BigReveal, TripleRare.Id)] = "res://Assets/Sfx/Gacha/big_reveal_rrr.ogg",
    };

    // Effects: the opening scene's EffectsPlayer plays "<cue>_<rarity id>" if it has it, else "<cue>",
    // eg; "BigReveal_RRR" or "LateHit". Add the animations there, nothing is needed in code
    public static string EffectName(GachaCue cue, Rarity? rarity) => rarity is null ? cue.ToString() : $"{cue}_{rarity.Id}";
    #endregion
}
