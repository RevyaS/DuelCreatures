using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using Godot;

// A player's hand, for any game: cards fly in from the deck (or wherever they were) and leave
// through the base EventBus' hand events
[Tool]
public partial class HandComponent : CardLineDynamic, ICardSpaceBindable<Hand>, IEventBusUtilizer<EventBus>
{
    public Hand Hand { get; private set; } = null!;
    PresentationContext? presentation = null;

    // Where drawn cards come from
    public ICardLocation? DrawSource { get; set; }

    public void SetEventBus(EventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.CardAddedToHand += OnCardAddedToHand;
        eventBus.CardTakenFromHand += OnCardTakenFromHand;
    }

    private void OnCardTakenFromHand(CardBase card, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            presentation.Present(() =>
            {
                presentation.Leaving(card, this);
                RemoveCard(card);
            });
        }
    }

    private void OnCardAddedToHand(CardBase newCard, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            presentation.PresentArrival(newCard,
                source => source is null || ReferenceEquals(source, DrawSource) ? CardMoves.DeckToHand : CardMoves.ToHand,
                this, DrawSource,
                () => AddCard(CardScenes.Create(newCard)));
        }
    }

    public void ShowContainerOfCard(CardBase card)
    {
        var cardComponent = FindCard((containedCard) => ReferenceEquals(containedCard.CurrentCard, card));
        if(cardComponent is not null)
        {
            cardComponent.CurrentlyDragged = false;
        }
    }

    public void Bind(Hand hand)
    {
        Hand = hand;
    }
}
