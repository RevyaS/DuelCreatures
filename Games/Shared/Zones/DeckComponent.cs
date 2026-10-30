using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Flow;
using Godot;

[Tool]
public partial class DeckComponent : CardVerticalStack, ICardSpaceBindable<Deck>, IEventBusUtilizer<EventBus>
{
    Deck Deck = null!;
    PresentationContext? presentation = null;

    public void Bind(Deck deck)
    {
        Deck = deck;
    }

    public void SetEventBus(EventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.AfterDeckDrawn += OnDeckDrawn;
        eventBus.CardTakenFromDeck += OnCardTakenFromDeck;
        eventBus.CardAddedToDeck += OnCardAddedToDeck;
        eventBus.DeckShuffled += OnDeckShuffled;
    }

    private void OnDeckDrawn()
    {
        PresentCount();
    }

    private void OnCardTakenFromDeck(Deck deck, CardBase card)
    {
        if(ReferenceEquals(deck, Deck))
        {
            PresentCount();
        }
    }

    private void OnCardAddedToDeck(Deck deck, CardBase card, bool bottom)
    {
        if(ReferenceEquals(deck, Deck))
        {
            var count = Deck.Cards.Count;
            presentation.PresentArrival(card, CardMoves.ToDeck, this, null, () => CountValue = count, faceUp: false);
        }
    }

    private void OnDeckShuffled(Deck deck)
    {
        if(!ReferenceEquals(deck, Deck) || presentation is null) return;

        var side = presentation.Side;
        var flights = presentation.Presenter.Flights;
        presentation.Present(async () =>
        {
            if(flights is not null && Presenter.Animates(side))
            {
                await flights.Shuffle(GetCardTransform(null), SleeveInfo);
            }
        });
    }

    private void PresentCount()
    {
        var count = Deck.Cards.Count;
        presentation.Present(() => CountValue = count);
    }
}
