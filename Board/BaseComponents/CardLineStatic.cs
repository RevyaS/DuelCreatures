using System;
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
                var nextCard = nextContainer.CurrentCard;
                nextContainer.RemoveCard();
                currentContainer.AddCard(nextCard!);
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

        lastIndex--;
    }

    public override void AddCard(Card card)
    {
        if(lastIndex == MaxCards) throw new InvalidOperationException("Already reached MAX amount of cards");

        var container = Container.GetChild<CardContainer>(lastIndex++);
        container.AddChild(card);
        card.IsFront = true;
        base.AddCard(card);
    }
}
