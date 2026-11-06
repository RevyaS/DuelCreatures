using ArC.CardGames.YuGiOh;
using Godot;

public partial class YuGiOhBoard : MarginContainer
{
    // Everything that wants a sound raises it here (see PresentationConfig for which sides play)
    public SfxBus Sfx { get; } = new();
    PresentationContext presentation = null!;
    PresentationContext playerPresentation = null!;
    PresentationContext oppPresentation = null!;

    YuGiOhPlayAreaComponent PlayerArea { get; set; } = null!;
    YuGiOhPlayAreaComponent OppArea { get; set; } = null!;
    HandComponent PlayerHand { get; set; } = null!;
    HandComponent OppHand = null!;

    public override void _Ready()
    {
        PlayerArea = GetNode<YuGiOhPlayAreaComponent>($"%{nameof(PlayerArea)}");
        OppArea = GetNode<YuGiOhPlayAreaComponent>($"%{nameof(OppArea)}");
        PlayerHand = GetNode<HandComponent>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandComponent>($"%{nameof(OppHand)}");
    }

    public void ApplySession(YuGiOhGameSession gameSession, Presenter presenter)
    {
        var game = gameSession.Game;
        presentation = new(presenter, Sfx, BoardSide.Both);
        playerPresentation = presentation with { Side = BoardSide.Player };
        playerPresentation = presentation with { Side = BoardSide.Opponent };

        PlayerArea.Bind(game.Board.Player1Area);
        OppArea.Bind(game.Board.Player2Area);

        PlayerHand.Bind(game.Board.Player1Area.Hand);
        OppHand.Bind(game.Board.Player2Area.Hand);
    }

    private void SetupEventBus(YuGiOhEventBus eventBus)
    {
        PlayerHand.DrawSource = PlayerArea.DeckLocation;
        OppHand.DrawSource = OppArea.DeckLocation;

        PlayerHand.SetEventBus(eventBus, playerPresentation);
        OppHand.SetEventBus(eventBus, oppPresentation);
    }
}
