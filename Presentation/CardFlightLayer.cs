using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using DuelCreatures.Data;
using Godot;

// Overlay above the board where card copies fly between zones, so the zones' own
// containers never see the in-between positions. All timings come from PresentationConfig.
public partial class CardFlightLayer : Control
{
    const string CardScenePath = "res://Card/Card.tscn";

    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
    }

    // Flies a copy of `card` from `from` to wherever `to` says (re-read every frame, so it
    // follows a destination that's still moving, e.g. a hand re-spacing its cards)
    public async Task Fly(CardBase card, Transform2D from, Func<Transform2D> to, Transition transition,
        bool faceUp, SleeveInfo? sleeve)
    {
        var copy = CreateCopy(SceneFactory.CreateVanguardCard((VanguardCard)card), faceUp, sleeve);
        Place(copy, from);

        var tween = CreateTween();
        if(transition.Delay > 0)
        {
            tween.TweenInterval(transition.Delay);
        }
        tween.TweenMethod(Callable.From<float>(weight => Place(copy, from.InterpolateWith(to(), weight))),
                0f, 1f, Duration(transition.Duration))
            .SetTrans(transition.Ease.ToTransitionType())
            .SetEase(transition.Ease.ToEaseType());

        await ToSignal(tween, Tween.SignalName.Finished);
        copy.QueueFree();
    }

    // Turns a card over in place; the face swaps at the keyframe where ScaleX reaches 0
    public async Task Flip(Card card, bool faceUp)
    {
        var frames = PresentationConfig.Flip.Frames;
        await PlayKeyframes(card, PresentationConfig.Flip, frameIndex =>
        {
            if(frames[frameIndex].ScaleX == 0)
            {
                card.IsFront = faceUp;
            }
        });

        if(IsInstanceValid(card))
        {
            card.IsFront = faceUp;
        }
    }

    // A few card backs ruffle above the deck and settle back
    public async Task Shuffle(Transform2D deck, SleeveInfo? sleeve, int cards = 3)
    {
        var shuffle = PresentationConfig.Shuffle;
        List<Task> animations = [];
        for(int i = 0; i < cards; i++)
        {
            var copy = CreateCopy(ResourceLoader.Load<PackedScene>(CardScenePath).Instantiate<Card>(), false, sleeve);
            Place(copy, deck);

            // Every other card moves the opposite way
            var keyframes = i % 2 == 0 ? shuffle : Mirror(shuffle);
            animations.Add(PlayAndFree(copy, keyframes, i * shuffle.Timing.Stagger));
        }
        await Task.WhenAll(animations);
    }

    // Plays `@keyframes`-style frames on `target`, relative to its current transform.
    // `onFrame` runs as each frame (by index) is reached.
    public async Task PlayKeyframes(Control target, Keyframes keyframes, Action<int>? onFrame = null, float delay = 0)
    {
        var frames = keyframes.Frames;
        if(frames.Length < 2) return;

        var timing = keyframes.Timing;
        var basePosition = target.Position;
        var baseRotation = target.Rotation;
        var baseScale = target.Scale;

        // Owned by this layer (not the target) so it still finishes if the target is freed
        var tween = CreateTween();
        if(delay + timing.Delay > 0)
        {
            tween.TweenInterval(delay + timing.Delay);
        }

        for(int i = 1; i < frames.Length; i++)
        {
            var frame = frames[i];
            var segment = Duration((frame.At - frames[i - 1].At) * timing.Duration);

            tween.TweenProperty(target, "position", basePosition + frame.Offset, segment)
                .SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
            tween.Parallel().TweenProperty(target, "rotation", baseRotation + Mathf.DegToRad(frame.RotationDeg), segment)
                .SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());
            tween.Parallel().TweenProperty(target, "scale", new Vector2(baseScale.X * frame.ScaleX, baseScale.Y), segment)
                .SetTrans(timing.Ease.ToTransitionType()).SetEase(timing.Ease.ToEaseType());

            if(onFrame is not null)
            {
                var frameIndex = i;
                tween.TweenCallback(Callable.From(() => onFrame(frameIndex)));
            }
        }

        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public SignalAwaiter Wait(float seconds)
    {
        return ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
    }

    private async Task PlayAndFree(Card copy, Keyframes keyframes, float delay)
    {
        await PlayKeyframes(copy, keyframes, delay: delay);
        copy.QueueFree();
    }

    private Card CreateCopy(Card copy, bool faceUp, SleeveInfo? sleeve)
    {
        AddChild(copy);
        copy.MouseFilter = MouseFilterEnum.Ignore;
        copy.SetAnchorsPreset(LayoutPreset.TopLeft);
        copy.PivotOffset = Vector2.Zero;
        copy.IsFront = faceUp;
        if(sleeve is not null)
        {
            copy.SleeveInfo = sleeve;
        }
        return copy;
    }

    // Transforms are global; this layer may not sit at the origin
    private void Place(Control copy, Transform2D globalTransform)
    {
        var local = GetGlobalTransform().AffineInverse() * globalTransform;
        copy.Position = local.Origin;
        copy.Rotation = local.Rotation;
        copy.Scale = local.Scale;
    }

    private static Keyframes Mirror(Keyframes keyframes)
    {
        var frames = Array.ConvertAll(keyframes.Frames, frame =>
            frame with { Offset = new Vector2(-frame.Offset.X, frame.Offset.Y), RotationDeg = -frame.RotationDeg });
        return keyframes with { Frames = frames };
    }

    private static float Duration(float seconds) => Mathf.Max(seconds, 0.001f);
}
