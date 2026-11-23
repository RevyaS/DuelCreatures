using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Setup;
using ArC.CardGames.Vanguard;
using SkillOption = ArC.CardGames.Skills.SkillOption;
using ActivateSkillRequest = ArC.CardGames.Skills.ActivateSkillRequest;

/// <summary>
/// Answers Vanguard's "pick from these" and "yes or no" questions (Choose, Confirm) by their shape: the mulligan by its
/// purpose; circles, and units by their circles, on the enlarged field (FieldSelectScreen: several are numbered, then
/// OK); any other cards on the card selection screen (CardSelectScreen, through an ICardSelectionStrategy per kind of
/// pick: counter blast, counter charge, heal, any other); skills and skill options by swiping one up out of a row (SwipeUpPicker). A shape with no gesture throws UnansweredQuestion.
/// </summary>
public sealed class VanguardChoices(VanguardInput input)
{
    /// <summary>The prompts' wording, in one place.</summary>
    public static class Text
    {
        public static string Select(int minimum, int maximum) => minimum == maximum ? $"Select {maximum}" : $"Select {minimum}-{maximum}";
        public static string ProvidePower = "Tap a unit to give the trigger's power";
        public static string ProvideCritical = "Tap a unit to give the trigger's critical";
        public static string Stand = "Tap a resting rear-guard to stand it";
        public static string ChooseSkillToResolve = "Select the skill to resolve first";
        public static string ActivateTrigger = "Select Skill to Activate";
        public static string Heal = "Select Card to Heal";
        public static string CounterBlast(int amount) => $"Counter Blast {amount}: select the damage to turn face down";
        public static string CounterCharge(int amount) => $"Counter Charge {amount}: select the damage to turn face up";
        public static string Yes = "Yes";
        public static string No = "No";
    }

    VanguardPlayArea PlayArea => input.PlayArea;
    readonly VanguardMulliganStrategy mulligan = new(input);

    // The card selection screen's kinds of picks, the most specific first; the default takes any other
    readonly IReadOnlyList<ICardSelectionStrategy> cardSelections =
    [
        new CounterBlastSelection(input),
        new CounterChargeSelection(input),
        new HealSelection(input),
        new DefaultCardSelection(input),
    ];

