using System;
using Godot;

[Tool]
public partial class CardLineStatic : CardLine
{
    int lastIndex = 0;

    private int _maxCards;
    [Export]
    public int MaxCards { 
        get => _maxCards; 
        set
        {
            _maxCards = value;
            EvaluateContainers();
            Render();
        } 
    }

    protected override void OnComponentsSet()
    {
        EvaluateContainers();
    }

    public void ClearCards()
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            child.RemoveCard();
        });
        lastIndex = 0;
    }

    private void EvaluateContainers()
    {
        if(!IsInsideTree())
        {
            return;
        }

        lastIndex = 0;
        GD.Print("Last Index: ", lastIndex);
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
                Container.AddChild(new CardContainer());
            }
        } 
        else if(MaxCards < currentCards)
        {
            MaxCards = currentCards;
        }
    }

    public override void AddCard(Card card)
    {
        if(lastIndex == MaxCards) throw new InvalidOperationException("Already reached MAX amount of cards");

        GD.Print("Last Index: ", lastIndex);
        var container = Container.GetChild<CardContainer>(lastIndex++);
        container.AddChild(card);
        card.IsFront = true;
    }
}
