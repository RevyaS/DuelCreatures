using ArC.CardGames.Flow;
using ArC.CardGames.Setup;
using ArC.CardGames.YuGiOh;
using ArC.CardGames.YuGiOh.Bots;

public class YuGiOhInputProviderFactory : TwoPlayerInputProviderFactory<YuGiOhPlayerProfile, IYuGiOhPlayerInputProvider>
{
    IYuGiOhPlayerInputProvider? player1 = null, player2 = null;
    YuGiOhInputProvider InputProviderComponent;
    YuGiOhGame Game;
    YuGiOhGameDataQuery DataQuery;
    YuGiOhSkillService SkillService;
    GameContext GameContext;
    public YuGiOhInputProviderFactory(YuGiOhGame game, YuGiOhGameDataQuery dataQuery, 
        YuGiOhInputProvider inputProviderComponent, GameContext gameContext, YuGiOhSkillService skillService)
        : base(game.Player1, game.Player2)
    {
        InputProviderComponent = inputProviderComponent;
        Game = game;
        DataQuery = dataQuery;
        SkillService = skillService;
        GameContext = gameContext;
    }
    // Created on first use: the game's board only exists once the game has started
    protected override IYuGiOhPlayerInputProvider GetPlayer1InputProvider()
    {
        if(player1 is null)
        {
            InputProviderComponent.Activate(Game.Board.Player1Area, Game.Board.Player2Area, SkillService);
            player1 = InputProviderComponent;
        }
        return player1;
    }

    protected override IYuGiOhPlayerInputProvider GetPlayer2InputProvider()
    {
        return player2 ??= new ExosisterTacticalTryDeckBot(Game.Player2, DataQuery, SkillService, GameContext, ArC.CardGames.Bots.BotDifficulty.MASTER);
    }
}
