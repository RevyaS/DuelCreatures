using Godot;

[Tool]
[GlobalClass]
[Icon("res://Assets/Icons/Control.svg")]
public partial class CardContainer : Control
{
    Card currentCard = null;
    protected bool UpdateSizeOnCardPlacedment = true;
    public override void _Ready()
    {
        ChildEnteredTree += OnChildEnteredTree;
        ChildExitingTree += OnChildExitedTree;

        if(GetChildCount() > 0)
        {
            currentCard = GetChild<Card>(0);
        }
    }

    public void FaceUp()
    {
        if(currentCard is not null)
        {
            currentCard.IsFront = true;
        }
    }

    public void AddCard(Card card)
    {
        if(currentCard is not null)
        {
            RemoveCard();
        }
        AddChild(card);
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

            card.Scale = SizeConstants.CardBoardScale;

            if(UpdateSizeOnCardPlacedment)
            {
                CustomMinimumSize = UpdateSizeFromCard(card);
            }
        }
    }

    protected virtual Vector2 UpdateSizeFromCard(Card card)
    {
        return card.EffectiveSize;
    }
}