using System.Threading.Tasks;
using ArC.CardGames.Bots;
using ArC.CardGames.YuGiOh;
using Godot;

public partial class YuGiOhSession : Control
{
    YuGiOhInputProvider InputProviderComponent = null!;

    // Set by the main screen before the scene enters the tree; the defaults are for running the scene alone (F6)
    YuGiOhDeckBuilder playerDeck = YuGiOhDeckFactory.ExosisterTacticalTryDeck;
    YuGiOhOpponentType opponent = YuGiOhOpponentType.ExosisterTacticalTryDeckBot;

    [Export] public BotDifficulty Difficulty { get; set; } = BotDifficulty.MASTER;

    public void Initialize(YuGiOhDeckBuilder playerDeck, YuGiOhOpponentType opponent)
    {
        this.playerDeck = playerDeck;
        this.opponent = opponent;
    }

    public async override void _Ready()
    {
        InputProviderComponent = GetNode<YuGiOhInputProvider>($"%{nameof(InputProviderComponent)}");

        var (session, services) = YuGiOhSessionBuilder.Create()
            .Player1(new(playerDeck), services =>
            {
                InputProviderComponent.Activate(services.Game.Board.Player1Area, services.Game.Board.Player2Area);
                return InputProviderComponent;
            })
            .Player2(new(YuGiOhOpponents.DeckOf(opponent)), services => YuGiOhOpponents.Create(opponent, Difficulty, services))
            .WithLogger(new GodotLogger())
            .Build();

        InputProviderComponent.SetEventBus(services.EventBus);

        await StartGame(session);
    }

    private async Task StartGame(YuGiOhGameSession session)
    {
        session.StartGame();
        // After StartGame: the game's board and decks exist from here on
        InputProviderComponent.Show(session, DataManager.Get(this).BoardSleevesFor<YuGiOhCard>());

        if(!await SessionLoop.Run(session, InputProviderComponent.Presenter)) return;

        var playerWins = session.Winner() == session.Game.Player1;
        GD.Print(playerWins ? "You won" : "You lose");
        GD.Print("Result ", session.Actions.End.Result);
    }
}
