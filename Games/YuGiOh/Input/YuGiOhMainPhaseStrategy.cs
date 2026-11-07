using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Rules;
using ArC.CardGames.YuGiOh;
using ArC.Common.Extensions;
using static YuGiOhQuestions;

/// <summary>
/// The Main Phase by touch, for the turn player:
/// <list type="bullet">
/// <item>drag a monster from the hand onto a monster zone, on Summon or Set: Normal Summon or Set it (a tribute summon asks for its tributes, then the zone);</item>
/// <item>drag a spell onto a spell &amp; trap zone, on Activate or Set; a trap, on Set;</item>
/// <item>swipe a monster left for Defense Position, right for Attack Position (a face-down one is Flip Summoned);</item>
/// <item>tap a card for its details: activate an effect there (hand, field, a Set card, the Extra Deck), or Xyz Summon a monster from the Extra Deck's list;</item>
/// <item>Next Phase, or End Phase.</item>
/// </list>
/// Only what YuGiOhGameRules allows now can be dragged, dropped, swiped or pressed. Everything else is answered by the strategy below.
/// </summary>
public sealed class YuGiOhMainPhaseStrategy(YuGiOhInput input, IYuGiOhAnswers below) : YuGiOhStrategy(input, below)
{
    public static class Text
    {
        public static string Play = "Your Main Phase: drag a card to play it, tap a card for its effects, swipe a monster to change its position";
    }

    /// <summary>The regions a zone splits into while a card from the hand hovers over it.</summary>
    public static class Choices
    {
        public static readonly DropChoice Summon = new("Summon");
        public static readonly DropChoice Set = new("Set");
        public static readonly DropChoice Activate = new("Activate");
    }

    /// <summary>What the card details offer beside effects.</summary>
    public static class Commands
    {
        public static readonly CardCommand XyzSummon = new("Xyz Summon");
    }

    IYuGiOhPlayerInputProvider Player => Input.Player;
    YuGiOhPlayArea PlayArea => Input.PlayArea;

    public override async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        using var scope = Input.Board.Scope()
            .Drag(PlayArea.Hand, CanPlayFromHand)
            .Activatable(CanActivate)
            .Commands(CommandsFor)
            .Button(BoardButton.NextPhase)
            .Button(BoardButton.EndPhase)
            .Hint(Text.Play);
        foreach(var zone in PlayArea.MonsterZones) scope.Drop(zone, card => MonsterChoices(card, zone));
        foreach(var zone in PlayArea.SpellAndTrapZones) scope.Drop(zone, card => SpellTrapChoices(card, zone));
        foreach(var zone in PlayArea.ControlledMonsterZones) scope.Swipe(zone, direction => SwipeAction(zone, direction) is not null);

