using System;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Bots;
using ArC.CardGames.Setup;

public enum RuleBotDeck
{
    ROYAL_PALADIN_TRIAL,
    KAGERO_TRIAL
}

public class RuleBotInputProviderFactory : IPlayerInputProviderFactory
{
    VanguardGame Game;
    InputProvider InputProviderComponent;
    GameContext GameContext;
    
    VanguardGameDataQuery DataQuery;
    VanguardSkillService SkillService;
    RuleBotDeck RuleBot;
    VanguardBotDifficulty BotDifficulty;

    public RuleBotInputProviderFactory(VanguardGame game, RuleBotDeck ruleBot, VanguardBotDifficulty botDifficulty, VanguardGameDataQuery dataQuery,
        InputProvider inputProviderComponent, GameContext gameContext, VanguardSkillService skillService)
    {
        Game = game;
        InputProviderComponent = inputProviderComponent;
        GameContext = gameContext;
        SkillService = skillService;
        DataQuery = dataQuery;
        RuleBot = ruleBot;
        BotDifficulty = botDifficulty;
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
            switch (RuleBot)
            {
                case RuleBotDeck.ROYAL_PALADIN_TRIAL:
                    return new RoyalPaladinTrialDeckBot(Game.Player2, DataQuery, SkillService, GameContext, BotDifficulty);
                case RuleBotDeck.KAGERO_TRIAL:
                    return new KageroTrialDeckBot(Game.Player2, DataQuery, SkillService, GameContext, BotDifficulty);
                default:
                    throw new NotSupportedException("Rule Bot is not supported");
            }
        }
    }
}
