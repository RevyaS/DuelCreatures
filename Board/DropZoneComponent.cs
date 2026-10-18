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
            PresentArrivals(list);
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
            PresentArrivals([card]);
        }
    }

    private void PresentArrivals(List<CardBase> cards)
    {
        // Cards another location still shows (the guard zone until the battle ends)
        // arrive later, when that location lets them go
        var top = TopCardOfGame(includeHeld: false);
        foreach(var card in cards.Where(card => !IsHeld(card)))
        {
            presentation.PresentArrival(card, CardMove.ToDropZone, this, null, () => ShowTopCard(top));
        }
    }

    private bool IsHeld(CardBase card) => presentation?.Presenter.IsHeld(card) ?? false;

    public VanguardCard? TopCardOfGame(bool includeHeld)
    {
        return (VanguardCard?)DropZone.Cards.LastOrDefault(card => includeHeld || !IsHeld(card));
    }

    public void ShowTopCard(VanguardCard? card)
    {
        topCard = card;
        Render();
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