using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Base.Rules;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Rules;
using ArC.CardGames.Vanguard;
using ArC.Common.Extensions;
using static VanguardQuestions;

/// <summary>
/// The Main Phase, as on the old board:
/// <list type="bullet">
/// <item>drag a card from the hand onto a rear-guard circle to call it (onto an occupied one: the unit there is retired);</item>
/// <item>drag a rear-guard onto the other circle of its column to move it there (trading places with the unit there);</item>
/// <item>tap a card for its details, and activate an [ACT] skill there (from the hand or a circle);</item>
/// <item>Next Phase.</item>
/// </list>
/// Only what VanguardGameRules allows now can be dragged, dropped or activated. Everything else is answered below.
/// </summary>
public sealed class VanguardMainPhaseStrategy(VanguardInput input, IVanguardAnswers below) : VanguardStrategy(input, below)
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

    IVanguardPlayerInputProvider Player => Input.Player;
    VanguardPlayArea PlayArea => Input.PlayArea;
    RearGuard[] Rearguards => [ ..PlayArea.FrontRearguards, ..PlayArea.BackRearguards ];

    public override async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        var swappable = PlayArea.GetUnitCirclesBySelector(new(UnitCircleEnum.SWAPPABLE_REARGUARD, UnitOccupancyFilterEnum.OCCUPIED_ONLY)).OfType<RearGuard>().ToList();
        bool canSwap = actions.OfType<SwapRearguard>().Any() && VanguardGameRules.CanSwapRearguards(PlayArea);
        bool canCall = actions.OfType<CallRearguard>().Any();

        using var scope = Input.Board.Scope()
            .Activatable(CanActivate)
            .Button(BoardButton.NextPhase)
            .Hint(Text.Play);
        if(canCall) scope.Drag(PlayArea.Hand, CanCall);
        foreach(var circle in Rearguards) scope.Drop(circle, card => DropChoices(card, circle, canCall, canSwap));
        if(canSwap) foreach(var circle in swappable) scope.Drag(circle, _ => true);

        switch(await scope.Next())
        {
            case ButtonPressed:
                Input.Presentation.PlaySfx(VanguardSfx.PhaseChange);
                return actions.FirstOf<EndMainPhase>();

            case CardDropped { To: RearGuard circle } drop when ReferenceEquals(drop.Choice, Choices.Call):
                Input.Answers.Set(CardFromHand, drop.Card).Set(OwnCircle, circle);
                return actions.FirstOf<CallRearguard>();

            case CardDropped { From: RearGuard from } drop when ReferenceEquals(drop.Choice, Choices.Move):
                // The game moves the unit to the other circle of its column
                Input.Answers.Set(OwnCircle, from);
                return actions.FirstOf<SwapRearguard>();

            case SkillActivated { Card: VanguardCard card, Skill: VanguardActivationSkill skill, From: Hand }:
                Input.Answers.Set(CardFromHand, card).Set(SkillToActivate, skill);
                return actions.FirstOf<ActivateSkillFromHand>();

            case SkillActivated { Skill: VanguardActivationSkill skill, From: UnitCircle circle }:
                Input.Answers.Set(OwnCircle, circle).Set(SkillToActivate, skill);
                return actions.FirstOf<ActivateSkillFromUnitCircle>();

            case var other:
                throw new System.InvalidOperationException($"{nameof(VanguardMainPhaseStrategy)} allowed {other} but doesn't handle it");
        }
    }

    // From the hand: called (any rear-guard circle, occupied or not). From a rear-guard: moved, onto its column's other circle
    private IReadOnlyList<DropChoice> DropChoices(CardBase card, RearGuard circle, bool canCall, bool canSwap)
    {
        if(canCall && Refs.Has(PlayArea.Hand.Cards, card)) return CanCall(card) ? [ Choices.Call ] : [];
        if(!canSwap) return [];
        var from = Rearguards.FirstOrDefault(rearguard => ReferenceEquals(rearguard.Card, card));
        return from is not null && VanguardGameRules.ValidateSwap(PlayArea, from, circle) is null ? [ Choices.Move ] : [];
    }

    private bool CanCall(CardBase card)
        => card is VanguardCard called && PlayArea.Vanguard.Card is { } vanguard && VanguardGameRules.CanCallRearguard(vanguard, called);

    // What the card details may activate now: [ACT] skills of the player's hand cards and units that can pay their cost
    private bool CanActivate(CardBase card, ICardSpace? from, ISkill skill) => (card, from, skill) switch
    {
        (VanguardCard handCard, Hand hand, VanguardActivationSkill activation) when ReferenceEquals(hand, PlayArea.Hand)
            => activation.Location == VanguardSkillCardLocation.HAND && VanguardGameRules.HandCardCanActivateSkill(Player, hand, handCard, activation),
        (_, UnitCircle circle, VanguardActivationSkill activation) when PlayArea.OwnsUnitCircle(circle)
            => activation.Location != VanguardSkillCardLocation.HAND && VanguardGameRules.UnitCircleCanActivateSkill(Player, circle, activation),
        _ => false,
    };
}
