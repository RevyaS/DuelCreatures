using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// How loud each sound plays, as a ratio of its file's own volume: 1 as recorded, 0.5 half as loud, 2 twice as loud,
/// 0 silent. Lets the sounds be balanced against each other here instead of editing every audio file. Games register
/// theirs (<see cref="Register"/>); a cue without a ratio plays at 1.
/// </summary>
public static class AudioBalancer
{
    public const float MinRatio = 0f;
    public const float MaxRatio = 2f;

    static readonly Dictionary<SfxCue, float> ratios = [];

    /// <summary>Adds (or replaces) the ratios of a game's cues; a ratio outside 0 to 2 fails.</summary>
    public static void Register(IReadOnlyDictionary<SfxCue, float> balance)
    {
        foreach(var (cue, ratio) in balance)
        {
            if(ratio is < MinRatio or > MaxRatio || float.IsNaN(ratio))
            {
                throw new ArgumentOutOfRangeException(nameof(balance), ratio, $"The volume ratio of {cue.Name} must be from {MinRatio} to {MaxRatio}");
            }
            ratios[cue] = ratio;
        }
    }

    /// <summary>The volume ratio of <paramref name="cue"/>: its registered one, or 1.</summary>
    public static float RatioOf(SfxCue cue) => ratios.GetValueOrDefault(cue, 1f);

    /// <summary>Whether <paramref name="cue"/> is balanced to silence (ratio 0), so it needn't play at all.</summary>
    public static bool IsMuted(SfxCue cue) => RatioOf(cue) <= 0f;

    /// <summary>The ratio of <paramref name="cue"/> in decibels, to add to a player's volume (2 is about +6 dB, 0.5 about -6 dB).</summary>
    public static float VolumeDbOf(SfxCue cue) => IsMuted(cue) ? float.NegativeInfinity : Mathf.LinearToDb(RatioOf(cue));
}
