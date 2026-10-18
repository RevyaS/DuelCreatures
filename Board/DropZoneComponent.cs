using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

[Tool]
public partial class DropZoneComponent: CardVerticalStack, ICardSpaceBindable<DropZone>, IEventBusUtilizer, ISetupCardList
{
    DropZone DropZone = null!;
    CardList CardList = null!;
    PresentationContext? presentation = null;

    // What the stack shows, as of the last presented event (not the live game)
    VanguardCard? topCard = null;

    public void Bind(DropZone dropZone)
    {
        DropZone = dropZone;
    }

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.CardAddedToDropZone += OnCardAddedToDropZone;
        eventBus.CardsAddedToDropZone += OnCardsAddedToDropZone;
    }

    private void OnCardsAddedToDropZone(DropZone zone, List<CardBase> list)
    {
        if(ReferenceEquals(DropZone, zone))
        {
            PresentTopCard();
        }
    }

    public void SetCardList(CardList cardList)
    {
        CardList = cardList;
    }

    private void OnCardAddedToDropZone(DropZone zone, CardBase card)
    {
        if(ReferenceEquals(DropZone, zone))
        {
            PresentTopCard();
        }
    }

    private void PresentTopCard()
    {
        var top = (VanguardCard?)DropZone.Cards.LastOrDefault();
        presentation.Present(() =>
        {
            topCard = top;
            Render();
        });
    }

    protected override void BeforeRender()
    {
        if (DropZone is not null)
        {
            if (topCard is not null)
            {
                SetCard(topCard);
            }
            else
            {
                ClearCard();
            }
        }
    }

    protected override void OnPressed()
    {
        if(DropZone.Cards.Count > 0)
        {
            CardList.Show("Drop Zone", DropZone.Cards.Cast<VanguardCard>().ToList());
        }
    }
}