using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;

public interface IInputProviderStrategy;

public interface ISelectCardsFromHand
{
    Task<List<CardBase>> SelectCardsFromHand(int minimum, int maximum);
}

public interface ISelectCardFromHandOrNot
{
    Task<CardBase?> SelectCardFromHandOrNot(VanguardCard currentVanguard);
}

public interface IRequestMainPhaseAction
{
    Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions);
}
public interface ISelectCardFromHand
{
    Task<CardBase> SelectCardFromHand();
}
public interface ISelectOwnRearguard
{
    Task<RearGuard> SelectOwnRearguard(UnitSelector unitSelector);
}

public interface IRequestAttackPhaseAction
{
    Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions);
}

public interface ISelectOwnUnitCircle
{
    Task<UnitCircle> SelectOwnUnitCircle();
}

public interface ISelectOpponentCircle
{
    Task<UnitCircle> SelectOpponentCircle(UnitSelector selector);
}

public interface IQueryActivateSkill
{
    Task<bool> QueryActivateSkill(VanguardCard Invoker, VanguardAutomaticSkill Skill);
}

public interface ISelectCardsFromDamageZone
{
    Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount);
}

public interface ISelectCardsFromSoul
{
    Task<List<VanguardCard>> SelectCardsFromSoul(int amount);
}

public interface ISelectSkillToActivate
{
    Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills);
}

public interface ISelectCardFromDeck
{
    Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade);
}

public interface ISelectCardFromDamageZone
{
    Task<VanguardCard> SelectCardFromDamageZone();
}