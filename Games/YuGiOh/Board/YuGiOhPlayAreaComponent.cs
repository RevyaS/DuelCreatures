using ArC.CardGames.YuGiOh;
using Godot;

public partial class YuGiOhPlayAreaComponent : MarginContainer
{
    private bool _flippedAppearance = false;
    [Export]
    public bool FlippedAppearance
    {
        get => _flippedAppearance;
        set
        {
            _flippedAppearance = value;
            Render();
        }
    }

    DeckComponent Deck = null!;

    // Cards with no known previous location come from here (draws, checks, superior calls)
    public ICardLocation DeckLocation => Deck;

    public override void _Ready()
    {
        base._Ready();
        Deck = GetNode<DeckComponent>($"%{nameof(Deck)}");
    }

    public void Bind(YuGiOhPlayArea playArea)
    {
        Deck.Bind(playArea.Deck);
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        // Circles.ForEach(x => x.FlippedAppearance = FlippedAppearance);
        // AllExtraFields.ForEach(x => x.FlippedAppearance = FlippedAppearance);
        // var turns = FlippedAppearance ? QuarterTurnContainer.QuarterTurn.Deg180 : QuarterTurnContainer.QuarterTurn.Deg0;
        // PhaseIndicatorTurn.Turns = turns;
        // DeckTurn.Turns = turns;
        // DropZoneTurn.Turns = turns;
        // Deck.SleeveInfo = SleeveInfo;
        // DamageZone.SleeveInfo = SleeveInfo;
    }

}
