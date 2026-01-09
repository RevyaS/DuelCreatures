using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Setup;
using Godot;
using static ArC.CardGames.Predefined.Vanguard.Cards.DuelMaidensCardsFactory;

public partial class Session : Control
{
    InputProvider InputProviderComponent = null!;
    DuelCreaturesBoard Board => InputProviderComponent.Board;

    DeckBuilder TrialDeck => DeckBuilder.Create()
        .AddCards(RoyalPaladin.Flogal, 4)
        .AddCards(RoyalPaladin.WeaponsDealerGovannon, 4)
        .AddCards(RoyalPaladin.YggdrasilMaidenElaine, 4)
        .AddCards(RoyalPaladin.BringerOfGoodLuckEpona, 4)
        .AddCards(RoyalPaladin.KnightOfRoseMorgana, 4)
        .AddCards(RoyalPaladin.StarlightUnicorn, 4)
        .AddCards(RoyalPaladin.Wingal, 2)
        .AddCards(RoyalPaladin.LittleSageMarron, 4)
        .AddCards(RoyalPaladin.CovenantKnightRandolf, 4)
        .AddCards(RoyalPaladin.KnightOfTheHarpTristan, 3)
        .AddCards(RoyalPaladin.BlasterBlade, 1)
        .AddCards(RoyalPaladin.KnightOfSilenceGallatin, 4)
        .AddCards(RoyalPaladin.SolitaryKnightGancelot, 2)
        .AddCards(RoyalPaladin.KnightOfConvictionBors, 1)
        .AddCards(RoyalPaladin.CrimsonButterflyBrigitte, 4);
    
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

        var player1 = new VanguardPlayerProfile(
            TrialDeck, RoyalPaladin.StardustTrumpeteer);

        var player2 = new VanguardPlayerProfile(
            TrialDeck, RoyalPaladin.StardustTrumpeteer);

        var game = new VanguardGame(player1, player2, eventBus, effectService);
        var skillService = new VanguardSkillService(eventBus, gameContext);
        var logger = new GodotLogger();
        logger.Enabled = true;
        var inputProviderFactory = new InputProviderFactory(game, InputProviderComponent, gameContext, skillService, logger);
        
        var session = new VanguardGameSession(game, inputProviderFactory, eventBus, effectService, skillService, gameContext, logger);

        await StartGame(session);
    }

    private async Task StartGame(VanguardGameSession session)
    {
        session.StartGame();
        Board.ApplySession(session);

        #region DEBUG
        // session.Game.Board.Player1Area.Vanguard.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player1Area.FrontLeft.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player1Area.FrontRight.Assign(RoyalPaladin.KnightOfRoseMorgana);
        // session.Game.Board.Player2Area.Vanguard.Assign(RoyalPaladin.KnightOfTheHarpTristan);
        // session.Game.Board.Player2Area.FrontLeft.Assign(RoyalPaladin.KnightOfTheHarpTristan);
        // session.Game.Board.Player2Area.FrontRight.Assign(RoyalPaladin.KnightOfTheHarpTristan);

        // session.Game.Board.Player1Area.Soul.AddCardsOnTop([
        //     RoyalPaladin.BlasterBlade
        // ]);
        // session.Game.Board.Player1Area.DamageZone.AddCardsOnTop([
        //     RoyalPaladin.SailorGuardianMichiru,
        //     RoyalPaladin.BlasterBlade,
        // ]);
        // session.Game.Board.Player1Area.Hand.AddCard(RoyalPaladin.BlasterBlade);
        // session.Game.Board.Player1Area.Hand.AddCard(RoyalPaladin.Wingal);
        // session.Game.Board.Player2Area.FrontLeft.Assign(RoyalPaladin.KnightOfSilenceGallatin);
        #endregion
        
        while(!session.IsGameOver())
        {
            GD.Print($"Applying session {session.CurrentPhase}");
            await session.ExecutePhase();
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