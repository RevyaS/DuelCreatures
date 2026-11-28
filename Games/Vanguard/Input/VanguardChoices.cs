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
        // Any card type (the heal asks for VanguardCards): asked as CardBase, the purpose and quantity kept
        if(typeof(CardBase).IsAssignableFrom(typeof(T)))
        {
            var cards = new Choosing<CardBase>(question.Offered.Cast<CardBase>().ToList(), question.Quantity, question.Prompt, question.Purpose, question.CanCancel);
            return (await ChooseCards(cards)).Cast<T>().ToList();
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

    // A trigger's unit: on the trigger screen, the board as it is with the attack drawn, the prompt between both sides
    private Task<ICardSpace> ChooseTriggerTarget(TriggerQuestion purpose, List<ICardSpace> candidates)
        => input.TriggerSelect.ChooseTriggerTarget(purpose, candidates, ZoneHint(purpose, ""));

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
    private Task<ActivateSkillRequest> ChooseSkill(Choosing<ActivateSkillRequest> question)
        => SkillPicks.ChooseSkill(input.SwipeUp, question, Text.ChooseSkillToResolve, input.ShowDetails);

    // "Choose one of": shown on the card of the player's skill being used
    private Task<IReadOnlyList<SkillOption>> ChooseOption(Choosing<SkillOption> question)
    {
        var card = input.ResolvingSkills.Current?.Card
            ?? throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(SkillOption), "no skill of the player's is being used, so there's no card to show its options on");
        return SkillPicks.ChooseOption(input.SwipeUp, question, card, input.ShowDetails);
    }
    #endregion

    #region Confirm
    /// <summary>"Use this skill?": swipe its card up out of the row to use it, Cancel not to. Any other question: Yes or No.</summary>
    public Task<bool> Confirm(Confirmation question) => question.Purpose is ActivateSkillRequest activation
        ? SkillPicks.ConfirmSkill(input.SwipeUp, activation, Text.ActivateTrigger, input.ShowDetails)
        : SkillPicks.YesOrNo(input.DialogParent, question, Text.Yes, Text.No);
    #endregion
}
