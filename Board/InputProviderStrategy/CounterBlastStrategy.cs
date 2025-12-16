using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;

public class CounterBlastSkillStrategy(DuelCreaturesBoard Board) : IInputProviderStrategy, ISelectCardsFromDamageZone
{
    public async Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount)
    {
        Board.PushPlayerPhaseIndicatorText("Counter Blast");
        Board.ShowLeftButton(TextConstants.Confirm);
        Board.DisableLeftButton();
        List<VanguardCard> cards = new();
        List<Card> options = Board.PlayerDamageZone.GetFaceUpCards().ToList();

        TaskCompletionSource completionSource = new();

        Action<Card> cardLongPressedHandler = (card) =>
        {
            if(options.Contains(card, ReferenceEqualityComparer.Instance))
            {
                if(card.IsFront && cards.Count != amount)
                {
                    card.IsFront = false;
                    cards.Add((VanguardCard)card.CurrentCard);
                    
                }
                else
                {
                    card.IsFront = true;
                    cards.Remove((VanguardCard)card.CurrentCard);
                }
                Board.EnableLeftButton(cards.Count == amount);
            }
        };
        Board.PlayerDamageZone.CardLongPressed += cardLongPressedHandler;

        Action leftButtonHandler = completionSource.SetResult;
        Board.LeftButtonPressed += leftButtonHandler;

        await completionSource.Task;

        Board.PlayerDamageZone.CardLongPressed -= cardLongPressedHandler;
        Board.LeftButtonPressed -= leftButtonHandler;
        Board.EnableLeftButton();
        Board.HideLeftButton();
        Board.PopPlayerPhaseIndicatorText();
        return cards;
    }
}