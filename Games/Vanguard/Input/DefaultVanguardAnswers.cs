using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Rules;
using ArC.CardGames.Vanguard;
using ArC.CardGames.Vanguard.Evaluations;
using static VanguardQuestions;

/// <summary>
/// Answers every Vanguard question with the old board's mobile prompts: tap a highlighted circle, long press damage to
/// pay a counter blast, pick from a list then Confirm, drag a skill or a card out of a list, drag guards onto the
/// guardian circle. It sits under every strategy, so the game is playable before a phase has its own. What it offers
/// is filtered by VanguardGameRules, the selector ArC asks with, and whatever the action that asks narrowed it to.
/// </summary>
public sealed class DefaultVanguardAnswers(VanguardInput input) : IVanguardAnswers
{
    /// <summary>The prompts' wording, in one place.</summary>
    public static class Text
    {
        public static string MainPhaseActions = "Main Phase: what do you do?";
        public static string AttackPhaseActions = "Attack Phase: what do you do?";
        public static string ChooseFromHand = "Hand";
        public static string Select(int minimum, int maximum) => minimum == maximum ? $"Select {maximum}" : $"Select {minimum}-{maximum}";
        public static string ChooseCircle = "Tap one of your circles";
        public static string ChooseOpponentCircle = "Tap one of your opponent's circles";
        public static string ProvidePower = "Tap a unit to give the trigger's power";
        public static string ProvideCritical = "Tap a unit to give the trigger's critical";
        public static string Stand = "Tap a resting rear-guard to stand it";
        public static string ChooseCircles(int minimum, int maximum) => $"{Select(minimum, maximum)} unit(s): tap to choose, then Confirm";
        public static string Ride = "Ride: tap a card in your hand, or Next Phase";
        public static string ChooseSkill = "Which skill? Drag it out of the list";
        public static string ChooseSkillToResolve = "Select the skill to resolve first";
        public static string ActivateTrigger = "Select Skill to Activate";
        public static string Heal = "Select Card to Heal";
        public static string DamageZone = "Damage Zone";
        public static string Soul = "Soul";
        public static string SoulBlast(int amount) => $"Soul Blast ({amount})";
        public static string FromDeck = "Select card from deck";
        public static string CounterBlast(int amount) => $"Counter Blast ({amount}): long press face-up damage, then Confirm";
        public static string CounterCharge(int amount) => $"Counter Charge ({amount}): long press face-down damage, then Confirm";
        public static string ChooseOption(string card) => $"{card}: choose one (drag it out)";
        public static string Guard = "Guard: drag cards onto the guardian circle, tap a front rear-guard to intercept, then Confirm";
    }

    /// <summary>The regions zones show while a card is dragged over them.</summary>
    public static class Choices
    {
        public static readonly DropChoice Guard = new("Guard");
        public static readonly DropChoice Return = new("Return");
    }

    IVanguardPlayerInputProvider Player => input.Player;
    VanguardPlayArea PlayArea => input.PlayArea;
    VanguardPlayArea OpponentPlayArea => input.OpponentPlayArea;

    #region Phases
    public Task<IMainPhaseAction> RequestMainPhaseAction(List<IMainPhaseAction> actions)
        => Prompts.Choose(input.DialogParent, Text.MainPhaseActions, actions.Select(action => new Prompts.Option<IMainPhaseAction>(Prompts.Words(action.GetType().Name), action)).ToList());

    public Task<IAttackPhaseAction> RequestAttackPhaseAction(List<IAttackPhaseAction> actions)
        => Prompts.Choose(input.DialogParent, Text.AttackPhaseActions, actions.Select(action => new Prompts.Option<IAttackPhaseAction>(Prompts.Words(action.GetType().Name), action)).ToList());
    #endregion

    #region Hand
    public async Task<CardBase> SelectCardFromHand()
    {
        var allowed = input.Narrowing.Take(CardFromHand);
        var cards = PlayArea.Hand.Cards.Where(allowed).ToList();
        EnsureAny(cards, CardFromHand);
        return (await Prompts.PickCards(input.CardPicker, Text.ChooseFromHand, Text.Select(1, 1), 1, 1, cards)).Single();
    }

    public async Task<CardBase?> SelectCardFromHandOrNot()
    {
        var allowed = input.Narrowing.Take(CardFromHandOrNot);
        using var scope = input.Board.Scope()
            .Cards(PlayArea.Hand, card => allowed(card))
            .Button(BoardButton.NextPhase)
            .Hint(Text.Ride);
        return await scope.Next() is CardClicked click ? click.Card : null;
    }

