using System;
using DuelCreatures.Data;
using Godot;
using Orientation = ArC.CardGames.Components.Orientation;

[Tool]
[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class CardContainer : Control
{
    Card? currentCard = null;
    protected bool UpdateSizeOnCardPlacedment = true;
    private SleeveInfo sleeveInfo = null!;
    private Orientation orientation = Orientation.VERTICAL;

    public Card? CurrentCard => currentCard;
    public bool HasCard => currentCard is not null && currentCard.CurrentCard is not null;
    float FlipRotation => FlippedAppearance ? Mathf.Pi : 0;

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

    private bool _flippedAppearance = false;
    [Export]
    public bool FlippedAppearance
    {
        get => _flippedAppearance;
        set
        {
            _flippedAppearance = value;
            Render();
        }
    }

    // Stored in degrees so the getter matches what the setter receives;
    // otherwise every save/load cycle converts the value again
    private float _containerRotationDeg = 0;
    [Export(PropertyHint.Range, "0, 359")]
    public float ContainerRotation
    {
        get => _containerRotationDeg;
        set
        {
            _containerRotationDeg = value;
            Render();
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

    public void SetSleeveInfo(SleeveInfo sleeveInfo)
    {
        this.sleeveInfo = sleeveInfo;
        if(currentCard is not null)
        {
            currentCard.SleeveInfo = sleeveInfo;
        }
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
        card.CardPressed += OnCardPressed;
        card.CardLongPressed += OnCardLongPressed;
        orientation = Orientation.VERTICAL;
        card.Rotation = CalculateCardRotation(orientation);
        AddChild(card);
    }

    private void OnCardLongPressed(Card card)
    {
        CardLongPressed?.Invoke(card);
    }

    private void OnCardPressed(Card card)
    {
        CardPressed?.Invoke(card);
    }

    private void Render()
    {
        if(CurrentCard is null) return;

        CurrentCard.Scale = new Vector2(CardScale, CardScale);
        CurrentCard.SleeveInfo = sleeveInfo;
        // Applied on every render so FlippedAppearance/ContainerRotation also reach
        // cards that were added directly as children
        CurrentCard.Rotation = CalculateCardRotation(orientation);

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
            var card = currentCard;
            RemoveChild(card);
            ClearCardState(card);
        }
    }

    private void OnChildExitedTree(Node node)
    {
        // ChildExitingTree also fires when this container itself leaves the tree
        // (scene tab switch, reparent, close). Only drop the card when it is
        // really going away; explicit removals go through RemoveCard.
        if(node is Card card && card.IsQueuedForDeletion())
        {
            ClearCardState(card);
        }
    }

    private void ClearCardState(Card card)
    {
        if(!ReferenceEquals(currentCard, card)) return;

        currentCard = null;
        card.Scale = Vector2.One;
        if(UpdateSizeOnCardPlacedment)
        {
            CustomMinimumSize = Vector2.Zero;
        }
    }

    private float CalculateCardRotation(Orientation orientation)
    {
        float orientationRotation = orientation switch
        {
            Orientation.VERTICAL => 0,
            Orientation.HORIZONTAL => Mathf.Pi /2,
            _ => throw new InvalidOperationException()
        };

        return Mathf.DegToRad(ContainerRotation) + FlipRotation + orientationRotation;
    }

    private void OnChildEnteredTree(Node node)
    {
        if(node is Card card)
        {
            if(currentCard is not null && !ReferenceEquals(currentCard, card))
            {
                RemoveCard();
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
        this.orientation = orientation;
        Render();
    }

    public Action<CardBaseComponent>? CardDragging;
    public Action<CardBaseComponent>? CardDragCancelled;
    public event Action<Card>? CardPressed;
    public event Action<Card>? CardLongPressed;
}