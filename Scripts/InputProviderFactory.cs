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

        using var file = FileAccess.Open("res://Scripts/VanguardAI/CurrentAI.bin", Godot.FileAccess.ModeFlags.Read);
        // Read all bytes
        ulong ulongLength = file.GetLength();
        int length = (int)ulongLength; // safe as long as file < 2GB
        byte[] buffer = file.GetBuffer(length);

        // Wrap in a MemoryStream
        var memoryStream = new System.IO.MemoryStream(buffer);
        WeightsBias = WeightsBiasData.Load(memoryStream);
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
            return new VanguardAIInputProvider(WeightsBias, Game.Board.Player2Area, Game.Board.Player1Area, SkillService, GameContext, Logger);
        }
    }
}
