using ArC.CardGames.YuGiOh;
using Godot;

// The YuGiOh board. Its zones are bound by name (see ArcDriver): %Player1Area and %Player2Area hold one zone node per
// YuGiOhPlayArea zone, %ExtraMonsterZone1/2 are shared. Only the hands sit outside the play areas, so they're bound here.
public partial class YuGiOhBoard : MarginContainer
{
    // Everything that wants a sound raises it here (see PresentationConfig for which sides play)
    public SfxBus Sfx { get; } = new();

    HandZone PlayerHand = null!;
    HandZone OppHand = null!;

    public override void _Ready()
    {
        PlayerHand = GetNode<HandZone>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandZone>($"%{nameof(OppHand)}");
    }

    /// <summary>Shows <paramref name="session"/> on this board (once its game has started, so every zone exists).</summary>
    public ArcDriver Show(YuGiOhGameSession session, Presenter presenter)
    {
        var board = session.Game.Board;
        var driver = new ArcDriver(session.EventBus, presenter, Sfx);
        driver.Bind(PlayerHand, board.Player1Area.Hand, BoardSide.Player);
        driver.Bind(OppHand, board.Player2Area.Hand, BoardSide.Opponent);
        // Cards waiting to become XYZ materials aren't shown anywhere
        driver.BindBoard(this, board, area => [ area.PendingMaterials ]);
        driver.Start();
        return driver;
    }
}
