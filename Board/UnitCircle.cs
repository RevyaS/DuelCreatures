using Godot;

[Tool]
public partial class UnitCircle : Control
{
    CardRotationContainer cardRotationContainer;

    public override void _Ready()
    {
        cardRotationContainer = GetNode<CardRotationContainer>($"%{nameof(CardRotationContainer)}");
    }

    public void Clear()
    {
        cardRotationContainer.RemoveCard();
    }
}
