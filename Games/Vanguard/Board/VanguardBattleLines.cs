using System;
using System.Collections.Generic;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>
/// Draws each Vanguard battle on the board: a line from the attacker to its target (marked with its arrows), and from
/// the booster to the unit it boosts, until the battle ends. Both players' attacks, so a bot's are easy to follow.
/// Bound to the session (ArcDriver.BindAll); finds the circles' views through the ArcDriver an ancestor provides, and
/// draws in step with the board.
/// </summary>
public partial class VanguardBattleLines : ZoneLinesLayer, IBindable<VanguardGameSession>
{
    [Export] public float AttackSpeed { get; set; } = 1.6f;
    [Export] public float AttackTextureScale { get; set; } = 0.4f;
    [Export] public float BoostSpeed { get; set; } = 1.6f;
    [Export] public float BoostTextureScale { get; set; } = 0.4f;

    readonly List<IDisposable> subscriptions = [];

    public void Bind(VanguardGameSession session)
    {
        Unbind();
        var presentation = this.DependOn<PresentationContext>();
        var driver = this.DependOn<ArcDriver>();
        Control ViewOf(UnitCircle circle) => (Control)driver.ViewOf(circle);

        subscriptions.Add(session.EventBus.Subscribe<BoostDeclared>(boost =>
        {
            var (from, to) = (ViewOf(boost.Booster), ViewOf(boost.Boosted));
            presentation.Present(() => Draw(from, to, BoostSpeed, BoostTextureScale));
        }));
        subscriptions.Add(session.EventBus.Subscribe<AttackDeclared>(attack =>
        {
            var (from, to) = (ViewOf(attack.Attacker), ViewOf(attack.Target));
            presentation.Present(() =>
            {
                presentation.PlaySfx(VanguardSfx.Target);
                Draw(from, to, AttackSpeed, AttackTextureScale);
                Target((IZoneView)to);
            });
        }));
        subscriptions.Add(session.EventBus.Subscribe<BattleEnded>(_ => presentation.Present(Clear)));
    }

    private void Unbind()
    {
        foreach(var subscription in subscriptions) subscription.Dispose();
        subscriptions.Clear();
    }

    public override void _ExitTree() => Unbind();
}
