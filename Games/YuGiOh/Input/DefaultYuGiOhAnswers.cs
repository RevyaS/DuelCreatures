using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Rules;
using ArC.CardGames.YuGiOh;
using static YuGiOhQuestions;

/// <summary>
/// Answers every YuGiOh question with plain prompts: click a highlighted zone or card, pick from a list, choose in a
/// dialog. It sits under every strategy, so a game is playable before any nicer strategy exists. What it offers is
/// filtered by YuGiOhGameRules and the game's selection states, plus whatever the action that asks narrowed it to.
/// </summary>
public sealed class DefaultYuGiOhAnswers(YuGiOhInput input) : IYuGiOhAnswers
{
    /// <summary>The prompts' wording, in one place.</summary>
    public static class Text
    {
        public static string MainPhaseActions = "Main Phase: what do you do?";
        public static string ChooseAttacker = "Choose a monster to attack with, or end the Battle Phase";
        public static string ChooseFromHand = "Choose a card from your hand";
        public static string ChooseZone = "Choose a zone";
        public static string ChooseOpponentZone = "Choose one of your opponent's zones";
        public static string ChooseZones = "Choose zones";
        public static string ChooseZonesOrFinish = "Choose more zones, or tap a chosen one to finish";
        public static string ChooseSkill = "Which effect? Drag it out of the list";
        public static string ChooseOption = "Choose an option";
        public static string Summon = "Summon";
        public static string Activate = "Activate";
        public static string Set = "Set";
        public static string Pass = "Don't respond";
        public static string Respond = "Respond to the chain? Drag an effect out, or close to pass";
        public static string ActivateTrigger = "Activate this effect? Drag the card out, or close not to";
        public static string Yes = "Yes";
        public static string No = "No";
        public static string Selected = "Selected";
        public static string DeclareName = "Declare a card name";
    }

    IYuGiOhPlayerInputProvider Player => input.Player;
    YuGiOhPlayArea PlayArea => input.PlayArea;
    YuGiOhPlayArea OpponentPlayArea => input.OpponentPlayArea;

    #region Phases
    public async Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
    {
        var options = actions.Select(action => new Prompts.Option<IMainPhaseAction>(Label(action), action, CanDoNow(action))).ToList();
        var chosen = await Prompts.Choose(input.DialogParent, Text.MainPhaseActions, options);
        NarrowFor(chosen);
        return chosen;
    }

    public async Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
    {
        var attack = actions.OfType<AttackAction>().FirstOrDefault();
        var end = actions.OfType<EndAttackPhase>().First();
        var attackers = attack is null
            ? []
            : PlayArea.ControlledMonsterZones.Where(zone => YuGiOhGameRules.CanAttack(zone, PlayArea)).Cast<ICardSpace>().ToList();

        using var scope = input.Board.Scope().Zones(attackers).Button(BoardButton.NextPhase).Hint(Text.ChooseAttacker);
        if(await scope.Next() is ZoneClicked { Zone: MonsterZone attacker } && attack is not null)
        {
            input.Answers.Set(OwnZone, attacker);
            input.Narrowing.Narrow(OpponentZone, target => target is MonsterZone monster && YuGiOhGameRules.ValidateTarget(monster) is null);
            return attack;
        }
        return end;
    }

    /// <summary>Whether <paramref name="action"/> has anything it could do now (YuGiOhGameRules).</summary>
    public bool CanDoNow(IMainPhaseAction action) => action switch
    {
        EndMainPhase => true,
        NormalSummonMonster => HandCards.OfType<YuGiOhNormalMonsterCard>().Any(monster => YuGiOhGameRules.CanNormalSummonNow(Player, monster)),
        ActivateSpellCard => HandCards.OfType<YuGiOhSpellCard>().Any(CanPlaySpell),
        SetTrapCard => HandCards.OfType<YuGiOhTrapCard>().Any(trap => YuGiOhGameRules.CanSetFromHand(Player, trap)),
        ActivateSetCard => PlayArea.SpellAndTrapZones.Any(zone => YuGiOhGameRules.UsableSetCardActivations(Player, zone).Count > 0),
        ActivateEffectsOnField => OwnFieldZones.Any(zone => YuGiOhGameRules.UsableFieldSkills(Player, zone).Count > 0),
        ActivateEffectsFromHand => HandCards.Any(card => YuGiOhGameRules.UsableHandSkills(Player, card).Count > 0),
        ActivateEffectsFromExtraDeck => ExtraDeckCards.Any(card => YuGiOhGameRules.UsableExtraDeckSkills(Player, card).Count > 0),
        XYZSummon => ExtraDeckCards.OfType<YuGiOhXYZMonsterCard>().Any(xyz => YuGiOhGameRules.XYZMaterialOptions(Player, xyz).Count > 0),
        FlipSummon => PlayArea.ControlledMonsterZones.Any(YuGiOhGameRules.CanFlipSummon),
        ChangeBattlePosition => PlayArea.ControlledMonsterZones.Any(YuGiOhGameRules.CanChangeBattlePosition),
        _ => true,
    };

