using System;
using System.Collections.Generic;

/// <summary>
/// Narrows what the next prompt for a question offers, once: whoever picked an action knows what its next question
/// may be answered with (e.g. "Flip Summon" asks for a zone; only face-down monsters can be flipped). Taken by the
/// prompt that asks that question next, so it never leaks into a later, unrelated one.
/// </summary>
public sealed class Narrowing
{
    readonly Dictionary<object, Delegate> filters = [];

    /// <summary>The next prompt for <paramref name="question"/> only offers what <paramref name="allowed"/> accepts.</summary>
    public void Narrow<TAnswer>(Question<TAnswer> question, Func<TAnswer, bool> allowed) => filters[question] = allowed;

    /// <summary>The next prompt for <paramref name="question"/> (a list) only offers items <paramref name="allowed"/> accepts.</summary>
    public void NarrowEach<TItem>(Question<List<TItem>> question, Func<TItem, bool> allowed) => filters[question] = allowed;

    public Func<TAnswer, bool> Take<TAnswer>(Question<TAnswer> question)
        => filters.Remove(question, out var filter) ? (Func<TAnswer, bool>)filter : _ => true;

    public Func<TItem, bool> TakeEach<TItem>(Question<List<TItem>> question)
        => filters.Remove(question, out var filter) ? (Func<TItem, bool>)filter : _ => true;

    public void Clear() => filters.Clear();
}
