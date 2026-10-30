using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;

[Tool]
public partial class DamageZoneComponent : CardLineStatic, ICardSpaceBindable<DamageZone>, IEventBusUtilizer<VanguardEventBus>
{
    DamageZone DamageZone = null!;
    PresentationContext? presentation = null;

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.CardAddedToDamageZone += OnCardAddedToDamageZone;
        eventBus.CardsAddedToDamageZone += OnCardsAddedToDamageZone;
        eventBus.CardTakenFromDamageZone += OnCardTakenFromDamageZone;
        eventBus.CardFaceChanged += OnCardFaceChanged;
    }

    private void OnCardFaceChanged(ICardSpace space, CardBase card, bool faceUp)
    {
        if(!ReferenceEquals(space, DamageZone) || presentation is null) return;

        var side = presentation.Side;
        var flights = presentation.Presenter.Flights;
        presentation.Present(async () =>
        {
            var node = FindCardNode(card);
            if(node is null || node.IsFront == faceUp) return;

            if(flights is not null && Presenter.Animates(side))
            {
                await flights.Flip(node, faceUp);
            }
            else
            {
                node.IsFront = faceUp;
            }
        });
    }

    private void OnCardTakenFromDamageZone(DamageZone zone, CardBase card)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            presentation.Present(() =>
            {
                presentation.Leaving(card, this);
                RemoveCard(card);
            });
        }
    }

    private void OnCardsAddedToDamageZone(DamageZone zone, List<CardBase> list)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            foreach(var card in list.ToList())
            {
                PresentArrival(card);
            }
        }
    }

    private void OnCardAddedToDamageZone(DamageZone zone, CardBase card)
    {
        if(ReferenceEquals(zone, DamageZone))
        {
            PresentArrival(card);
        }
    }

    private void PresentArrival(CardBase card)
    {
        presentation.PresentArrival(card, VanguardMoves.TriggerToDamage, this, null,
            () => AddCard(SceneFactory.CreateVanguardCard((VanguardCard)card)));
    }

    public void Bind(DamageZone damageZone)
    {
        DamageZone = damageZone;
    }
}