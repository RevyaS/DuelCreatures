using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class DamageZoneComponent : CardLineStatic, IEventBusUtilizer
{
    DamageZone DamageZone = null!;
    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.CardAddedToDamageZone += OnCardAddedToDamageZone;
    }

    private void OnCardAddedToDamageZone(DamageZone zone, CardBase card)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            AddCard(SceneFactory.CreateVanguardCard((VanguardCard)card));
        }
    }

    public void BindDamageZone(DamageZone damageZone)
    {
        DamageZone = damageZone;
    }
}