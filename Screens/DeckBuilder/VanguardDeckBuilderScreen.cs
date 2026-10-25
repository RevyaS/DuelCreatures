using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

/// <summary>
/// Edits one deck of the current profile. Pool cards: swipe up adds a copy, swipe down removes one, long press sets a grade 0 as the starter.
/// Deck cards: swipe down removes a copy, long press sets it as the starter. Pressing any card shows its info.
/// </summary>
public partial class VanguardDeckBuilderScreen : Control
{
    const float PoolCardScale = 0.7f;
    const float DeckCardScale = 0.7f;
    static readonly Color StarterOutline = new(1f, 0.8f, 0.2f);

    DataManager DataManager = null!;
    CardCatalog Catalog = null!;
    IReadOnlyDictionary<string, int> OwnedCards = null!;
    DeckInfo Deck = null!;
    int? DeckIndex;
    bool Dirty;

    bool ownedOnly = true;
    int? gradeFilter;
    string search = string.Empty;

    Label DeckName = null!, G1Count = null!, G2Count = null!, G3Count = null!, TriggerCount = null!, TotalCount = null!, StatusLabel = null!;
    Button RenameButton = null!, OwnedButton = null!, AllButton = null!, ConfirmDeckButton = null!;
    Button SearchButton = null!, ResetButton = null!, AllGrades = null!, BackButton = null!, SaveButton = null!;
    LineEdit SearchBar = null!;
    Control StarterSlot = null!, DeckCards = null!, CardPoolList = null!;
    Control CardInfoOverlay = null!, DeckListOverlay = null!;
    VanguardCardInfo CardInfo = null!;
    VanguardDeckList DeckList = null!;

    /// <summary>Call before adding the screen to the tree. A null index makes a new deck in the profile on save.</summary>
    public void Initialize(DeckInfo deck, int? deckIndex)
    {
        Deck = deck.Clone();
        DeckIndex = deckIndex;
    }

    public override void _Ready()
    {
        DataManager = DataManager.Get(this);
        Catalog = DataManager.Cards;
        OwnedCards = DataManager.CurrentProfile?.OwnedCards ?? throw new InvalidOperationException("The deck builder needs a loaded profile");

        DeckName = GetNode<Label>($"%{nameof(DeckName)}");
        G1Count = GetNode<Label>($"%{nameof(G1Count)}");
        G2Count = GetNode<Label>($"%{nameof(G2Count)}");
        G3Count = GetNode<Label>($"%{nameof(G3Count)}");
        TriggerCount = GetNode<Label>($"%{nameof(TriggerCount)}");
        TotalCount = GetNode<Label>($"%{nameof(TotalCount)}");
        StatusLabel = GetNode<Label>($"%{nameof(StatusLabel)}");
        StarterSlot = GetNode<Control>($"%{nameof(StarterSlot)}");
        DeckCards = GetNode<Control>($"%{nameof(DeckCards)}");
        CardPoolList = GetNode<Control>($"%{nameof(CardPoolList)}");
        SearchBar = GetNode<LineEdit>($"%{nameof(SearchBar)}");
        CardInfoOverlay = GetNode<Control>($"%{nameof(CardInfoOverlay)}");
        DeckListOverlay = GetNode<Control>($"%{nameof(DeckListOverlay)}");
        CardInfo = GetNode<VanguardCardInfo>($"%{nameof(CardInfo)}");
        DeckList = GetNode<VanguardDeckList>($"%{nameof(DeckList)}");

        RenameButton = GetNode<Button>($"%{nameof(RenameButton)}");
        RenameButton.Pressed += ShowRenameDialog;
        ConfirmDeckButton = GetNode<Button>($"%{nameof(ConfirmDeckButton)}");
        ConfirmDeckButton.Pressed += ShowDeckList;
        SearchButton = GetNode<Button>($"%{nameof(SearchButton)}");
        SearchButton.Pressed += ToggleSearch;
        ResetButton = GetNode<Button>($"%{nameof(ResetButton)}");
        ResetButton.Pressed += ResetFilters;
        BackButton = GetNode<Button>($"%{nameof(BackButton)}");
        BackButton.Pressed += BackPressed;
        SaveButton = GetNode<Button>($"%{nameof(SaveButton)}");
        SaveButton.Pressed += SaveDeck;
        SearchBar.TextChanged += text =>
        {
            search = text;
            RenderPool();
        };

        OwnedButton = GetNode<Button>($"%{nameof(OwnedButton)}");
        AllButton = GetNode<Button>($"%{nameof(AllButton)}");
        var ownershipGroup = new ButtonGroup();
        ownershipGroup.Pressed += button =>
        {
            ownedOnly = button == OwnedButton;
            RenderPool();
        };
        SetButtonGroup(ownershipGroup, OwnedButton, AllButton);
        OwnedButton.ButtonPressed = true;

        AllGrades = GetNode<Button>($"%{nameof(AllGrades)}");
        var gradeGroup = new ButtonGroup();
        var gradeButtons = Enumerable.Range(0, 5).Select(grade => GetNode<Button>($"%Grade{grade}")).ToArray();
        SetButtonGroup(gradeGroup, [AllGrades, ..gradeButtons]);
        gradeGroup.Pressed += button =>
        {
            gradeFilter = button == AllGrades ? null : Array.IndexOf(gradeButtons, button);
            RenderPool();
        };
        AllGrades.ButtonPressed = true;

        CardInfo.Closed += () => CardInfoOverlay.Hide();
        DeckList.Closed += () => DeckListOverlay.Hide();
        DeckList.CardPressed += ShowCardInfo;

        Render();
    }

