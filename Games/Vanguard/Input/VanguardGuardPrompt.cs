using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;

/// <summary>
/// Guarding one of the player's units (the VanguardAttackPhase.GuardStep window), on the guard screen
/// (VanguardGuardScreen): the battle on top, the hand cards that can guard and the rear-guards that can intercept in one
/// grid; tap to pick, Guard to answer, No Guard to pass. The defender's overall power previews the picked shields.
/// <para>
/// Nothing moves in the game until Guard. ArC asks again
/// after every guardian until the player passes, so the staged guards are answered one per question, each matched
/// (by card and circle) against what that question offers, then the window is passed.
/// </para>
/// </summary>
public sealed class VanguardGuardPrompt(VanguardInput input)
{
    public static class Text
    {
        public static string Guard = "Select the cards to guard with";
    }

    // The guard step being answered, and what's left of what the player staged in it
    OpenWindow? window;
    readonly Queue<object> staged = new();

    VanguardPlayArea PlayArea => input.PlayArea;

    public async Task<Request> Answer(Offered offered, OpenWindow open)
    {
        if(!ReferenceEquals(window, open))
        {
            window = open;
            staged.Clear();
            var attack = open.About as AttackRequest ?? throw new InvalidOperationException($"The guard step is about {open.About?.ToString() ?? "nothing"}, not an attack");
            var (guards, intercepts) = await Stage(offered, attack);
            foreach(var circle in intercepts) staged.Enqueue(circle);
            foreach(var card in guards) staged.Enqueue(card);
        }

        if(staged.Count == 0) return offered.Pass;
        return staged.Dequeue() switch
        {
            RearGuard circle => offered.One<InterceptRequest>(intercept => ReferenceEquals(intercept.RearGuard, circle), $"Intercepting with {circle}"),
            VanguardCard card => offered.One<CallGuardianRequest>(call => ReferenceEquals(call.Guardian, card), $"Guarding with {card.Name}"),
            var other => throw new InvalidOperationException($"Staged {other} is neither a guardian nor an interceptor"),
        };
    }

    private async Task<(List<CardBase> Guards, List<RearGuard> Intercepts)> Stage(Offered offered, AttackRequest attack)
    {
        var guardians = offered.Of<CallGuardianRequest>().Select(call => call.Guardian).Cast<VanguardCard>().ToList();
        var interceptors = offered.Of<InterceptRequest>().Select(intercept => intercept.RearGuard).ToList();
        var (picked, intercepts) = await input.GuardScreen.Stage(Text.Guard, attack.Attacker, attack.Target, PlayArea,
            guardians, interceptors, input.CardTags);
        return (picked.Cast<CardBase>().ToList(), intercepts);
    }
}
