using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;
using static ArC.CardGames.Predefined.Vanguard.Cards.DuelMaidensCardsFactory;

public partial class Session : Control
{
    InputProvider InputProviderComponent;
    DuelCreaturesBoard Board => InputProviderComponent.Board;

    public async override void _Ready()
    {
        InputProviderComponent = GetNode<InputProvider>($"%{nameof(InputProviderComponent)}");

        var eventBus = new VanguardEventBus();
        var effectService = new VanguardEffectService();

        var player1 = new VanguardPlayerProfile(new([
            ..Enumerable.Repeat(RoyalPaladin.LittleSageMarron, 4),
            ..Enumerable.Repeat(RoyalPaladin.SailorGuardianMichiru, 4),
            ..Enumerable.Repeat(RoyalPaladin.BlasterBlade, 4),
            ..Enumerable.Repeat(RoyalPaladin.KnightOfSilenceGallatin, 4),
            ..Enumerable.Repeat(RoyalPaladin.KingOfKnightsAlfred, 4)
         ], eventBus), RoyalPaladin.StardustTrumpeteer);

        var player2 = new VanguardPlayerProfile(new([
            ..Enumerable.Repeat(RoyalPaladin.LittleSageMarron, 4),
            ..Enumerable.Repeat(RoyalPaladin.SailorGuardianMichiru, 4),
            ..Enumerable.Repeat(RoyalPaladin.BlasterBlade, 4),
            ..Enumerable.Repeat(RoyalPaladin.KnightOfSilenceGallatin, 4),
            ..Enumerable.Repeat(RoyalPaladin.KingOfKnightsAlfred, 4)
        ], eventBus), RoyalPaladin.StardustTrumpeteer);

        var game = new VanguardGame(player1, player2, eventBus, effectService);
        var inputProviderFactory = new InputProviderFactory(game, InputProviderComponent);
        var skillService = new VanguardSkillService(game);

        var session = new VanguardGameSession(game, inputProviderFactory, eventBus, effectService, skillService);

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

    public string GetPhaseName(IPhase phase)
    {
        switch(phase)
        {
            case MulliganPhase:
                return "Mulligan Phase";
        }

        return "Unknown Phase";
    }
}