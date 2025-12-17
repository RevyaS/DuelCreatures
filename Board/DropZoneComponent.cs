using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;

public partial class DropZoneComponent: CardVerticalStack, IEventBusUtilizer, ISetupCardList
{
    DropZone DropZone = null!;
    CardList CardList = null!;

    public void BindDropZone(DropZone dropZone)
    {
        DropZone = dropZone;
    }

    public void SetEventBus(VanguardEventBus eventBus)
    {
        eventBus.CardAddedToDropZone += OnCardAddedToDropZone;
        eventBus.CardsAddedToDropZone += OnCardsAddedToDropZone;
    }

    private void OnCardsAddedToDropZone(DropZone zone, List<CardBase> list)
    {
        if(ReferenceEquals(DropZone, zone))
        {
            Render();
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
            Render();
        }
    }

    private void Render()
    {
        if (DropZone.Cards.Count > 0)
        {
            SetCard((VanguardCard)DropZone.Cards.Last());
        }
        else
        {
            ClearCard();
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