using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class HandComponent : CardLineDynamic
{
    VanguardEventBus EventBus;
    Hand Hand;

    public void SetEventBus(VanguardEventBus eventBus)
    {
        EventBus = eventBus;
        eventBus.CardAddedToHand += OnCardAddedToHand;
    }

    private void OnCardAddedToHand(CardBase newCard, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            AddCard(SceneFactory.CreateVanguardCard((VanguardCard)newCard));
        }
    }

    public void BindHand(Hand hand)
    {
        Hand = hand;
    }
}
