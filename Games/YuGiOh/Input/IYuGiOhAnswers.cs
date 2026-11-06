using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.YuGiOh;

/// <summary>
/// Every question YuGiOh asks a player (IYuGiOhPlayerInputProvider's), to answer through the UI. DefaultYuGiOhAnswers
/// answers all of them (a new question there is a build error until it does); strategies (YuGiOhStrategy) answer some
/// and pass the rest down.
/// </summary>
public interface IYuGiOhAnswers
{
    Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions);
    Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions);
    Task<CardBase> SelectCardFromHand();
    Task<CardBase?> SelectCardFromHandOrNot();
    Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum);
    Task<IYuGiOhZone> SelectOwnZone(ZoneSelector selector);
    Task<IYuGiOhZone> SelectOpponentZone(ZoneSelector selector);
    Task<List<IYuGiOhZone>> SelectOwnZonesRange(ZoneSelector selector, int minimum, int maximum);
    Task<YuGiOhNonPassiveSkill> SelectSkillToActivate(List<YuGiOhNonPassiveSkill> skills);
    Task<bool> QuerySetCard(YuGiOhCard card);
    Task<YuGiOhCard> SelectCardFromExtraDeck();
    Task<YuGiOhCard> SelectCardFromDeck();
    Task<YuGiOhCard> SelectCardFromGraveyard();
    Task<YuGiOhCard> SelectCardFromOpponentHand();
    Task<YuGiOhCard> SelectCardFromOpponentGraveyard();
    Task<string> DeclareCardName();
    Task<List<YuGiOhCard>> SelectXYZMaterials(MonsterZone zone, int amount);
    Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain);
    Task<bool> QueryActivateSkill(YuGiOhCard invoker, YuGiOhAutomaticSkill skill);
    Task<YuGiOhSkills.SkillAction> SelectSkillOption(IReadOnlyList<YuGiOhSkills.SkillAction> options, YuGiOhSkillContextObject context);
}

/// <summary>
/// A way of answering some questions (e.g. the main phase with clicks), stacked on the answers below it: override what
/// it answers; every other question goes to <see cref="Below"/> (base call). The input provider asks the top one.
/// </summary>
public abstract class YuGiOhStrategy(YuGiOhInput input, IYuGiOhAnswers below) : IYuGiOhAnswers
{
    protected YuGiOhInput Input { get; } = input;
    protected IYuGiOhAnswers Below { get; } = below;

    public virtual Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions) => Below.RequestMainPhaseAction(actions);
    public virtual Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions) => Below.RequestAttackPhaseAction(actions);
    public virtual Task<CardBase> SelectCardFromHand() => Below.SelectCardFromHand();
    public virtual Task<CardBase?> SelectCardFromHandOrNot() => Below.SelectCardFromHandOrNot();
    public virtual Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum) => Below.SelectCardsFromHandRange(minimum, maximum);
    public virtual Task<IYuGiOhZone> SelectOwnZone(ZoneSelector selector) => Below.SelectOwnZone(selector);
    public virtual Task<IYuGiOhZone> SelectOpponentZone(ZoneSelector selector) => Below.SelectOpponentZone(selector);
    public virtual Task<List<IYuGiOhZone>> SelectOwnZonesRange(ZoneSelector selector, int minimum, int maximum) => Below.SelectOwnZonesRange(selector, minimum, maximum);
    public virtual Task<YuGiOhNonPassiveSkill> SelectSkillToActivate(List<YuGiOhNonPassiveSkill> skills) => Below.SelectSkillToActivate(skills);
    public virtual Task<bool> QuerySetCard(YuGiOhCard card) => Below.QuerySetCard(card);
    public virtual Task<YuGiOhCard> SelectCardFromExtraDeck() => Below.SelectCardFromExtraDeck();
    public virtual Task<YuGiOhCard> SelectCardFromDeck() => Below.SelectCardFromDeck();
    public virtual Task<YuGiOhCard> SelectCardFromGraveyard() => Below.SelectCardFromGraveyard();
    public virtual Task<YuGiOhCard> SelectCardFromOpponentHand() => Below.SelectCardFromOpponentHand();
    public virtual Task<YuGiOhCard> SelectCardFromOpponentGraveyard() => Below.SelectCardFromOpponentGraveyard();
    public virtual Task<string> DeclareCardName() => Below.DeclareCardName();
    public virtual Task<List<YuGiOhCard>> SelectXYZMaterials(MonsterZone zone, int amount) => Below.SelectXYZMaterials(zone, amount);
    public virtual Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain) => Below.SelectChainResponse(chain);
    public virtual Task<bool> QueryActivateSkill(YuGiOhCard invoker, YuGiOhAutomaticSkill skill) => Below.QueryActivateSkill(invoker, skill);
    public virtual Task<YuGiOhSkills.SkillAction> SelectSkillOption(IReadOnlyList<YuGiOhSkills.SkillAction> options, YuGiOhSkillContextObject context) => Below.SelectSkillOption(options, context);
}
