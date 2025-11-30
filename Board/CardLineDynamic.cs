using Godot;

[Tool]
public partial class CardLineDynamic : PanelContainer
{
    HBoxNodeContainer Container;

    IChildManagerComponent ContainerNodeManager => Container;    

    [Export]
    public int Separation { 
        get => !IsInsideTree() ? 0 : Container.GetThemeConstant("separation"); 
        set
        {
            if(!IsInsideTree()) return;
            Container.AddThemeConstantOverride("separation", value);
        } 
    }

    public override void _Ready()
    {
        Container = GetNode<HBoxNodeContainer>($"%{nameof(Container)}");
    }

    public void AddCard(Card card)
    {
        CardContainer cardContainer = new();
        Container.AddChild(cardContainer);
        cardContainer.AddChild(card);
    }

    public void RemoveCard(Card card)
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            var containedCard = child.GetChild<Card>(0);
            if(ReferenceEquals(containedCard, card))
            {
                child.QueueFree();
            }
        });
    }

    public void ClearCards()
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            child.QueueFree();
        });
    }
}
