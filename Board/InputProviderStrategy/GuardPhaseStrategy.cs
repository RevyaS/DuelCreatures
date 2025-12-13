using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;

public class GuardPhaseStrategy(DuelCreaturesBoard Board, UnitCircleComponent GuardedCircle) : IInputProviderStrategy, ISelectCardsFromHand
{
    public async Task<List<CardBase>> SelectCardsFromHand()
    {
        Board.ShowLeftButton(TextConstants.ConfirmGuard);
        Board.EnablePlayerHandDragging();
        Board.EnableGuardDropping();

        List<VanguardCard> selectedCards = [];
        int basePower = Board.PlayerVanguard.UnitCircle.GetOverallPower();

        bool returningCard = false;
                
        while(true)
        {
            TaskCompletionSource<Card?> completionSource = new();
            Action<Card> guardDroppedHandler = (card) =>
            {
                Board.DisableGuardDropping();
                Board.PlayerHand.RemoveCard(card);
                Board.GuardZone.AddCard(card);
                completionSource.SetResult(card);
            };
            Board.GuardZone.CardDropped += guardDroppedHandler; 

            Action<CardBaseComponent> guardDraggedHandler = (card) =>
            {
                returningCard = true;
                Board.EnablePlayerHandDropping();
            };
            Board.GuardZone.CardDragging += guardDraggedHandler;

            Action<Card> handDroppedHandler = (card) =>
            {
                Board.DisablePlayerHandDropping();
                Board.GuardZone.RemoveCard(card, false);
                Board.PlayerHand.AddCard(card);
                completionSource.SetResult(card);
            };
            Board.PlayerHand.CardDropped += handDroppedHandler; 

            Action<CardBaseComponent> handDraggedHandler = (card) =>
            {
                returningCard = false;
                Board.EnableGuardDropping();
            };
            Board.PlayerHand.CardDragging += handDraggedHandler;

            Action confirmGuardHandler = () =>
            {
                completionSource.SetResult(null);
            };
            Board.LeftButtonPressed += confirmGuardHandler;

            var result = await completionSource.Task;
            Board.GuardZone.CardDropped -= guardDroppedHandler; 
            Board.GuardZone.CardDragging -= guardDraggedHandler;
            Board.PlayerHand.CardDropped -= handDroppedHandler; 
            Board.PlayerHand.CardDragging -= handDraggedHandler;
            Board.LeftButtonPressed -= confirmGuardHandler; 

            if(result is null)
            {
                break;
            } 

            if(!returningCard)
            {
                selectedCards.Add((VanguardCard)result.CurrentCard);
            } else
            {
                selectedCards.Remove((VanguardCard)result.CurrentCard);
            }
            GuardedCircle.UpdatePower(basePower + selectedCards.Sum(x => x.Guard));
            result.CurrentlyDragged = false;
        }

        Board.HideLeftButton();
        Board.DisablePlayerHandDragging();

        return selectedCards.Cast<CardBase>().ToList();
    }
}