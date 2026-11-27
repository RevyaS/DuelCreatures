using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.YuGiOh;

/// <summary>
/// The Battle Phase by touch, for the turn player: drag from a monster that can attack onto the monster to attack, or
/// onto the opponent's hand for a direct attack. Only a target an offered attack names shows the line and its marker;
/// let go anywhere else, nothing happens. Next Phase ends the Battle Phase.
/// </summary>
public sealed class YuGiOhBattlePhaseStrategy(YuGiOhInput input, Offered offered)
{
    public static class Text
    {
        public static string Attack = "Battle Phase: drag a monster onto the one to attack, or onto your opponent's hand to attack directly";
    }

    public async Task<Request> Answer()
    {
        using var scope = input.Board.Scope().Button(BoardButton.NextPhase).Hint(Text.Attack);
        var attackers = offered.Of<AttackRequest>()
            .Select(attack => input.PlayArea.FieldSpaceOf<MonsterZone>(attack.Attacker))
            .OfType<MonsterZone>()
            .Distinct(ReferenceEqualityComparer.Instance).Cast<MonsterZone>();
        foreach(var attacker in attackers)
        {
            scope.Aim(attacker, target => AttackOn(attacker, target) is not null);
        }

        return await scope.Next() switch
        {
            Aimed { From: MonsterZone attacker } aim => AttackOn(attacker, aim.Target)!,
            _ => offered.EndPhase,
        };
    }

    // The offered attack of the attacker on the target: the monster in a zone, or the opponent's hand for a direct attack
    private AttackRequest? AttackOn(MonsterZone attacker, AimTarget target) => target switch
    {
        ZoneTarget { Zone: MonsterZone monster } => offered.Of<AttackRequest>()
            .FirstOrDefault(attack => Holds(attacker, attack.Attacker) && attack.UnitTargets.Any(unit => Holds(monster, unit))),
        ZoneTarget { Zone: var zone } when ReferenceEquals(zone, input.OpponentPlayArea.Hand) => offered.Of<AttackRequest>()
            .FirstOrDefault(attack => Holds(attacker, attack.Attacker) && attack.IsDirect),
        _ => null,
    };

    private static bool Holds(MonsterZone zone, ArC.CardGames.Components.CardInPlay inPlay) => ReferenceEquals(zone.InPlay, inPlay);
}
