using Godot;

[Tool]
[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
// A square slot as wide as the card is long, so turning the card (e.g. resting a unit)
// never changes the layout around it
public partial class CardRotationContainer : CardContainer
{
    protected override Vector2 CalculateSlotSize()
    {
        var side = Mathf.Round(Mathf.Max(CardSize.X, CardSize.Y));
        return new Vector2(side, side);
    }
}