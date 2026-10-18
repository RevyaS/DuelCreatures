using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class DamageZoneComponent : CardLineStatic, ICardSpaceBindable<DamageZone>, IEventBusUtilizer
{
    DamageZone DamageZone = null!;
    PresentationContext? presentation = null;

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.CardAddedToDamageZone += OnCardAddedToDamageZone;
        eventBus.CardsAddedToDamageZone += OnCardsAddedToDamageZone;
        eventBus.CardTakenFromDamageZone += OnCardTakenFromDamageZone;
    }

    private void OnCardTakenFromDamageZone(DamageZone zone, CardBase card)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            presentation.Present(() => RemoveCard(card));
        }
    }

    private void OnCardsAddedToDamageZone(DamageZone zone, List<CardBase> list)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            var cards = list.ToList();
            presentation.Present(() =>
            {
                foreach(var card in cards)
                {
                    AddCard(SceneFactory.CreateVanguardCard((VanguardCard)card));
                }
            });
        }
    }

    private void OnCardAddedToDamageZone(DamageZone zone, CardBase card)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            presentation.Present(() => AddCard(SceneFactory.CreateVanguardCard((VanguardCard)card)));
        }
    }

    public void Bind(DamageZone damageZone)
    {
        DamageZone = damageZone;
    }
}