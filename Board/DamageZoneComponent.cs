using System.Collections.Generic;
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
        eventBus.CardsAddedToDamageZone += OnCardsAddedToDamageZone;
        eventBus.CardTakenFromDamageZone += OnCardTakenFromDamageZone;
    }

    private void OnCardTakenFromDamageZone(DamageZone zone, CardBase card)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            RemoveCard(card);
        }
    }

    private void OnCardsAddedToDamageZone(DamageZone zone, List<CardBase> list)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            foreach(var card in list)
            {
                AddCard(SceneFactory.CreateVanguardCard((VanguardCard)card));
            }
        }
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