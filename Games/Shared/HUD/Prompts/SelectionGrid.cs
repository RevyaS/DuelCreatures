using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using Godot;

/// <summary>
/// The cards an input screen offers, in rows that wrap (%Items) and scroll beyond the grid's height: one
/// <see cref="ItemScene"/> (a SelectableCard scene) per card. Given a <see cref="CardSelection{T}"/>, a tap on a card
/// picks or drops it through it, and every card shows its pick number and whether it can still be picked. Every tap on a
/// card is also <see cref="CardPressed"/> (its details). Sized by ratios, like every card holder: its cards are
/// <see cref="CardScale"/> of the card component's own size, and it is as tall as its rows of cards, up to
/// <see cref="VisibleRows"/> of them (one row of cards: one row tall, so a layout can keep it at the bottom).
/// Laid out in SelectionGrid.tscn.
/// </summary>
public partial class SelectionGrid : ScrollContainer
{
    /// <summary>The scene shown per card; its root is a SelectableCard.</summary>
    [Export] public PackedScene ItemScene { get; set; } = null!;
    /// <summary>Each card's size, as a ratio of the card component's own size (SizeConstants.CardDefaultSize).</summary>
    [Export(PropertyHint.Range, "0.05, 2.0, 0.01, or_greater")] public float CardScale { get; set; } = 0.8f;
    /// <summary>The most rows of cards the grid grows to (e.g. 1.5 shows that there's more below); more scroll.</summary>
    [Export(PropertyHint.Range, "0.5, 3.0, 0.1, or_greater")] public float VisibleRows { get; set; } = 1;

    Container? items;
    Container ItemsContainer => items ??= GetNode<Container>("%Items");

    readonly List<SelectableCard> shown = [];
    // Stops following the current selection
    Action? unbind;

    /// <summary>A tap on a card: show its details.</summary>
    public event Action<SelectableCard>? CardPressed;

    /// <summary>The cards shown, in order.</summary>
    public IReadOnlyList<SelectableCard> Items => shown;

    /// <summary>Shows <paramref name="cards"/> only to read: taps don't pick them.</summary>
    public IReadOnlyList<SelectableCard> Show(IEnumerable<CardBase> cards)
    {
        Clear();
        foreach(var card in cards) Add(card).Selectable = false;
        return shown;
    }

    /// <summary>
    /// Shows one card per item <paramref name="selection"/> offers (<paramref name="cardOf"/>: the card standing for it,
    /// e.g. a skill's card), in the offered order; a tap on one toggles it. Returns the cards in that order (e.g.
    /// to tag them).
    /// </summary>
    public IReadOnlyList<SelectableCard> Show<T>(CardSelection<T> selection, Func<T, CardBase> cardOf) where T : class
    {
        Clear();
        var byCard = new Dictionary<SelectableCard, T>();
        foreach(var item in selection.Offered)
        {
            var card = Add(cardOf(item));
            card.Selectable = true;
            byCard[card] = item;
            card.SelectPressed += pressed =>
            {
                if(selection.CanToggle(byCard[pressed])) selection.Toggle(byCard[pressed]);
            };
        }

        void Refresh()
        {
            foreach(var (card, item) in byCard)
            {
                card.PickNumber = selection.NumberOf(item);
                card.CanSelect = selection.CanToggle(item);
            }
        }
        selection.Changed += Refresh;
        unbind = () => selection.Changed -= Refresh;
        Refresh();
        return shown;
    }

    public void Clear()
    {
        unbind?.Invoke();
        unbind = null;
        foreach(var card in shown) card.QueueFree();
        shown.Clear();
        ScrollVertical = 0;
    }

    public override void _Ready() => ItemsContainer.Resized += FitRows;

    // As tall as the rows of cards, up to VisibleRows of them: a row is a card and the space between rows
    private void FitRows()
    {
        if(shown.Count == 0 || !IsInstanceValid(shown[0])) return;
        var row = shown[0].GetCombinedMinimumSize().Y + ItemsContainer.GetThemeConstant("v_separation");
        var height = Mathf.Min(ItemsContainer.Size.Y, row * VisibleRows);
        CustomMinimumSize = CustomMinimumSize with { Y = Mathf.Ceil(height) };
    }

    public override void _ExitTree()
    {
        unbind?.Invoke();
        unbind = null;
    }

    private SelectableCard Add(CardBase card)
    {
        if(ItemScene is null) throw new InvalidOperationException($"{GetPath()}: set {nameof(ItemScene)} (a {nameof(SelectableCard)} scene)");
        var node = ItemScene.Instantiate() as SelectableCard
            ?? throw new InvalidOperationException($"{GetPath()}: {nameof(ItemScene)} {ItemScene.ResourcePath} isn't a {nameof(SelectableCard)} scene");
        node.CardScale = CardScale;
        ItemsContainer.AddChild(node);
        node.Show(card);
        Callable.From(FitRows).CallDeferred();
        node.Pressed += pressed => CardPressed?.Invoke(pressed);
        shown.Add(node);
        return node;
    }
}
