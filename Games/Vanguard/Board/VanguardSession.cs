using System;
using System.Threading.Tasks;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Bots;
using Godot;
using static ArC.CardGames.Vanguard.Cards.VanguardCardsFactory;

public enum OpponentType
{
    RULE_BOT_ROYAL_PALADIN_TRIAL_DECK,
    RULE_BOT_KAGERO_TRIAL_DECK,
    RULE_BOT_OTT_TRIAL_DECK,
    RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK,
    AI
}

public partial class VanguardSession : Control
{
    OpponentType Opponent;
    VanguardDeckBuilder PlayerDeck = null!;
    VanguardBotDifficulty Difficulty = VanguardBotDifficulty.MASTER;

    VanguardInputProvider InputProviderComponent = null!;
    VanguardBoard Board => InputProviderComponent.Board;
    
    PanelContainer HUD = null!;
    GameOverDialog GameOverDialog = null!;

    public void Initialize(VanguardDeckBuilder playerDeck, OpponentType opponent)
    {
        PlayerDeck = playerDeck;
        Opponent = opponent;
    }

    public async override void _Ready()
    {
        HUD = GetNode<PanelContainer>($"%{nameof(HUD)}");
        GameOverDialog = GetNode<GameOverDialog>($"%{nameof(GameOverDialog)}");
        GameOverDialog.Initialize(PlayerDeck);
        InputProviderComponent = GetNode<VanguardInputProvider>($"%{nameof(InputProviderComponent)}");

        VanguardPlayerProfile player2 = Opponent switch
        {
            OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK => new(RoyalPaladinTrialDeckBot.Deck),
            OpponentType.RULE_BOT_KAGERO_TRIAL_DECK => new(KageroTrialDeckBot.Deck),
            OpponentType.RULE_BOT_OTT_TRIAL_DECK => new(OracleThinkTankTrialDeckBot.Deck),
            OpponentType.RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK => new(NovaGrapplerTrialDeckBot.Deck),
            OpponentType.AI => new(VanguardDeckFactory.RoyalPaladinTrialDeck),
            _ => throw new NotSupportedException("Invalid Opponent")
        };

        var (session, services) = VanguardSessionBuilder.Create()
            .Player1(new(PlayerDeck), services =>
            {
                InputProviderComponent.Activate(services.Game.Board.Player1Area, services.Game.Board.Player2Area, services.SkillService, services.GameContext);
                return InputProviderComponent;
            })
            .Player2(player2, services => VanguardOpponents.Create(Opponent, Difficulty, services))
            .WithLogger(new GodotLogger { Enabled = true })
            .Build();

        InputProviderComponent.SetEventBus(services.EventBus);

        await StartGame(session, services.DataQuery);
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