using System;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

// A line of MaxCards slots. It always takes the room of MaxCards cards, but only filled
// slots show, so Alignment places the cards within that room (e.g. centered).
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
        UpdateSlots();
    }

    protected override void RenderCore()
    {
        base.RenderCore();
        UpdateSlots();
        ReserveRoom();
    }

    // Empty slots take no room, so the cards line up by Alignment instead of
    // always starting at the first slot. The editor keeps showing every slot.
    private void UpdateSlots()
    {
        if(Engine.IsEditorHint()) return;
        ContainerNodeManager.ApplyToChildren<CardContainer>(child => child.Visible = child.HasCard);
    }

    // The row is always as wide as MaxCards cards, so the zone's size doesn't change as it fills
    private void ReserveRoom()
    {
        var slot = Container.GetChildren<CardContainer>().FirstOrDefault();
        if(slot is null || MaxCards == 0)
        {
            Container.CustomMinimumSize = Vector2.Zero;
            return;
        }

        var size = slot.GetCombinedMinimumSize();
        var width = MaxCards * size.X + (MaxCards - 1) * CalculateSeparation();
        Container.CustomMinimumSize = new Vector2(Mathf.Max(width, size.X), size.Y);
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
        UpdateSlots();
    }

    public bool IsFull => lastIndex >= MaxCards;

    // Where the next card lands once it's shown: the filled cards plus it, placed by Alignment
    // (the empty slot itself is hidden, so it has no place of its own yet)
    protected override Transform2D GetNextSlotTransform()
    {
        var slots = Container.GetChildren<CardContainer>().ToList();
        if(lastIndex >= slots.Count) return base.GetNextSlotTransform();

        var slot = slots[lastIndex];
        var size = slot.GetCombinedMinimumSize();
        var separation = CalculateSeparation();
        var count = lastIndex + 1;
        var free = Container.Size.X - (count * size.X + (count - 1) * separation);
        var start = Alignment switch
        {
            BoxContainer.AlignmentMode.Center => free / 2,
            BoxContainer.AlignmentMode.End => free,
            _ => 0
        };
        var x = start + lastIndex * (size.X + separation);
        return slot.GetCardGlobalTransformAt(new Rect2(x, 0, size.X, Container.Size.Y));
    }

    public void AddCard(Card card)
    {
        if(lastIndex == MaxCards) throw new InvalidOperationException("Already reached MAX amount of cards");

        var container = Container.GetChild<CardContainer>(lastIndex++);
        container.AddCard(card);
        UpdateSlots();

        // New cards arrive face up; afterwards the game owns the face (e.g. counter blast)
        card.IsFront = true;
    }
}
