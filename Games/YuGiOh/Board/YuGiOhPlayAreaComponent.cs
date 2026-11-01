using Godot;
using System;

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
