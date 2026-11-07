using System;
using ArC.CardGames.Rules;
using ArC.CardGames.Vanguard;
using Godot;

// The Vanguard board. Its zones are bound by name (see ArcDriver): %Player1Area and %Player2Area are
// VanguardPlayAreaView scenes holding one zone node per VanguardPlayArea zone, %GuardZone (the guardian circle) is
// shared. Only the hands sit outside the play areas, so they're bound here. Its components (zone extensions like
// UnitStats, and session components like the phase label and VanguardBattleLines) find the session, the board's
// presentation and the driver through it.
public partial class VanguardBoardView : Control, IProvide<VanguardGameSession>, IProvide<PresentationContext>, IProvide<ArcDriver>
{
    // Everything that wants a sound raises it here (see PresentationConfig for which sides play)
    public SfxBus Sfx { get; } = new();

    HandZone PlayerHand = null!;
    HandZone OppHand = null!;
    VanguardPlayAreaView Player1Area = null!;
    VanguardPlayAreaView Player2Area = null!;

    /// <summary>The player's play area scene (e.g. where the input hint is: under its phase label).</summary>
    public VanguardPlayAreaView PlayerArea => Player1Area;

    VanguardGameSession? session;
    PresentationContext? presentation;
    ArcDriver? driver;

    VanguardGameSession IProvide<VanguardGameSession>.Value() => session ?? throw NotShown<VanguardGameSession>();
    PresentationContext IProvide<PresentationContext>.Value() => presentation ?? throw NotShown<PresentationContext>();
    ArcDriver IProvide<ArcDriver>.Value() => driver ?? throw NotShown<ArcDriver>();

    private InvalidOperationException NotShown<T>() => new($"{Name} provides a {typeof(T).Name} once it shows a game ({nameof(Show)})");

    public override void _Ready()
    {
        // Vanguard's moves, sounds and card scene for the shared components
        VanguardPresentationConfig.Register();
        GetNode<SfxPlayer>("%SfxPlayer").Listen(Sfx);

        PlayerHand = GetNode<HandZone>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandZone>($"%{nameof(OppHand)}");
        Player1Area = GetNode<VanguardPlayAreaView>($"%{nameof(Player1Area)}");
        Player2Area = GetNode<VanguardPlayAreaView>($"%{nameof(Player2Area)}");

    }

    /// <summary>Shows <paramref name="session"/> on this board (once its game has started, so every zone exists), face-down cards in <paramref name="sleeves"/>.</summary>
    public ArcDriver Show(VanguardGameSession session, Presenter presenter, BoardSleeves sleeves)
    {
        if(driver is not null) throw new InvalidOperationException($"{Name} already shows a game");
        this.session = session;
        presentation = new(presenter, Sfx, BoardSide.Both);

        var board = session.Game.Board;
        driver = new ArcDriver(session.EventBus, presenter, Sfx);
        driver.Bind(PlayerHand, board.Player1Area.Hand, BoardSide.Player);
        driver.Bind(OppHand, board.Player2Area.Hand, BoardSide.Opponent);
        // The soul is under the vanguard: nothing shows it on the board (it's listed from the vanguard, step 8d)
        driver.BindBoard(this, board, area => [ area.Soul ]);
        // The phase and turn labels, the battle lines
        driver.BindAll<GameSession>(this, session);
        driver.ShowVanguardMoves();
        driver.UseSleeves(sleeves);
        driver.Start();
        return driver;
    }
}
