using System;
using Godot;

[Tool]
public partial class CardLineStatic : CardLine
{
    int lastIndex = 0;

    private int _maxCards = 0;
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
        EvaluateContainers(true);
    }

    public void ClearCards()
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            child.RemoveCardAndFree();
        });
        lastIndex = 0;
    }

    private void EvaluateContainers(bool initiation = false)
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

        GD.Print($"MaxCards: {MaxCards}, CurrentCards: {currentCards}");
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
            int extraContainers = currentCards - MaxCards;
            GD.Print($"Removing {extraContainers}");
            for (int i = 0; i < extraContainers; i++)
            {
                // Remove extra container
                var lastIndex = Container.GetChildCount() - 1;
                GD.Print($"Removed Last Index {lastIndex}");
                var lastContainer = Container.GetChild(lastIndex);
                Container.RemoveChild(lastContainer);
            }
            if(initiation)
            {
                MaxCards = currentCards;
            }
        }
    }

    public void RemoveCard(Card card, bool freeCard = true)
    {
        int removeIndex = -1;

        // Find the index
        for (int i = 0; i < Container.GetChildCount(); i++)
        {
            var currentContainer = Container.GetChild<CardContainer>(i);
            if (currentContainer.HasCard &&
                ReferenceEquals(currentContainer.CurrentCard, card))
            {
                removeIndex = i;
                break;
            }
        }

        if (removeIndex == -1)
            return;

        // 2️⃣ Shift cards left
        for (int i = removeIndex; i < Container.GetChildCount() - 1; i++)
        {
            var currentContainer = Container.GetChild<CardContainer>(i);
            var nextContainer = Container.GetChild<CardContainer>(i + 1);
            if(nextContainer.HasCard)
            {
                currentContainer.AddCard(nextContainer.CurrentCard!);
            } else
            {
                if(freeCard)
                {
                    currentContainer.RemoveCardAndFree();
                } else
                {
                    currentContainer.RemoveCard();
                }
            }
        }
    }

    public override void AddCard(Card card)
    {
        if(lastIndex == MaxCards) throw new InvalidOperationException("Already reached MAX amount of cards");

        GD.Print("Last Index: ", lastIndex);
        var container = Container.GetChild<CardContainer>(lastIndex++);
        container.AddChild(card);
        card.IsFront = true;
        base.AddCard(card);
    }
}
