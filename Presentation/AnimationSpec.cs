using System;
using Godot;

// Which side(s) of the board a setting applies to
[Flags]
public enum BoardSide
{
    None = 0,
    Player = 1,
    Opponent = 2,
    Both = Player | Opponent,
}

// Every card movement that can be animated; each one gets a Transition in PresentationConfig.Moves
public enum CardMove
{
    DeckToHand,
    HandToDeck,
    DeckToTriggerZone,
    TriggerToDamage,
    TriggerToHand,
    ToDropZone,
    GuardToDropZone,
    ToUnitCircle,
}

// CSS-like timing functions
public enum Easing
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    BackOut,
}

// Like `transition: <duration> <timing-function> <delay>`. Durations are in seconds.
// Stagger is the extra delay per card when several cards move together.
public record Transition(float Duration, Easing Ease = Easing.EaseOut, float Delay = 0, float Stagger = 0);

// Like one step of `@keyframes`: At goes from 0 (0%) to 1 (100%),
// the rest is relative to the card's resting transform
public record Keyframe(float At, Vector2 Offset = default, float RotationDeg = 0, float ScaleX = 1);

// Like `@keyframes name { ... }` plus the animation's timing
public record Keyframes(Transition Timing, params Keyframe[] Frames);

public static class BoardSideExtensions
{
    public static bool Includes(this BoardSide flags, BoardSide side)
    {
        return side != BoardSide.None && (flags & side) == side;
    }
}

public static class EasingExtensions
{
    public static Tween.TransitionType ToTransitionType(this Easing easing) => easing switch
    {
        Easing.Linear => Tween.TransitionType.Linear,
        Easing.BackOut => Tween.TransitionType.Back,
        _ => Tween.TransitionType.Cubic,
    };

    public static Tween.EaseType ToEaseType(this Easing easing) => easing switch
    {
        Easing.EaseIn => Tween.EaseType.In,
        Easing.EaseInOut => Tween.EaseType.InOut,
        _ => Tween.EaseType.Out,
    };
}
