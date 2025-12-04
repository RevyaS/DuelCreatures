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

    public async override void _Ready()
    {
        var eventBus = new VanguardEventBus();
        var effectService = new VanguardEffectService();
        var gameContext = new GameContext();

        InputProviderComponent = GetNode<InputProvider>($"%{nameof(InputProviderComponent)}");
        InputProviderComponent.SetEventBus(eventBus);

        var player1 = new VanguardPlayerProfile(new(
            DeckBuilder.Create()
                .AddCards(RoyalPaladin.LittleSageMarron, 4)
                .AddCards(RoyalPaladin.SailorGuardianMichiru, 4)
                .AddCards(RoyalPaladin.BlasterBlade, 4)
                .AddCards(RoyalPaladin.KnightOfSilenceGallatin, 4)
                .AddCards(RoyalPaladin.KingOfKnightsAlfred, 4)
                .GetCards(), eventBus), RoyalPaladin.StardustTrumpeteer);

        var player2 = new VanguardPlayerProfile(new(DeckBuilder.Create()
                .AddCards(RoyalPaladin.LittleSageMarron, 4)
                .AddCards(RoyalPaladin.SailorGuardianMichiru, 4)
                .AddCards(RoyalPaladin.BlasterBlade, 4)
                .AddCards(RoyalPaladin.KnightOfSilenceGallatin, 4)
                .AddCards(RoyalPaladin.KingOfKnightsAlfred, 4)
                .GetCards(), eventBus), RoyalPaladin.StardustTrumpeteer);

        var game = new VanguardGame(player1, player2, eventBus, effectService);
        var inputProviderFactory = new InputProviderFactory(game, InputProviderComponent, gameContext);
        var skillService = new VanguardSkillService(game);

        var session = new VanguardGameSession(game, inputProviderFactory, eventBus, effectService, skillService, gameContext);

        await StartGame(session);
    }

    private async Task StartGame(VanguardGameSession session)
    {
        session.StartGame();
        Board.ApplySession(session);
        
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
    }
}