    #region Deck changes
    private void AddCard(CardView view)
    {
        string? problem = CanAdd(view.CardId);
        if(problem is not null)
        {
            SetStatus(problem);
            return;
        }
        Deck.Cards[view.CardId] = Deck.Cards.GetValueOrDefault(view.CardId) + 1;
        ChangeDeck($"Added {view.ShownCard.Name}");
    }

    private void RemoveCard(CardView view)
    {
        if(!Deck.Cards.TryGetValue(view.CardId, out int count)) return;

        if(count > 1) Deck.Cards[view.CardId] = count - 1;
        else Deck.Cards.Remove(view.CardId);
        ChangeDeck($"Removed {view.ShownCard.Name}");
    }

    private void RemoveStarter(CardView view)
    {
        Deck.FirstVanguardId = null;
        ChangeDeck($"Removed {view.ShownCard.Name} as the starter");
    }

    private void SetStarter(CardView view)
    {
        string cardId = view.CardId;
        if(Deck.FirstVanguardId == cardId) return;
        if(view.ShownCard.Grade != VanguardDeckRules.StarterGrade)
        {
            SetStatus($"Only a grade {VanguardDeckRules.StarterGrade} can be the starter");
            return;
        }

        // Takes a copy from the deck when no other copy is free
        if(CanAdd(cardId, countsTowardDeckSize: false) is string problem)
        {
            if(!Deck.Cards.ContainsKey(cardId))
            {
                SetStatus(problem);
                return;
            }
            Deck.Cards[cardId]--;
            if(Deck.Cards[cardId] == 0) Deck.Cards.Remove(cardId);
        }
        Deck.FirstVanguardId = cardId;
        ChangeDeck($"{view.ShownCard.Name} is the starter");
    }

    /// <summary>Why another copy can't go in the deck, null when it can.</summary>
    private string? CanAdd(string cardId, bool countsTowardDeckSize = true)
    {
        var name = Catalog.GetData(cardId).Name;
        int inDeck = Deck.GetCount(cardId);
        int owned = OwnedCards.GetValueOrDefault(cardId);

        if(inDeck >= owned) return owned == 0 ? $"{name} is not owned" : $"All {owned} owned copies of {name} are in the deck";
        if(inDeck >= VanguardDeckRules.MaxCopies) return $"At most {VanguardDeckRules.MaxCopies} copies of {name}";
        // Leaves room for the starter when there is none yet
        int deckSpace = VanguardDeckRules.DeckSize - (Deck.FirstVanguardId is null ? 1 : 0);
        if(countsTowardDeckSize && Deck.Cards.Values.Sum() >= deckSpace) return $"The deck is full";
        return null;
    }

    private void ChangeDeck(string status)
    {
        Dirty = true;
        SetStatus(status);
        Render();
    }

    private void SaveDeck()
    {
        DeckIndex = DataManager.SaveDeck(Deck.Clone(), DeckIndex);
        Dirty = false;

        var problems = DataManager.ValidateDeck(Deck);
        SetStatus(problems.Count == 0
            ? "Saved"
            : $"Saved, not playable yet: {problems[0]}{(problems.Count > 1 ? $" (+{problems.Count - 1} more)" : string.Empty)}");
    }
    #endregion

    #region Rendering
    private void Render()
    {
        RenderHeader();
        RenderDeck();
        RenderPool();
    }

    private void RenderHeader()
    {
        var cards = Deck.GetAllCards().Select(card => (Data: Catalog.GetData(card.Key), Count: card.Value)).ToList();
        int CountGrade(int grade) => cards.Where(card => card.Data.Grade == grade).Sum(card => card.Count);

        DeckName.Text = Deck.Name;
        G1Count.Text = CountGrade(1).ToString();
        G2Count.Text = CountGrade(2).ToString();
        G3Count.Text = CountGrade(3).ToString();
        TriggerCount.Text = cards.Where(card => card.Data.Trigger != VanguardTrigger.NONE).Sum(card => card.Count).ToString();
        TotalCount.Text = $"{cards.Sum(card => card.Count)}/{VanguardDeckRules.DeckSize}";
    }

