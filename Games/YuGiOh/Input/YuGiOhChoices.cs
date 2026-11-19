using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;
using ArC.CardGames.Skills;
using ArC.CardGames.YuGiOh;

/// <summary>
/// Answers YuGiOh's "pick from these" and "yes or no" questions (Choose, Confirm) by their shape: zones by tapping them
/// (several: numbered badges, then Confirm); cards on the field by tapping their zones; cards anywhere else from a list
/// (one: drag it out; several: pick, then Confirm); "Apply 1 of the following" by dragging an option out of the list;
/// skills by dragging one out of the list. A shape with no gesture throws UnansweredQuestion.
/// </summary>
public sealed class YuGiOhChoices(YuGiOhInput input)
{
    /// <summary>The prompts' wording, in one place.</summary>
    public static class Text
    {
        public static string Select(int minimum, int maximum) => minimum == maximum ? $"Select {maximum}" : $"Select {minimum}-{maximum}";
        public static string ChooseZones(string prompt, int minimum, int maximum) => $"{prompt} ({Select(minimum, maximum)}): tap to choose, then Confirm";
        public static string ChooseSkillToResolve = "Select the effect to resolve first";
        public static string ActivateTrigger = "Activate this effect?";
        public static string Selected = "Selected";
        public static string Yes = "Yes";
        public static string No = "No";
    }

    #region Choose
    public async Task<IReadOnlyList<T>> Choose<T>(Choosing<T> question)
    {
        if(typeof(ICardSpace).IsAssignableFrom(typeof(T)))
        {
            var zones = await ChooseZones(input, question.Offered.Cast<ICardSpace>().ToList(), question.Minimum, question.Maximum, question.Prompt);
            return zones.Cast<T>().ToList();
        }
        if(typeof(CardBase).IsAssignableFrom(typeof(T)))
        {
            var cards = await ChooseCards(question.Offered.Cast<CardBase>().ToList(), question.Minimum, question.Maximum, question.Prompt, question.CanCancel);
            return cards.Cast<T>().ToList();
        }
        if(question is Choosing<SkillOption> options)
        {
            IReadOnlyList<SkillOption> option = [ await ChooseOption(options) ];
            return (IReadOnlyList<T>)option;
        }
        if(question is Choosing<ActivateSkillRequest> skills)
        {
            IReadOnlyList<ActivateSkillRequest> next = [ await ChooseSkill(skills) ];
            return (IReadOnlyList<T>)next;
        }
        throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(T));
    }

    /// <summary>One: tap it. Several: tap zones to choose them (badges number them), tap a chosen one again to drop it, then Confirm.</summary>
    public static async Task<List<ICardSpace>> ChooseZones(YuGiOhInput input, IReadOnlyList<ICardSpace> candidates, int minimum, int maximum, string hint)
    {
        if(minimum == 1 && maximum == 1)
        {
            using var scope = input.Board.Scope().Zones(candidates).Hint(hint);
            return [ ((ZoneClicked)await scope.Next()).Zone ];
        }

        List<ICardSpace> chosen = [];
        while(true)
        {
            using var scope = input.Board.Scope().Hint(Text.ChooseZones(hint, minimum, maximum));
            if(chosen.Count < maximum) scope.Zones(candidates.Where(zone => !Refs.Has(chosen, zone)));
            foreach(var zone in chosen) scope.Cards(zone);
            scope.Numbered(chosen);
            if(chosen.Count >= minimum) scope.Button(BoardButton.Confirm);

            switch(await scope.Next())
            {
                case ButtonPressed:
                    return chosen;
                case ZoneClicked { Zone: var zone }:
                    chosen.Add(zone);
                    break;
                case CardClicked { Zone: var zone }:
                    Refs.Remove(chosen, zone);
                    break;
            }
        }
    }

    // By where the offered cards are: on the field, tap their zones; anywhere else (hand, deck, Graveyard, banished,
    // Extra Deck, materials), pick from a list
    private async Task<IReadOnlyList<CardBase>> ChooseCards(List<CardBase> cards, int minimum, int maximum, string prompt, bool canCancel)
    {
        if(cards.Count == 0) return [];

        var zones = cards.Select(ZoneOnField).ToList();
        if(zones.All(zone => zone is not null))
        {
            var chosen = await ChooseZones(input, zones.Cast<ICardSpace>().ToList(), minimum, maximum, prompt);
            return chosen.Select(zone => ZoneSnapshot.Of(zone).Cards.Single().Card).ToList();
        }

        if(maximum == 1)
        {
            var (picked, card) = await Prompts.DragOutOfCardList(input.EffectPicker, prompt, cards.Select(card => (card, card)).ToList(),
                canDecline: canCancel || minimum == 0, (shown, _) => input.ShowDetails(new CardDetail(shown)));
            return picked ? [ card ] : [];
        }
        return await Prompts.PickCards(input.CardPicker, prompt, Text.Selected, minimum, maximum, cards);
    }

    // The monster, spell or trap zone (either player's) holding the card, if it's on the field
    private ICardSpace? ZoneOnField(CardBase card)
        => new[] { input.PlayArea, input.OpponentPlayArea }
            .SelectMany(area => area.MonsterZones.Cast<ICardSpace>().Concat(area.ExtraMonsterZones).Concat(area.SpellAndTrapZones).Append(area.FieldZone))
            .FirstOrDefault(zone => ZoneSnapshot.Of(zone).Cards.Any(shown => ReferenceEquals(shown.Card, card)));

    // "Apply 1 of the following": one card per option (the card whose skill asks), its details showing the option alone
    private async Task<SkillOption> ChooseOption(Choosing<SkillOption> question)
    {
        if(question.Offered.Count == 1) return question.Offered[0];
        var card = (question.Purpose as Step is { } step ? input.CardAsking(step) : null)
            ?? throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(SkillOption), "no skill being used owns this choice, so there's no card to show its options on");
        var (_, option) = await Prompts.DragOutOfCardList(input.EffectPicker, question.Prompt, question.Offered.Select(option => (card, option)).ToList(), canDecline: false,
            (shown, option) => input.ShowDetails(new CardDetail(shown) { FocusText = option.Label }));
        return option;
    }

    // Several effects wait at once: drag the one to resolve first out of the list
    private async Task<ActivateSkillRequest> ChooseSkill(Choosing<ActivateSkillRequest> question)
    {
        if(question.Offered.Count == 1 && question.Minimum == 1) return question.Offered[0];
        var (_, skill) = await Prompts.DragOutOfCardList(input.EffectPicker, Text.ChooseSkillToResolve,
            question.Offered.Select(activation => (activation.Card, activation)).ToList(), canDecline: false,
            (card, activation) => input.ShowDetails(new CardDetail(card) { Focus = activation.Skill }));
        return skill;
    }
    #endregion

    #region Confirm
    /// <summary>"Use this effect?": drag its card out of the list to use it, close the list not to. Any other question: Yes or No.</summary>
    public async Task<bool> Confirm(Confirmation question)
    {
        if(question.Purpose is ActivateSkillRequest activation)
        {
            var (picked, _) = await Prompts.DragOutOfCardList<ActivateSkillRequest>(input.EffectPicker, Text.ActivateTrigger, [ (activation.Card, activation) ], canDecline: true,
                (card, shown) => input.ShowDetails(new CardDetail(card) { Focus = shown.Skill }));
            return picked;
        }
        return await Prompts.Choose<bool>(input.DialogParent, question.Prompt, [ new Prompts.Option<bool>(Text.Yes, true), new Prompts.Option<bool>(Text.No, false) ]);
    }
    #endregion
}
