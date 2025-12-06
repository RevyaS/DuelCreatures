using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class HandComponent : CardLineDynamic, IEventBusUtilizer
{
    Hand Hand = null!;
    
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
        }
    }

    private void OnCardAddedToHand(CardBase newCard, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            AddCard(SceneFactory.CreateVanguardCard((VanguardCard)newCard));
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

    public void BindHand(Hand hand)
    {
        Hand = hand;
    }
}
