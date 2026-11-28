using System.Collections.Generic;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Actions;
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

    private const string SfxPath = "res://Assets/Vanguard/Sfx/";
    // Sfx cue -> stream
    public static readonly Dictionary<SfxCue, string> SfxStreams = new()
    {
        [VanguardSfx.Ride] = SfxPath + "ride.ogg",
        [VanguardSfx.Call] = SfxPath + "call.ogg",
        [VanguardSfx.Target] = SfxPath + "target.ogg",
        [VanguardSfx.PhaseChange] = SfxPath + "phase_change.ogg",
        [VanguardSfx.PhaseChangeButton] = SfxPath + "button_next_phase.ogg",
        [VanguardSfx.PowerUp] = SfxPath + "power_up.ogg",
        [VanguardSfx.CriticalUp] = SfxPath + "trigger_critical.ogg",
        [VanguardSfx.Draw] = SfxPath + "card_draw.ogg",
        [VanguardSfx.Shuffle] = SfxPath + "deck_shuffle.ogg",
        [VanguardSfx.SkillActivated] = SfxPath + "skill.ogg",
    };

    // Sfx cue -> how loud it plays, as a ratio of its file's volume (0 silent, 1 as recorded, 2 twice as loud);
    // cues left out play at 1
    public static readonly Dictionary<SfxCue, float> SfxBalance = new()
    {
    };

    // Cues that come in bursts (the opening draws, several power ups at once): how close together each may play
    public static readonly Dictionary<SfxCue, SfxSpacing> SfxSpacings = new()
    {
        [VanguardSfx.Draw] = new(Gap: 0.08f, MaxWaiting: 5),
        [VanguardSfx.PowerUp] = new(Gap: 0.2f, MaxWaiting: 2),
        [VanguardSfx.CriticalUp] = new(Gap: 0.2f, MaxWaiting: 2),
    };

    /// <summary>How each kind of Vanguard move looks and sounds on a board shown by <paramref name="driver"/>.</summary>
    public static ArcDriver ShowVanguardMoves(this ArcDriver driver) => driver
        // A ride: the new vanguard onto its circle (sounded), the old one into the soul
        .Animate<RideRequest, ICardSpace, Soul>(VanguardMoves.ToSoul)
        .Animate<RideRequest>(VanguardMoves.ToUnitCircle).Sound<RideRequest, ICardSpace, Vanguard>(VanguardSfx.Ride)
        .Animate<SuperiorRideFromHandRequest, ICardSpace, Soul>(VanguardMoves.ToSoul)
        .Animate<SuperiorRideFromHandRequest>(VanguardMoves.ToUnitCircle).Sound<SuperiorRideFromHandRequest, ICardSpace, Vanguard>(VanguardSfx.Ride)
        // A call: the called card onto its circle (sounded), an overcalled unit into the drop zone
        .Animate<CallRequest, ICardSpace, DropZone>(VanguardMoves.ToDropZone)
        .Animate<CallRequest>(VanguardMoves.ToUnitCircle).Sound<CallRequest, ICardSpace, RearGuard>(VanguardSfx.Call)
        .Animate<SuperiorCallRequest, ICardSpace, DropZone>(VanguardMoves.ToDropZone)
        .Animate<SuperiorCallRequest>(VanguardMoves.ToUnitCircle).Sound<SuperiorCallRequest, ICardSpace, RearGuard>(VanguardSfx.Call)
        .Animate<ForerunnerRequest>(VanguardMoves.ToUnitCircle)
        .Animate<SwapRequest>(VanguardMoves.ToUnitCircle)
        .Animate<RetireRequest>(VanguardMoves.ToDropZone)
        // Guarding
        .Animate<CallGuardianRequest>(VanguardMoves.ToGuardZone)
        .Animate<InterceptRequest>(VanguardMoves.ToGuardZone)
        // The battle moves the guardians and a hit rear-guard; a check reveals a card, then its trigger check sends it on
        .Animate<VanguardBattle, GuardZone, DropZone>(VanguardMoves.GuardToDropZone)
        .Animate<VanguardBattle, ICardSpace, DropZone>(VanguardMoves.ToDropZone)
        .Animate<TriggerCheckRequest>(VanguardMoves.DeckToTriggerZone)
        .Animate<TriggerCheck, TriggerZone, DamageZone>(VanguardMoves.TriggerToDamage)
        .Animate<TriggerCheck, DamageZone, DropZone>(VanguardMoves.ToDropZone)
        // Held to be read: drive and damage checks, and the opponent's guards
        .Hold<TriggerCheckRequest>(CheckRevealHold, CheckReveal)
        .Hold<CallGuardianRequest>(GuardRevealHold, GuardReveal)
        .Hold<InterceptRequest>(GuardRevealHold, GuardReveal);
    // Drive checks into the hand, cards returned to the hand or deck: the shared hand and deck moves (ArcDriver's defaults)

    // Once, when a Vanguard board starts: its moves and sounds (its card scene is in VanguardStyle.tres)
    public static void Register()
    {
        PresentationConfig.Register(Moves, SfxStreams, SfxSpacings);
        AudioBalancer.Register(SfxBalance);
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
    public static readonly SfxCue PhaseChangeButton = new(nameof(PhaseChangeButton));
    public static readonly SfxCue PowerUp = new(nameof(PowerUp));
    public static readonly SfxCue CriticalUp = new(nameof(CriticalUp));
    public static readonly SfxCue Draw = new(nameof(Draw));
    public static readonly SfxCue Shuffle = new(nameof(Shuffle));
    public static readonly SfxCue SkillActivated = new(nameof(SkillActivated));
}