    private void RenderDeck()
    {
        StarterSlot.FreeChildren();
        if(Deck.FirstVanguardId is not null)
        {
            var starter = CreateCardView(Deck.FirstVanguardId, DeckCardScale);
            starter.Outline = StarterOutline;
            starter.SwipedDown += RemoveStarter;
            StarterSlot.AddChild(starter);
        }
        else
        {
            StarterSlot.AddChild(new Label
            {
                Text = "Long press\na grade 0",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        DeckCards.FreeChildren();
        foreach(var (cardId, count) in SortById(Deck.Cards))
        {
            var view = CreateCardView(cardId, DeckCardScale);
            view.BadgeText = $"x{count}";
            view.SwipedDown += RemoveCard;
            view.LongPressed += SetStarter;
            DeckCards.AddChild(view);
        }
    }

    private void RenderPool()
    {
        CardPoolList.FreeChildren();
        var cards = Catalog.GetCards(Deck.Clan)
            .Where(card => !ownedOnly || OwnedCards.GetValueOrDefault(card.Id) > 0)
            .Where(card => gradeFilter is null || card.Grade == gradeFilter)
            .Where(card => search.Length == 0 || card.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(card => card.Grade).ThenBy(card => card.Name);

        foreach(var card in cards)
        {
            int owned = OwnedCards.GetValueOrDefault(card.Id);
            int inDeck = Deck.GetCount(card.Id);

            var view = CreateCardView(card.Id, PoolCardScale);
            view.BadgeText = $"{inDeck}/{owned}";
            view.Dimmed = CanAdd(card.Id) is not null;
            view.SwipedUp += AddCard;
            view.SwipedDown += RemoveCard;
            view.LongPressed += SetStarter;
            CardPoolList.AddChild(view);
        }
    }

    private CardView CreateCardView(string cardId, float scale)
    {
        var view = CardView.Create(cardId, Catalog.Create(cardId), scale);
        view.Pressed += ShowCardInfo;
        return view;
    }

    private IEnumerable<KeyValuePair<string, int>> SortById(Dictionary<string, int> cards) => cards
        .OrderBy(card => Catalog.GetData(card.Key).Grade)
        .ThenBy(card => Catalog.GetData(card.Key).Name);

    private void SetStatus(string status) => StatusLabel.Text = status;
    #endregion

    #region Overlays and dialogs
    private void ShowCardInfo(CardView view)
    {
        CardInfo.ShowCard(view.CardId, view.ShownCard, OwnedCards.GetValueOrDefault(view.CardId), Deck.GetCount(view.CardId));
        CardInfoOverlay.Show();
    }

    private void ShowDeckList()
    {
        DeckList.ShowDeck(Deck, Catalog, DataManager.ValidateDeck(Deck));
        DeckListOverlay.Show();
    }

    private void ToggleSearch()
    {
        SearchBar.Visible = !SearchBar.Visible;
        if(SearchBar.Visible) SearchBar.GrabFocus();
    }

    private void ResetFilters()
    {
        SearchBar.Text = string.Empty;
        SearchBar.Hide();
        search = string.Empty;
        AllGrades.ButtonPressed = true;
        gradeFilter = null;
        RenderPool();
    }

    private void ShowRenameDialog()
    {
        var nameEdit = new LineEdit { Text = Deck.Name, CustomMinimumSize = new Vector2(700, 0) };
        var dialog = CreateDialog(new ConfirmationDialog { Title = "Deck Name" });
        dialog.AddChild(nameEdit);
        dialog.RegisterTextEnter(nameEdit);
        dialog.Confirmed += () =>
        {
            var name = nameEdit.Text.Trim();
            if(name.Length == 0 || name == Deck.Name) return;
            Deck.Name = name;
            ChangeDeck($"Renamed to {name}");
        };
        dialog.PopupCentered();
        nameEdit.GrabFocus();
        nameEdit.SelectAll();
    }

    private void BackPressed()
    {
        var guiManager = GUIManager.Get(this);
        if(!Dirty)
        {
            guiManager.OpenMainScreen();
            return;
        }

        var dialog = CreateDialog(new ConfirmationDialog { Title = "Unsaved Changes", DialogText = "Leave without saving the deck?" });
        dialog.OkButtonText = "Leave";
        dialog.Confirmed += guiManager.OpenMainScreen;
        dialog.PopupCentered();
    }

    private T CreateDialog<T>(T dialog) where T : AcceptDialog
    {
        dialog.Theme = GD.Load<Theme>("res://Resources/Themes/GUITheme.tres");
        dialog.MinSize = new Vector2I(800, 300);
        dialog.VisibilityChanged += () =>
        {
            if(!dialog.Visible) dialog.QueueFree();
        };
        AddChild(dialog);
        return dialog;
    }
    #endregion

    private static void SetButtonGroup(ButtonGroup group, params Button[] buttons)
    {
        foreach(var button in buttons)
        {
            button.ToggleMode = true;
            button.ButtonGroup = group;
        }
    }
}
