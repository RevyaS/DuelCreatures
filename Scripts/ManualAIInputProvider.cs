
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Setup;
using ArC.Common.Extensions;

public class AIInputProvider(VanguardPlayArea playArea, GameContext gameContext) : IVanguardPlayerInputProvider
{
    public VanguardPlayArea PlayArea => playArea;
    public Hand Hand => PlayArea.Hand;

    public VanguardPlayArea OpponentPlayArea => throw new System.NotImplementedException();

    public VanguardSkillService SkillService => throw new System.NotImplementedException();

    GameContext GameContext => gameContext;
    PlayAreaBase IPlayerInputProvider.PlayArea => PlayArea;

    public Task<bool> QueryActivateSkill(VanguardSkillCost SkillCost)
    {
        throw new System.NotImplementedException();
    }

    public Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        throw new System.NotImplementedException();
    }

    public Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        throw new System.NotImplementedException();
    }

    public Task<VanguardCard> SelectCardFromDamageZone()
    {
        throw new System.NotImplementedException();
    }

    public Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade)
    {
        throw new System.NotImplementedException();
    }

    public Task<CardBase> SelectCardFromHand()
    {
        throw new System.NotImplementedException();
    }

    public Task<CardBase?> SelectCardFromHandOrNot()
    {
        throw new System.NotImplementedException();
    }

    public Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        if(GameContext.GameState is MulliganState)
        {
            return Task.Run(() => Mulligan(Hand.Cards));
        }
        throw new System.NotImplementedException($"Select Cards From Hand State {GameContext.GameState.GetType().Name} not implemented yet");
    }

    public Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
    {
        throw new System.NotImplementedException();
    }

    public Task<ArC.CardGames.Predefined.Vanguard.UnitCircle> SelectCircleToProvideCritical()
    {
        throw new System.NotImplementedException();
    }

    public Task<ArC.CardGames.Predefined.Vanguard.UnitCircle> SelectCircleToProvidePower()
    {
        throw new System.NotImplementedException();
    }

    public Task<ArC.CardGames.Predefined.Vanguard.UnitCircle> SelectOpponentFrontRow(UnitSelector selector)
    {
        throw new System.NotImplementedException();
    }

    public Task<RearGuard> SelectOwnRearguard()
    {
        throw new System.NotImplementedException();
    }

    public Task<ArC.CardGames.Predefined.Vanguard.UnitCircle> SelectOwnUnitCircle()
    {
        throw new System.NotImplementedException();
    }

    public Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills)
    {
        throw new System.NotImplementedException();
    }

    #region Manual Logic
    private List<CardBase> Mulligan(List<CardBase> handOriginal)
    {
        var hand = handOriginal.ToList();
        List<CardBase> toReturn = new();

        // Helper local function
        bool HasGrade(int grade) =>
            hand.Any(c => c is VanguardCard v && v.Grade == grade);

        // 1. Remove all G0s (except starter)
        toReturn.AddRange(hand.RemoveWhere<CardBase, VanguardCard>(
            card => card.Grade == 0
        ));

        // 2. Make sure we have G1
        if (!HasGrade(1))
        {
            // Mulligan a random lowest-value card (prefer highest grade)
            toReturn.Add(MulliganOneJunkCard(hand));
        }

        // 3. Make sure we have G2
        if (!HasGrade(2))
        {
            toReturn.Add(MulliganOneJunkCard(hand));
        }

        // 4. Make sure we have G3
        if (!HasGrade(3))
        {
            toReturn.Add(MulliganOneJunkCard(hand));
        }

        // 5. Remove duplicates: keep only 1 of each ride grade
        for (int grade = 1; grade <= 3; grade++)
        {
            var duplicates = hand
                .Where(c => c is VanguardCard v && v.Grade == grade)
                .Skip(1)
                .ToList();

            foreach (var d in duplicates)
            {
                hand.Remove(d);
                toReturn.Add(d);
            }
        }

        return toReturn;
    }
    private CardBase MulliganOneJunkCard(List<CardBase> hand)
    {
        // Priority: grade 3 > 2 > 1 > 0 (because extra G3 are worst)
        var ordered = hand
            .OrderByDescending(c => ((VanguardCard)c).Grade)
            .ToList();

        var card = ordered.First();
        hand.Remove(card);
        return card;
    }
    #endregion
}