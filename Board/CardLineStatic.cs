using Godot;

[Tool]
public partial class CardLineStatic : PanelContainer
{
    HBoxContainer Container;    
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
        get => Container.GetThemeConstant("separation"); 
        set
        {
            Container.AddThemeConstantOverride("separation", value);
        } 
    }

    public override void _Ready()
    {
        Container = GetNode<HBoxContainer>($"%{nameof(Container)}");
        EvaluateContainers();
    }

    public void ClearCards()
    {
    }

    private void EvaluateContainers()
    {
        int currentContainerCount = 0;
        lastIndex = 0;
        for(int i = 0; i < Container.GetChildCount(); i++)
        {
            currentContainerCount++;
            var container = Container.GetChild<CardContainer>(i);
            if(container.GetChildCount() > 0)
            {
                lastIndex++;
            }
        }

        int missingContainers = MaxCards - Container.GetChildCount();
        for(int i = 0; i < missingContainers; i++)
        {
            // Create missing container
            AddChild(new CardContainer());
        }
    }
}