    // The action's first question may only be answered with what the action can do now
    private void NarrowFor(IMainPhaseAction action)
    {
        var narrowing = input.Narrowing;
        switch(action)
        {
            case NormalSummonMonster:
                narrowing.Narrow(CardFromHand, card => card is YuGiOhNormalMonsterCard monster && YuGiOhGameRules.CanNormalSummonNow(Player, monster));
                break;
            case ActivateSpellCard:
                narrowing.Narrow(CardFromHand, card => card is YuGiOhSpellCard spell && CanPlaySpell(spell));
                break;
            case SetTrapCard:
                narrowing.Narrow(CardFromHand, card => card is YuGiOhTrapCard trap && YuGiOhGameRules.CanSetFromHand(Player, trap));
                break;
            case ActivateSetCard:
                narrowing.Narrow(OwnZone, zone => zone is SpellAndTrapZone set && YuGiOhGameRules.UsableSetCardActivations(Player, set).Count > 0);
                break;
            case ActivateEffectsOnField:
                narrowing.Narrow(OwnZone, zone => YuGiOhGameRules.UsableFieldSkills(Player, zone).Count > 0);
                break;
            case ActivateEffectsFromHand:
                narrowing.Narrow(CardFromHand, card => card is YuGiOhCard skilled && YuGiOhGameRules.UsableHandSkills(Player, skilled).Count > 0);
                break;
            case ActivateEffectsFromExtraDeck:
                narrowing.Narrow(CardFromExtraDeck, card => YuGiOhGameRules.UsableExtraDeckSkills(Player, card).Count > 0);
                break;
            case XYZSummon:
                narrowing.Narrow(CardFromExtraDeck, card => card is YuGiOhXYZMonsterCard xyz && YuGiOhGameRules.XYZMaterialOptions(Player, xyz).Count > 0);
                break;
            case FlipSummon:
                narrowing.Narrow(OwnZone, zone => zone is MonsterZone monster && YuGiOhGameRules.CanFlipSummon(monster));
                break;
            case ChangeBattlePosition:
                narrowing.Narrow(OwnZone, zone => zone is MonsterZone monster && YuGiOhGameRules.CanChangeBattlePosition(monster));
                break;
        }
    }

    private static string Label(IMainPhaseAction action) => Prompts.Words(action.GetType().Name);
    #endregion

    #region Hand
    public async Task<CardBase> SelectCardFromHand()
    {
        var allowed = Allowed(input.Narrowing.Take(CardFromHand));
        EnsureAny(PlayArea.Hand.Cards.Where(allowed), CardFromHand);
        using var scope = input.Board.Scope().Cards(PlayArea.Hand, allowed).Hint(Text.ChooseFromHand);
        return ((CardClicked)await scope.Next()).Card;
    }

