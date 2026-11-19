using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Actions;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;
using ArC.CardGames.Setup;
using ActivateSkillRequest = ArC.CardGames.Skills.ActivateSkillRequest;

/// <summary>
/// The requests a menu or a window offers now (<see cref="ActionChoice.Available"/>), for strategies to match gestures
/// against: a gesture is allowed when an offered request could use it, and its answer is the offered request it
/// describes, never one the strategy builds. Cards and zones are compared by reference.
/// </summary>
public sealed class Offered(ActionChoice choice)
{
    public ActionChoice Choice { get; } = choice;

    public IEnumerable<TRequest> Of<TRequest>() where TRequest : Request => Choice.Available.OfType<TRequest>();
    public bool Any<TRequest>(Func<TRequest, bool> match) where TRequest : Request => Of<TRequest>().Any(match);

    /// <summary>The one offered request matching <paramref name="match"/>; throws naming <paramref name="gesture"/> when there are none or several.</summary>
    public TRequest One<TRequest>(Func<TRequest, bool> match, string gesture) where TRequest : Request
    {
        var matching = Of<TRequest>().Where(match).ToList();
        return matching.Count == 1 ? matching[0] : throw new InvalidOperationException(
            $"{gesture} matches {matching.Count} offered {typeof(TRequest).Name}s, not one. Offered: {Describe()}");
    }

    /// <summary>Leaving the phase (Next Phase).</summary>
    public EndPhaseRequest EndPhase => Of<EndPhaseRequest>().Single();
    /// <summary>Leaving the window.</summary>
    public PassRequest Pass => Of<PassRequest>().Single();

    /// <summary>Whether an offered skill activation has this card, place and skill (what the card details may activate).</summary>
    public bool CanActivate(CardBase card, ICardSpace? from, ISkill skill) => Of<ActivateSkillRequest>().Any(activation => IsActivation(activation, card, from, skill));

    /// <summary>The offered activation of <paramref name="skill"/> of <paramref name="card"/> from <paramref name="from"/>.</summary>
    public ActivateSkillRequest Activation(CardBase card, ICardSpace? from, ISkill skill)
        => One<ActivateSkillRequest>(activation => IsActivation(activation, card, from, skill), $"Activating {skill} of {card.Name}");

    private static bool IsActivation(ActivateSkillRequest activation, CardBase card, ICardSpace? from, ISkill skill)
        => ReferenceEquals(activation.Card, card) && ReferenceEquals(activation.From, from) && ReferenceEquals(activation.Skill, skill);

    public string Describe() => string.Join(", ", Choice.Available.Select(request => request.GetType().Name));
}
