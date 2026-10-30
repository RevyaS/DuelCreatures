using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Evaluations;

public class GuardPhaseStrategy(VanguardBoard Board, UnitCircleComponent GuardedCircle, VanguardPlayArea PlayArea, GuardState GuardState) : IInputProviderStrategy, ISelectCardsFromHand, ISelectOwnUnitCircles
{
    List<UnitCircle> intercepts = [];
    public async Task<List<CardBase>> SelectCardsFromHand(int minimum, int maximum)
    {
        Board.PushPlayerPhaseIndicatorText("Guard Phase");
        Board.ShowLeftButton(TextConstants.ConfirmGuard);
        Board.EnablePlayerHandDragging();
        Board.EnableGuardDropping();

        // Guards can be dragged back to the hand, intercepting units can't
        // (they're returned through their circle's Deselect button)
        HashSet<CardBase> interceptingCards = [];
        Board.GuardZone.CanDragCard = card => !interceptingCards.Contains(card.CurrentCard);
        Board.GuardZone.OutlineCard = card => interceptingCards.Contains(card.CurrentCard) ? VanguardPresentationConfig.InterceptOutline : null;
        Board.EnableGuardDragging();

        Board.PlayerHand.SetGuardMode(true);
        Board.EnableSelectOwnUnitCircle(new(PlayArea.SelectCirclesExcept(UnitCircleEnum.FRONT_REARGUARD, GuardState.TargetCircle), Skill: UnitSkillFilterEnum.INTERCEPT_ONLY));
        intercepts = [];

        List<VanguardCard> selectedCards = [];
        List<VanguardCard> selectedInterceptCards = [];
        // The unit being guarded, which isn't always the vanguard
        int basePower = GuardedCircle.UnitCircle.GetOverallPower();

        bool returningCard = false;
        bool interceptCard = false;
                
        while(true)
        {
            TaskCompletionSource<Card?> completionSource = new();

            Action<UnitCircleComponent> playerCircleSelectedHandler = (uc) =>
            {
                Card currentCard = SceneFactory.CreateVanguardCard((VanguardCard)uc.CurrentCard!.CurrentCard);
                interceptingCards.Add(currentCard.CurrentCard);
                intercepts.Add(uc.UnitCircle);
                uc.ClearCard();
                Board.GuardZone.AddCard(currentCard);
                returningCard = false;
                interceptCard = true;
                completionSource.SetResult(currentCard);
            };
            Board.PlayerCircleSelected += playerCircleSelectedHandler;

            Action<UnitCircleComponent> playerCircleDeselectedHandler = (uc) =>
            {
                intercepts.Remove(uc.UnitCircle);
                interceptingCards.Remove(uc.UnitCircle.Card!);
                Board.GuardZone.RemoveCard(uc.UnitCircle.Card!);
                uc.SetCard(uc.UnitCircle.Card!);
                returningCard = true;
                interceptCard = true;
                completionSource.SetResult(uc.CurrentCard);
            };
            Board.PlayerCircleDeselected += playerCircleDeselectedHandler;

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
            Board.PlayerCircleSelected -= playerCircleSelectedHandler;
            Board.PlayerCircleDeselected -= playerCircleDeselectedHandler;

            if(result is null)
            {
                break;
            } 

            if(!returningCard)
            {
                if(interceptCard)
                {
                    selectedInterceptCards.Add((VanguardCard)result.CurrentCard);
                } else
                {
                    selectedCards.Add((VanguardCard)result.CurrentCard);
                }
            } else
            {
                if(interceptCard)
                {
                    selectedInterceptCards.Remove((VanguardCard)result.CurrentCard);
                } else
                {
                    selectedCards.Remove((VanguardCard)result.CurrentCard);
                }
            }
            // Interceptors can have "when placed on (GC)" shield (e.g. Security Guardian), so ask ArC
            GuardedCircle.UpdatePower(basePower + selectedCards.Sum(x => x.Guard)
                + selectedInterceptCards.Sum(x => VanguardSkillJudge.ExpectedGuardianShield(x, intercepting: true)));
            result.CurrentlyDragged = false;
        }

        Board.DisableGuardDragging();
        Board.GuardZone.CanDragCard = null;
        Board.GuardZone.OutlineCard = null;
        Board.DisableSelectOwnUnitCircle();
        Board.HideLeftButton();
        Board.DisablePlayerHandDragging();
        Board.DisablePlayerHandDropping();
        Board.PlayerHand.SetGuardMode(false);
        Board.PopPlayerPhaseIndicatorText();

        return selectedCards.Cast<CardBase>().ToList();
    }

    public Task<List<UnitCircle>> SelectOwnUnitCircles(UnitSelector selector)
    {
        return Task.FromResult(intercepts);
    }
}