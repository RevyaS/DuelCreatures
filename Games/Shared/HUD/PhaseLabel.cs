using System;
using System.Linq;
using System.Text.RegularExpressions;
using ArC.CardGames;
using ArC.CardGames.Rules;
using Godot;

/// <summary>
/// Shows whose turn and which phase it is, for any game: it binds a GameSession (found by ArcDriver.BindAll) and shows
/// each PhaseStarted in step with the board (through the PresentationContext an ancestor provides).
/// </summary>
public partial class PhaseLabel : Label, IBindable<GameSession>
{
    IDisposable? subscription;

    public void Bind(GameSession session)
    {
        var presentation = this.DependOn<PresentationContext>();
        var player1 = session.Game.Profiles.First();
        subscription?.Dispose();
        subscription = session.EventBus.Subscribe<PhaseStarted>(started =>
        {
            var text = $"{(ReferenceEquals(started.TurnPlayer, player1) ? "Your" : "Opponent's")} {Describe(started.Phase)}";
            presentation.Present(() => Text = text);
        });
    }

    public override void _ExitTree()
    {
        subscription?.Dispose();
        subscription = null;
    }

    // "YuGiOhMainPhase1" -> "Main Phase 1": the type's name in words, without the game's prefix
    private static string Describe(IPhase phase)
    {
        var words = Regex.Replace(phase.GetType().Name, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Za-z])(?=[0-9])", " ");
        return Regex.Replace(words, "^(Yu Gi Oh|Vanguard) ", "");
    }
}
