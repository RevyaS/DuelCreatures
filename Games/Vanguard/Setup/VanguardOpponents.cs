using System;
using ArC.CardGames.Bots;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Bots;

// How player 2 is played for each opponent type, given the game's services (VanguardSessionBuilder.Player2).
// The session builder creates it once, the first time the game asks: bots and the AI keep their state for the whole game.
public static class VanguardOpponents
{
    const string AIModelPath = "res://Scripts/VanguardAI/CurrentAI.onnx";

    public static IVanguardPlayerInputProvider Create(OpponentType opponent, BotDifficulty difficulty, VanguardServices services) => opponent switch
    {
        OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK => new RoyalPaladinTrialDeckBot(services.Game.Player2, services.DataQuery, difficulty),
        OpponentType.RULE_BOT_KAGERO_TRIAL_DECK => new KageroTrialDeckBot(services.Game.Player2, services.DataQuery, difficulty),
        OpponentType.RULE_BOT_OTT_TRIAL_DECK => new OracleThinkTankTrialDeckBot(services.Game.Player2, services.DataQuery, difficulty),
        OpponentType.RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK => new NovaGrapplerTrialDeckBot(services.Game.Player2, services.DataQuery, difficulty),
        OpponentType.AI => CreateAI(services),
        _ => throw new NotSupportedException($"{opponent} is not a supported opponent"),
    };

    // ArC.VanguardAI isn't on the rewritten ArC yet (it still plans with the old skill services): the AI opponent is off until it is
    static IVanguardPlayerInputProvider CreateAI(VanguardServices services)
        => throw new NotSupportedException($"The AI opponent ({AIModelPath}) waits for ArC.VanguardAI to move to the rewritten ArC");
}
