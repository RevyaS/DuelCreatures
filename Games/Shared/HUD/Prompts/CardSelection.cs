using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The picks of one "pick between <see cref="Minimum"/> and <see cref="Maximum"/>, numbered, then OK" question, in the
/// order they were made: the board's circles (ChooseZones) and a SelectionGrid count and number them the same way.
/// Items are compared by reference (ArC's cards and zones are records). Picking more than the maximum, or picking
/// something not offered, is an error.
/// </summary>
public sealed class CardSelection<T> where T : class
{
    readonly List<T> picks = [];

    public CardSelection(IEnumerable<T> offered, int minimum, int maximum)
    {
        Offered = offered.ToList();
        if(minimum < 0 || maximum < minimum) throw new ArgumentException($"A selection needs 0 <= minimum <= maximum, got {minimum}-{maximum}");
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>What may be picked.</summary>
    public IReadOnlyList<T> Offered { get; }
    public int Minimum { get; }
    public int Maximum { get; }

    /// <summary>The picks, first pick first.</summary>
    public IReadOnlyList<T> Picks => picks;
    public int Count => picks.Count;

    /// <summary>Enough is picked to answer.</summary>
    public bool CanConfirm => picks.Count >= Minimum && picks.Count <= Maximum;
    /// <summary>Another pick still fits (false: unpicked items can't be picked until one is dropped).</summary>
    public bool CanPickMore => picks.Count < Maximum;

    /// <summary>Picks or drops changed.</summary>
    public event Action? Changed;

    public bool IsPicked(T item) => Refs.Has(picks, item);

    /// <summary>Which pick <paramref name="item"/> was (1, 2, …), or null if it isn't picked.</summary>
    public int? NumberOf(T item)
    {
        var index = picks.FindIndex(pick => ReferenceEquals(pick, item));
        return index < 0 ? null : index + 1;
    }

    /// <summary>Whether <paramref name="item"/> could be pressed now: picked ones can be dropped, others picked while there's room.</summary>
    public bool CanToggle(T item) => IsPicked(item) || (CanPickMore && Refs.Has(Offered, item));

    /// <summary>Drops <paramref name="item"/> if it's picked (later picks move up a number), else picks it. Returns whether it's picked now.</summary>
    public bool Toggle(T item)
    {
        if(Refs.Remove(picks, item))
        {
            Changed?.Invoke();
            return false;
        }
        if(!Refs.Has(Offered, item)) throw new InvalidOperationException($"{item} isn't one of the {Offered.Count} offered to pick");
        if(!CanPickMore) throw new InvalidOperationException($"{item} can't be picked: {Maximum} already are (the most this asks for)");
        picks.Add(item);
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        if(picks.Count == 0) return;
        picks.Clear();
        Changed?.Invoke();
    }
}
