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
    RULE_BOT_KAGERO_TRIAL_DECK,
    RULE_BOT_OTT_TRIAL_DECK,
    RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK,
    AI
}

public partial class Session : Control
{
    OpponentType Opponent;
    VanguardDeckBuilder PlayerDeck = null!;
    VanguardBotDifficulty Difficulty = VanguardBotDifficulty.MASTER;

    InputProvider InputProviderComponent = null!;
    DuelCreaturesBoard Board => InputProviderComponent.Board;
    
    PanelContainer HUD = null!;
    GameOverDialog GameOverDialog = null!;

    VanguardDeckBuilder TestDeck => VanguardDeckBuilder.Create()
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
        .AddCards(RoyalPaladin.SolitaryKnightGancelot, 4)
        .SetStartingVanguard(RoyalPaladin.StardustTrumpeteer);

    public void Initialize(VanguardDeckBuilder playerDeck, OpponentType opponent)
    {
        PlayerDeck = playerDeck;
        Opponent = opponent;
    }

    public async override void _Ready()
    {
        var eventBus = new VanguardEventBus();
        var effectService = new VanguardEffectService(eventBus);
        var gameContext = new GameContext();

        HUD = GetNode<PanelContainer>($"%{nameof(HUD)}");
        GameOverDialog = GetNode<GameOverDialog>($"%{nameof(GameOverDialog)}");
        GameOverDialog.Initialize(PlayerDeck);
        InputProviderComponent = GetNode<InputProvider>($"%{nameof(InputProviderComponent)}");
        InputProviderComponent.SetEventBus(eventBus);

        var aiDeck = DeckFactory.RoyalPaladinTrialDeck;

        var player1 = new VanguardPlayerProfile(PlayerDeck);

        VanguardPlayerProfile player2 = Opponent switch
        {
            OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK => new(RoyalPaladinTrialDeckBot.Deck),
            OpponentType.RULE_BOT_KAGERO_TRIAL_DECK => new(KageroTrialDeckBot.Deck),
            OpponentType.RULE_BOT_OTT_TRIAL_DECK => new(OracleThinkTankTrialDeckBot.Deck),
            OpponentType.RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK => new(NovaGrapplerTrialDeckBot.Deck),
            OpponentType.AI => new(aiDeck),
            _ => throw new NotSupportedException("Invalid Opponent")
        };

        var dataQuery = new VanguardGameDataQuery();
        var game = new VanguardGame(player1, player2, dataQuery, eventBus, effectService);
        var skillService = new VanguardSkillService(eventBus, gameContext, dataQuery);
        var logger = new GodotLogger();
        logger.Enabled = true;

        VanguardAIGameStateEncoder gameStateEncoder = new(dataQuery);
        VanguardAIDecisionContextEncoder decisionContextEncoder = new(dataQuery);
        VanguardAIOptionEncoder optionEncoder = new(dataQuery);

        IPlayerInputProviderFactory<IVanguardPlayerInputProvider> inputProviderFactory;
        if(Opponent == OpponentType.AI)
        {
            inputProviderFactory = new InputProviderFactory(game, dataQuery, gameStateEncoder, decisionContextEncoder, optionEncoder, InputProviderComponent, gameContext, skillService, logger);
        } else
        {
            inputProviderFactory = new RuleBotInputProviderFactory(game, Opponent, VanguardBotDifficulty.MASTER, dataQuery, InputProviderComponent, gameContext, skillService);
        }
        
        var session = new VanguardGameSession(game, inputProviderFactory, eventBus, effectService, skillService, gameContext, logger);
        dataQuery.Init(session);

        await StartGame(session, dataQuery);
    }

    private async Task StartGame(VanguardGameSession session, VanguardGameDataQuery dataQuery)
    {
        session.StartGame();
        Board.ApplySession(session, InputProviderComponent.Presenter);

        #region DEBUG
        // session.Game.Board.Player1Area.Vanguard.Assign(RoyalPaladin.LittleSageMarron);

        // session.Game.Board.Player1Area.Vanguard.Assign(NovaGrappler.KingOfSword);
        // session.Game.Board.Player1Area.Hand.AddCard(NovaGrappler.MrInvincible);
        // session.Game.Board.Player1Area.Hand.AddCard(NovaGrappler.MrInvincible);
        // session.Game.Board.Player1Area.FrontLeft.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player1Area.FrontRight.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player2Area.Vanguard.Assign(RoyalPaladin.KnightOfTheHarpTristan);
        // session.Game.Board.Player2Area.BackLeft.Assign(RoyalPaladin.LittleSageMarron);
        // session.Game.Board.Player2Area.BackRight.Assign(RoyalPaladin.LittleSageMarron);
        // session.Game.Board.Player2Area.FrontRight.Assign(RoyalPaladin.KnightOfTheHarpTristan);

        // session.Game.Board.Player1Area.Soul.AddCardsOnTop([
        //     RoyalPaladin.BlasterBlade
        // ]);
        // session.Game.Board.Player1Area.DamageZone.AddCardsOnTop([
        //     RoyalPaladin.SailorGuardianMichiru,
        //     RoyalPaladin.BlasterBlade,
        // ]);
        // session.Game.Board.Player2Area.DamageZone.AddCard(Kagero.BerserkDragon);
        // session.Game.Board.Player2Area.DamageZone.AddCard(Kagero.BerserkDragon);
        // session.Game.Board.Player2Area.DamageZone.AddCard(Kagero.BerserkDragon);
        // session.Game.Board.Player2Area.DamageZone.AddCard(Kagero.BerserkDragon);
        // session.Game.Board.Player2Area.DamageZone.AddCard(Kagero.BerserkDragon);
        // session.Game.Board.Player1Area.Hand.AddCard(Kagero.BerserkDragon);
        // session.Game.Board.Player1Area.Hand.AddCard(Kagero.WyvernStrikeTejas);
        // session.Game.Board.Player1Area.Hand.AddCard(Kagero.DemonicDragonMadonnaJoka);
        // session.Game.Board.Player1Area.Hand.AddCard(RoyalPaladin.Wingal);
        // session.Game.Board.Player2Area.FrontLeft.Assign(RoyalPaladin.KnightOfSilenceGallatin);
        #endregion
        
        while(!session.IsGameOver())
        {
            GD.Print($"Applying session {session.CurrentPhase}");
            await session.ExecutePhase();
            await InputProviderComponent.Presenter.WhenIdle();
        }

        var playerWins = session.Winner() == session.Game.Player1;
        if(playerWins)
        {
            GD.Print("You won");
        } else
        {
            GD.Print("You lose");
        }

        GameOverDialog.Set(playerWins, dataQuery.GetPlayerDamage(session.Game.Player2), session.GameOverContext);
        HUD.Show();

        GD.Print("Win Condition ", session.GameOverContext.ToString());
    }
}