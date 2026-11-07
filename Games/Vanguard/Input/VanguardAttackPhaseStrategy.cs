using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;
using ArC.Common.Extensions;
using static VanguardQuestions;

/// <summary>
/// The Attack Phase, as on the old board:
/// <list type="bullet">
/// <item>drag from a front-row unit onto an opponent's unit to attack it;</item>
/// <item>drag from a back-row unit, over the unit in front of it, onto an opponent's unit: a boosted attack (the line
/// reaches for the boosted unit first, then for the target);</item>
/// <item>Next Phase to end the attacks.</item>
/// </list>
/// Only units VanguardGameRules lets attack (or boost) can be dragged from, and only onto targets it allows.
/// </summary>
public sealed class VanguardAttackPhaseStrategy(VanguardInput input, IVanguardAnswers below) : VanguardStrategy(input, below)
{
    public static class Text
    {
        public static string Attack = "Attack Phase: drag from a unit onto an opponent's unit to attack; from the back row, pass over the unit in front to boost it";
    }

    VanguardPlayArea PlayArea => Input.PlayArea;
    VanguardPlayArea OpponentPlayArea => Input.OpponentPlayArea;

    public override async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        using var scope = Input.Board.Scope()
            .Button(BoardButton.NextPhase)
            .Hint(Text.Attack);
        bool canAttack = actions.OfType<AttackAction>().Any();
        bool canBoost = actions.OfType<BoostedAttackAction>().Any();
        foreach(var attacker in PlayArea.FrontRowUnits.Where(circle => VanguardGameRules.UnitCircleCanAttack(PlayArea, circle)))
        {
            if(canAttack) scope.Aim(attacker, target => CanTarget(attacker, target));
            if(canBoost && VanguardGameRules.AttackingUnitCircleCanBeBoosted(PlayArea, attacker))
            {
                scope.Aim(PlayArea.GetBackUnit(attacker), attacker, target => CanTarget(attacker, target));
            }
        }

        switch(await scope.Next())
        {
            case ButtonPressed:
                Input.Presentation.PlaySfx(VanguardSfx.PhaseChange);
                return actions.FirstOf<EndAttackPhase>();

            case Aimed { Via: UnitCircle boosted, Target: ZoneTarget { Zone: UnitCircle target } }:
                Input.Answers.Set(OwnCircle, boosted).Set(OpponentCircle, target);
                return actions.FirstOf<BoostedAttackAction>();

            case Aimed { From: UnitCircle attacker, Target: ZoneTarget { Zone: UnitCircle target } }:
                Input.Answers.Set(OwnCircle, attacker).Set(OpponentCircle, target);
                return actions.FirstOf<AttackAction>();

            case var other:
                throw new System.InvalidOperationException($"{nameof(VanguardAttackPhaseStrategy)} allowed {other} but doesn't handle it");
        }
    }

    // An opponent's unit this attacker may attack (the front row, plus its back-row target for e.g. Tejas)
    private bool CanTarget(UnitCircle attacker, AimTarget target)
        => target is ZoneTarget { Zone: UnitCircle circle } && OpponentPlayArea.OwnsUnitCircle(circle)
            && VanguardGameRules.TargetCircleCanBeTargetted(PlayArea, attacker, OpponentPlayArea, circle);
}
