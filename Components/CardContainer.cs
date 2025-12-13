using System;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

[Tool]
[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class CardContainer : Control
{
    Card? currentCard = null;
    public Card? CurrentCard => currentCard;
    public bool HasCard => currentCard is not null && currentCard.CurrentCard is not null;
    protected bool UpdateSizeOnCardPlacedment = true;

    private float _cardScale = SizeConstants.CardScaleFactor;
    [Export(PropertyHint.Range, "0.0, 1.0")]
    public float CardScale
    {
        get => _cardScale;
        set
        {
            _cardScale = value;
            Render();
        }
    }

    public bool Draggable
    {
        get => CurrentCard?.Draggable ?? false;
        set
        {
            if(CurrentCard is not null)
            {
                CurrentCard.Draggable = value;
            }
        }
    }

    public override void _Ready()
    {
        ChildEnteredTree += OnChildEnteredTree;
        ChildExitingTree += OnChildExitedTree;

        if(GetChildCount() > 0)
        {
            currentCard = GetChild<Card>(0);
        }

        Render();
    }

    public void FaceUp()
    {
        if(currentCard is not null)
        {
            currentCard.IsFront = true;
        }
    }

    public void FaceDown()
    {
        if(currentCard is not null)
        {
            currentCard.IsFront = false;
        }
    }

    public void UpdatePower(int newPower)
    {
        ((VanguardCardComponent)CurrentCard!).Power = newPower;
    }
    public void UpdateCrit(int newCrit)
    {
        ((VanguardCardComponent)CurrentCard!).Critical = newCrit;
    }

    public void AddCard(Card card)
    {
        if(currentCard is not null)
        {
            RemoveCardAndFree();
        }
        card.Draggable = Draggable;
        card.CardDragging += OnCardDragging;
        card.CardDragCancelled += OnCardDragCancelled;
        AddChild(card);
    }

    private void Render()
    {
        if(CurrentCard is null) return;

        CurrentCard.Scale = new Vector2(CardScale, CardScale);

        if(UpdateSizeOnCardPlacedment)
        {
            CustomMinimumSize = UpdateSizeFromCard(CurrentCard);
        }
    }

    private void OnCardDragCancelled(CardBaseComponent component)
    {
        CardDragCancelled?.Invoke(component);
    }

    private void OnCardDragging(CardBaseComponent component)
    {
        CardDragging?.Invoke(component);
    }

    public void RemoveCardAndFree()
    {
        currentCard?.QueueFree();
        RemoveCard();
    }

    public void RemoveCard()
    {
        if(currentCard is not null)
        {
            RemoveChild(currentCard);
        }
    }

    private void OnChildExitedTree(Node node)
    {
        if(node is Card card)
        {
            currentCard = null;
            card.Scale = Vector2.One;
            if(UpdateSizeOnCardPlacedment)
            {
                CustomMinimumSize = Vector2.Zero;
            }
        }
    }

    private void OnChildEnteredTree(Node node)
    {
        if(node is Card card)
        {
            if(currentCard is not null)
            {
                RemoveChild(currentCard);
            } 

            currentCard = card;
            Render();
        }
    }

    protected virtual Vector2 UpdateSizeFromCard(Card card)
    {
        float w = card.EffectiveSize.X;
        float h = card.EffectiveSize.Y;

        float cos = Mathf.Cos(card.Rotation);
        float sin = Mathf.Sin(card.Rotation);

        float newWidth = Mathf.Abs(w * cos) + Mathf.Abs(h * sin);
        float newHeight = Mathf.Abs(w * sin) + Mathf.Abs(h * cos);

        var result = new Vector2(newWidth, newHeight);
        return result;

    }

    public void ChangeOrientation(Orientation orientation)
    {
        if(CurrentCard is null) return;
        switch(orientation)
        {
            case Orientation.HORIZONTAL:
                CurrentCard.Rotation = Mathf.DegToRad(90);
                break;
            case Orientation.VERTICAL:
                CurrentCard.Rotation = Mathf.DegToRad(0);
                break;
        }
    }

    public Action<CardBaseComponent>? CardDragging;
    public Action<CardBaseComponent>? CardDragCancelled;
}