    public Task<List<CardBase>> SelectCardsFromHandRange(int minimum, int maximum)
    {
        // Guarding one of the player's units: the guard prompt (it also answers the interceptors)
        if(input.OwnGuard is { } guard) return new VanguardGuardPrompt(input, guard).Run();

        var allowed = input.Narrowing.TakeEach(CardsFromHand);
        return Prompts.PickCards(input.CardPicker, Text.ChooseFromHand, Text.Select(minimum, maximum), minimum, maximum, PlayArea.Hand.Cards.Where(allowed).ToList());
    }
    #endregion

    #region Circles
    public async Task<UnitCircle> SelectOwnUnitCircle(UnitSelector selector)
    {
        var allowed = input.Narrowing.Take(OwnCircle);
        var circles = PlayArea.GetUnitCirclesBySelector(selector).Where(allowed).ToList();
        EnsureAny(circles, OwnCircle);
        var hint = input.Game.GameState switch
        {
            TriggerPowerState => Text.ProvidePower,
            TriggerCriticalState => Text.ProvideCritical,
            TriggerStandState => Text.Stand,
            _ => Text.ChooseCircle,
        };
        using var scope = input.Board.Scope().Zones(circles).Hint(hint);
        return (UnitCircle)((ZoneClicked)await scope.Next()).Zone;
    }

    public async Task<UnitCircle> SelectOpponentCircle(UnitSelector selector)
    {
        var allowed = input.Narrowing.Take(OpponentCircle);
        var circles = OpponentPlayArea.GetUnitCirclesBySelector(selector).Where(allowed).ToList();
        EnsureAny(circles, OpponentCircle);
        using var scope = input.Board.Scope().Zones(circles).Hint(Text.ChooseOpponentCircle);
        return (UnitCircle)((ZoneClicked)await scope.Next()).Zone;
    }

    // Any number (e.g. which rear-guards intercept): the guard prompt answers it while guarding
    public Task<List<UnitCircle>> SelectOwnUnitCircles(UnitSelector selector)
        => ChooseCircles(PlayArea.GetUnitCirclesBySelector(selector).Where(input.Narrowing.TakeEach(OwnCircles)).ToList(), 0, int.MaxValue);

    public Task<List<UnitCircle>> SelectOwnUnitCirclesRange(UnitSelector selector, int minimum, int maximum)
        => ChooseCircles(PlayArea.GetUnitCirclesBySelector(selector).Where(input.Narrowing.TakeEach(OwnCirclesRange)).ToList(), minimum, maximum);

    // Tap circles to choose them (their unit is outlined), tap a chosen one again to drop it, then Confirm
    private async Task<List<UnitCircle>> ChooseCircles(List<UnitCircle> candidates, int minimum, int maximum)
    {
        maximum = Math.Min(maximum, candidates.Count);
        minimum = Math.Min(minimum, maximum);
        List<UnitCircle> chosen = [];
        while(true)
        {
            using var scope = input.Board.Scope().Hint(Text.ChooseCircles(minimum, maximum));
            if(chosen.Count < maximum) scope.Zones(candidates.Where(circle => !Refs.Has(chosen, circle)));
            foreach(var circle in chosen) scope.Cards(circle);
            scope.Numbered(chosen);
            if(chosen.Count >= minimum) scope.Button(BoardButton.Confirm);

            switch(await scope.Next())
            {
                case ButtonPressed:
                    return chosen;
                case ZoneClicked { Zone: UnitCircle circle }:
                    chosen.Add(circle);
                    break;
                case CardClicked { Zone: UnitCircle circle }:
                    Refs.Remove(chosen, circle);
                    break;
            }
        }
    }
    #endregion

    #region Skills
    public async Task<VanguardActivationSkill> SelectSkillToActivate(List<VanguardActivationSkill> skills)
    {
        if(skills.Count == 1) return skills[0];
        var (_, skill) = await PickEffect(Text.ChooseSkill, skills.Select(skill => (OwnerOf(skill), skill)).ToList(), canDecline: false);
        return skill;
    }

    // Several of the player's skills triggered at once: drag the one to resolve first out of the list (the rest are
    // offered again after it, as it may change what they can do)
    public async Task<PendingSkill> SelectSkillToInvoke(IReadOnlyList<PendingSkill> skills)
    {
        if(skills.Count == 1) return skills[0];
        var (_, pending) = await Prompts.DragOutOfCardList(input.EffectPicker, Text.ChooseSkillToResolve,
            skills.Select(skill => ((CardBase)skill.Source.Card, skill)).ToList(), canDecline: false,
            (card, pending) => input.ShowDetails(new CardDetail(card) { Focus = pending.Skill }));
        return pending;
    }

    public async Task<bool> QueryActivateSkill(VanguardCard invoker, VanguardAutomaticSkill skill)
    {
        var (picked, _) = await PickEffect(Text.ActivateTrigger, [ ((CardBase)invoker, skill) ], canDecline: true);
        return picked;
    }

