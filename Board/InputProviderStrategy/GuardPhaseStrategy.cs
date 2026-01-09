using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;

public class GuardPhaseStrategy(DuelCreaturesBoard Board, UnitCircleComponent GuardedCircle, VanguardPlayArea PlayArea, GuardState GuardState) : IInputProviderStrategy, ISelectCardsFromHand, ISelectOwnUnitCircles
{
    List<UnitCircle> intercepts = [];
    public async Task<List<CardBase>> SelectCardsFromHand(int minimum, int maximum)
    {
        Board.PushPlayerPhaseIndicatorText("Guard Phase");
        Board.ShowLeftButton(TextConstants.ConfirmGuard);
        Board.EnablePlayerHandDragging();
        Board.EnableGuardDropping();

        Board.PlayerHand.SetGuardMode(true);
        Board.EnableSelectOwnUnitCircle(new(PlayArea.SelectCirclesExcept(UnitCircleEnum.FRONT_REARGUARD, GuardState.TargetCircle), Skill: UnitSkillFilterEnum.INTERCEPT_ONLY));
        intercepts = [];

        List<VanguardCard> selectedCards = [];
        List<VanguardCard> selectedInterceptCards = [];
        int basePower = Board.PlayerArea.Vanguard.UnitCircle.GetOverallPower();

        bool returningCard = false;
        bool interceptCard = false;
                
        while(true)
        {
            TaskCompletionSource<Card?> completionSource = new();

            Action<UnitCircleComponent> playerCircleSelectedHandler = (uc) =>
            {
                Card currentCard = SceneFactory.CreateVanguardCard((VanguardCard)uc.CurrentCard!.CurrentCard);
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
            GuardedCircle.UpdatePower(basePower + selectedCards.Sum(x => x.Guard) + selectedInterceptCards.Sum(x => x.Guard));
            result.CurrentlyDragged = false;
        }

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