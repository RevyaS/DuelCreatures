using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;

/// <summary>
/// The Main Phase:
/// <list type="bullet">
/// <item>drag a card from the hand onto a rear-guard circle to call it (onto an occupied one: the unit there is retired);</item>
/// <item>drag a rear-guard onto the other circle of its column to move it there (trading places with the unit there);</item>
/// <item>tap a card for its details, and activate an [ACT] skill there (from the hand or a circle);</item>
/// <item>Next Phase.</item>
/// </list>
/// Only what an offered request (CallRequest, SwapRequest, ActivateSkillRequest) uses can be dragged, dropped or activated.
/// </summary>
public sealed class VanguardMainPhaseStrategy(VanguardInput input, Offered offered)
{
    public static class Text
    {
        public static string Play = "Main Phase: drag a card onto a circle to call it, drag a rear-guard to move it, tap a card for its skills";
    }

    public static class Choices
    {
        public static readonly DropChoice Call = new("Call");
        public static readonly DropChoice Move = new("Move");
    }

    VanguardPlayArea PlayArea => input.PlayArea;

    public async Task<Request> Answer()
    {
        using var scope = input.Board.Scope()
            .Activatable(offered.CanActivate)
            .Button(BoardButton.NextPhase)
            .Hint(Text.Play)
            .Drag(PlayArea.Hand, card => offered.Any<CallRequest>(call => ReferenceEquals(call.Card, card)));
        foreach(var circle in PlayArea.Rearguards) scope.Drop(circle, card => DropChoices(card, circle));
        foreach(var swap in offered.Of<SwapRequest>()) scope.Drag(swap.From, _ => true);

        switch(await scope.Next())
        {
            case ButtonPressed:
                input.Presentation.PlaySfx(VanguardSfx.PhaseChangeButton);
                return offered.EndPhase;

            case CardDropped { To: RearGuard circle } drop when ReferenceEquals(drop.Choice, Choices.Call):
                return offered.One<CallRequest>(call => ReferenceEquals(call.Card, drop.Card) && ReferenceEquals(call.Circle, circle), "Calling");

            case CardDropped { From: RearGuard from } drop when ReferenceEquals(drop.Choice, Choices.Move):
                // The game moves the unit to the other circle of its column
                return offered.One<SwapRequest>(swap => ReferenceEquals(swap.From, from), "Moving");

            case SkillActivated activated:
                return offered.Activation(activated.Card, activated.From, activated.Skill);

            case var other:
                throw new System.InvalidOperationException($"{nameof(VanguardMainPhaseStrategy)} allowed {other} but doesn't handle it");
        }
    }

    // From the hand: called (onto any circle a CallRequest has). From a rear-guard: moved, onto its column's other circle
    private IReadOnlyList<DropChoice> DropChoices(CardBase card, RearGuard circle)
    {
        if(offered.Any<CallRequest>(call => ReferenceEquals(call.Card, card) && ReferenceEquals(call.Circle, circle))) return [ Choices.Call ];
        bool moves = offered.Any<SwapRequest>(swap => ReferenceEquals(swap.From.Card, card) && ReferenceEquals(PlayArea.GetOppositeRearguard(swap.From), circle));
        return moves ? [ Choices.Move ] : [];
    }
}
