using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Flow;
using ArC.CardGames.Setup;
using ArC.VanguardAI.InputProviders;
using ArC.VanguardAI.IO;
using ArC.VanguardAI.Models;
using Godot;

// One provider per player for the whole game: ArC asks for them constantly (every event
// with a reacting skill), and building a new one each time would drop the AI's state
public class InputProviderFactory : TwoPlayerInputProviderFactory<VanguardPlayerProfile, IVanguardPlayerInputProvider>
{
    IVanguardPlayerInputProvider? player1 = null, player2 = null;

    VanguardGame Game;
    InputProvider InputProviderComponent;
    GameContext GameContext;
    
    VanguardGameDataQuery DataQuery;
    VanguardSkillService SkillService;
    IVanguardLogger Logger;
    VanguardAIGameStateEncoder GameStateEncoder; 
    VanguardAIDecisionContextEncoder DecisionContextEncoder; 
    VanguardAIOptionEncoder OptionEncoder;
    VanguardAIModel Model;

    public InputProviderFactory(VanguardGame game, VanguardGameDataQuery dataQuery,
        VanguardAIGameStateEncoder gameStateEncoder, VanguardAIDecisionContextEncoder decisionContextEncoder, VanguardAIOptionEncoder optionEncoder,
        InputProvider inputProviderComponent, GameContext gameContext, VanguardSkillService skillService, IVanguardLogger logger)
        : base(game.Player1, game.Player2)
    {
        Game = game;
        InputProviderComponent = inputProviderComponent;
        GameContext = gameContext;
        SkillService = skillService;
        Logger = logger;
        DataQuery = dataQuery;

        GameStateEncoder = gameStateEncoder;
        DecisionContextEncoder = decisionContextEncoder;
        OptionEncoder = optionEncoder;

        using var file = FileAccess.Open("res://Scripts/VanguardAI/CurrentAI.onnx", Godot.FileAccess.ModeFlags.Read);
        // Read all bytes
        ulong ulongLength = file.GetLength();
        int length = (int)ulongLength; // safe as long as file < 2GB
        byte[] buffer = file.GetBuffer(length);

        Model = new(buffer);
    }
    // Created on first use: the game's board only exists once the game has started
    protected override IVanguardPlayerInputProvider GetPlayer1InputProvider()
    {
        if(player1 is null)
        {
            InputProviderComponent.Activate(Game.Board.Player1Area, Game.Board.Player2Area, SkillService, GameContext);
            player1 = InputProviderComponent;
        }
        return player1;
    }

    protected override IVanguardPlayerInputProvider GetPlayer2InputProvider()
    {
        return player2 ??= new VanguardAIInputProvider(VanguardAITuningSettings.GameplayTuning,
            GameStateEncoder, DecisionContextEncoder, OptionEncoder, Model,
            Game.Player2, DataQuery, SkillService, GameContext, Logger);
    }
}
