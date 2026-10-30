using Godot;

[Tool]
[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class CardRotationContainer : CardContainer
{
    public override void _Ready()
    {
        base._Ready();
        UpdateSizeOnCardPlacedment = false;

        var scaledSize = SizeConstants.CardDefaultSize * SizeConstants.CardBoardScale;
        var maxDimension = Mathf.Max(scaledSize.X, scaledSize.Y);
        CustomMinimumSize = new(maxDimension, maxDimension);
    }
}