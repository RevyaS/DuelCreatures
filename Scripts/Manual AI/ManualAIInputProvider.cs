
using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Flow;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Setup;

public partial class AIInputProvider(VanguardPlayArea playArea, GameContext gameContext) : IVanguardPlayerInputProvider
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
        return Task.Run(() => DecideAtackPhaseAction(actions));
    }

    public Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        return Task.Run(() => DecideMainPhaseAction(actions));
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
        if(GameContext.GameState is RidePhaseState)
        {
            return Task.Run(() => SelectRideCard());
        }

        throw new System.NotImplementedException($"Select Card From Hand Or Not State {GameContext.GameState.GetType().Name} not implemented yet");
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
}