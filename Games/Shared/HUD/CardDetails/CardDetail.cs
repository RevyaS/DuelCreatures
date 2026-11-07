using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;

/// <summary>A card to show in a card details view: where it is (null outside a game, e.g. the deck builder) and the skills effects gave it.</summary>
public sealed record CardDetail(CardBase Card, ICardSpace? From, IReadOnlyList<ISkill> GrantedSkills)
{
    public CardDetail(CardBase card) : this(card, null, []) {}

    /// <summary>
    /// One skill to show alone (e.g. the effect a card stands for in a list of effects to pick from): the skill text
    /// shows only it, and nothing offers to activate it.
    /// </summary>
    public ISkill? Focus { get; init; }

    /// <summary>
    /// One part of a skill to show alone, as text (e.g. one option of a "Choose one of"): like <see cref="Focus"/>, the
    /// skill text shows only it, and nothing offers to activate anything.
    /// </summary>
    public string? FocusText { get; init; }

    /// <summary>Whether one skill, or one part of one, is shown alone (<see cref="Focus"/> or <see cref="FocusText"/>).</summary>
    public bool IsFocused => Focus is not null || FocusText is not null;

    /// <summary>The card's own skills, then the granted ones.</summary>
    public IEnumerable<ISkill> AllSkills => (Card is SkilledCard skilled ? skilled.Skills : []).Concat(GrantedSkills);
}

/// <summary>
/// A part of a card details scene, bound to some of the card's properties (its name, a stat, its skills). The scene's
/// root (CardDetailsView) calls every part under it; a part that doesn't apply to the card (e.g. ATK on a spell) hides.
/// </summary>
public interface ICardDetailPart
{
    void Show(CardDetail detail);
}

/// <summary>
/// Which of a card's effects its details activate with a button, where the card is now; the others read as text
/// (e.g. a spell in the hand is activated by dropping it, not from its details). Provided by the details root
/// (CardDetailsView, per game).
/// </summary>
public interface ISkillButtons
{
    bool HasButton(ISkill skill, CardDetail detail);
}

public static class SkillButtons
{
    /// <summary>Every effect the player activates (IManuallyActivatedSkill) has a button.</summary>
    public static ISkillButtons ManuallyActivated { get; } = new ManuallyActivatedButtons();

    sealed class ManuallyActivatedButtons : ISkillButtons
    {
        public bool HasButton(ISkill skill, CardDetail detail) => skill is IManuallyActivatedSkill;
    }
}

/// <summary>
/// What a card's details may do now: activate a skill shown in them, or a card command (e.g. "Summon" an Xyz monster
/// from the Extra Deck). The details only show the buttons: what is allowed is decided outside (the current input
/// strategy), so a stale button can't fire. Provided by an ancestor (IProvide); without one, the details are read-only.
/// </summary>
public interface IActivationGate
{
    bool CanActivate(CardBase card, ICardSpace? from, ISkill skill);
    void Activate(CardBase card, ICardSpace? from, ISkill skill);
    /// <summary>The commands the details offer for <paramref name="card"/> where it is now.</summary>
    IReadOnlyList<CardCommand> CommandsFor(CardBase card, ICardSpace? from);
    void Invoke(CardBase card, ICardSpace? from, CardCommand command);
    /// <summary>What can be done changed: shown buttons are re-checked.</summary>
    event Action? Changed;
}

public static class ActivationGates
{
    /// <summary>Allows nothing (outside a game, or while nothing may be activated).</summary>
    public static IActivationGate DenyAll { get; } = new DenyAllGate();

    /// <summary>
    /// The gate an ancestor of <paramref name="node"/> provides, or null when none does: the details are read-only
    /// (e.g. the deck builder), so activated skills read as text rather than buttons.
    /// </summary>
    public static IActivationGate? ProvidedTo(Godot.Node node) => node.DependOn<IActivationGate?>(null);

    sealed class DenyAllGate : IActivationGate
    {
        public bool CanActivate(CardBase card, ICardSpace? from, ISkill skill) => false;
        public void Activate(CardBase card, ICardSpace? from, ISkill skill) => throw new InvalidOperationException("Nothing can be activated now");
        public IReadOnlyList<CardCommand> CommandsFor(CardBase card, ICardSpace? from) => [];
        public void Invoke(CardBase card, ICardSpace? from, CardCommand command) => throw new InvalidOperationException("Nothing can be done now");
        public event Action? Changed { add {} remove {} }
    }
}

/// <summary>
/// The gate an input provider hands its card details: whichever gate is in use now, nothing by default. An input
/// strategy puts its own gate in for as long as it lasts (<see cref="Use"/>, disposed with its scope).
/// </summary>
public sealed class ActivationGateSlot : IActivationGate
{
    IActivationGate current = ActivationGates.DenyAll;

    public event Action? Changed;

    /// <summary>Uses <paramref name="gate"/> until the result is disposed, then the gate before it again.</summary>
    public IDisposable Use(IActivationGate gate)
    {
        var previous = current;
        Switch(gate);
        return new Disposable(() => Switch(previous));
    }

    public bool CanActivate(CardBase card, ICardSpace? from, ISkill skill) => current.CanActivate(card, from, skill);

    public void Activate(CardBase card, ICardSpace? from, ISkill skill)
    {
        if(!current.CanActivate(card, from, skill)) throw new InvalidOperationException($"{card.Name}'s skill can't be activated now");
        current.Activate(card, from, skill);
    }

    public IReadOnlyList<CardCommand> CommandsFor(CardBase card, ICardSpace? from) => current.CommandsFor(card, from);

    public void Invoke(CardBase card, ICardSpace? from, CardCommand command)
    {
        if(!current.CommandsFor(card, from).Contains(command)) throw new InvalidOperationException($"{command} can't be done with {card.Name} now");
        current.Invoke(card, from, command);
    }

    private void Switch(IActivationGate gate)
    {
        current.Changed -= OnChanged;
        current = gate;
        current.Changed += OnChanged;
        Changed?.Invoke();
    }

    private void OnChanged() => Changed?.Invoke();
}

/// <summary>Skills as text when no game's interpreter is provided: the kind of skill, in words.</summary>
public sealed class SkillKindInterpreter : ISkillTextInterpreter
{
    public static SkillKindInterpreter Instance { get; } = new();

    public string Describe(ISkill skill) => Regex.Replace(skill.GetType().Name, "(?<=[a-z0-9])(?=[A-Z])", " ");
}
