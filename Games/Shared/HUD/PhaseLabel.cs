using System;
using System.Text.RegularExpressions;
using ArC.CardGames;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using Godot;

/// <summary>
/// Shows which phase it is, for any game: it binds a GameSession (found by ArcDriver.BindAll) and shows each
/// PhaseStarted in step with the board (through the PresentationContext an ancestor provides). Whose turn it is:
/// <see cref="TurnLabel"/>, or put one of these in each play area scene with <see cref="OnlyOnItsAreasTurn"/>, so
/// the phase shows on the side of whoever is playing.
/// </summary>
public partial class PhaseLabel : Label, IBindable<GameSession>
{
    /// <summary>
    /// Inside a play area scene: shown only while it's that play area's player's turn (found through the ArcDriver an
    /// ancestor provides), hidden otherwise.
    /// </summary>
    [Export] public bool OnlyOnItsAreasTurn { get; set; }

    IDisposable? subscription;

    public void Bind(GameSession session)
    {
        var presentation = this.DependOn<PresentationContext>();
        var owner = OnlyOnItsAreasTurn ? OwnerOfItsArea(session) : null;
        if(owner is not null) Visible = false;

        subscription?.Dispose();
        subscription = session.EventBus.Subscribe<PhaseStarted>(started =>
        {
            var text = Describe(started.Phase);
            var shown = owner is null || ReferenceEquals(started.TurnPlayer, owner);
            presentation.Present(() =>
            {
                Text = text;
                Visible = shown;
            });
        });
    }

    // The player of the play area this label is in: player 1 plays the Player side (the profiles are in seat order)
    private PlayerProfile OwnerOfItsArea(GameSession session)
    {
        var driver = this.DependOn<ArcDriver>();
        var area = driver.AreaHolding(this)
            ?? throw new InvalidOperationException($"{GetPath()}: {nameof(OnlyOnItsAreasTurn)} needs the label inside a play area scene");
        var profiles = session.Game.Profiles;
        return driver.SideOf(area) == BoardSide.Player ? profiles[0] : profiles[1];
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
