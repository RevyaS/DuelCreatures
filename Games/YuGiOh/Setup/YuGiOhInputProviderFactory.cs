using ArC.CardGames.Setup;
using ArC.CardGames.YuGiOh;

public class YuGiOhInputProviderFactory : TwoPlayerInputProviderFactory<YuGiOhPlayerProfile, IYuGiOhPlayerInputProvider>
{
    IYuGiOhPlayerInputProvider? player1 = null, player2 = null;
    YuGiOhInputProvider InputProviderComponent;
    public YuGiOhInputProviderFactory(YuGiOhGame game, YuGiOhInputProvider inputProviderComponent)
        : base(game.Player1, game.Player2)
    {
        InputProviderComponent = inputProviderComponent;
    }
    // Created on first use: the game's board only exists once the game has started
    protected override IYuGiOhPlayerInputProvider GetPlayer1InputProvider()
    {
        if(player1 is null)
        {
            // InputProviderComponent.Activate(Game.Board.Player1Area, Game.Board.Player2Area, SkillService, GameContext);
            player1 = InputProviderComponent;
        }
        return player1;
    }

    protected override IYuGiOhPlayerInputProvider GetPlayer2InputProvider()
    {
        return player2 ??= new ExosisterTacticalTryDeckBot();
    }
}
