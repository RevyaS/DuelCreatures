using System;
using ArC.CardGames.Flow;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Bots;
using ArC.CardGames.Setup;

// One provider per player for the whole game: the bots keep their plans between requests
public class RuleBotInputProviderFactory : TwoPlayerInputProviderFactory<VanguardPlayerProfile, IVanguardPlayerInputProvider>
{
    IVanguardPlayerInputProvider? player1 = null, player2 = null;

    VanguardGame Game;
    InputProvider InputProviderComponent;
    GameContext GameContext;
    
    VanguardGameDataQuery DataQuery;
    VanguardSkillService SkillService;
    OpponentType RuleBot;
    VanguardBotDifficulty BotDifficulty;

    public RuleBotInputProviderFactory(VanguardGame game, OpponentType ruleBot, VanguardBotDifficulty botDifficulty, VanguardGameDataQuery dataQuery,
        InputProvider inputProviderComponent, GameContext gameContext, VanguardSkillService skillService)
        : base(game.Player1, game.Player2)
    {
        Game = game;
        InputProviderComponent = inputProviderComponent;
        GameContext = gameContext;
        SkillService = skillService;
        DataQuery = dataQuery;
        RuleBot = ruleBot;
        BotDifficulty = botDifficulty;
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
        return player2 ??= RuleBot switch
        {
            OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK => new RoyalPaladinTrialDeckBot(Game.Player2, DataQuery, SkillService, GameContext, BotDifficulty),
            OpponentType.RULE_BOT_KAGERO_TRIAL_DECK => new KageroTrialDeckBot(Game.Player2, DataQuery, SkillService, GameContext, BotDifficulty),
            OpponentType.RULE_BOT_OTT_TRIAL_DECK => new OracleThinkTankTrialDeckBot(Game.Player2, DataQuery, SkillService, GameContext, BotDifficulty),
            OpponentType.RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK => new NovaGrapplerTrialDeckBot(Game.Player2, DataQuery, SkillService, GameContext, BotDifficulty),
            _ => throw new NotSupportedException("Rule Bot is not supported"),
        };
    }
}
