using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class HandComponent : CardLineDynamic, ICardSpaceBindable<Hand>, IEventBusUtilizer
{
    public Hand Hand { get; private set; } = null!;

    const int HandLimit = 8;
    
    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.CardAddedToHand += OnCardAddedToHand;
        eventBus.CardTakenFromHand += OnCardTakenFromHand;
    }

    private void OnCardTakenFromHand(CardBase card, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            RemoveCard(card);

            UpdateSeparation();
        }
    }

    private void OnCardAddedToHand(CardBase newCard, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            AddCard(SceneFactory.CreateVanguardCard((VanguardCard)newCard));

            UpdateSeparation();
        }
    }

    private void UpdateSeparation()
    {
        if (CardCount > HandLimit)
        {
            var separation = (HandLimit - CardCount) * 20;
            GD.Print("New Separation: ", separation);
            Separation = separation;
        }
        else
        {
            Separation = 0;
        }
    }

    public void ShowContainerOfCard(VanguardCard card)
    {
        var cardComponent = FindCard((containedCard) => ReferenceEquals(containedCard.CurrentCard, card));
        if(cardComponent is not null)
        {
            cardComponent.Show();
        }
    }

    public void Bind(Hand hand)
    {
        Hand = hand;
    }

    public void SetGuardMode(bool value)
    {
        ContainerNodeManager.ApplyToChildren<CardContainer>(child =>
        {
            var containedCard = child.GetChild<VanguardCardComponent>(0);
            containedCard.GuardMode = value;
        });
    }
}
