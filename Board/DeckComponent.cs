using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class DeckComponent : CardVerticalStack, ICardSpaceBindable<Deck>, IEventBusUtilizer
{
    Deck Deck = null!;
    PresentationContext? presentation = null;

    public void Bind(Deck deck)
    {
        Deck = deck;
    }

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.AfterDeckDrawn += OnDeckDrawn;
    }

    private void OnDeckDrawn()
    {
        var count = Deck.Cards.Count;
        presentation.Present(() => CountValue = count);
    }
}
