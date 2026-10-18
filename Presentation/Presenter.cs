using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public record PresentationStep(BoardSide Side, Func<Task> Play);

// Plays board updates strictly in the order the game raised them. ArC runs ahead at
// full speed, so handlers must snapshot the game state they need when they enqueue,
// not read it when the step plays. Input prompts wait for WhenIdle().
public class Presenter
{
    readonly Queue<PresentationStep> steps = new();
    bool running = false;
    TaskCompletionSource? idle = null;

    public bool IsIdle => !running;

    public void Enqueue(BoardSide side, Func<Task> play)
    {
        steps.Enqueue(new(side, play));
        if(!running)
        {
            _ = RunAsync();
        }
    }

    // Instant step; runs immediately when nothing is playing
    public void Enqueue(BoardSide side, Action apply)
    {
        Enqueue(side, () =>
        {
            apply();
            return Task.CompletedTask;
        });
    }

    public Task WhenIdle()
    {
        if(!running) return Task.CompletedTask;

        idle ??= new();
        return idle.Task;
    }

    // Whether a step for this side should animate, or just apply its end state
    public static bool Animates(BoardSide side) => PresentationConfig.Animations.Includes(side);

    private async Task RunAsync()
    {
        running = true;
        while(steps.TryDequeue(out var step))
        {
            try
            {
                await step.Play();
            }
            catch(Exception e)
            {
                GD.PushError(e.ToString());
            }
        }
        running = false;

        var waiting = idle;
        idle = null;
        waiting?.SetResult();
    }
}

// What a board component needs to present itself: the shared queue and Sfx bus,
// and which side of the board it belongs to
public record PresentationContext(Presenter Presenter, SfxBus Sfx, BoardSide Side);

// Extensions (not instance methods) so they're safe on a null context: components can
// be used before a game is bound, and then simply apply right away
public static class PresentationContextExtensions
{
    public static void Present(this PresentationContext? context, Action apply)
    {
        if(context is null)
        {
            apply();
            return;
        }
        context.Presenter.Enqueue(context.Side, apply);
    }

    public static void Present(this PresentationContext? context, Func<Task> play)
    {
        if(context is null)
        {
            _ = play();
            return;
        }
        context.Presenter.Enqueue(context.Side, play);
    }

    public static void PlaySfx(this PresentationContext? context, SfxCue cue)
    {
        context?.Sfx.Play(cue, context.Side);
    }
}
