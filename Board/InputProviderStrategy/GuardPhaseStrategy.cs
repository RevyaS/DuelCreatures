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
                
        while(true)
        {
            TaskCompletionSource<Card?> completionSource = new();
            Action<Card> selectionHandler = (card) =>
            {
                Board.DisableGuardDropping();
                Board.PlayerHand.RemoveCard(card);
                Board.GuardZone.AddCard(card);
                completionSource.SetResult(card);
            };
            Board.GuardZone.CardDropped += selectionHandler; 

            Action<CardBaseComponent> handDraggedHandler = (card) =>
            {
                Board.EnableGuardDropping();
            };
            Board.PlayerHand.CardDragging += handDraggedHandler;

            Action confirmGuardHandler = () =>
            {
                completionSource.SetResult(null);
            };
            Board.LeftButtonPressed += confirmGuardHandler;

            var result = await completionSource.Task;
            Board.GuardZone.CardDropped -= selectionHandler; 
            Board.PlayerHand.CardDragging -= handDraggedHandler;
            Board.LeftButtonPressed -= confirmGuardHandler; 

            if(result is null)
            {
                break;
            } 

            selectedCards.Add((VanguardCard)result.CurrentCard);
            GuardedCircle.UpdatePower(basePower + selectedCards.Sum(x => x.Power));

            result.CurrentlyDragged = false;
        }

        Board.HideLeftButton();
        Board.DisablePlayerHandDragging();

        return selectedCards.Cast<CardBase>().ToList();
    }
}