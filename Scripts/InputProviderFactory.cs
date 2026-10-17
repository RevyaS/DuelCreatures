using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Flow;
using ArC.CardGames.Setup;
using ArC.VanguardAI.InputProviders;
using ArC.VanguardAI.IO;
using ArC.VanguardAI.Models;
using Godot;

public class InputProviderFactory : IPlayerInputProviderFactory
{
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
    public IPlayerInputProvider GetInputProvider(PlayerProfile playerProfile)
    {
        if(ReferenceEquals(Game.Player1, playerProfile))
        {
            InputProviderComponent.Activate(Game.Board.Player1Area, Game.Board.Player2Area, SkillService, GameContext);
            return InputProviderComponent;
        } else
        {
            InputProviderComponent.Deactivate();
            return new VanguardAIInputProvider(VanguardAITuningSettings.GameplayTuning, 
                GameStateEncoder, DecisionContextEncoder, OptionEncoder, Model,
                Game.Player2, DataQuery, SkillService, GameContext, Logger);
        }
    }
}
