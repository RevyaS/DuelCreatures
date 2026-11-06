using System;
using System.Linq;
using ArC.CardGames.Rules;
using Godot;

/// <summary>
/// Shows whose turn it is, for any two player game: it binds a GameSession (found by ArcDriver.BindAll) and updates
/// on each PhaseStarted, in step with the board (through the PresentationContext an ancestor provides). Player 1 is the
/// one playing on this screen.
/// </summary>
public partial class TurnLabel : Label, IBindable<GameSession>
{
    [Export] public string PlayerTurnText { get; set; } = "Your Turn";
    [Export] public string OpponentTurnText { get; set; } = "Opponent Turn";

    IDisposable? subscription;

    public void Bind(GameSession session)
    {
        var presentation = this.DependOn<PresentationContext>();
        var player1 = session.Game.Profiles.First();
        subscription?.Dispose();
        subscription = session.EventBus.Subscribe<PhaseStarted>(started =>
        {
            var text = ReferenceEquals(started.TurnPlayer, player1) ? PlayerTurnText : OpponentTurnText;
            presentation.Present(() => Text = text);
        });
    }

    public override void _ExitTree()
    {
        subscription?.Dispose();
        subscription = null;
    }
}
