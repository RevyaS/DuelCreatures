using Godot;

[Tool]
public partial class CardLineStatic : PanelContainer
{
    HBoxNodeContainer Container = null!;

    IChildManagerComponent ContainerNodeManager => Container;    
    int lastIndex = 0;

    private int _maxCards;
    [Export]
    public int MaxCards { 
        get => _maxCards; 
        set
        {
            _maxCards = value;
            EvaluateContainers();
        } 
    }

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
        EvaluateContainers();
    }

    public void ClearCards()
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            child.RemoveCard();
        });
    }

    private void EvaluateContainers()
    {
        if(!IsInsideTree())
        {
            return;
        }

        lastIndex = 0;
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            if(child.HasChild<Card>())
            {
                lastIndex++;
            }
        });

        var currentCards = Container.GetChildCount<CardContainer>();

        if(MaxCards > currentCards)
        {
            int missingContainers = MaxCards - currentCards;
            for (int i = 0; i < missingContainers; i++)
            {
                // Create missing container
                AddChild(new CardContainer());
            }
        } 
        else if(MaxCards < currentCards)
        {
            MaxCards = currentCards;
        }
    }
}
