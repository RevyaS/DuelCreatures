using System;
using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Predefined.Vanguard.Bots;
using ArC.CardGames.Setup;
using ArC.VanguardAI.IO;
using Godot;
using static ArC.CardGames.Predefined.Vanguard.Cards.VanguardCardsFactory;

public enum OpponentType
{
    RULE_BOT_ROYAL_PALADIN_TRIAL_DECK,
    AI
}

public enum PlayerDeck
{
    ROYAL_PALADIN_TRIAL_DECK,
    KAGERO_TRIAL_DECK
}

public partial class Session : Control
{
    OpponentType Opponent = OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK;
    PlayerDeck PlayerDeck = PlayerDeck.KAGERO_TRIAL_DECK;
    VanguardBotDifficulty Difficulty = VanguardBotDifficulty.MASTER;

    InputProvider InputProviderComponent = null!;
    DuelCreaturesBoard Board => InputProviderComponent.Board;

    DeckBuilder TestDeck => DeckBuilder.Create()
        .AddCards(RoyalPaladin.ArdentJewelKnightPolli, 4)
        .AddCards(RoyalPaladin.BringerOfGoodLuckEpona, 4)
        .AddCards(RoyalPaladin.Flogal, 4)
        .AddCards(RoyalPaladin.DevotingJewelKnightTabitha, 4)
        .AddCards(RoyalPaladin.StarlightUnicorn, 4)
        .AddCards(RoyalPaladin.Wingal, 4)
        .AddCards(RoyalPaladin.KnightOfRoseMorgana, 4)
        .AddCards(RoyalPaladin.LittleSageMarron, 4)
        .AddCards(RoyalPaladin.BlasterBlade, 4)
        .AddCards(RoyalPaladin.KnightOfSilenceGallatin, 4)
        .AddCards(RoyalPaladin.KnightOfTheHarpTristan, 4)
        .AddCards(RoyalPaladin.KingOfKnightsAlfred, 4) 
        .AddCards(RoyalPaladin.CrimsonButterflyBrigitte, 4)
        .AddCards(RoyalPaladin.SolitaryKnightGancelot, 4);

    public async override void _Ready()
    {
        var eventBus = new VanguardEventBus();
        var effectService = new VanguardEffectService(eventBus);
        var gameContext = new GameContext();

        InputProviderComponent = GetNode<InputProvider>($"%{nameof(InputProviderComponent)}");
        InputProviderComponent.SetEventBus(eventBus);

        var aiDeck = DeckFactory.RoyalPaladinTrialDeck;

        var player1 = PlayerDeck switch
        {
            PlayerDeck.ROYAL_PALADIN_TRIAL_DECK => new VanguardPlayerProfile(DeckFactory.RoyalPaladinTrialDeck, RoyalPaladin.StardustTrumpeteer),
            PlayerDeck.KAGERO_TRIAL_DECK => new VanguardPlayerProfile(DeckFactory.KageroTrialDeck, Kagero.LizardRunnerUndeux),
            _ => throw new NotSupportedException("Invalid Deck")
        };

        VanguardPlayerProfile player2 = Opponent switch
        {
            OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK => new(RoyalPaladinTrialDeckBot.Deck, RoyalPaladinTrialDeckBot.StartingVanguard),
            OpponentType.AI => new(aiDeck, RoyalPaladin.StardustTrumpeteer),
            _ => throw new NotSupportedException("Invalid Opponent")
        };

        var dataQuery = new VanguardGameDataQuery();
        var game = new VanguardGame(player1, player2, dataQuery, eventBus, effectService);
        var skillService = new VanguardSkillService(eventBus, gameContext);
        var logger = new GodotLogger();
        logger.Enabled = true;

        VanguardAIGameStateEncoder gameStateEncoder = new(dataQuery);
        VanguardAIDecisionContextEncoder decisionContextEncoder = new(dataQuery);
        VanguardAIOptionEncoder optionEncoder = new(dataQuery);

        IPlayerInputProviderFactory inputProviderFactory = Opponent switch
        {
            OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK => new RuleBotInputProviderFactory(game, RuleBotDeck.ROYAL_PALADIN_TRIAL, VanguardBotDifficulty.MASTER, dataQuery, InputProviderComponent, gameContext, skillService),
            OpponentType.AI => new InputProviderFactory(game, dataQuery, gameStateEncoder, decisionContextEncoder, optionEncoder, InputProviderComponent, gameContext, skillService, logger),
            _ => throw new NotSupportedException("Invalid Opponent")
        };
        
        var session = new VanguardGameSession(game, inputProviderFactory, eventBus, effectService, skillService, gameContext, logger);
        dataQuery.Init(session);

        await StartGame(session);
    }

    private async Task StartGame(VanguardGameSession session)
    {
        session.StartGame();
        Board.ApplySession(session, InputProviderComponent.Presenter);

        #region DEBUG
        // session.Game.Board.Player1Area.Vanguard.Assign(RoyalPaladin.LittleSageMarron);

        // session.Game.Board.Player1Area.Vanguard.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player1Area.FrontLeft.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player1Area.FrontRight.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player2Area.Vanguard.Assign(RoyalPaladin.KnightOfTheHarpTristan);
        // session.Game.Board.Player2Area.BackLeft.Assign(RoyalPaladin.LittleSageMarron);
        // session.Game.Board.Player2Area.FrontRight.Assign(RoyalPaladin.KnightOfTheHarpTristan);

        // session.Game.Board.Player1Area.Soul.AddCardsOnTop([
        //     RoyalPaladin.BlasterBlade
        // ]);
        // session.Game.Board.Player1Area.DamageZone.AddCardsOnTop([
        //     RoyalPaladin.SailorGuardianMichiru,
        //     RoyalPaladin.BlasterBlade,
        // ]);
        // session.Game.Board.Player1Area.Hand.AddCard(Kagero.WyvernStrikeTejas);
        // session.Game.Board.Player1Area.Hand.AddCard(RoyalPaladin.Wingal);
        // session.Game.Board.Player2Area.FrontLeft.Assign(RoyalPaladin.KnightOfSilenceGallatin);
        #endregion
        
        while(!session.IsGameOver())
        {
            GD.Print($"Applying session {session.CurrentPhase}");
            await session.ExecutePhase();
            await InputProviderComponent.Presenter.WhenIdle();
        }

        if(session.Winner() == session.Game.Player1)
        {
            GD.Print("You won");
        } else
        {
            GD.Print("You lose");
        }

        GD.Print("Win Condition ", session.GameOverContext.ToString());
    }
}