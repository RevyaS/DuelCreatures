using System.Collections.Generic;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Vanguard;

/// <summary>
/// The questions Vanguard asks a player (IVanguardPlayerInputProvider), as typed keys: a strategy pre-answers them
/// (<see cref="PreAnswers"/>) and narrows what a prompt offers (<see cref="Narrowing"/>) through these.
/// </summary>
public static class VanguardQuestions
{
    public static readonly Question<CardBase> CardFromHand = new(nameof(CardFromHand));
    public static readonly Question<CardBase?> CardFromHandOrNot = new(nameof(CardFromHandOrNot));
    public static readonly Question<List<CardBase>> CardsFromHand = new(nameof(CardsFromHand));
    public static readonly Question<IMainPhaseAction> MainPhaseAction = new(nameof(MainPhaseAction));
    public static readonly Question<IAttackPhaseAction> AttackPhaseAction = new(nameof(AttackPhaseAction));
    public static readonly Question<UnitCircle> OwnCircle = new(nameof(OwnCircle));
    public static readonly Question<UnitCircle> OpponentCircle = new(nameof(OpponentCircle));
    public static readonly Question<List<UnitCircle>> OwnCircles = new(nameof(OwnCircles));
    public static readonly Question<List<UnitCircle>> OwnCirclesRange = new(nameof(OwnCirclesRange));
    public static readonly Question<VanguardActivationSkill> SkillToActivate = new(nameof(SkillToActivate));
    public static readonly Question<PendingSkill> SkillToInvoke = new(nameof(SkillToInvoke));
    public static readonly Question<VanguardCard> CardFromDamageZone = new(nameof(CardFromDamageZone));
    public static readonly Question<VanguardCard> CardFromDeck = new(nameof(CardFromDeck));
    public static readonly Question<List<VanguardCard>> CardsFromDamageZone = new(nameof(CardsFromDamageZone));
    public static readonly Question<List<VanguardCard>> CardsFromSoul = new(nameof(CardsFromSoul));
    public static readonly Question<bool> ActivateTrigger = new(nameof(ActivateTrigger));
    public static readonly Question<VanguardSkills.SkillAction> SkillOption = new(nameof(SkillOption));
}
