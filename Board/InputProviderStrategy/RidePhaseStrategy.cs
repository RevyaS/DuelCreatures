using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public class RidePhaseStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy
{
    public async Task<CardBase?> SelectCardFromHandOrNot(VanguardCard currentVanguard)
    {
        Board.ShowEndPhaseButton();
        Board.EnablePlayerVanguardDropping();
        
        VanguardCard? newVanguard = null;

        while(true)
        {
            TaskCompletionSource<Card?> completionSource = new();
            Action<Card> selectionHandler = completionSource.SetResult;
            Board.PlayerVanguard.CardDropped += selectionHandler; 
            Action endPhaseHandler = () =>
            {
                completionSource.SetResult(null);
            };
            Board.EndPhasePressed += endPhaseHandler;

            var result = await completionSource.Task;
            Board.PlayerVanguard.CardDropped -= selectionHandler; 
            Board.EndPhasePressed -= endPhaseHandler; 

            if(result is null)
            {
                break;
            }

            newVanguard = (VanguardCard)result.CurrentCard;
            var exception = VanguardValidator.ValidateRide(currentVanguard, newVanguard);

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
        Board.HideEndPhaseButton();

        return newVanguard;
    }

    public Task<List<CardBase>> SelectCardsFromHand()
    {
        throw new NotImplementedException();
    }
}