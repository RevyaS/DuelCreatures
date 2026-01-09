using System;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public class RidePhaseStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy, ISelectCardFromHandOrNot
{
    public async Task<CardBase?> SelectCardFromHandOrNot(VanguardCard currentVanguard)
    {
        Board.ShowLeftButton(TextConstants.EndPhase);
        Board.EnablePlayerHandDragging();
        Board.DisablePlayerRearguardDropping();
        
        VanguardCard? newVanguard = null;

        while(true)
        {
            TaskCompletionSource<Card?> completionSource = new();
            Action<Card> selectionHandler = completionSource.SetResult;
            Board.PlayerArea.Vanguard.CardDropped += selectionHandler; 
            Action endPhaseHandler = () =>
            {
                completionSource.SetResult(null);
            };
            Board.LeftButtonPressed += endPhaseHandler;

            Action<CardBaseComponent> handCardDraggingHandler = (card) =>
            {
                Board.EnablePlayerVanguardDropping();
            };
            Board.PlayerHand.CardDragging += handCardDraggingHandler;

            Action<CardBaseComponent> handCardDraggingCancelHandler = (card) =>
            {
                card.CurrentlyDragged = false;
                Board.DisablePlayerVanguardDropping();
            };
            Board.PlayerHand.CardDragCancelled += handCardDraggingCancelHandler;

            var result = await completionSource.Task;

            Board.PlayerArea.Vanguard.CardDropped -= selectionHandler; 
            Board.LeftButtonPressed -= endPhaseHandler; 
            Board.PlayerHand.CardDragging -= handCardDraggingHandler;
            Board.PlayerHand.CardDragCancelled -= handCardDraggingCancelHandler;

            if(result is null)
            {
                break;
            }

            newVanguard = (VanguardCard)result.CurrentCard;
            var exception = VanguardGameRules.ValidateRide(currentVanguard, newVanguard);

            if (exception is not null)
            {
                result.CurrentlyDragged = false;
                GD.PushError(exception.Message);
            } else
            {
                break;
            }
        }

        Board.DisablePlayerVanguardDropping();
        Board.HideLeftButton();
        Board.DisablePlayerHandDragging();

        return newVanguard;
    }
}