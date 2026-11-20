using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Setup;
using ArC.CardGames.Vanguard;
using ActivateSkillRequest = ArC.CardGames.Skills.ActivateSkillRequest;

/// <summary>
/// Answers Vanguard's "pick from these" and "yes or no" questions (Choose, Confirm) by their shape: the mulligan by its
/// purpose; circles by tapping them (several: numbered badges, then Confirm); cards by where they are (damage: long press
/// to turn for a counter blast or charge; the deck: drag one out of the list; the hand, the soul, the drop zone: pick
/// from a list, then Confirm); skills by dragging one out of the list. A shape with no gesture throws UnansweredQuestion.
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
        public static string ChooseCircles(string prompt, int minimum, int maximum) => $"{prompt} ({Select(minimum, maximum)}): tap to choose, then Confirm";
        public static string ChooseSkillToResolve = "Select the skill to resolve first";
        public static string ActivateTrigger = "Select Skill to Activate";
        public static string Heal = "Select Card to Heal";
        public static string Hand = "Hand";
        public static string DamageZone = "Damage Zone";
        public static string Soul = "Soul";
        public static string Drop = "Drop Zone";
        public static string Deck = "Deck";
        public static string CounterBlast(int amount) => $"Counter Blast ({amount}): long press face-up damage, then Confirm";
        public static string CounterCharge(int amount) => $"Counter Charge ({amount}): long press face-down damage, then Confirm";
        public static string Yes = "Yes";
        public static string No = "No";
    }

    VanguardPlayArea PlayArea => input.PlayArea;
    readonly VanguardMulliganStrategy mulligan = new(input);

    #region Choose
    public async Task<IReadOnlyList<T>> Choose<T>(Choosing<T> question)
    {
        if(question.Purpose is MulliganQuestion.RETURN && question is Choosing<CardBase> redraw)
        {
            return (IReadOnlyList<T>)await mulligan.Answer(redraw);
        }
        if(typeof(ICardSpace).IsAssignableFrom(typeof(T)))
        {
            return (await ChooseZones(question.Offered.Cast<ICardSpace>().ToList(), question.Minimum, question.Maximum, ZoneHint(question.Purpose, question.Prompt))).Cast<T>().ToList();
        }
        if(typeof(CardBase).IsAssignableFrom(typeof(T)))
        {
            return (await ChooseCards(question.Offered.Cast<CardBase>().ToList(), question.Minimum, question.Maximum, question.Purpose, question.Prompt, question.CanCancel)).Cast<T>().ToList();
        }
        if(question is Choosing<ActivateSkillRequest> skills)
        {
            IReadOnlyList<ActivateSkillRequest> next = new List<ActivateSkillRequest> { await ChooseSkill(skills) };
            return (IReadOnlyList<T>)next;
        }
        throw new UnansweredQuestion(question.Purpose, question.Prompt, typeof(T));
    }

    private static string ZoneHint(object purpose, string prompt) => purpose switch
    {
        TriggerQuestion.Power => Text.ProvidePower,
        TriggerQuestion.Critical => Text.ProvideCritical,
        TriggerQuestion.Stand => Text.Stand,
        _ => prompt,
    };

    // One: tap it. Several: tap circles to choose them (badges number them), tap a chosen one again to drop it, then Confirm
    private async Task<List<ICardSpace>> ChooseZones(List<ICardSpace> candidates, int minimum, int maximum, string hint)
    {
        if(minimum == 1 && maximum == 1)
        {
            using var scope = input.Board.Scope().Zones(candidates).Hint(hint);
            return [ ((ZoneClicked)await scope.Next()).Zone ];
        }

        var chosen = new CardSelection<ICardSpace>(candidates, minimum, maximum);
        while(true)
        {
            using var scope = input.Board.Scope().Hint(Text.ChooseCircles(hint, minimum, maximum));
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

    // By where the offered cards are: the gesture depends on the place, not on the skill asking
    private async Task<IReadOnlyList<CardBase>> ChooseCards(List<CardBase> cards, int minimum, int maximum, object purpose, string prompt, bool canCancel)
    {
        if(cards.Count == 0) return [];
        bool All(IEnumerable<CardBase> zone) => cards.All(card => Refs.Has(zone, card));

        if(All(PlayArea.DamageZone.Cards))
        {
            if(purpose is TriggerQuestion.Heal) return await Prompts.PickCards(input.CardPicker, Text.DamageZone, Text.Heal, minimum, maximum, cards);
            if(minimum == maximum && cards.All(card => card.IsFaceUp)) return await TurnDamage(cards, maximum, blast: true);
            if(minimum == maximum && cards.All(card => !card.IsFaceUp)) return await TurnDamage(cards, maximum, blast: false);
            return await Prompts.PickCards(input.CardPicker, Text.DamageZone, prompt, minimum, maximum, cards);
        }
        if(All(PlayArea.Deck.Cards) && maximum == 1)
        {
            var (picked, card) = await Prompts.DragOutOfCardList(input.EffectPicker, prompt, cards.Select(card => (card, card)).ToList(), canDecline: canCancel || minimum == 0,
                (shown, _) => input.ShowDetails(new CardDetail(shown)));
            return picked ? [ card ] : [];
        }

        var title = All(PlayArea.Hand.Cards) ? Text.Hand : All(PlayArea.Soul.Cards) ? Text.Soul : All(PlayArea.DropZone.Cards) ? Text.Drop : All(PlayArea.Deck.Cards) ? Text.Deck : null;
        if(title is not null) return await Prompts.PickCards(input.CardPicker, title, prompt, minimum, maximum, cards);

        // Units: tap their circles
        var circles = cards.Select(CircleOf).ToList();
        if(circles.All(circle => circle is not null))
        {
            var chosen = await ChooseZones(circles.Cast<ICardSpace>().ToList(), minimum, maximum, prompt);
            return chosen.Select(zone => ((UnitCircle)zone).Card!).Cast<CardBase>().ToList();
        }
        throw new UnansweredQuestion(purpose, prompt, typeof(CardBase), "the offered cards are in several places or in none the board can pick from");
    }

    private UnitCircle? CircleOf(CardBase card)
        => new[] { PlayArea, input.OpponentPlayArea }
            .SelectMany(area => area.Rearguards.Cast<UnitCircle>().Prepend(area.Vanguard))
            .FirstOrDefault(circle => ReferenceEquals(circle.Card, card));

    // Counter blast (face-up damage turns face down) or counter charge (face down turns face up): long press the damage
    // to turn it, long press again to turn it back, then Confirm once exactly enough are turned
    private async Task<IReadOnlyList<CardBase>> TurnDamage(List<CardBase> candidates, int amount, bool blast)
    {
        var damage = (LineZone)input.ViewOf(PlayArea.DamageZone);
        List<CardBase> chosen = [];
        try
        {
            while(true)
            {
                using var scope = input.Board.Scope()
                    .LongPress(PlayArea.DamageZone, card => Refs.Has(candidates, card) && (Refs.Has(chosen, card) || chosen.Count < amount))
                    .Hint(blast ? Text.CounterBlast(amount) : Text.CounterCharge(amount));
                if(chosen.Count == amount) scope.Button(BoardButton.Confirm);

                switch(await scope.Next())
                {
                    case ButtonPressed:
                        return chosen;
                    case CardLongPressed { Card: var card }:
                        // Previewed by turning the card where it lies; the game turns it for real once confirmed
                        bool add = !Refs.Remove(chosen, card);
                        if(add) chosen.Add(card);
                        if(damage.FindCardNode(card) is { } node) node.IsFront = add ? !blast : blast;
                        break;
                }
            }
        }
        finally
        {
            damage.Resync();
        }
    }

    // Several of the player's skills wait at once: drag the one to resolve first out of the list
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
    /// <summary>"Use this skill?": drag its card out of the list to use it, close the list not to. Any other question: Yes or No.</summary>
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
