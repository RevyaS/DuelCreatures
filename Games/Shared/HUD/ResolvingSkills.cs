using System;
using System.Collections.Generic;
using ArC.CardGames.Components;
using ArC.CardGames.Rules;

/// <summary>
/// The player's skills being activated or resolved, innermost last (a skill used while another one resolves is on
/// top). The input provider feeds it from ArC's skill events; every view of "the skill asking" reads it (the pinned
/// ResolvingSkillDetails, an input screen's skill banner), so they always agree.
/// </summary>
public sealed class ResolvingSkills
{
    readonly List<(CardBase Card, ISkill Skill)> resolving = [];

    /// <summary>The innermost skill being used, or null.</summary>
    public (CardBase Card, ISkill Skill)? Current => resolving.Count == 0 ? null : resolving[^1];

    /// <summary>
    /// Whether the player is being asked something now (the input provider sets it once the board is idle and clears it
    /// on the answer). The skill is pinned only then: while the board plays what the answer started (a counter blast's
    /// flips), the board is to be seen.
    /// </summary>
    public bool IsAsking { get; private set; }

    public event Action? Changed;

    /// <summary>Runs <paramref name="answer"/> (a question being answered) with <see cref="IsAsking"/> set.</summary>
    public async System.Threading.Tasks.Task<T> Asking<T>(Func<System.Threading.Tasks.Task<T>> answer)
    {
        SetAsking(true);
        try { return await answer(); }
        finally { SetAsking(false); }
    }

    private void SetAsking(bool asking)
    {
        if(IsAsking == asking) return;
        IsAsking = asking;
        Changed?.Invoke();
    }

    /// <summary><paramref name="skill"/> of <paramref name="card"/> started asking the player things.</summary>
    public void Started(CardBase card, ISkill skill)
    {
        resolving.Add((card, skill));
        Changed?.Invoke();
    }

    /// <summary><paramref name="skill"/> is done: the skill around it (if any) is current again.</summary>
    public void Finished(ISkill skill)
    {
        var index = resolving.FindLastIndex(entry => ReferenceEquals(entry.Skill, skill));
        if(index < 0) return;
        resolving.RemoveAt(index);
        Changed?.Invoke();
    }
}
