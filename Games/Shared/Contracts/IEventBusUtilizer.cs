using ArC.CardGames.Flow;

// A board component that presents a game's events. Handlers snapshot what they need from the game
// when an event fires and present it through the context's queue (see Presenter).
// Contravariant: one written for the base EventBus (e.g. a hand or a deck) works on any game's bus.
public interface IEventBusUtilizer<in TEventBus> where TEventBus : EventBus
{
    void SetEventBus(TEventBus eventBus, PresentationContext presentation);
}
