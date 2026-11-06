using System;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Bots;
using ArC.VanguardAI.InputProviders;
using ArC.VanguardAI.IO;
using ArC.VanguardAI.Models;
using Godot;

// How player 2 is played for each opponent type, given the game's services (VanguardSessionBuilder.Player2).
// The session builder creates it once, the first time the game asks: bots and the AI keep their state for the whole game.
public static class VanguardOpponents
{
    const string AIModelPath = "res://Scripts/VanguardAI/CurrentAI.onnx";

    public static IVanguardPlayerInputProvider Create(OpponentType opponent, VanguardBotDifficulty difficulty, VanguardServices services) => opponent switch
    {
        OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK => new RoyalPaladinTrialDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, difficulty),
        OpponentType.RULE_BOT_KAGERO_TRIAL_DECK => new KageroTrialDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, difficulty),
        OpponentType.RULE_BOT_OTT_TRIAL_DECK => new OracleThinkTankTrialDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, difficulty),
        OpponentType.RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK => new NovaGrapplerTrialDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, difficulty),
        OpponentType.AI => CreateAI(services),
        _ => throw new NotSupportedException($"{opponent} is not a supported opponent"),
    };

    static IVanguardPlayerInputProvider CreateAI(VanguardServices services)
    {
        using var file = FileAccess.Open(AIModelPath, FileAccess.ModeFlags.Read);
        // Safe as long as the model is under 2GB
        byte[] buffer = file.GetBuffer((long)file.GetLength());

        return new VanguardAIInputProvider(VanguardAITuningSettings.GameplayTuning,
            new VanguardAIGameStateEncoder(services.DataQuery),
            new VanguardAIDecisionContextEncoder(services.DataQuery),
            new VanguardAIOptionEncoder(services.DataQuery),
            new VanguardAIModel(buffer),
            services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, services.Logger);
    }
}
