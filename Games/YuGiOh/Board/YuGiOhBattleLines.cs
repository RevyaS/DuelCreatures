using System;
using System.Collections.Generic;
using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>
/// Draws each YuGiOh battle on the board: a line from the attacking monster to the monster it attacks (marked), or to
/// the defending player's hand for a direct attack, until the battle ends. Both players' attacks, so a bot's are easy
/// to follow. Bound to the session (ArcDriver.BindAll); finds the zones' views through the ArcDriver an ancestor
/// provides, and draws in step with the board.
/// </summary>
public partial class YuGiOhBattleLines : ZoneLinesLayer, IBindable<YuGiOhGameSession>
{
    [Export] public float Speed { get; set; } = 1.6f;
    [Export] public float TextureScale { get; set; } = 0.4f;

    readonly List<IDisposable> subscriptions = [];

    public void Bind(YuGiOhGameSession session)
    {
        Unbind();
        var presentation = this.DependOn<PresentationContext>();
        var driver = this.DependOn<ArcDriver>();
        var board = session.Game.Board;

        subscriptions.Add(session.EventBus.Subscribe<AttackDeclared>(attack =>
        {
            // A direct attack is on the defending player: their hand stands for them
            ICardSpace target = attack.Target is { } monster ? monster : board.OpponentAreaOf(attack.PlayArea).Hand;
            var (from, to) = (driver.ViewOf(attack.Attacker), driver.ViewOf(target));
            presentation.Present(() =>
            {
                Draw((Control)from, (Control)to, Speed, TextureScale);
                Target(to);
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
