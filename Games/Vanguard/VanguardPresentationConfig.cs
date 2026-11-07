using System.Collections.Generic;
using ArC.CardGames.Vanguard;
using Godot;

// Vanguard's presentation: its moves, sounds, holds and colors. The shared settings
// (who gets animations/Sfx, flips, shuffles, ...) are in PresentationConfig.
public static class VanguardPresentationConfig
{
    // Drive/damage check "reveal" hold, separate from animations
    public static BoardSide CheckReveal = BoardSide.Both;
    public static float CheckRevealHold = 0.4f;

    // How long each guard stays readable on the guardian circle before the battle goes on
    // (your own are placed by you, so by default only the opponent's hold)
    public static BoardSide GuardReveal = BoardSide.Opponent;
    public static float GuardRevealHold = 0.8f;

    // transition: ...
    public static readonly Dictionary<CardMove, Transition> Moves = new()
    {
        [VanguardMoves.DeckToTriggerZone] = new(0.30f, Easing.EaseOut),
        [VanguardMoves.TriggerToDamage]   = new(0.35f, Easing.EaseInOut),
        [VanguardMoves.ToDropZone]        = new(0.30f, Easing.EaseIn, Stagger: 0.05f),
        [VanguardMoves.GuardToDropZone]   = new(0.30f, Easing.EaseIn, Stagger: 0.05f),
        [VanguardMoves.ToGuardZone]       = new(0.30f, Easing.EaseOut, Stagger: 0.06f),
        [VanguardMoves.ToUnitCircle]      = new(0.30f, Easing.EaseOut),
        [VanguardMoves.ToSoul]            = new(0.30f, Easing.EaseIn, Stagger: 0.06f),
    };

    // Sfx cue -> stream
    public static readonly Dictionary<SfxCue, string> SfxStreams = new()
    {
        [VanguardSfx.Ride] = "res://Assets/Sfx/ride.ogg",
        [VanguardSfx.Call] = "res://Assets/Sfx/call.ogg",
        [VanguardSfx.Target] = "res://Assets/Sfx/target.ogg",
        [VanguardSfx.PhaseChange] = "res://Assets/Sfx/phase_change.ogg",
        // Cues without a stream are silent; map these when there are sounds for them
        // [VanguardSfx.PowerUp] = "res://Assets/Sfx/power_up.ogg",
        // [VanguardSfx.CriticalUp] = "res://Assets/Sfx/critical_up.ogg",
    };



    /// <summary>How each kind of Vanguard move looks and sounds on a board shown by <paramref name="driver"/>.</summary>
    public static ArcDriver ShowVanguardMoves(this ArcDriver driver) => driver
        // Onto the circles
        .Animate<Rode>(VanguardMoves.ToUnitCircle).Sound<Rode>(VanguardSfx.Ride)
        .Animate<SuperiorRode>(VanguardMoves.ToUnitCircle).Sound<SuperiorRode>(VanguardSfx.Ride)
        .Animate<Called>(VanguardMoves.ToUnitCircle).Sound<Called>(VanguardSfx.Call)
        .Animate<SuperiorCalled>(VanguardMoves.ToUnitCircle).Sound<SuperiorCalled>(VanguardSfx.Call)
        .Animate<Swapped>(VanguardMoves.ToUnitCircle)
        // Into the soul
        .Animate<RodeUpon>(VanguardMoves.ToSoul)
        .Animate<SoulCharged>(VanguardMoves.ToSoul)
        // Battle
        .Animate<Guarded>(VanguardMoves.ToGuardZone)
        .Animate<Intercepted>(VanguardMoves.ToGuardZone)
        .Animate<GuardFinished>(VanguardMoves.GuardToDropZone)
        .Animate<DriveChecked>(VanguardMoves.DeckToTriggerZone)
        .Animate<DamageChecked>(VanguardMoves.DeckToTriggerZone)
        .Animate<TookDamage>(VanguardMoves.TriggerToDamage)
        // Into the drop zone
        .Animate<Overcalled>(VanguardMoves.ToDropZone)
        .Animate<DefeatedInBattle>(VanguardMoves.ToDropZone)
        .Animate<RetiredBySkill>(VanguardMoves.ToDropZone)
        .Animate<PaidAsCost>(VanguardMoves.ToDropZone)
        .Animate<DiscardedBySkill>(VanguardMoves.ToDropZone)
        .Animate<Healed>(VanguardMoves.ToDropZone)
        // Held to be read: drive and damage checks, and the opponent's guards
        .Hold<DriveChecked>(CheckRevealHold, CheckReveal)
        .Hold<DamageChecked>(CheckRevealHold, CheckReveal)
        .Hold<Guarded>(GuardRevealHold, GuardReveal)
        .Hold<Intercepted>(GuardRevealHold, GuardReveal);
    // Drive checks into the hand, cards returned to the hand or deck: the shared hand and deck moves (ArcDriver's defaults)

    // Once, when a Vanguard board starts: its moves and sounds (its card scene is in VanguardStyle.tres)
    public static void Register()
    {
        PresentationConfig.Register(Moves, SfxStreams);
    }
}

// Vanguard's own card movements (the shared ones are in CardMoves)
public static class VanguardMoves
{
    public static readonly CardMove DeckToTriggerZone = new(nameof(DeckToTriggerZone));
    public static readonly CardMove TriggerToDamage = new(nameof(TriggerToDamage));
    public static readonly CardMove ToDropZone = new(nameof(ToDropZone));
    public static readonly CardMove GuardToDropZone = new(nameof(GuardToDropZone));
    public static readonly CardMove ToGuardZone = new(nameof(ToGuardZone));
    public static readonly CardMove ToUnitCircle = new(nameof(ToUnitCircle));
    public static readonly CardMove ToSoul = new(nameof(ToSoul));
}

public static class VanguardSfx
{
    public static readonly SfxCue Ride = new(nameof(Ride));
    public static readonly SfxCue Call = new(nameof(Call));
    public static readonly SfxCue Target = new(nameof(Target));
    public static readonly SfxCue PhaseChange = new(nameof(PhaseChange));
    public static readonly SfxCue PowerUp = new(nameof(PowerUp));
    public static readonly SfxCue CriticalUp = new(nameof(CriticalUp));
}
