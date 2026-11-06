using System.Collections.Generic;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.YuGiOh;

/// <summary>
/// The questions YuGiOh asks a player (IYuGiOhPlayerInputProvider), as typed keys: a strategy pre-answers them
/// (<see cref="PreAnswers"/>) and narrows what a prompt offers (<see cref="Narrowing"/>) through these.
/// </summary>
public static class YuGiOhQuestions
{
    public static readonly Question<CardBase> CardFromHand = new(nameof(CardFromHand));
    public static readonly Question<CardBase?> CardFromHandOrNot = new(nameof(CardFromHandOrNot));
    public static readonly Question<List<CardBase>> CardsFromHand = new(nameof(CardsFromHand));
    public static readonly Question<IMainPhaseAction> MainPhaseAction = new(nameof(MainPhaseAction));
    public static readonly Question<IAttackPhaseAction> AttackPhaseAction = new(nameof(AttackPhaseAction));
    public static readonly Question<IYuGiOhZone> OwnZone = new(nameof(OwnZone));
    public static readonly Question<IYuGiOhZone> OpponentZone = new(nameof(OpponentZone));
    public static readonly Question<List<IYuGiOhZone>> OwnZones = new(nameof(OwnZones));
    public static readonly Question<YuGiOhNonPassiveSkill> SkillToActivate = new(nameof(SkillToActivate));
    public static readonly Question<bool> SetCard = new(nameof(SetCard));
    public static readonly Question<YuGiOhCard> CardFromExtraDeck = new(nameof(CardFromExtraDeck));
    public static readonly Question<YuGiOhCard> CardFromDeck = new(nameof(CardFromDeck));
    public static readonly Question<YuGiOhCard> CardFromGraveyard = new(nameof(CardFromGraveyard));
    public static readonly Question<YuGiOhCard> CardFromOpponentHand = new(nameof(CardFromOpponentHand));
    public static readonly Question<YuGiOhCard> CardFromOpponentGraveyard = new(nameof(CardFromOpponentGraveyard));
    public static readonly Question<string> CardName = new(nameof(CardName));
    public static readonly Question<List<YuGiOhCard>> MaterialsOfXYZ = new(nameof(MaterialsOfXYZ));
    public static readonly Question<YuGiOhChainResponse?> ChainResponse = new(nameof(ChainResponse));
    public static readonly Question<bool> ActivateTrigger = new(nameof(ActivateTrigger));
    public static readonly Question<YuGiOhSkills.SkillAction> SkillOption = new(nameof(SkillOption));
}
