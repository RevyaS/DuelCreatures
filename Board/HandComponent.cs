using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;
using Godot;

[Tool]
public partial class HandComponent : CardLineDynamic, ICardSpaceBindable<Hand>, IEventBusUtilizer
{
    public Hand Hand { get; private set; } = null!;
    PresentationContext? presentation = null;

    // Where drawn cards come from
    public ICardLocation? DrawSource { get; set; }

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.CardAddedToHand += OnCardAddedToHand;
        eventBus.CardTakenFromHand += OnCardTakenFromHand;
    }

    private void OnCardTakenFromHand(CardBase card, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            presentation.Present(() =>
            {
                presentation.Leaving(card, this);
                RemoveCard(card);
            });
        }
    }

    private void OnCardAddedToHand(CardBase newCard, Hand hand)
    {
        if(ReferenceEquals(Hand, hand))
        {
            presentation.PresentArrival(newCard,
                source => source is null || ReferenceEquals(source, DrawSource) ? CardMove.DeckToHand : CardMove.TriggerToHand,
                this, DrawSource,
                () => AddCard(SceneFactory.CreateVanguardCard((VanguardCard)newCard)));
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

    private bool guardMode = false;

    public void SetGuardMode(bool value)
    {
        guardMode = value;
        Render();
    }

    protected override void ConfigureCard(Card card)
    {
        base.ConfigureCard(card);
        if(card is VanguardCardComponent vanguardCard)
        {
            vanguardCard.GuardMode = guardMode;
        }
    }
}