    // "Choose one of": the options usable now (ArC rejects the others), each as the skill's card in the skill list; press
    // one to read it, drag it out to choose it
    public async Task<VanguardSkills.SkillAction> SelectSkillOption(IReadOnlyList<VanguardSkills.SkillAction> options, VanguardSkillContextObject context)
    {
        var choosable = options.Where(option => VanguardSkills.SkillRules.CanChooseSkillOption(option, context)).ToList();
        if(choosable.Count == 0) throw new InvalidOperationException($"{context.Invoker.Card.Name}: none of its options can be chosen");
        if(choosable.Count == 1) return choosable[0];
        var (_, option) = await Prompts.DragOutOfCardList(input.EffectPicker, Text.ChooseOption(context.Invoker.Card.Name),
            choosable.Select(option => ((CardBase)context.Invoker.Card, option)).ToList(), canDecline: false,
            (card, option) => input.ShowDetails(new CardDetail(card) { FocusText = VanguardSkillToStringInterpreter.ExtractSkillAction(option) }));
        return option;
    }

    private Task<(bool Picked, T Value)> PickEffect<T>(string title, IReadOnlyList<(CardBase Card, T Value)> options, bool canDecline) where T : ISkill
        => Prompts.DragOutOfCardList(input.EffectPicker, title, options, canDecline,
            (card, skill) => input.ShowDetails(new CardDetail(card) { Focus = skill }));

    // The card a skill is printed on (copies share their skills, so any copy will do): the player's cards first
    private CardBase OwnerOf(ISkill skill)
        => new[] { PlayArea, OpponentPlayArea }
            .SelectMany(area => area.Zones)
            .SelectMany(zone => ZoneSnapshot.Of(zone).Cards.Select(card => card.Card))
            .OfType<SkilledCard>()
            .FirstOrDefault(card => card.Skills.Contains(skill))
            ?? throw new InvalidOperationException($"No card has the skill {skill.GetType().Name}");
    #endregion

    #region Damage, soul and deck
    public async Task<VanguardCard> SelectCardFromDamageZone()
        => (VanguardCard)(await Prompts.PickCards(input.CardPicker, Text.DamageZone, Text.Heal, 1, 1, PlayArea.DamageZone.Cards.ToList())).Single();

    public async Task<List<VanguardCard>> SelectCardsFromSoul(int amount)
        => (await Prompts.PickCards(input.CardPicker, Text.Soul, Text.SoulBlast(amount), amount, amount, PlayArea.Soul.Cards.ToList())).Cast<VanguardCard>().ToList();

    public async Task<VanguardCard> SelectCardFromDeck(int minGrade, int maxGrade)
    {
        var cards = PlayArea.Deck.Cards.Cast<VanguardCard>().Where(card => minGrade <= card.Grade && card.Grade <= maxGrade).ToList();
        EnsureAny(cards, CardFromDeck);
        var (_, card) = await Prompts.DragOutOfCardList(input.EffectPicker, Text.FromDeck, cards.Select(card => ((CardBase)card, card)).ToList(), canDecline: false,
            (shown, _) => input.ShowDetails(new CardDetail(shown)));
        return card;
    }

    // Counter blast (face-up damage turns face down) or counter charge (face down turns face up): long press the damage
    // to turn it, long press again to turn it back, then Confirm once exactly enough are turned
    public async Task<List<VanguardCard>> SelectCardsFromDamageZone(int amount)
    {
        bool blast = input.Game.GameState switch
        {
            CounterBlastPhaseState => true,
            CounterChargePhaseState => false,
            var state => throw new NotSupportedException($"Damage is chosen for {state?.GetType().Name ?? "nothing"}: only a counter blast or a counter charge is supported"),
        };
        var damage = (LineZone)input.ViewOf(PlayArea.DamageZone);
        var candidates = PlayArea.DamageZone.Cards.Where(card => card.IsFaceUp == blast).ToList();
        List<VanguardCard> chosen = [];
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
                    case CardLongPressed { Card: VanguardCard card }:
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
    #endregion

    private static void EnsureAny<T>(IEnumerable<T> options, object question)
    {
        if(!options.Any()) throw new InvalidOperationException($"\"{question}\" was asked with nothing to choose from");
    }
}

/// <summary>
/// Guarding one of the player's units, as on the old board: the hand shows each card's shield; drag cards onto the
/// guardian circle (and back to the hand), tap a front rear-guard that can intercept to send it there (tap it on the
/// guardian circle to call it back), then Confirm. The guarded unit shows the power it would have. Nothing moves in
/// the game until Confirm: the board previews the guards, then shows the game again before answering. Answers the
/// guards, and pre-answers the interceptors (asked next).
/// </summary>
public sealed class VanguardGuardPrompt(VanguardInput input, GuardState guard)
{
    VanguardPlayArea PlayArea => input.PlayArea;

