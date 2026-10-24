using System.Collections.Generic;
using Godot;

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

    // How long guards stay readable in the guard zone before the battle goes on
    // (your own are placed by you, so by default only the opponent's hold)
    public static BoardSide GuardReveal = BoardSide.Opponent;
    public static float GuardRevealHold = 0.8f;

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
        [CardMove.ToGuardZone]       = new(0.30f, Easing.EaseOut, Stagger: 0.06f),
        [CardMove.ToUnitCircle]      = new(0.30f, Easing.EaseOut),
        [CardMove.ToSoul]            = new(0.30f, Easing.EaseIn, Stagger: 0.06f),
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

    // Power/critical changes on a unit: the numbers count to the new value...
    public static readonly Transition StatChange = new(0.30f, Easing.EaseOut);

    // ...while the stat panel pulses (@keyframes pulse)
    public static readonly Keyframes StatChangePulse = new(new(0.30f, Easing.EaseOut),
        new(0.0f),
        new(0.4f, Scale: 1.25f),
        new(1.0f));

    // Sfx cue -> stream
    public static readonly Dictionary<SfxCue, string> SfxStreams = new()
    {
        [SfxCue.Ride] = "res://Assets/Sfx/ride.ogg",
        [SfxCue.Call] = "res://Assets/Sfx/call.ogg",
        [SfxCue.Target] = "res://Assets/Sfx/target.ogg",
        [SfxCue.PhaseChange] = "res://Assets/Sfx/phase_change.ogg",
        // Cues without a stream are silent; map these when there are sounds for them
        // [SfxCue.PowerUp] = "res://Assets/Sfx/power_up.ogg",
        // [SfxCue.CriticalUp] = "res://Assets/Sfx/critical_up.ogg",
    };

    // Border of intercepting units in the guard zone (they go back through Deselect, not to the hand)
    public static Color InterceptOutline = new(0.25f, 0.85f, 0.35f);

    // Card info: skills a unit got from effects, shown under its printed ones
    public static Color GrantedSkillColor = new(0.55f, 0.8f, 1f);

    // Simultaneous sounds before the oldest one gets cut off
    public static int SfxVoices = 4;
}
