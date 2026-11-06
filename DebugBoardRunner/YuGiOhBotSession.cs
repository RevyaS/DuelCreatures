using ArC.CardGames.Bots;
using ArC.CardGames.YuGiOh;
using ArC.CardGames.YuGiOh.Bots;
using Godot;

/// <summary>
/// Debug only: two Exosister bots play a whole YuGiOh game on the real board, to watch how it's shown. Nothing asks
/// for input; the board's input provider only shows the game. Run it with F6.
/// </summary>
public partial class YuGiOhBotSession : Control
{
    YuGiOhInputProvider InputProviderComponent = null!;

    public async override void _Ready()
    {
        InputProviderComponent = GetNode<YuGiOhInputProvider>($"%{nameof(InputProviderComponent)}");

        var (session, _) = YuGiOhSessionBuilder.Create()
            .Player1(new(YuGiOhDeckFactory.ExosisterTacticalTryDeck), services =>
                new ExosisterTacticalTryDeckBot(services.Game.Player1, services.DataQuery, services.SkillService, services.GameContext, BotDifficulty.MASTER))
            .Player2(new(YuGiOhDeckFactory.ExosisterTacticalTryDeck), services =>
                new ExosisterTacticalTryDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, BotDifficulty.MASTER))
            .WithLogger(new GodotLogger())
            .Build();

        session.StartGame();
        InputProviderComponent.Show(session);

        if(await SessionLoop.Run(session, InputProviderComponent.Presenter))
        {
            GD.Print($"Bot game over: {(session.Winner() == session.Game.Player1 ? "player 1" : session.Winner() is null ? "nobody" : "player 2")} won ({session.GameOverContext}), turn {session.TurnCount}");
        }
    }
}
