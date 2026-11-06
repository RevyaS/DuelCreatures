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

    /// <summary>
    /// One of <paramref name="options"/>, each shown as its card in <paramref name="list"/> (the same card once per
    /// option, e.g. once per effect): pressing a card calls <paramref name="inspect"/> with its option (to show what
    /// it does), dragging it out of the list picks it. With <paramref name="canDecline"/>, closing the list picks
    /// nothing (Picked is false); without, the list can't be closed. The mobile-friendly choice of an effect.
    /// </summary>
    public static async Task<(bool Picked, T Value)> DragOutOfCardList<T>(CardList list, string title,
        IReadOnlyList<(CardBase Card, T Value)> options, bool canDecline, Action<CardBase, T>? inspect = null)
    {
        if(options.Count == 0) throw new ArgumentException($"Nothing to choose from for \"{title}\"", nameof(options));

        list.Show(title, options.Select(option => option.Card));
        var shown = list.ShownCards;
        var optionOf = new Dictionary<Card, T>(ReferenceEqualityComparer.Instance);
        for(int i = 0; i < shown.Count && i < options.Count; i++) optionOf[shown[i]] = options[i].Value;

        var result = new TaskCompletionSource<(bool, T)>();
        Action<Card> onDropped = node =>
        {
            if(optionOf.TryGetValue(node, out var value)) result.TrySetResult((true, value));
        };
        Action<Card> onPressed = node =>
        {
            if(optionOf.TryGetValue(node, out var value)) inspect?.Invoke(node.CurrentCard, value);
        };
        Action onClosed = () =>
        {
            if(canDecline) result.TrySetResult((false, default!));
        };

        list.CanClose = canDecline;
        list.BaseDroppable = true;
        list.CardsDraggable = true;
        list.CardDroppedOutside += onDropped;
        list.CardPressed += onPressed;
        list.OnClosed += onClosed;
        try
        {
            return await result.Task;
        }
        finally
        {
            list.CardDroppedOutside -= onDropped;
            list.CardPressed -= onPressed;
            list.OnClosed -= onClosed;
            list.BaseDroppable = false;
            list.CardsDraggable = false;
            list.CanClose = true;
            if(list.Visible) list.Hide();
        }
    }

    /// <summary>A type or enum name in words: "ActivateEffectsOnField" -> "Activate Effects On Field".</summary>
    public static string Words(string name) => System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z0-9])(?=[A-Z])", " ");
}
