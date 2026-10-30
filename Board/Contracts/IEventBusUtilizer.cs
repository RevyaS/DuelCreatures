using ArC.CardGames.Vanguard;

public interface IEventBusUtilizer
{
    // Handlers snapshot what they need from the game when an event fires and
    // present it through the context's queue (see Presenter)
    void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation);
}