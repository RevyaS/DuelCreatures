using DuelCreatures.Data;
using ArC.CardGames.Vanguard;
using Godot;

// The Vanguard board. Its zones are bound by name (see ArcDriver): %Player1Area and %Player2Area are
// VanguardPlayAreaView scenes holding one zone node per VanguardPlayArea zone, %GuardZone (the guardian circle) is
// shared. Only the hands sit outside the play areas, so they're bound here.
public partial class VanguardBoardView : Control
{
    // Everything that wants a sound raises it here (see PresentationConfig for which sides play)
    public SfxBus Sfx { get; } = new();

    HandZone PlayerHand = null!;
    HandZone OppHand = null!;
    VanguardPlayAreaView Player1Area = null!;
    VanguardPlayAreaView Player2Area = null!;

    public override void _Ready()
    {
        // Vanguard's moves, sounds and card scene for the shared components
        VanguardPresentationConfig.Register();
        GetNode<SfxPlayer>("%SfxPlayer").Listen(Sfx);

        PlayerHand = GetNode<HandZone>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandZone>($"%{nameof(OppHand)}");
        Player1Area = GetNode<VanguardPlayAreaView>($"%{nameof(Player1Area)}");
        Player2Area = GetNode<VanguardPlayAreaView>($"%{nameof(Player2Area)}");

        // Hands live outside the play areas, so they take their side's sleeve from there
        if(Player1Area.SleeveInfo is { } playerSleeve) PlayerHand.SleeveInfo = playerSleeve;
        if(Player2Area.SleeveInfo is { } oppSleeve) OppHand.SleeveInfo = oppSleeve;
    }

    /// <summary>Shows <paramref name="session"/> on this board (once its game has started, so every zone exists).</summary>
    public ArcDriver Show(VanguardGameSession session, Presenter presenter)
    {
        var board = session.Game.Board;
        var driver = new ArcDriver(session.EventBus, presenter, Sfx);
        driver.Bind(PlayerHand, board.Player1Area.Hand, BoardSide.Player);
        driver.Bind(OppHand, board.Player2Area.Hand, BoardSide.Opponent);
        // The soul is under the vanguard: nothing shows it on the board (it's listed from the vanguard, step 8d)
        driver.BindBoard(this, board, area => [ area.Soul ]);
        driver.ShowVanguardMoves();
        driver.Start();
        return driver;
    }
}
