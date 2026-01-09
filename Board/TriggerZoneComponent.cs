using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class TriggerZoneComponent : HorizontalCardArea, ICardSpaceBindable<TriggerZone>, IEventBusUtilizer
{
    [Export]
    public float RemoveCardDelayTime { get; set; } = 0;   

    TriggerZone TriggerZone = null!;
    public void Bind(TriggerZone triggerZone)
    {
        TriggerZone = triggerZone;
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.CardAddedToTriggerZone += OnCardAddedToTriggerZone;
        eventBus.CardRemovedFromTriggerZone += OnCardRemovedFromTriggerZone;
    }

    private async Task OnCardRemovedFromTriggerZone(TriggerZone zone)
    {
        if(ReferenceEquals(zone, TriggerZone))
        {
            await ToSignal(GetTree().CreateTimer(RemoveCardDelayTime), "timeout");
            Render();
        }
    }

    private void Render()
    {
        if(TriggerZone.IsEmpty) {
            ClearCard();
        } else {
            AddCard(SceneFactory.CreateVanguardCard((VanguardCard)TriggerZone.Card!));
        }
    }

    private Task OnCardAddedToTriggerZone(TriggerZone zone)
    {
        if(ReferenceEquals(zone, TriggerZone))
        {
            Render();
        }
        return Task.CompletedTask;
    }
}
