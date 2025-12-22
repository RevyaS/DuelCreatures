using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class DeckComponent : CardVerticalStack, IEventBusUtilizer
{
    Deck Deck = null!;

    public void BindDeck(Deck deck)
    {
        Deck = deck;
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.AfterDeckDrawn += OnDeckDrawn;
    }

    private void OnDeckDrawn()
    {
        CountValue = Deck.Cards.Count;
    }
}
