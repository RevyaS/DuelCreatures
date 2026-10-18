using System.Collections.Generic;

// One stop shop for presentation: who gets animations/Sfx, and every timing.
// Reads like CSS: Transition ~ `transition: <duration> <timing-function> <delay>`,
// Keyframes ~ `@keyframes`.
public static class PresentationConfig
{
    // Who gets what
    public static BoardSide Animations = BoardSide.Both;
    public static BoardSide Sfx = BoardSide.Both;

    // Drive/damage check "reveal" hold, separate from animations
    public static BoardSide CheckReveal = BoardSide.Both;
    public static float CheckRevealHold = 0.4f;

    // transition: ...
    public static readonly Dictionary<CardMove, Transition> Moves = new()
    {
        [CardMove.DeckToHand]        = new(0.35f, Easing.EaseOut, Stagger: 0.08f),
        [CardMove.HandToDeck]        = new(0.30f, Easing.EaseIn, Stagger: 0.06f),
        [CardMove.DeckToTriggerZone] = new(0.30f, Easing.EaseOut),
        [CardMove.TriggerToDamage]   = new(0.35f, Easing.EaseInOut),
        [CardMove.TriggerToHand]     = new(0.35f, Easing.EaseInOut),
        [CardMove.ToDropZone]        = new(0.30f, Easing.EaseIn, Stagger: 0.05f),
        [CardMove.GuardToDropZone]   = new(0.30f, Easing.EaseIn, Stagger: 0.05f),
        [CardMove.ToUnitCircle]      = new(0.30f, Easing.EaseOut),
    };

    // @keyframes flip (the face swaps at the frame where ScaleX is 0)
    public static readonly Keyframes Flip = new(new(0.25f, Easing.EaseInOut),
        new(0.0f),
        new(0.5f, ScaleX: 0f),
        new(1.0f));

    // @keyframes shuffle
    public static readonly Keyframes Shuffle = new(new(0.5f, Easing.EaseInOut, Stagger: 0.06f),
        new(0.0f),
        new(0.33f, Offset: new(-30, -20), RotationDeg: -8),
        new(0.66f, Offset: new(30, -20), RotationDeg: 8),
        new(1.0f));

    // Sfx cue -> stream
    public static readonly Dictionary<SfxCue, string> SfxStreams = new()
    {
        [SfxCue.Ride] = "res://Assets/Sfx/ride.ogg",
        [SfxCue.Call] = "res://Assets/Sfx/call.ogg",
        [SfxCue.Target] = "res://Assets/Sfx/target.ogg",
        [SfxCue.PhaseChange] = "res://Assets/Sfx/phase_change.ogg",
    };

    // Simultaneous sounds before the oldest one gets cut off
    public static int SfxVoices = 4;
}
