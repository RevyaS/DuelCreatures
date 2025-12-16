using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;

public interface IInputProviderStrategy;

public interface ISelectCardsFromHand
{
    Task<List<CardBase>> SelectCardsFromHand();
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
    Task<RearGuard> SelectOwnRearguard();
}

public interface IRequestAttackPhaseAction
{
    Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions);
}

public interface ISelectOwnUnitCircle
{
    Task<UnitCircle> SelectOwnUnitCircle();
}

public interface ISelectOpponentFrontRow
{
    Task<UnitCircle> SelectOpponentFrontRow(UnitSelector selector);
}

public interface IQueryActivateSkill
{
    Task<bool> QueryActivateSkill(VanguardCard Invoker, VanguardAutomaticSkill Skill);
}

public interface ISelectCardsFromDamageZone
{
    Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount);
}