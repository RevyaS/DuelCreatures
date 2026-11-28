using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;
using ArC.CardGames.Skills;
using Godot;

/// <summary>
/// The skill questions every game asks the same way, on the swipe-up picker (SwipeUpPicker): which waiting skill
/// resolves first, which option of a "choose one of", and "use this skill?". A tap on a card shows what it stands for
/// (<paramref name="showDetails"/>: the skill, or the option alone). Any other yes/no is a Yes / No dialog.
/// </summary>
public static class SkillPicks
{
    /// <summary>Several skills wait at once: the one swiped up resolves first. One that must be picked is answered without asking.</summary>
    public static async Task<ActivateSkillRequest> ChooseSkill(SwipeUpPicker picker, Choosing<ActivateSkillRequest> question, string hint, Action<CardDetail> showDetails)
    {
        if(question.Offered.Count == 1 && question.Minimum == 1) return question.Offered[0];
        var (_, skill) = await picker.PickOne(hint,
            question.Offered.Select(activation => new SwipeUpOption<ActivateSkillRequest>(activation.Card, activation)).ToList(),
            canCancel: false, activation => showDetails(FocusedOn(activation)));
        return skill;
    }

    /// <summary>
    /// "Choose one of": one card per option ArC offers, all <paramref name="card"/> (the card whose skill asks), the
    /// option's label as its tag; the one swiped up applies. None when declined (the question may be cancelled).
    /// </summary>
    public static async Task<IReadOnlyList<SkillOption>> ChooseOption(SwipeUpPicker picker, Choosing<SkillOption> question, CardBase card, Action<CardDetail> showDetails)
    {
        if(question.Offered.Count == 1 && question.Minimum == 1) return [ question.Offered[0] ];
        var (picked, option) = await picker.PickOne(question.Prompt,
            question.Offered.Select(option => new SwipeUpOption<SkillOption>(card, option, new CardTag { Text = option.Label })).ToList(),
            question.CanCancel, shown => showDetails(new CardDetail(card) { FocusText = shown.Label }));
        return picked ? [ option ] : [];
    }

    /// <summary>"Use this skill?" (a Confirm about an ActivateSkillRequest): swipe its card up to use it, Cancel not to.</summary>
    public static async Task<bool> ConfirmSkill(SwipeUpPicker picker, ActivateSkillRequest activation, string hint, Action<CardDetail> showDetails)
    {
        var (picked, _) = await picker.PickOne(hint, [ new SwipeUpOption<ActivateSkillRequest>(activation.Card, activation) ],
            canCancel: true, shown => showDetails(FocusedOn(shown)));
        return picked;
    }

    /// <summary>Any other yes/no question: a dialog with <paramref name="yes"/> and <paramref name="no"/>.</summary>
    public static Task<bool> YesOrNo(Node dialogParent, Confirmation question, string yes, string no)
        => Prompts.Choose<bool>(dialogParent, question.Prompt, [ new Prompts.Option<bool>(yes, true), new Prompts.Option<bool>(no, false) ]);

    /// <summary>A card's details showing only the skill an activation would use.</summary>
    public static CardDetail FocusedOn(ActivateSkillRequest activation) => new(activation.Card) { Focus = activation.Skill };
}
