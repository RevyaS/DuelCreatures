using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;
using ArC.CardGames.Skills;
using ArC.CardGames.YuGiOh;

/// <summary>
/// Answers YuGiOh's "pick from these" and "yes or no" questions (Choose, Confirm) by their shape, on the same screens as
/// Vanguard: zones, and cards on the field by their zones, on the enlarged field (FieldSelectScreen: one, tap it;
/// several, tap to number them, then OK); cards anywhere else, or from several places at once, on the card selection
/// screen (CardSelectScreen, tagged with where they are); "Apply 1 of the following", which effect resolves first and
/// "activate this effect?" by swiping a card up out of a row (SwipeUpPicker, SkillPicks). A shape with no gesture throws
/// UnansweredQuestion.
/// </summary>
public sealed class YuGiOhChoices(YuGiOhInput input)
{
    readonly YuGiOhCardSelection cardSelection = new(input);

    /// <summary>The prompts' wording, in one place.</summary>
    public static class Text
    {
        public static string Select(int minimum, int maximum) => minimum == maximum ? $"Select {maximum}" : $"Select {minimum}-{maximum}";
        public static string ChooseZones(string prompt, int minimum, int maximum) => $"{prompt} ({Select(minimum, maximum)}): tap to choose, then Confirm";
        public static string ChooseSkillToResolve = "Select the effect to resolve first";
        public static string ActivateTrigger = "Activate this effect?";
        public static string Yes = "Yes";
        public static string No = "No";
    }

    #region Choose
    public async Task<IReadOnlyList<T>> Choose<T>(Choosing<T> question)
    {
        if(typeof(ICardSpace).IsAssignableFrom(typeof(T)))
        {
            var zones = await input.Screens.FieldSelect.ChooseZones(question.Offered.Cast<ICardSpace>().ToList(), question.Minimum, question.Maximum, question.Prompt, question.CanCancel);
            return zones.Cast<T>().ToList();
        }
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
            IReadOnlyList<ActivateSkillRequest> next = [ await ChooseSkill(skills) ];
            return (IReadOnlyList<T>)next;
        }
        throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(T));
    }

    /// <summary>
    /// On the board itself, for the strategies' own picks (a spell's zone, an Xyz Monster's zone). One: tap it. Several: tap zones to choose them (badges number them), tap a chosen one again to drop it, then Confirm.
    /// </summary>
    public static async Task<List<ICardSpace>> ChooseZones(YuGiOhInput input, IReadOnlyList<ICardSpace> candidates, int minimum, int maximum, string hint)
    {
        if(minimum == 1 && maximum == 1)
        {
            using var scope = input.Board.Scope().Zones(candidates).Hint(hint);
            return [ ((ZoneClicked)await scope.Next()).Zone ];
        }

        var chosen = new CardSelection<ICardSpace>(candidates, minimum, maximum);
        while(true)
        {
            using var scope = input.Board.Scope().Hint(Text.ChooseZones(hint, minimum, maximum));
            if(chosen.CanPickMore) scope.Zones(candidates.Where(zone => !chosen.IsPicked(zone)));
            foreach(var zone in chosen.Picks) scope.Cards(zone);
            scope.Numbered(chosen.Picks);
            if(chosen.CanConfirm) scope.Button(BoardButton.Confirm);

            switch(await scope.Next())
            {
                case ButtonPressed:
                    return chosen.Picks.ToList();
                case ZoneClicked { Zone: var zone }:
                    chosen.Toggle(zone);
                    break;
                case CardClicked { Zone: var zone }:
                    chosen.Toggle(zone);
                    break;
            }
        }
    }

    // Cards all on the field: by their zones, on the enlarged field. Anywhere else, or several places at once: the card
    // selection screen
    private async Task<IReadOnlyList<CardBase>> ChooseCards(Choosing<CardBase> question)
    {
        if(question.Offered.Count == 0) return [];

        var zones = question.Offered.Select(ZoneOnField).ToList();
        if(zones.All(zone => zone is not null))
        {
            var chosen = await input.Screens.FieldSelect.ChooseZones(zones.Cast<ICardSpace>().ToList(), question.Minimum, question.Maximum, question.Prompt, question.CanCancel);
            return chosen.Select(zone => ZoneSnapshot.Of(zone).Cards.Single().Card).ToList();
        }
        return await cardSelection.Choose(question);
    }

    // The monster, spell or trap zone (either player's) holding the card, if it's on the field
    private ICardSpace? ZoneOnField(CardBase card)
        => new[] { input.PlayArea, input.OpponentPlayArea }
            .SelectMany(area => area.MonsterZones.Cast<ICardSpace>().Concat(area.ExtraMonsterZones).Concat(area.SpellAndTrapZones).Append(area.FieldZone))
            .FirstOrDefault(zone => ZoneSnapshot.Of(zone).Cards.Any(shown => ReferenceEquals(shown.Card, card)));

    // "Apply 1 of the following": shown on the card whose skill asks
    private Task<IReadOnlyList<SkillOption>> ChooseOption(Choosing<SkillOption> question)
    {
        var card = (question.Purpose as Step is { } step ? input.CardAsking(step) : null)
            ?? throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(SkillOption), "no skill being used owns this choice, so there's no card to show its options on");
        return SkillPicks.ChooseOption(input.SwipeUp, question, card, input.ShowDetails);
    }

    // Several effects wait at once: swipe the one to resolve first up out of the row
    private Task<ActivateSkillRequest> ChooseSkill(Choosing<ActivateSkillRequest> question)
        => SkillPicks.ChooseSkill(input.SwipeUp, question, Text.ChooseSkillToResolve, input.ShowDetails);
    #endregion

    #region Confirm
    /// <summary>"Use this effect?": swipe its card up out of the row to use it, Cancel not to. Any other question: Yes or No.</summary>
    public Task<bool> Confirm(Confirmation question) => question.Purpose is ActivateSkillRequest activation
        ? SkillPicks.ConfirmSkill(input.SwipeUp, activation, Text.ActivateTrigger, input.ShowDetails)
        : SkillPicks.YesOrNo(input.DialogParent, question, Text.Yes, Text.No);
    #endregion
}