        switch(await scope.Next())
        {
            case ButtonPressed { Button: BoardButton.NextPhase }:
                return actions.First(action => action.GetType() == typeof(EndMainPhase));
            case ButtonPressed { Button: BoardButton.EndPhase }:
                return actions.FirstOf<SkipToEndPhase>();

            case CardDropped { Card: YuGiOhNormalMonsterCard monster, To: MonsterZone zone } drop:
                Input.Answers.Set(CardFromHand, monster).Set(SetCard, ReferenceEquals(drop.Choice, Choices.Set));
                // A tribute summon asks for its zone after the tributes free one
                if(YuGiOhGameRules.TributesNeeded(monster) == 0) Input.Answers.Set(OwnZone, zone);
                return actions.FirstOf<NormalSummonMonster>();
            case CardDropped { Card: YuGiOhSpellCard spell, To: SpellAndTrapZone zone } drop:
                Input.Answers.Set(CardFromHand, spell).Set(OwnZone, zone).Set(SetCard, ReferenceEquals(drop.Choice, Choices.Set));
                return actions.FirstOf<ActivateSpellCard>();
            case CardDropped { Card: YuGiOhTrapCard trap, To: SpellAndTrapZone zone }:
                Input.Answers.Set(CardFromHand, trap).Set(OwnZone, zone);
                return actions.FirstOf<SetTrapCard>();

            case ZoneSwiped { Zone: MonsterZone zone } swipe:
                Input.Answers.Set(OwnZone, zone);
                return SwipeAction(zone, swipe.Direction) switch
                {
                    PositionChange.Flip => actions.FirstOf<FlipSummon>(),
                    _ => actions.FirstOf<ChangeBattlePosition>(),
                };

            case CommandPressed { Command: var command, Card: YuGiOhXYZMonsterCard xyz } when ReferenceEquals(command, Commands.XyzSummon):
                Input.EffectPicker.Hide();
                Input.Answers.Set(CardFromExtraDeck, xyz);
                // Its materials: only monsters that can make it together
                var options = YuGiOhGameRules.XYZMaterialOptions(Player, xyz);
                Input.Narrowing.NarrowEach(OwnZones, zone => options.Any(option => option.Contains(zone)));
                return actions.FirstOf<XYZSummon>();

            case SkillActivated { Card: YuGiOhCard card, Skill: YuGiOhNonPassiveSkill skill } activation:
                Input.EffectPicker.Hide();
                return Activate(actions, card, activation.From, skill);

            case var other:
                throw new System.InvalidOperationException($"{nameof(YuGiOhMainPhaseStrategy)} allowed {other} but doesn't handle it");
        }
    }

    // What a monster from the hand can be dropped on this zone as: an empty zone, or any zone for a tribute summon
    private IReadOnlyList<DropChoice> MonsterChoices(CardBase card, MonsterZone zone)
    {
        if(card is not YuGiOhNormalMonsterCard monster || !YuGiOhGameRules.CanNormalSummonNow(Player, monster)) return [];
        if(YuGiOhGameRules.TributesNeeded(monster) == 0 && !zone.IsEmpty) return [];
        return [ Choices.Summon, Choices.Set ];
    }

    // A spell can be activated (when it can be now) or Set; a trap can only be Set from the hand
    private IReadOnlyList<DropChoice> SpellTrapChoices(CardBase card, SpellAndTrapZone zone)
    {
        if(!zone.IsEmpty) return [];
        return card switch
        {
            YuGiOhSpellCard spell when YuGiOhGameRules.CanActivateSpellFromHand(Player, PlayArea.Hand, spell) => [ Choices.Activate, Choices.Set ],
            YuGiOhSpellCard spell when YuGiOhGameRules.CanSetFromHand(Player, spell) => [ Choices.Set ],
            YuGiOhTrapCard trap when YuGiOhGameRules.CanSetFromHand(Player, trap) => [ Choices.Set ],
            _ => [],
        };
    }

    enum PositionChange { Change, Flip }

    // Left: to Defense Position. Right: to Attack Position, Flip Summoning a face-down monster
    private static PositionChange? SwipeAction(MonsterZone zone, GestureDetector.SwipeDirection direction)
    {
        if(zone.Card is null) return null;
        bool attackPosition = zone.Orientation == Orientation.VERTICAL;
        return direction switch
        {
            GestureDetector.SwipeDirection.Left when zone.Card.IsFaceUp && attackPosition && YuGiOhGameRules.CanChangeBattlePosition(zone) => PositionChange.Change,
            GestureDetector.SwipeDirection.Right when !zone.Card.IsFaceUp && YuGiOhGameRules.CanFlipSummon(zone) => PositionChange.Flip,
            GestureDetector.SwipeDirection.Right when zone.Card.IsFaceUp && !attackPosition && YuGiOhGameRules.CanChangeBattlePosition(zone) => PositionChange.Change,
            _ => null,
        };
    }

    // An effect activated from the card details: from the hand, a Set card, a face-up card on the field, or the Extra Deck
    private IMainPhaseAction Activate(List<IMainPhaseAction> actions, YuGiOhCard card, ICardSpace? from, YuGiOhNonPassiveSkill skill)
    {
        switch(from)
        {
            case Hand:
                Input.Answers.Set(CardFromHand, card);
                // The action only asks which effect when the card has more than one usable from there
                if(SkillsUsableFrom(card, YuGiOhSkillCardLocation.HAND) > 1) Input.Answers.Set(SkillToActivate, skill);
                return actions.FirstOf<ActivateEffectsFromHand>();

            case var extraDeck when ReferenceEquals(extraDeck, PlayArea.ExtraMonsterDeck):
                Input.Answers.Set(CardFromExtraDeck, card);
                if(SkillsUsableFrom(card, YuGiOhSkillCardLocation.EXTRA_DECK) > 1) Input.Answers.Set(SkillToActivate, skill);
                return actions.FirstOf<ActivateEffectsFromExtraDeck>();

            case SpellAndTrapZone set when card is YuGiOhSpellOrTrapCard { IsFaceUp: false } setCard:
                Input.Answers.Set(OwnZone, set);
                if(YuGiOhGameRules.MainPhaseActivationsOf(setCard).Count > 1) Input.Answers.Set(SkillToActivate, skill);
                return actions.FirstOf<ActivateSetCard>();

            case IYuGiOhZone zone:
                Input.Answers.Set(OwnZone, zone);
                if(YuGiOhGameRules.MainPhaseSkillsOf(card).Count > 1) Input.Answers.Set(SkillToActivate, skill);
                return actions.FirstOf<ActivateEffectsOnField>();

            default:
                throw new System.InvalidOperationException($"{card.Name}'s effect can't be activated from {from?.GetType().Name ?? "nowhere"}");
        }
    }

    private static int SkillsUsableFrom(YuGiOhCard card, YuGiOhSkillCardLocation location)
        => YuGiOhGameRules.MainPhaseSkillsOf(card).Count(candidate => candidate.Location.HasFlag(location));

    private bool CanPlayFromHand(CardBase card) => card switch
    {
        YuGiOhNormalMonsterCard monster => YuGiOhGameRules.CanNormalSummonNow(Player, monster),
        YuGiOhSpellCard spell => YuGiOhGameRules.CanActivateSpellFromHand(Player, PlayArea.Hand, spell) || YuGiOhGameRules.CanSetFromHand(Player, spell),
        YuGiOhTrapCard trap => YuGiOhGameRules.CanSetFromHand(Player, trap),
        _ => false,
    };

    // What the card details may activate now: YuGiOhGameRules' usable skills, of the player's own cards
    private bool CanActivate(CardBase card, ICardSpace? from, ISkill skill) => (card, from, skill) switch
    {
        (YuGiOhCard skilled, Hand hand, _) when ReferenceEquals(hand, PlayArea.Hand)
            => YuGiOhGameRules.UsableHandSkills(Player, skilled).Contains(skill),
        (YuGiOhCard skilled, Deck deck, _) when ReferenceEquals(deck, PlayArea.ExtraMonsterDeck)
            => YuGiOhGameRules.UsableExtraDeckSkills(Player, skilled).Contains(skill),
        (_, SpellAndTrapZone set, _) when Input.IsOwnZone(set) && set.Card is { IsFaceUp: false }
            => YuGiOhGameRules.UsableSetCardActivations(Player, set).Contains(skill),
        (_, IYuGiOhZone zone, _) when Input.IsOwnZone(zone)
            => YuGiOhGameRules.UsableFieldSkills(Player, zone).Contains(skill),
        _ => false,
    };

    // An Xyz monster in the player's Extra Deck that has materials on the field now
    private IEnumerable<CardCommand> CommandsFor(CardBase card, ICardSpace? from)
    {
        if(ReferenceEquals(from, PlayArea.ExtraMonsterDeck) && card is YuGiOhXYZMonsterCard xyz && YuGiOhGameRules.XYZMaterialOptions(Player, xyz).Count > 0)
        {
            yield return Commands.XyzSummon;
        }
    }
}
