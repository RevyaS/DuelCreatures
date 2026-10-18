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

    private bool _draggable = false;
    public bool Draggable
    {
        get => _draggable;
        set
        {
            _draggable = value;
            Render();
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

        if(GetChildCount() > 0 && GetChild(0) is Card card)
        {
            AdoptCard(card);
        }
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
        orientation = Orientation.VERTICAL;

        // Setup happens in OnChildEnteredTree, the same as for cards added directly as children
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

    // The one place the container's properties are applied to its card,
    // used both when a card arrives and when a property changes
    private void Render()
    {
        if(CurrentCard is null) return;

        CurrentCard.Scale = new Vector2(CardScale, CardScale);
        CurrentCard.SleeveInfo = sleeveInfo;
        CurrentCard.Draggable = _draggable;
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
        card.CardDragging -= OnCardDragging;
        card.CardDragCancelled -= OnCardDragCancelled;
        card.CardPressed -= OnCardPressed;
        card.CardLongPressed -= OnCardLongPressed;
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
        if(node is not Card card) return;

        // Re-entering the tree with the same card (e.g. scene tab switch) keeps it as is
        if(ReferenceEquals(currentCard, card))
        {
            Render();
            return;
        }

        if(currentCard is not null)
        {
            RemoveCard();
        }

        AdoptCard(card);
    }

    private void AdoptCard(Card card)
    {
        currentCard = card;
        card.CardDragging += OnCardDragging;
        card.CardDragCancelled += OnCardDragCancelled;
        card.CardPressed += OnCardPressed;
        card.CardLongPressed += OnCardLongPressed;
        Render();
        CardAdded?.Invoke(card);
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

    // Global transform of the card in this slot, or of where one would sit
    public Transform2D GetCardGlobalTransform()
    {
        if(CurrentCard is not null) return CurrentCard.GetGlobalTransform();
        return CardPlacement.Centered(this, CardScale, CalculateCardRotation(orientation));
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
    public event Action<Card>? CardAdded;
}