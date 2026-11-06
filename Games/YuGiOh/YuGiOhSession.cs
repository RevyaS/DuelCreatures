using System.Threading.Tasks;
using ArC.CardGames.Bots;
using ArC.CardGames.YuGiOh;
using ArC.CardGames.YuGiOh.Bots;
using Godot;

public partial class YuGiOhSession : Control
{
    YuGiOhInputProvider InputProviderComponent = null!;
    YuGiOhBoard Board => InputProviderComponent.Board;

    public async override void _Ready()
    {
        InputProviderComponent = GetNode<YuGiOhInputProvider>($"%{nameof(InputProviderComponent)}");

        var (session, services) = YuGiOhSessionBuilder.Create()
            .Player1(new(YuGiOhDeckFactory.ExosisterTacticalTryDeck), services =>
            {
                InputProviderComponent.Activate(services.Game.Board.Player1Area, services.Game.Board.Player2Area, services.SkillService);
                return InputProviderComponent;
            })
            .Player2(new(YuGiOhDeckFactory.ExosisterTacticalTryDeck), services =>
                new ExosisterTacticalTryDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, BotDifficulty.MASTER))
            .WithLogger(new GodotLogger())
            .Build();

        InputProviderComponent.SetEventBus(services.EventBus);

        await StartGame(session);
    }

    private async Task StartGame(YuGiOhGameSession session)
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
