using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;

/// <summary>
/// Guarding one of the player's units (the VanguardAttackPhase.GuardStep window): the hand shows each card's shield;
/// drag cards onto the guardian circle (and back to the hand), tap a front rear-guard that can intercept to send it
/// there (tap it on the guardian circle to call it back), then Confirm. The guarded unit shows the power it would have.
/// <para>
/// Nothing moves in the game until Confirm: the board previews the guards, then shows the game again. ArC asks again
/// after every guardian until the player passes, so the staged guards are answered one per question, each matched
/// (by card and circle) against what that question offers, then the window is passed.
/// </para>
/// </summary>
public sealed class VanguardGuardPrompt(VanguardInput input)
{
    public static class Text
    {
        public static string Guard = "Guard: drag cards onto the guardian circle, tap a front rear-guard to intercept, then Confirm";
    }

    public static class Choices
    {
        public static readonly DropChoice Guard = new("Guard");
        public static readonly DropChoice Return = new("Return");
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
        var hand = (HandZone)input.ViewOf(PlayArea.Hand);
        var guardian = (LineZone)input.ViewOf(PlayArea.GuardZone);
        var guarded = (CardArea)input.ViewOf(attack.Target);
        var interceptable = offered.Of<InterceptRequest>().Select(intercept => intercept.RearGuard).ToList();
        bool CanGuard(CardBase card) => offered.Any<CallGuardianRequest>(call => ReferenceEquals(call.Guardian, card));

        List<CardBase> guards = [];
        List<RearGuard> intercepts = [];
        List<CardBase> interceptingCards = [];

        // The hand shows shields while guarding
        hand.DecorateCard = node =>
        {
            if(node is VanguardCardComponent card) card.GuardMode = true;
        };
        try
        {
            while(true)
            {
                // Interceptors are outlined on the guardian circle: they go back by a tap there, not by a drag
                using var scope = input.Board.Scope()
                    .Drag(PlayArea.Hand, card => CanGuard(card) && !Refs.Has(guards, card))
                    .Drop(PlayArea.GuardZone, card => Refs.Has(PlayArea.Hand.Cards, card) && CanGuard(card) && !Refs.Has(guards, card) ? [ Choices.Guard ] : [])
                    .Drag(PlayArea.GuardZone, card => Refs.Has(guards, card))
                    .Drop(PlayArea.Hand, card => Refs.Has(guards, card) ? [ Choices.Return ] : [])
                    .Zones(interceptable.Where(circle => !Refs.Has(intercepts, circle)))
                    .Cards(PlayArea.GuardZone, card => Refs.Has(interceptingCards, card))
                    .Button(BoardButton.Confirm)
                    .Hint(Text.Guard);

                switch(await scope.Next())
                {
                    case ButtonPressed:
                        return (guards, intercepts);

                    case CardDropped { Card: var card, To: GuardZone }:
                        guards.Add(card);
                        hand.RemoveCard(card);
                        guardian.AddCard(CardScenes.Create(card));
                        break;

                    case CardDropped { Card: var card, To: Hand }:
                        Refs.Remove(guards, card);
                        guardian.RemoveCard(card);
                        hand.AddCard(CardScenes.Create(card));
                        break;

                    case ZoneClicked { Zone: RearGuard circle } when circle.Card is { } unit:
                        intercepts.Add(circle);
                        interceptingCards.Add(unit);
                        ((CardArea)input.ViewOf(circle)).ClearCard();
                        guardian.AddCard(CardScenes.Create(unit));
                        break;

                    case CardClicked { Card: var card }:
                        var back = intercepts.First(circle => ReferenceEquals(circle.Card, card));
                        Refs.Remove(intercepts, back);
                        Refs.Remove(interceptingCards, card);
                        guardian.RemoveCard(card);
                        input.ViewOf(back).Resync();
                        break;
                }
                ShowGuardedPower(attack.Target, guarded, guards, intercepts);
            }
        }
        finally
        {
            // The board shows the game again; the guards then move there for real
            hand.DecorateCard = null;
            hand.Resync();
            guardian.Resync();
            foreach(var circle in intercepts) input.ViewOf(circle).Resync();
            if(attack.Target.Card is { } target && guarded.FindCardNode(target) is VanguardCardComponent node)
            {
                node.Power = attack.Target.GetOverallPower();
            }
        }
    }

    // The guarded unit's power with the printed shields staged so far (shield a skill adds once placed shows after Confirm)
    private static void ShowGuardedPower(UnitCircle target, CardArea guarded, List<CardBase> guards, List<RearGuard> intercepts)
    {
        if(target.Card is not { } unit || guarded.FindCardNode(unit) is not VanguardCardComponent node) return;
        node.Power = target.GetOverallPower()
            + guards.Cast<VanguardCard>().Sum(card => card.Guard)
            + intercepts.Sum(circle => circle.Card is VanguardCard card ? card.Guard : 0);
    }
}
