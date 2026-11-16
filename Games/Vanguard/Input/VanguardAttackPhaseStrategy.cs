using System;
using System.Threading.Tasks;
using ArC.CardGames.Actions;
using ArC.CardGames.Vanguard;

/// <summary>
/// The Attack Phase:
/// <list type="bullet">
/// <item>drag from a front-row unit onto an opponent's unit to attack it;</item>
/// <item>drag from a back-row unit, over the unit in front of it, onto an opponent's unit: a boosted attack (the line
/// reaches for the boosted unit first, then for the target);</item>
/// <item>Next Phase to end the attacks.</item>
/// </list>
/// Only attackers, boosts and targets of an offered AttackRequest are allowed.
/// </summary>
public sealed class VanguardAttackPhaseStrategy(VanguardInput input, Offered offered)
{
    public static class Text
    {
        public static string Attack = "Attack Phase: drag from a unit onto an opponent's unit to attack; from the back row, pass over the unit in front to boost it";
    }

    VanguardPlayArea PlayArea => input.PlayArea;

    public async Task<Request> Answer()
    {
        using var scope = input.Board.Scope()
            .Button(BoardButton.NextPhase)
            .Hint(Text.Attack);
        foreach(var attacker in PlayArea.FrontRowUnits)
        {
            if(offered.Any<AttackRequest>(attack => Is(attack, attacker, boosted: false)))
            {
                scope.Aim(attacker, target => Find(attacker, target, boosted: false) is not null);
            }
            if(offered.Any<AttackRequest>(attack => Is(attack, attacker, boosted: true)))
            {
                scope.Aim(PlayArea.GetBackUnit(attacker), attacker, target => Find(attacker, target, boosted: true) is not null);
            }
        }

        switch(await scope.Next())
        {
            case ButtonPressed:
                input.Presentation.PlaySfx(VanguardSfx.PhaseChange);
                return offered.EndPhase;

            case Aimed { Via: UnitCircle boosted } aimed:
                return Find(boosted, aimed.Target, boosted: true) ?? throw Unoffered(aimed);

            case Aimed { From: UnitCircle attacker } aimed:
                return Find(attacker, aimed.Target, boosted: false) ?? throw Unoffered(aimed);

            case var other:
                throw new InvalidOperationException($"{nameof(VanguardAttackPhaseStrategy)} allowed {other} but doesn't handle it");
        }
    }

    private static bool Is(AttackRequest attack, UnitCircle attacker, bool boosted) => ReferenceEquals(attack.Attacker, attacker) && attack.Boosted == boosted;

    // The offered attack by this attacker on the circle aimed at, boosted or not
    private AttackRequest? Find(UnitCircle attacker, AimTarget target, bool boosted)
    {
        if(target is not ZoneTarget { Zone: UnitCircle circle }) return null;
        foreach(var attack in offered.Of<AttackRequest>())
        {
            if(Is(attack, attacker, boosted) && ReferenceEquals(attack.Target, circle)) return attack;
        }
        return null;
    }

    private InvalidOperationException Unoffered(Aimed aimed) => new($"{aimed} matches no offered attack. Offered: {offered.Describe()}");
}
