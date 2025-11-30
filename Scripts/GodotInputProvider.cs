using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using ArC.CardGames.Setup;
using Godot;

public class GodotInputProvider(InputProvider inputProviderComponent, VanguardPlayArea playArea) : IVanguardPlayerInputProvider
{
    public VanguardPlayArea PlayArea => playArea;

    public VanguardPlayArea OpponentPlayArea => throw new System.NotImplementedException();

    public VanguardSkillService SkillService => throw new System.NotImplementedException();

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

    public Task<CardBase> SelectCardFromHandOrNot()
    {
        throw new System.NotImplementedException();
    }

    public Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        GD.Print("Select card from hand requested");
        throw new System.NotImplementedException();
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