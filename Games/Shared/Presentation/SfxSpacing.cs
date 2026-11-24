/// <summary>
/// How close together one sound cue may play, so a burst of the same cue (the opening five draws, two power ups at once)
/// plays one after another instead of on top of each other: each play waits until <paramref name="Gap"/> seconds after
/// the one before it. At most <paramref name="MaxWaiting"/> plays wait at once; further ones in the burst are dropped,
/// so a long burst doesn't trail on after what it was for.
/// </summary>
public sealed record SfxSpacing(float Gap, int MaxWaiting = 3);