    public async Task<List<CardBase>> Run()
    {
        var hand = (HandZone)input.ViewOf(PlayArea.Hand);
        var guardian = (LineZone)input.ViewOf(PlayArea.GuardZone);
        var guarded = (CardArea)input.ViewOf(guard.TargetCircle);
        var interceptable = PlayArea.GetUnitCirclesBySelector(new(PlayArea.SelectCirclesExcept(UnitCircleEnum.FRONT_REARGUARD, guard.TargetCircle), Skill: UnitSkillFilterEnum.INTERCEPT_ONLY));

        List<CardBase> guards = [];
        List<UnitCircle> intercepts = [];
        List<CardBase> interceptingCards = [];

        // The hand shows shields while guarding
        hand.DecorateCard = node =>
        {
            if(node is VanguardCardComponent card) card.GuardMode = true;
        };
        try
        {
            while(true)
            {
                // Interceptors are outlined on the guardian circle: they go back by a tap there, not by a drag
                using var scope = input.Board.Scope()
                    .Drag(PlayArea.Hand, card => !Refs.Has(guards, card))
                    .Drop(PlayArea.GuardZone, card => Refs.Has(PlayArea.Hand.Cards, card) && !Refs.Has(guards, card) ? [ DefaultVanguardAnswers.Choices.Guard ] : [])
                    .Drag(PlayArea.GuardZone, card => Refs.Has(guards, card))
                    .Drop(PlayArea.Hand, card => Refs.Has(guards, card) ? [ DefaultVanguardAnswers.Choices.Return ] : [])
                    .Zones(interceptable.Where(circle => !Refs.Has(intercepts, circle)))
                    .Cards(PlayArea.GuardZone, card => Refs.Has(interceptingCards, card))
                    .Button(BoardButton.Confirm)
                    .Hint(DefaultVanguardAnswers.Text.Guard);

                switch(await scope.Next())
                {
                    case ButtonPressed:
                        input.Answers.Set(VanguardQuestions.OwnCircles, intercepts);
                        return guards;

                    case CardDropped { Card: var card, To: GuardZone }:
                        guards.Add(card);
                        hand.RemoveCard(card);
                        guardian.AddCard(CardScenes.Create(card));
                        break;

                    case CardDropped { Card: var card, To: Hand }:
                        Refs.Remove(guards, card);
                        guardian.RemoveCard(card);
                        hand.AddCard(CardScenes.Create(card));
                        break;

                    case ZoneClicked { Zone: UnitCircle circle } when circle.Card is { } unit:
                        intercepts.Add(circle);
                        interceptingCards.Add(unit);
                        ((CardArea)input.ViewOf(circle)).ClearCard();
                        guardian.AddCard(CardScenes.Create(unit));
                        break;

                    case CardClicked { Card: var card }:
                        var back = intercepts.First(circle => ReferenceEquals(circle.Card, card));
                        Refs.Remove(intercepts, back);
                        Refs.Remove(interceptingCards, card);
                        guardian.RemoveCard(card);
                        input.ViewOf(back).Resync();
                        break;
                }
                ShowGuardedPower(guarded, guards, intercepts);
            }
        }
        finally
        {
            // The board shows the game again; the guards then move there for real
            hand.DecorateCard = null;
            hand.Resync();
            guardian.Resync();
            foreach(var circle in intercepts) input.ViewOf(circle).Resync();
            if(guard.TargetCircle.Card is { } target && guarded.FindCardNode(target) is VanguardCardComponent node)
            {
                node.Power = guard.TargetCircle.GetOverallPower();
            }
        }
    }

    // The guarded unit's power with the guards so far (interceptors can have "when placed on (GC)" shield, so ArC is asked)
    private void ShowGuardedPower(CardArea guarded, List<CardBase> guards, List<UnitCircle> intercepts)
    {
        if(guard.TargetCircle.Card is not { } target || guarded.FindCardNode(target) is not VanguardCardComponent node) return;
        node.Power = guard.TargetCircle.GetOverallPower()
            + guards.Cast<VanguardCard>().Sum(card => card.Guard)
            + intercepts.Sum(circle => VanguardSkillJudge.ExpectedGuardianShield(circle.Card!, intercepting: true));
    }
}

/// <summary>
/// Lists of cards and zones compared by reference: ArC's cards and zones are records, so two copies of a card (or two
/// empty circles) are Equal, but are never the same one.
/// </summary>
public static class Refs
{
    public static bool Has<T>(IEnumerable<T> items, T item) where T : class => items.Any(candidate => ReferenceEquals(candidate, item));

    public static bool Remove<T>(List<T> items, T item) where T : class
    {
        var index = items.FindIndex(candidate => ReferenceEquals(candidate, item));
        if(index < 0) return false;
        items.RemoveAt(index);
        return true;
    }
}
