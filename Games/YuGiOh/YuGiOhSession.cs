using System.Threading.Tasks;
using ArC.CardGames.Bots;
using ArC.CardGames.YuGiOh;
using ArC.CardGames.YuGiOh.Bots;
using Godot;

public partial class YuGiOhSession : Control
{
    YuGiOhInputProvider InputProviderComponent = null!;

    public async override void _Ready()
    {
        InputProviderComponent = GetNode<YuGiOhInputProvider>($"%{nameof(InputProviderComponent)}");

        var (session, services) = YuGiOhSessionBuilder.Create()
            .Player1(new(YuGiOhDeckFactory.ExosisterTacticalTryDeck), services =>
            {
                InputProviderComponent.Activate(services.Game.Board.Player1Area, services.Game.Board.Player2Area, services.SkillService);
                return InputProviderComponent;
            })
            .Player2(new(YuGiOhDeckFactory.ExosisterTacticalTryDeck), services =>
                new ExosisterTacticalTryDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, BotDifficulty.MASTER))
            .WithLogger(new GodotLogger())
            .Build();

        InputProviderComponent.SetEventBus(services.EventBus);

        await StartGame(session);
    }

    private async Task StartGame(YuGiOhGameSession session)
    {
        session.StartGame();
        // After StartGame: the game's board and decks exist from here on
        InputProviderComponent.Show(session);

        if(!await SessionLoop.Run(session, InputProviderComponent.Presenter)) return;

        var playerWins = session.Winner() == session.Game.Player1;
        GD.Print(playerWins ? "You won" : "You lose");
        GD.Print("Win Condition ", session.GameOverContext.ToString());
    }
}
