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

    // How long guards stay readable in the guard zone before the battle goes on
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

    // Border of intercepting units in the guard zone (they go back through Deselect, not to the hand)
    public static Color InterceptOutline = new(0.25f, 0.85f, 0.35f);

    // Card info: skills a unit got from effects, shown under its printed ones
    public static Color GrantedSkillColor = new(0.55f, 0.8f, 1f);

    // Once, when a Vanguard board starts: its moves, sounds, and the card scene shared components use
    public static void Register()
    {
        PresentationConfig.Register(Moves, SfxStreams);
        CardScenes.Create = card => SceneFactory.CreateVanguardCard((VanguardCard)card);
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
