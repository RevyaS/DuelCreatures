using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public class SkillExecutionStrategy(DuelCreaturesBoard Board, VanguardPlayArea PlayArea, CardList CardList, SelectFromCardList SelectFromCardList) : BaseStrategy(Board), ISelectOpponentCircle, IQueryActivateSkill, ISelectCardsFromDamageZone, ISelectCardsFromSoul, ISelectCardFromDeck, ISelectCardFromHand, ISelectCardsFromHand, ISelectOwnUnitCircle
{
    // QueryActivateSkill
    public async Task<bool> QueryActivateSkill(VanguardCard Invoker, VanguardAutomaticSkill Skill)
    {
        CardList.Show("Select Skill to Activate", [Invoker]);
        CardList.BaseDroppable = true;
        CardList.CardsDraggable = true;
        TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

        Action<Card> cardDroppedHandler = (card) =>
        {
            completionSource.SetResult(true);
        };
        CardList.CardDroppedOutside += cardDroppedHandler;

        Action onClosedHandler = () =>
        {
            completionSource.SetResult(false);
        };
        CardList.OnClosed += onClosedHandler;

        GD.Print("Waiting for skill activation input...");
        var result = await completionSource.Task;

        GD.Print("Skill activation input received: ", result);
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
        GameBoard.PushPlayerPhaseIndicatorText($"Counter Blast ({amount})");
        GameBoard.ShowLeftButton(TextConstants.Confirm);
        GameBoard.DisableLeftButton();
        List<VanguardCard> cards = new();
        List<Card> options = GameBoard.PlayerArea.DamageZone.GetFaceUpCards().ToList();

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
                GameBoard.EnableLeftButton(cards.Count == amount);
            }
        };
        GameBoard.PlayerArea.DamageZone.CardLongPressed += cardLongPressedHandler;

        Action leftButtonHandler = completionSource.SetResult;
        GameBoard.LeftButtonPressed += leftButtonHandler;

        await completionSource.Task;

        GameBoard.PlayerArea.DamageZone.CardLongPressed -= cardLongPressedHandler;
        GameBoard.LeftButtonPressed -= leftButtonHandler;
        GameBoard.EnableLeftButton();
        GameBoard.HideLeftButton();
        GameBoard.PopPlayerPhaseIndicatorText();
        return cards;
    }

    public async Task<UnitCircle> SelectOpponentCircle(UnitSelector selector)
    {
        GameBoard.EnableSelectOppCircle(selector);
        TaskCompletionSource<UnitCircle> completionSource = new();
        
        Action<UnitCircleComponent> oppCircleSelectedHandler = (uc) =>
        {
            completionSource.SetResult(uc.UnitCircle);
        };
        GameBoard.OppCircleSelected += oppCircleSelectedHandler;

        var result = await completionSource.Task;
        GameBoard.DisableSelectOppUnitCircle();
        GameBoard.OppCircleSelected -= oppCircleSelectedHandler;

        return result;
    }

    public async Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade)
    {
        var selection = PlayArea.Deck.Cards.Cast<VanguardCard>().Where(x => minGrade <= x.Grade && x.Grade <= maxGrade).ToList();
        CardList.Show("Select card from deck", selection);
        CardList.BaseDroppable = true;
        CardList.CardsDraggable = true;
        CardList.CanClose = false;
        TaskCompletionSource<VanguardCard> completionSource = new TaskCompletionSource<VanguardCard>();

        Action<Card> cardDroppedHandler = (card) =>
        {
            completionSource.SetResult((VanguardCard)card.CurrentCard);
        };
        CardList.CardDroppedOutside += cardDroppedHandler;

        var result = await completionSource.Task;

        CardList.CardDroppedOutside -= cardDroppedHandler;
        CardList.BaseDroppable = false;
        CardList.CardsDraggable = false;
        CardList.CanClose = true;
        CardList.Hide();
        return result;
    }

    public async Task<CardBase> SelectCardFromHand()
    {
        SelectFromCardList.Show("Hand", $"Discard", 1, 1, PlayArea.Hand.Cards.Cast<VanguardCard>().ToList());

        TaskCompletionSource<CardBase> completionSource = new();

        Action<List<Card>> onConfirmHandler = (cards) =>
        {
            completionSource.SetResult(cards.First().CurrentCard);
        };

        SelectFromCardList.ConfirmedCards += onConfirmHandler;

        var result = await completionSource.Task;

        SelectFromCardList.ConfirmedCards -= onConfirmHandler;
        SelectFromCardList.Deactivate();
        
        return result;
    }

    public async Task<List<CardBase>> SelectCardsFromHand(int minimum, int maximum)
    {
        SelectFromCardList.Show("Hand", $"Discard", minimum, maximum, PlayArea.Hand.Cards.Cast<VanguardCard>().ToList());

        TaskCompletionSource<List<CardBase>> completionSource = new();

        Action<List<Card>> onConfirmHandler = (cards) =>
        {
            completionSource.SetResult(cards.Select(x => x.CurrentCard).ToList());
        };

        SelectFromCardList.ConfirmedCards += onConfirmHandler;

        var result = await completionSource.Task;

        SelectFromCardList.ConfirmedCards -= onConfirmHandler;
        SelectFromCardList.Deactivate();
        
        return result;
    }

    public Task<UnitCircle> SelectOwnUnitCircle(UnitSelector selector)
    {
        return SelectUnitCircle(selector);
    }
}