using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using Godot;

/// <summary>The plain prompts default strategies use: a choice from a list, and picking cards from a list.</summary>
public static class Prompts
{
    public sealed record Option<T>(string Label, T Value, bool Enabled = true);

    /// <summary>
    /// One of <paramref name="options"/>, from a ChoiceDialog under <paramref name="parent"/>. Closing the dialog
    /// without choosing opens it again: the game is waiting for an answer.
    /// </summary>
    public static async Task<T> Choose<T>(Node parent, string title, IReadOnlyList<Option<T>> options)
    {
        if(options.Count == 0) throw new ArgumentException($"Nothing to choose from for \"{title}\"", nameof(options));
        while(true)
        {
            var chosen = new TaskCompletionSource<Option<T>?>();
            var dialog = ChoiceDialog.Show(parent, title,
                options.Select(option => new ChoiceDialog.Choice(option.Label, () => chosen.TrySetResult(option), !option.Enabled)));
            dialog.VisibilityChanged += () =>
            {
                // Deferred: picking a choice hides the dialog before it runs the choice, which must win
                if(!dialog.Visible) Callable.From(() => chosen.TrySetResult(null)).CallDeferred();
            };
            if(await chosen.Task is { } option) return option.Value;
        }
    }

    /// <summary>Between <paramref name="minimum"/> and <paramref name="maximum"/> of <paramref name="cards"/>, picked in a SelectFromCardList.</summary>
    public static async Task<List<CardBase>> PickCards(SelectFromCardList list, string title, string selectionTitle, int minimum, int maximum, IReadOnlyList<CardBase> cards)
    {
        var confirmed = new TaskCompletionSource<List<Card>>();
        Action<List<Card>> onConfirmed = picked => confirmed.TrySetResult(picked);
        list.ConfirmedCards += onConfirmed;
        try
        {
            list.Show(title, selectionTitle, minimum, maximum, cards);
            var picked = await confirmed.Task;
            return picked.Select(node => node.CurrentCard).ToList();
        }
        finally
        {
            list.ConfirmedCards -= onConfirmed;
            list.Deactivate();
        }
    }

    /// <summary>A type or enum name in words: "ActivateEffectsOnField" -> "Activate Effects On Field".</summary>
    public static string Words(string name) => System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
}