    #region Choose
    public async Task<IReadOnlyList<T>> Choose<T>(Choosing<T> question)
    {
        if(question.Purpose is MulliganQuestion.Return && question is Choosing<CardBase> redraw)
        {
            return (IReadOnlyList<T>)await mulligan.Answer(redraw);
        }
        if(question.Purpose is TriggerQuestion.Power or TriggerQuestion.Critical or TriggerQuestion.Stand && typeof(ICardSpace).IsAssignableFrom(typeof(T)))
        {
            IReadOnlyList<T> target = [ (T)await ChooseTriggerTarget((TriggerQuestion)question.Purpose, question.Offered.Cast<ICardSpace>().ToList()) ];
            return target;
        }
        if(typeof(ICardSpace).IsAssignableFrom(typeof(T)))
        {
            return (await ChooseZones(question.Offered.Cast<ICardSpace>().ToList(), question.Minimum, question.Maximum, ZoneHint(question.Purpose, question.Prompt), question.CanCancel)).Cast<T>().ToList();
        }
        if(question is Choosing<CardBase> cards)
        {
            return (IReadOnlyList<T>)await ChooseCards(cards);
        }
        if(question is Choosing<SkillOption> options)
        {
            return (IReadOnlyList<T>)await ChooseOption(options);
        }
        if(question is Choosing<ActivateSkillRequest> skills)
        {
            IReadOnlyList<ActivateSkillRequest> next = new List<ActivateSkillRequest> { await ChooseSkill(skills) };
            return (IReadOnlyList<T>)next;
        }
        throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(T));
    }

    private static string ZoneHint(IQuestionPurpose purpose, string prompt) => purpose switch
    {
        TriggerQuestion.Power => Text.ProvidePower,
        TriggerQuestion.Critical => Text.ProvideCritical,
        TriggerQuestion.Stand => Text.Stand,
        _ => prompt,
    };

    // A trigger's unit: on the trigger screen, the battle and the revealed trigger on top (read from the battle and the
    // check ArC is resolving now); a tap previews what it would do, OK answers
    private async Task<ICardSpace> ChooseTriggerTarget(TriggerQuestion purpose, List<ICardSpace> candidates)
    {
        var actions = input.Session().Actions;
        var battle = actions.Current<VanguardBattle>()
            ?? throw new InvalidOperationException($"A {purpose} trigger question came outside a battle");
        var trigger = actions.Current<TriggerCheck>()?.Checked
            ?? throw new InvalidOperationException($"A {purpose} trigger question came with no trigger check revealing a card");
        var target = battle.State.TargetCircle!;
        var defenderArea = PlayArea.Owns(target) ? PlayArea : input.OpponentPlayArea;
        return await input.TriggerSelect.ChooseTriggerTarget(purpose, candidates, ZoneHint(purpose, ""), battle.State, trigger, defenderArea);
    }

    // On the enlarged field (FieldSelectScreen): one, tap it; several, tap to number them, tap again to drop, then OK
    private async Task<List<ICardSpace>> ChooseZones(List<ICardSpace> candidates, int minimum, int maximum, string hint, bool canCancel)
        => (await input.FieldSelect.ChooseZones(candidates, minimum, maximum, hint, canCancel)).ToList();

    // Units only: on the enlarged field. Anything else (the hand, the soul, the drop zone, the deck, the damage zone, or
    // several places at once): the card selection screen, by the first selection strategy that handles the question
    private async Task<IReadOnlyList<CardBase>> ChooseCards(Choosing<CardBase> question)
    {
        if(question.Offered.Count == 0) return [];

        var circles = question.Offered.Select(CircleOf).ToList();
        if(circles.All(circle => circle is not null))
        {
            var chosen = await ChooseZones(circles.Cast<ICardSpace>().ToList(), question.Minimum, question.Maximum, question.Prompt, question.CanCancel);
            return chosen.Select(zone => ((UnitCircle)zone).Card!).Cast<CardBase>().ToList();
        }
        return await cardSelections.First(selection => selection.Handles(question)).Choose(question);
    }

    private UnitCircle? CircleOf(CardBase card)
        => new[] { PlayArea, input.OpponentPlayArea }
            .SelectMany(area => area.Rearguards.Cast<UnitCircle>().Prepend(area.Vanguard))
            .FirstOrDefault(circle => ReferenceEquals(circle.Card, card));

    // Several of the player's skills wait at once: swipe the one to resolve first up out of the row
    private async Task<ActivateSkillRequest> ChooseSkill(Choosing<ActivateSkillRequest> question)
    {
        if(question.Offered.Count == 1 && question.Minimum == 1) return question.Offered[0];
        var (_, skill) = await input.SwipeUp.PickOne(Text.ChooseSkillToResolve,
            question.Offered.Select(activation => new SwipeUpOption<ActivateSkillRequest>(activation.Card, activation)).ToList(), canCancel: false, ShowSkill);
        return skill;
    }

    // "Choose one of": one card per option ArC offers (the card of the skill asking, the option's label as its tag);
    // swipe the one to apply up out of the row
    private async Task<IReadOnlyList<SkillOption>> ChooseOption(Choosing<SkillOption> question)
    {
        if(question.Offered.Count == 1 && question.Minimum == 1) return [ question.Offered[0] ];
        var card = input.ResolvingSkills.Current?.Card
            ?? throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(SkillOption), "no skill of the player's is being used, so there's no card to show its options on");
        var (picked, option) = await input.SwipeUp.PickOne(question.Prompt,
            question.Offered.Select(option => new SwipeUpOption<SkillOption>(card, option, new CardTag { Text = option.Label })).ToList(),
            question.CanCancel, shown => input.ShowDetails(new CardDetail(card) { FocusText = shown.Label }));
        return picked ? [ option ] : [];
    }

    private void ShowSkill(ActivateSkillRequest activation) => input.ShowDetails(new CardDetail(activation.Card) { Focus = activation.Skill });
    #endregion

    #region Confirm
    /// <summary>"Use this skill?": swipe its card up out of the row to use it, Cancel not to. Any other question: Yes or No.</summary>
    public async Task<bool> Confirm(Confirmation question)
    {
        if(question.Purpose is ActivateSkillRequest activation)
        {
            var (picked, _) = await input.SwipeUp.PickOne(Text.ActivateTrigger, [ new SwipeUpOption<ActivateSkillRequest>(activation.Card, activation) ], canCancel: true, ShowSkill);
            return picked;
        }
        return await Prompts.Choose<bool>(input.DialogParent, question.Prompt, [ new Prompts.Option<bool>(Text.Yes, true), new Prompts.Option<bool>(Text.No, false) ]);
    }
    #endregion
}
