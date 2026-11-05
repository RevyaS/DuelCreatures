using System.Threading.Tasks;
using ArC.CardGames.Flow;
using ArC.CardGames.YuGiOh;
using Godot;

public partial class YuGiOhSession : Control
{
    YuGiOhInputProvider InputProviderComponent = null!;
    YuGiOhBoard Board => InputProviderComponent.Board;

    public async override void _Ready()
    {
        InputProviderComponent = GetNode<YuGiOhInputProvider>($"%{nameof(InputProviderComponent)}");

        var gameContext = new GameContext();
        var eventBus = new YuGiOhEventBus();
        var effectService = new YuGiOhEffectService();
        var player1 = new YuGiOhPlayerProfile(YuGiOhDeckFactory.ExosisterTacticalTryDeck);
        var player2 = new YuGiOhPlayerProfile(YuGiOhDeckFactory.ExosisterTacticalTryDeck);

        InputProviderComponent.SetEventBus(eventBus);
        
        var game = new YuGiOhGame(player1, player2, eventBus, effectService, new GodotLogger());

        YuGiOhGameSession createdSession = null!;
        var dataQuery = new YuGiOhGameDataQuery(() => createdSession);
        var skillService = new YuGiOhSkillService(gameContext, dataQuery, eventBus);
        var inputProviderFactory = new YuGiOhInputProviderFactory(game, dataQuery, InputProviderComponent, gameContext, skillService);
        createdSession = new YuGiOhGameSession(game, inputProviderFactory, eventBus, gameContext, new GodotLogger());

        await StartGame(createdSession);
    }

    private async Task StartGame(YuGiOhGameSession session)
    {
        session.StartGame();
        // Board.ApplySession(session, InputProviderComponent.Presenter);

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
            // await InputProviderComponent.Presenter.WhenIdle();
        }

        var playerWins = session.Winner() == session.Game.Player1;
        if(playerWins)
        {
            GD.Print("You won");
        } else
        {
            GD.Print("You lose");
        }

        // GameOverDialog.Set(playerWins, dataQuery.GetPlayerDamage(session.Game.Player2), session.GameOverContext);
        // HUD.Show();

        GD.Print("Win Condition ", session.GameOverContext.ToString());
    }
}
