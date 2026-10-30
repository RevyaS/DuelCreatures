using System;
using System.Linq;
using ArC.CardGames.Components;
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
        ContainerNodeManager.ApplyToChildren<CardContainer>((child) =>
        {
            if(child.HasChild<Card>())
            {
                lastIndex++;
            }
        });

        var currentCards = Container.GetChildCount<CardContainer>();

        if(initiation && MaxCards < currentCards)
        {
            // Adopt the containers authored in the scene instead of tearing them down
            _maxCards = currentCards;
            return;
        }

        // Adding/removing nodes in the edited scene bypasses the editor's
        // ownership and undo tracking, so only do it at runtime
        if(Engine.IsEditorHint())
        {
            return;
        }

        if(MaxCards > currentCards)
        {
            int missingContainers = MaxCards - currentCards;
            for (int i = 0; i < missingContainers; i++)
            {
                // Create missing container
                var newContainer = new CardContainer();
                Container.AddChild(newContainer);
            }
        }
        else if(MaxCards < currentCards)
        {
            int extraContainers = currentCards - MaxCards;
            for (int i = 0; i < extraContainers; i++)
            {
                // Remove extra container
                var lastContainer = Container.GetChild(Container.GetChildCount() - 1);
                Container.RemoveChild(lastContainer);
                lastContainer.QueueFree();
            }
        }
    }

    public void RemoveCard(CardBase card, bool freeCard = true)
    {
        CardContainer cardComponent = Container.FirstChild<CardContainer>(child =>
        {
            var cardContainer = child;
            if(ReferenceEquals(cardContainer.CurrentCard?.CurrentCard, card))
            {
                return true;
            }
            return false;
        });
        RemoveCard(cardComponent.CurrentCard!, freeCard);
    }
    public void RemoveCard(Card card, bool freeCard = true)
    {
        var containers = Container.GetChildren<CardContainer>().ToList();
        int removeIndex = containers.FindIndex(container =>
            container.HasCard && ReferenceEquals(container.CurrentCard, card));

        if (removeIndex == -1)
            return;

        if(freeCard)
        {
            containers[removeIndex].RemoveCardAndFree();
        } else
        {
            containers[removeIndex].RemoveCard();
        }

        // Shift the following cards left to close the gap
        for (int i = removeIndex; i < containers.Count - 1; i++)
        {
            var nextContainer = containers[i + 1];
            if(!nextContainer.HasCard) break;

            var nextCard = nextContainer.CurrentCard!;
            nextContainer.RemoveCard();
            containers[i].AddCard(nextCard);
        }

        lastIndex--;
    }

    public bool IsFull => lastIndex >= MaxCards;

    protected override Transform2D GetNextSlotTransform()
    {
        if(lastIndex < Container.GetChildCount<CardContainer>())
        {
            return Container.GetChild<CardContainer>(lastIndex).GetCardGlobalTransform();
        }
        return base.GetNextSlotTransform();
    }

    public void AddCard(Card card)
    {
        if(lastIndex == MaxCards) throw new InvalidOperationException("Already reached MAX amount of cards");

        var container = Container.GetChild<CardContainer>(lastIndex++);
        container.AddCard(card);

        // New cards arrive face up; afterwards the game owns the face (e.g. counter blast)
        card.IsFront = true;
    }
}
