using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Flow;
using ArC.CardGames.Setup;
using ArC.VanguardAI.InputProviders;
using ArC.VanguardAI.Models;
using Godot;

public class InputProviderFactory : IPlayerInputProviderFactory
{
    VanguardGame Game;
    InputProvider InputProviderComponent;
    GameContext GameContext;
    
    WeightsBiasData WeightsBias;
    VanguardSkillService SkillService;
    IVanguardLogger Logger;

    public InputProviderFactory(VanguardGame game, InputProvider inputProviderComponent, GameContext gameContext, VanguardSkillService skillService, IVanguardLogger logger)
    {
        Game = game;
        InputProviderComponent = inputProviderComponent;
        GameContext = gameContext;
        SkillService = skillService;
        Logger = logger;

        WeightsBias = WeightsBiasData.LoadFromFile("/media/rev/DATA/Users/Files/Works/Revya/() H/Duel Maidens/Arc Engine based/DuelCreatures/Scripts/VanguardAI/CurrentAI.bin");
    }
    public IPlayerInputProvider GetInputProvider(PlayerProfile playerProfile)
    {
        if(ReferenceEquals(Game.Player1, playerProfile))
        {
            InputProviderComponent.Setup(Game.Board.Player1Area, Game.Board.Player2Area, GameContext);
            return InputProviderComponent;
        } else
        {
            return new VanguardAIInputProvider(WeightsBias, Game.Board.Player2Area, Game.Board.Player1Area, SkillService, GameContext, Logger);
        }
    }
}
