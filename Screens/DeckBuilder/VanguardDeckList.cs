using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>A deck's overview: starter, clan, trigger counts and its cards per grade.</summary>
public partial class VanguardDeckList : VBoxContainer
{
    const float StarterScale = 0.7f;
    const float CardScale = 0.56f;
    const int CardsPerRow = 5;
    const int FontSize = 32;

    static readonly VanguardTrigger[] Triggers = [VanguardTrigger.CRITICAL, VanguardTrigger.DRAW, VanguardTrigger.HEAL, VanguardTrigger.STAND];

    Label DeckName = null!, ClanName = null!, TotalTriggers = null!, DeckTotal = null!, Problems = null!;
    Control StarterSlot = null!;
    Table TriggerTable = null!, CardsPerGrade = null!;
    Button BackButton = null!;

    public event Action? Closed;
    public event Action<CardView>? CardPressed;

    public override void _Ready()
    {
        DeckName = GetNode<Label>($"%{nameof(DeckName)}");
        ClanName = GetNode<Label>($"%{nameof(ClanName)}");
        TotalTriggers = GetNode<Label>($"%{nameof(TotalTriggers)}");
        DeckTotal = GetNode<Label>($"%{nameof(DeckTotal)}");
        Problems = GetNode<Label>($"%{nameof(Problems)}");
        StarterSlot = GetNode<Control>($"%{nameof(StarterSlot)}");
        TriggerTable = GetNode<Table>($"%{nameof(TriggerTable)}");
        CardsPerGrade = GetNode<Table>($"%{nameof(CardsPerGrade)}");
        BackButton = GetNode<Button>($"%{nameof(BackButton)}");
        BackButton.Pressed += () => Closed?.Invoke();
    }

    public void ShowDeck(DeckInfo deck, CardCatalog catalog, IReadOnlyList<string> problems)
    {
        var cards = deck.GetAllCards()
            .Where(card => catalog.Contains(card.Key))
            .Select(card => (Id: card.Key, Data: catalog.GetData(card.Key), Count: card.Value))
            .OrderBy(card => card.Data.Grade).ThenBy(card => card.Data.Name)
            .ToList();

        DeckName.Text = deck.Name;
        ClanName.Text = deck.Clan.DisplayName;

        StarterSlot.FreeChildren();
        if(deck.FirstVanguardId is not null && catalog.Contains(deck.FirstVanguardId))
        {
            StarterSlot.AddChild(CreateCardView(deck.FirstVanguardId, catalog, StarterScale, null));
        }

        TriggerTable.ClearRows();
        TriggerTable.AddRow(Triggers
            .Select(trigger => (Control)CreateLabel(cards.Where(card => card.Data.Trigger == trigger).Sum(card => card.Count).ToString()))
            .ToArray());
        int triggers = cards.Where(card => card.Data.Trigger != VanguardTrigger.NONE).Sum(card => card.Count);
        TotalTriggers.Text = $"Triggers: {triggers}/{VanguardDeckRules.TriggerCount}";
        DeckTotal.Text = $"Deck: {cards.Sum(card => card.Count)}/{VanguardDeckRules.DeckSize}";

        CardsPerGrade.ClearRows();
        foreach(var grade in cards.GroupBy(card => card.Data.Grade))
        {
            var info = new VBoxContainer();
            info.AddChild(CreateLabel($"Grade {grade.Key}"));
            info.AddChild(CreateLabel(grade.Sum(card => card.Count).ToString()));

            var grid = new GridContainer { Columns = CardsPerRow };
            foreach(var card in grade)
            {
                grid.AddChild(CreateCardView(card.Id, catalog, CardScale, $"x{card.Count}"));
            }
            CardsPerGrade.AddRow(info, grid);
        }

        Problems.Visible = problems.Count > 0;
        Problems.Text = string.Join("\n", problems);
    }

    private CardView CreateCardView(string cardId, CardCatalog catalog, float scale, string? badge)
    {
        var view = CardView.Create(cardId, catalog.Create(cardId), scale);
        view.BadgeText = badge;
        view.Pressed += pressed => CardPressed?.Invoke(pressed);
        return view;
    }

    private static Label CreateLabel(string text)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", FontSize);
        return label;
    }
}
