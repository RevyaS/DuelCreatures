using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;

/// <summary>
/// Every question Vanguard asks a player (IVanguardPlayerInputProvider's), to answer through the UI.
/// DefaultVanguardAnswers answers all of them (a new question there is a build error until it does); strategies
/// (VanguardStrategy) answer some and pass the rest down.
/// </summary>
public interface IVanguardAnswers
{
    Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions);
    Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions);
    Task<CardBase> SelectCardFromHand();
    Task<CardBase?> SelectCardFromHandOrNot();
    Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum);
    Task<UnitCircle> SelectOwnUnitCircle(UnitSelector selector);
    Task<UnitCircle> SelectOpponentCircle(UnitSelector selector);
    Task<List<UnitCircle>> SelectOwnUnitCircles(UnitSelector selector);
    Task<List<UnitCircle>> SelectOwnUnitCirclesRange(UnitSelector selector, int minimum, int maximum);
    Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills);
    Task<PendingSkill> SelectSkillToInvoke(IReadOnlyList<PendingSkill> skills);
    Task<VanguardCard> SelectCardFromDamageZone();
    Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade);
    Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount);
    Task<List<VanguardCard>> SelectCardsFromSoul(int amount);
    Task<bool> QueryActivateSkill(VanguardCard invoker, VanguardAutomaticSkill skill);
    Task<VanguardSkills.SkillAction> SelectSkillOption(IReadOnlyList<VanguardSkills.SkillAction> options, VanguardSkillContextObject context);
}

/// <summary>
/// A way of answering some questions (e.g. the main phase by dragging), stacked on the answers below it: override
/// what it answers; every other question goes to <see cref="Below"/> (base call). The input provider asks the top one.
/// </summary>
public abstract class VanguardStrategy(VanguardInput input, IVanguardAnswers below) : IVanguardAnswers
{
    protected VanguardInput Input { get; } = input;
    protected IVanguardAnswers Below { get; } = below;

    public virtual Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions) => Below.RequestMainPhaseAction(actions);
    public virtual Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions) => Below.RequestAttackPhaseAction(actions);
    public virtual Task<CardBase> SelectCardFromHand() => Below.SelectCardFromHand();
    public virtual Task<CardBase?> SelectCardFromHandOrNot() => Below.SelectCardFromHandOrNot();
    public virtual Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum) => Below.SelectCardsFromHandRange(minimum, maximum);
    public virtual Task<UnitCircle> SelectOwnUnitCircle(UnitSelector selector) => Below.SelectOwnUnitCircle(selector);
    public virtual Task<UnitCircle> SelectOpponentCircle(UnitSelector selector) => Below.SelectOpponentCircle(selector);
    public virtual Task<List<UnitCircle>> SelectOwnUnitCircles(UnitSelector selector) => Below.SelectOwnUnitCircles(selector);
    public virtual Task<List<UnitCircle>> SelectOwnUnitCirclesRange(UnitSelector selector, int minimum, int maximum) => Below.SelectOwnUnitCirclesRange(selector, minimum, maximum);
    public virtual Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills) => Below.SelectSkillToActivate(skills);
    public virtual Task<PendingSkill> SelectSkillToInvoke(IReadOnlyList<PendingSkill> skills) => Below.SelectSkillToInvoke(skills);
    public virtual Task<VanguardCard> SelectCardFromDamageZone() => Below.SelectCardFromDamageZone();
    public virtual Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade) => Below.SelectCardFromDeck(minGrade, maxGrade);
    public virtual Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount) => Below.SelectCardsFromDamageZone(amount);
    public virtual Task<List<VanguardCard>> SelectCardsFromSoul(int amount) => Below.SelectCardsFromSoul(amount);
    public virtual Task<bool> QueryActivateSkill(VanguardCard invoker, VanguardAutomaticSkill skill) => Below.QueryActivateSkill(invoker, skill);
    public virtual Task<VanguardSkills.SkillAction> SelectSkillOption(IReadOnlyList<VanguardSkills.SkillAction> options, VanguardSkillContextObject context) => Below.SelectSkillOption(options, context);
}
