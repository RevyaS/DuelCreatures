using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Predefined.Vanguard;

public class SkillExecutionStrategy(DuelCreaturesBoard Board, VanguardPlayArea PlayArea, CardList CardList, SelectFromCardList SelectFromCardList) : IInputProviderStrategy, ISelectOpponentCircle, IQueryActivateSkill, ISelectCardsFromDamageZone, ISelectCardsFromSoul
{
    // QueryActivateSkill
    public async Task<bool> QueryActivateSkill(VanguardCard Invoker, VanguardAutomaticSkill Skill)
    {
        CardList.Show("Select Skill to Activate", [Invoker]);
        CardList.BaseDroppable = true;
        CardList.CardsDraggable = true;
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

        Action cardDroppedHandler = () =>
        {
            completionSource.SetResult(true);
        };
        CardList.CardDroppedOutside += cardDroppedHandler;

        Action onClosedHandler = () =>
        {
            completionSource.SetResult(false);
        };
        CardList.OnClosed += onClosedHandler;

        var result = await completionSource.Task;

        CardList.CardDroppedOutside -= cardDroppedHandler;
        CardList.OnClosed -= onClosedHandler;
        CardList.BaseDroppable = false;
        CardList.CardsDraggable = false;
        CardList.Hide();
        return result;
    }

    // SoulBlast
    public async Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
    {
        SelectFromCardList.Show("Soul", $"Soul Blast ({amount})", amount, amount, PlayArea.Soul.Cards.Cast<VanguardCard>().ToList());

        TaskCompletionSource<List<VanguardCard>> completionSource = new();

        Action<List<Card>> onConfirmHandler = (cards) =>
        {
            completionSource.SetResult(cards.Select(x => x.CurrentCard).Cast<VanguardCard>().ToList());
        };

        SelectFromCardList.ConfirmedCards += onConfirmHandler;

        var result = await completionSource.Task;

        SelectFromCardList.ConfirmedCards -= onConfirmHandler;
        SelectFromCardList.Deactivate();
        
        return result;
    }

    // CounterBlast
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

    public async Task<UnitCircle> SelectOpponentCircle(UnitSelector selector)
    {
        Board.EnableSelectOppCircle(selector);
        TaskCompletionSource<UnitCircle> completionSource = new();
        
        Action<UnitCircleComponent> oppCircleSelectedHandler = (uc) =>
        {
            completionSource.SetResult(uc.UnitCircle);
        };
        Board.OppCircleSelected += oppCircleSelectedHandler;

        var result = await completionSource.Task;
        Board.DisableSelectOppUnitCircle();
        Board.OppCircleSelected -= oppCircleSelectedHandler;

        return result;
    }
}