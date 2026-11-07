using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.YuGiOh;
using static YuGiOhQuestions;

/// <summary>
/// The Battle Phase by touch, for the turn player: drag from a monster that can attack onto the monster to attack, or,
/// when the opponent has no monsters, onto the opponent's hand for a direct attack. Only a valid target shows the line
/// and its marker; let go anywhere else, nothing happens. Next Phase ends the Battle Phase. Everything else is answered
/// by the strategy below.
/// </summary>
public sealed class YuGiOhBattlePhaseStrategy(YuGiOhInput input, IYuGiOhAnswers below) : YuGiOhStrategy(input, below)
{
    public static class Text
    {
        public static string Attack = "Battle Phase: drag a monster onto the one to attack, or onto your opponent's hand to attack directly";
    }

    YuGiOhPlayArea PlayArea => Input.PlayArea;
    YuGiOhPlayArea OpponentPlayArea => Input.OpponentPlayArea;

    public override async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        var attack = actions.OfType<AttackAction>().FirstOrDefault();
        var end = actions.OfType<EndAttackPhase>().First();
        bool opponentHasMonsters = OpponentPlayArea.ControlledMonsterZones.Any(zone => !zone.IsEmpty);

        using var scope = Input.Board.Scope().Button(BoardButton.NextPhase).Hint(Text.Attack);
        if(attack is not null)
        {
            foreach(var monster in PlayArea.ControlledMonsterZones.Where(zone => YuGiOhGameRules.CanAttack(zone, PlayArea)))
            {
                scope.Aim(monster, target => CanTarget(target, opponentHasMonsters));
            }
        }

        if(await scope.Next() is Aimed { From: MonsterZone attacker } aim && attack is not null)
        {
            Input.Answers.Set(OwnZone, attacker);
            // The target is only asked when the opponent has monsters; else it's a direct attack
            if(opponentHasMonsters && aim.Target is ZoneTarget { Zone: MonsterZone target }) Input.Answers.Set(OpponentZone, target);
            return attack;
        }
        return end;
    }

    // A monster the opponent controls that can be attacked; with none, the opponent's hand (the player themselves)
    private bool CanTarget(AimTarget target, bool opponentHasMonsters) => target switch
    {
        ZoneTarget { Zone: MonsterZone monster } when opponentHasMonsters
            => OpponentPlayArea.ControlledMonsterZones.Any(zone => ReferenceEquals(zone, monster)) && YuGiOhGameRules.ValidateTarget(monster) is null,
        ZoneTarget { Zone: var zone } when !opponentHasMonsters => ReferenceEquals(zone, OpponentPlayArea.Hand),
        _ => false,
    };
}
