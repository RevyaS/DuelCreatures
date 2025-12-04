using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Setup;

public class InputProviderFactory(VanguardGame game, InputProvider inputProviderComponent, GameContext gameContext) : IPlayerInputProviderFactory
{
    public IPlayerInputProvider GetInputProvider(PlayerProfile playerProfile)
    {
        if(game.Player1 == playerProfile)
        {
            inputProviderComponent.Setup(game.Board.Player1Area, gameContext);
            return inputProviderComponent;
        } else
        {
            return new AIInputProvider(game.Board.Player2Area, gameContext);
        }
    }
}
