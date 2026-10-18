using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class TriggerZoneComponent : CardArea, ICardSpaceBindable<TriggerZone>, IEventBusUtilizer
{
    TriggerZone TriggerZone = null!;
    PresentationContext? presentation = null;

    // Where checked cards come from
    public ICardLocation? DeckLocation { get; set; }

    public void Bind(TriggerZone triggerZone)
    {
        TriggerZone = triggerZone;
    }

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.CardAddedToTriggerZone += OnCardAddedToTriggerZone;
        eventBus.CardRemovedFromTriggerZone += OnCardRemovedFromTriggerZone;
    }

    private Task OnCardRemovedFromTriggerZone(TriggerZone zone, CardBase? removedCard)
    {
        if(ReferenceEquals(zone, TriggerZone) && presentation is not null)
        {
            // Keep the checked card visible for a moment (drive/damage check reveal).
            // It's held in the presentation queue, so it also delays the next prompt.
            var side = presentation.Side;
            presentation.Present(async () =>
            {
                if(PresentationConfig.CheckReveal.Includes(side) && PresentationConfig.CheckRevealHold > 0)
                {
                    await ToSignal(GetTree().CreateTimer(PresentationConfig.CheckRevealHold), SceneTreeTimer.SignalName.Timeout);
                }
                if(removedCard is not null)
                {
                    presentation.Leaving(removedCard, this);
                }
                ShowCard(null);
            });
        }
        return Task.CompletedTask;
    }

    private void ShowCard(VanguardCard? card)
    {
        if(card is null) {
            ClearCard();
        } else {
            AddCard(SceneFactory.CreateVanguardCard(card));
        }
    }

    private Task OnCardAddedToTriggerZone(TriggerZone zone)
    {
        if(ReferenceEquals(zone, TriggerZone))
        {
            var card = (VanguardCard?)zone.Card;
            if(card is null)
            {
                presentation.Present(() => ShowCard(null));
            }
            else
            {
                presentation.PresentArrival(card, CardMove.DeckToTriggerZone, this, DeckLocation, () => ShowCard(card), faceUp: true);
            }
        }
        return Task.CompletedTask;
    }
}
