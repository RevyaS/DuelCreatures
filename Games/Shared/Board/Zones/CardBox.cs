using Godot;

/// <summary>
/// One card, in a square box as wide as a card is long, so its card can lie sideways (e.g. a unit circle resting a
/// unit, a monster zone in Defense Position) without changing the layout around it.
/// </summary>
[Tool]
public partial class CardBox : CardSlot
{
    protected override bool AllowsSideways => true;
}
