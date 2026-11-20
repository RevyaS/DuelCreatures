using System;
using Godot;

/// <summary>
/// What an input screen asks: the hint (%Hint) and, while cards are picked, how many of how many (%Counter, e.g.
/// "2 / 3"). Laid out in PromptHeader.tscn; the counter's wording is exported.
/// </summary>
public partial class PromptHeader : Control
{
    /// <summary>The counter: {0} is how many are picked, {1} the most that may be.</summary>
    [Export] public string CounterFormat { get; set; } = "{0} / {1}";

    Label? hint, counter;
    Label HintLabel => hint ??= GetNode<Label>("%Hint");
    Label CounterLabel => counter ??= GetNode<Label>("%Counter");
    Action? unfollow;

    public string Hint
    {
        get => HintLabel.Text;
        set => HintLabel.Text = value;
    }

    /// <summary>Shows <paramref name="picked"/> of <paramref name="maximum"/>.</summary>
    public void ShowCount(int picked, int maximum)
    {
        CounterLabel.Text = string.Format(CounterFormat, picked, maximum);
        CounterLabel.Show();
    }

    public void HideCount()
    {
        unfollow?.Invoke();
        unfollow = null;
        CounterLabel.Hide();
    }

    /// <summary>Counts <paramref name="selection"/>'s picks as they're made, until <see cref="HideCount"/> or another Follow.</summary>
    public void Follow<T>(CardSelection<T> selection) where T : class
    {
        HideCount();
        void Update() => ShowCount(selection.Count, selection.Maximum);
        selection.Changed += Update;
        unfollow = () => selection.Changed -= Update;
        Update();
    }

    public override void _ExitTree() => HideCount();
}
