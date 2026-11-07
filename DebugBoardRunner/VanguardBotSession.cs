using System.Linq;
using ArC.CardGames.Bots;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Bots;
using Godot;

/// <summary>
/// Debug only: two trial deck bots play a whole Vanguard game on the board built from zone scenes
/// (VanguardBoardView), to watch how it's shown. Nothing asks for input; pressing a card shows its details and
/// pressing the drop zone lists it. Run it with F6.
/// </summary>
public partial class VanguardBotSession : Control
{
    CardList cardList = null!;
    CardDetailsOverlay details = null!;
    IZoneView? listedZone;

    public async override void _Ready()
    {
        var board = GetNode<VanguardBoardView>("%Board");
        var presenter = new Presenter { Flights = GetNode<CardFlightLayer>("%CardFlights") };
        cardList = GetNode<CardList>("%CardList");
        details = GetNode<CardDetailsOverlay>("%CardDetails");
        cardList.LetThrough(details.Details);
        cardList.CardPressed += node =>
        {
            if(node.CurrentCard is { } card) details.Show(new CardDetail(card, listedZone?.Space, []));
        };

        var (session, _) = VanguardSessionBuilder.Create()
            .Player1(new(RoyalPaladinTrialDeckBot.Deck), services =>
                new RoyalPaladinTrialDeckBot(services.Game.Player1, services.DataQuery, services.SkillService, services.GameContext, BotDifficulty.MASTER))
            .Player2(new(KageroTrialDeckBot.Deck), services =>
                new KageroTrialDeckBot(services.Game.Player2, services.DataQuery, services.SkillService, services.GameContext, BotDifficulty.MASTER))
            .WithLogger(new GodotLogger())
            .Build();

        session.StartGame();
        var driver = board.Show(session, presenter);
        driver.CardInspected += (view, card) => details.Show(new CardDetail(card, view.Space, []));
        driver.ZoneInspected += view =>
        {
            if(view.Space is null) return;
            var cards = ZoneSnapshot.Of(view.Space).Cards.Select(card => card.Card).ToList();
            if(cards.Count == 0) return;
            listedZone = view;
            cardList.Show(((Node)view).Name, cards);
        };

        // The play areas must keep their size: warn about anything in them that changes size
        LayoutWatch.Watch(this, board.GetNode<Control>("%Player1Area"), board.GetNode<Control>("%Player2Area"));

        if(await SessionLoop.Run(session, presenter))
        {
            GD.Print($"Bot game over: {(session.Winner() == session.Game.Player1 ? "player 1" : session.Winner() is null ? "nobody" : "player 2")} won ({session.GameOverContext})");
        }
    }
}
