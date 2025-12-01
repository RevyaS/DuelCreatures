using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using Godot;

public partial class InputProvider : Control
{
    DuelCreaturesBoard board;
    public DuelCreaturesBoard Board => board;

    #region States
    bool DragHandToZone = false;
    #endregion


    SelectCardsFromHandComponent SelectCardsFromHandComponent;

    public override void _Ready()
    {
        board = GetNode<DuelCreaturesBoard>($"%{nameof(DuelCreaturesBoard)}");
        SelectCardsFromHandComponent = GetNode<SelectCardsFromHandComponent>($"%{nameof(SelectCardsFromHand)}");
        SelectCardsFromHandComponent.CardReturned += OnCardReturned;
        SelectCardsFromHandComponent.CardSelected += OnCardSelected;

        Board.HandCardPressed += OnHandCardPressed;
    }

    private void OnCardSelected(Card card)
    {
        board.PlayerHand.RemoveCard(card);
    }

    private void OnCardReturned(Card card)
    {
        board.PlayerHand.AddCard(card);
        card.CurrentlyDragged = false;
    }

    private void OnHandCardPressed(Card card)
    {
        VanguardCardComponent component = (VanguardCardComponent)card;
    }

    public async Task<List<CardBase>> SelectCardsFromHand()
    {
        DragHandToZone = true;
        TaskCompletionSource<List<CardBase>> completionSource = new();
        SelectCardsFromHandComponent.Activate(0, 5);
        SelectCardsFromHandComponent.ConfirmedCards += (cards) =>
        {
            SelectCardsFromHandComponent.Deactivate();
            completionSource.SetResult(cards.Cast<VanguardCardComponent>().Select(x => x.Card).Cast<CardBase>().ToList());
        };
        
        return await completionSource.Task;
    }
}