    public async Task<CardBase?> SelectCardFromHandOrNot()
    {
        var cards = PlayArea.Hand.Cards.Where(Allowed(input.Narrowing.Take(CardFromHandOrNot)!)).ToList();
        var picked = await Prompts.PickCards(input.CardPicker, Text.ChooseFromHand, Text.Selected, 0, 1, cards);
        return picked.FirstOrDefault();
    }

    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        var cards = PlayArea.Hand.Cards.Where(Allowed(input.Narrowing.TakeEach(CardsFromHand))).ToList();
        return Prompts.PickCards(input.CardPicker, Text.ChooseFromHand, Text.Selected, minimum, maximum, cards);
    }

    // What a skill selecting from the hand allows (its game state), and what the asking action narrowed it to
    private Func<CardBase, bool> Allowed(Func<CardBase, bool> narrowed)
    {
        var bySkill = input.State<SelectFromHandBySkillState>();
        var fromHand = input.State<ActivateFromHandState>();
        return card => narrowed(card)
            && (bySkill is null || card is YuGiOhCard filtered && YuGiOhGameRules.ValidateCardFilter(filtered, bySkill.Filter) is null)
            && (fromHand is null || card is YuGiOhCard skilled && YuGiOhGameRules.UsableHandSkills(Player, skilled).Count > 0);
    }
    #endregion

    #region Zones
    public Task<IYuGiOhZone> SelectOwnZone(ZoneSelector selector)
        => PickZone(PlayArea.GetZoneBySelector(selector).Where(input.Narrowing.Take(OwnZone)).ToList(), Text.ChooseZone, OwnZone);

    public Task<IYuGiOhZone> SelectOpponentZone(ZoneSelector selector)
        => PickZone(OpponentPlayArea.GetZoneBySelector(selector).Where(input.Narrowing.Take(OpponentZone)).ToList(), Text.ChooseOpponentZone, OpponentZone);

    public async Task<List<IYuGiOhZone>> SelectOwnZonesRange(ZoneSelector selector, int minimum, int maximum)
    {
        var candidates = PlayArea.GetZoneBySelector(selector).Where(input.Narrowing.TakeEach(OwnZones)).ToList();
        if(candidates.Count < minimum) throw new InvalidOperationException($"{OwnZones} needs {minimum} zone(s), only {candidates.Count} can be chosen");

        // Tap zones until the most there can be; once there are enough, tapping a chosen one again finishes (no buttons, for touch)
        List<IYuGiOhZone> chosen = [];
        while(chosen.Count < maximum)
        {
            bool canFinish = chosen.Count >= minimum;
            var offered = candidates.Except(chosen, ReferenceEqualityComparer.Instance).Cast<ICardSpace>().ToList();
            if(canFinish) offered.AddRange(chosen);
            using var scope = input.Board.Scope()
                .Zones(offered)
                .Hint($"{(canFinish ? Text.ChooseZonesOrFinish : Text.ChooseZones)} ({chosen.Count}/{minimum}-{maximum})");

            var zone = (IYuGiOhZone)((ZoneClicked)await scope.Next()).Zone;
            if(chosen.Contains(zone, ReferenceEqualityComparer.Instance)) break;
            chosen.Add(zone);
        }
        return chosen;
    }

    private async Task<IYuGiOhZone> PickZone(List<IYuGiOhZone> candidates, string hint, object question)
    {
        EnsureAny(candidates, question);
        using var scope = input.Board.Scope().Zones(candidates.Cast<ICardSpace>()).Hint(hint);
        return (IYuGiOhZone)((ZoneClicked)await scope.Next()).Zone;
    }
    #endregion

    #region Cards from other zones
    public Task<YuGiOhCard> SelectCardFromExtraDeck()
        => PickOne(PlayArea.ExtraMonsterDeck.Cards.OfType<YuGiOhCard>(), input.State<SelectFromExtraDeckState>()?.Filter, CardFromExtraDeck);

    public Task<YuGiOhCard> SelectCardFromDeck()
        => PickOne(PlayArea.Deck.Cards.OfType<YuGiOhCard>(), input.State<SearchDeckState>()?.Filter, CardFromDeck);

    public Task<YuGiOhCard> SelectCardFromGraveyard()
        => PickOne(PlayArea.Graveyard.Cards.OfType<YuGiOhCard>(), input.State<SelectFromGraveyardState>()?.Filter, CardFromGraveyard);

    public Task<YuGiOhCard> SelectCardFromOpponentHand()
        => PickOne(OpponentPlayArea.Hand.Cards.OfType<YuGiOhCard>(), null, CardFromOpponentHand);

    public Task<YuGiOhCard> SelectCardFromOpponentGraveyard()
        => PickOne(OpponentPlayArea.Graveyard.Cards.OfType<YuGiOhCard>(), input.State<SelectFromGraveyardState>()?.Filter, CardFromOpponentGraveyard);

    public async Task<List<YuGiOhCard>> SelectXYZMaterials(MonsterZone zone, int amount)
    {
        var materials = YuGiOhGameRules.XYZMaterialsOf(zone).Cast<CardBase>().ToList();
        var picked = await Prompts.PickCards(input.CardPicker, Prompts.Words(YuGiOhQuestions.MaterialsOfXYZ.Name), Text.Selected, amount, amount, materials);
        return picked.Cast<YuGiOhCard>().ToList();
    }

    private async Task<YuGiOhCard> PickOne(IEnumerable<YuGiOhCard> cards, YuGiOhCardFilter? filter, Question<YuGiOhCard> question)
    {
        var narrowed = input.Narrowing.Take(question);
        var candidates = cards.Where(card => narrowed(card) && (filter is null || YuGiOhGameRules.ValidateCardFilter(card, filter) is null)).Cast<CardBase>().ToList();
        EnsureAny(candidates, question);
        var picked = await Prompts.PickCards(input.CardPicker, Prompts.Words(question.Name), Text.Selected, 1, 1, candidates);
        return (YuGiOhCard)picked.Single();
    }
    #endregion

    #region Skills and choices
    // Effects are picked from the effect picker: one card per effect, pressed to read it, dragged out to pick it
    public async Task<YuGiOhNonPassiveSkill> SelectSkillToActivate(List<YuGiOhNonPassiveSkill> skills)
    {
        var (_, skill) = await PickEffect(Text.ChooseSkill, skills.Select(skill => (OwnerOf(skill), skill)).ToList(), canDecline: false);
        return skill;
    }

    public Task<bool> QuerySetCard(YuGiOhCard card) => card switch
    {
        YuGiOhMonsterCard => Prompts.Choose<bool>(input.DialogParent, card.Name, [ new(Text.Summon, false), new(Text.Set, true) ]),
        YuGiOhSpellCard spell => Prompts.Choose<bool>(input.DialogParent, card.Name,
            [ new(Text.Activate, false, YuGiOhGameRules.CanActivateSpellFromHand(Player, PlayArea.Hand, spell)), new(Text.Set, true) ]),
        // A trap is only ever Set from the hand
        _ => Task.FromResult(true),
    };

    // Drag a response out to activate it; close the list to pass
    public async Task<YuGiOhChainResponse?> SelectChainResponse(YuGiOhChain chain)
    {
        var responses = YuGiOhGameRules.ChainResponses(Player, chain);
        var (picked, response) = await PickEffect(Text.Respond, responses.Select(response => ((CardBase)response.Card, response)).ToList(), canDecline: true,
            focusOf: response => response.Skill);
        return picked ? response : null;
    }

    // Drag the card out to activate its effect; close the list not to
    public async Task<bool> QueryActivateSkill(YuGiOhCard invoker, YuGiOhAutomaticSkill skill)
    {
        var (picked, _) = await PickEffect(Text.ActivateTrigger, [ ((CardBase)invoker, skill) ], canDecline: true);
        return picked;
    }

    private Task<(bool Picked, T Value)> PickEffect<T>(string title, IReadOnlyList<(CardBase Card, T Value)> options, bool canDecline, Func<T, ISkill>? focusOf = null)
        => Prompts.DragOutOfCardList(input.EffectPicker, title, options, canDecline,
            (card, value) => input.ShowDetails(new CardDetail(card) { Focus = focusOf?.Invoke(value) ?? value as ISkill }));

    // The card a skill is printed on (copies share their skills, so any copy will do): the player's cards first
    private CardBase OwnerOf(ISkill skill)
        => new[] { PlayArea, OpponentPlayArea }
            .SelectMany(area => area.Zones)
            .SelectMany(zone => ZoneSnapshot.Of(zone).Cards.Select(card => card.Card))
            .OfType<SkilledCard>()
            .FirstOrDefault(card => card.Skills.Contains(skill))
            ?? throw new InvalidOperationException($"No card has the skill \"{input.SkillText.Describe(skill)}\"");

    public Task<YuGiOhSkills.SkillAction> SelectSkillOption(IReadOnlyList<YuGiOhSkills.SkillAction> options, YuGiOhSkillContextObject context)
        => Prompts.Choose(input.DialogParent, Text.ChooseOption, options.Select(option => new Prompts.Option<YuGiOhSkills.SkillAction>(Prompts.Words(option.GetType().Name), option)).ToList());

    public Task<string> DeclareCardName()
    {
        // Every card name in the game
        var names = new[] { PlayArea, OpponentPlayArea }
            .SelectMany(area => area.Zones)
            .SelectMany(zone => ZoneSnapshot.Of(zone).Cards.Select(card => card.Card.Name))
            .Distinct()
            .Order()
            .Select(name => new Prompts.Option<string>(name, name))
            .ToList();
        return Prompts.Choose(input.DialogParent, Text.DeclareName, names);
    }
    #endregion

    List<YuGiOhCard> HandCards => PlayArea.Hand.Cards.OfType<YuGiOhCard>().ToList();
    List<YuGiOhCard> ExtraDeckCards => PlayArea.ExtraMonsterDeck.Cards.OfType<YuGiOhCard>().ToList();
    IEnumerable<IYuGiOhZone> OwnFieldZones => PlayArea.ControlledMonsterZones.Cast<IYuGiOhZone>().Concat(PlayArea.SpellAndTrapZones).Append(PlayArea.FieldZone);
    bool CanPlaySpell(YuGiOhSpellCard spell) => YuGiOhGameRules.CanActivateSpellFromHand(Player, PlayArea.Hand, spell) || YuGiOhGameRules.CanSetFromHand(Player, spell);

    // Nothing to offer means the game asked something it can't be answered: fail now rather than wait forever
    private static void EnsureAny<T>(IEnumerable<T> candidates, object question)
    {
        if(!candidates.Any()) throw new InvalidOperationException($"Nothing can answer {question} now");
    }
}
