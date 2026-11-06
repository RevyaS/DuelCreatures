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
/// The Main Phase by clicking, for the turn player:
/// <list type="bullet">
/// <item>a monster in the hand, then an empty monster zone: Normal Summon (or Set) it there (a tribute summon asks for its tributes, then the zone);</item>
/// <item>a spell or trap in the hand: activate or Set it (the zone, and activate or Set, are asked next);</item>
/// <item>an effect's button in the card details: activate it (hand, field, or a Set card);</item>
/// <item>Next Phase, End Phase, or Actions for everything else (Xyz Summon, battle position, Flip Summon) as a list.</item>
/// </list>
/// What can be clicked is what YuGiOhGameRules allows now. Everything else is answered by the strategy below.
/// </summary>
public sealed class YuGiOhMainPhaseStrategy(YuGiOhInput input, IYuGiOhAnswers below) : YuGiOhStrategy(input, below)
{
    public static class Text
    {
        public static string Play = "Your Main Phase: play a card, activate an effect, or move on";
        public static string ChooseSummonZone = "Choose a monster zone (or another card)";
    }

    IYuGiOhPlayerInputProvider Player => Input.Player;
    YuGiOhPlayArea PlayArea => Input.PlayArea;

    public override async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        CardBase? picked = null;
        while(true)
        {
            // A monster picked last time waits for its zone (or another card)
            if(picked is YuGiOhNormalMonsterCard pickedMonster && YuGiOhGameRules.TributesNeeded(pickedMonster) == 0)
            {
                var placed = await PickSummonZone(pickedMonster);
                if(placed is MonsterZone zone)
                {
                    Input.Answers.Set(CardFromHand, pickedMonster).Set(OwnZone, zone);
                    return actions.FirstOf<NormalSummonMonster>();
                }
                picked = placed is CardBase other ? other : null;
                if(picked is not null) continue;
            }

            using var scope = Input.Board.Scope()
                .Cards(PlayArea.Hand, CanPlayFromHand)
                .Button(BoardButton.NextPhase)
                .Button(BoardButton.EndPhase)
                .Button(BoardButton.Actions)
                .Activatable(CanActivate)
                .Hint(Text.Play);

            switch(await scope.Next())
            {
                case ButtonPressed { Button: BoardButton.NextPhase }:
                    return actions.First(action => action.GetType() == typeof(EndMainPhase));
                case ButtonPressed { Button: BoardButton.EndPhase }:
                    return actions.FirstOf<SkipToEndPhase>();
                case ButtonPressed { Button: BoardButton.Actions }:
                    // Everything, as a list (the answers below narrow what it asks next)
                    return await Below.RequestMainPhaseAction(actions);

                case CardClicked { Card: YuGiOhNormalMonsterCard monster } when YuGiOhGameRules.TributesNeeded(monster) == 0:
                    picked = monster;
                    continue;
                case CardClicked { Card: YuGiOhNormalMonsterCard monster }:
                    // A tribute summon: the tributes free the zone, so it's asked after them
                    Input.Answers.Set(CardFromHand, monster);
                    return actions.FirstOf<NormalSummonMonster>();
                case CardClicked { Card: YuGiOhSpellCard spell }:
                    Input.Answers.Set(CardFromHand, spell);
                    return actions.FirstOf<ActivateSpellCard>();
                case CardClicked { Card: YuGiOhTrapCard trap }:
                    Input.Answers.Set(CardFromHand, trap);
                    return actions.FirstOf<SetTrapCard>();

                case SkillActivated { Card: YuGiOhCard card, Skill: YuGiOhNonPassiveSkill skill } activation:
                    return Activate(actions, card, activation.From, skill);
            }
        }
    }

    // The zone to summon a monster to, or another card in the hand to play instead
    private async Task<object> PickSummonZone(YuGiOhNormalMonsterCard monster)
    {
        using var scope = Input.Board.Scope()
            .Zones(PlayArea.MonsterZones.Where(zone => zone.IsEmpty))
            .Cards(PlayArea.Hand, card => !ReferenceEquals(card, monster) && CanPlayFromHand(card))
            .Hint(Text.ChooseSummonZone);
        return await scope.Next() switch
        {
            ZoneClicked { Zone: MonsterZone zone } => zone,
            CardClicked click => click.Card,
            var other => other,
        };
    }

    // An effect activated from the card details: from the hand, a Set card, or a face-up card on the field
    private IMainPhaseAction Activate(List<IMainPhaseAction> actions, YuGiOhCard card, ICardSpace? from, YuGiOhNonPassiveSkill skill)
    {
        switch(from)
        {
            case Hand:
                Input.Answers.Set(CardFromHand, card);
                // The action only asks which effect when the card has more than one usable from the hand
                if(YuGiOhGameRules.MainPhaseSkillsOf(card).Count(candidate => candidate.Location.HasFlag(YuGiOhSkillCardLocation.HAND)) > 1)
                {
                    Input.Answers.Set(SkillToActivate, skill);
                }
                return actions.FirstOf<ActivateEffectsFromHand>();

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
        (_, SpellAndTrapZone set, _) when Input.IsOwnZone(set) && set.Card is { IsFaceUp: false }
            => YuGiOhGameRules.UsableSetCardActivations(Player, set).Contains(skill),
        (_, IYuGiOhZone zone, _) when Input.IsOwnZone(zone)
            => YuGiOhGameRules.UsableFieldSkills(Player, zone).Contains(skill),
        _ => false,
    };
}
