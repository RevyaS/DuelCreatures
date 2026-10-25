using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class MainScreen : Control
{
    /// <summary>A deck that can be picked to play, only built when the game starts.</summary>
    sealed record DeckChoice(string Label, Func<VanguardDeckBuilder> Build, bool Playable);

    VBoxNodeContainer? Deck, Opponent;
    ButtonGroup? DeckChoiceGroup, OpponentChoiceGroup;
    Button? PlayButton;
    Button? NewProfile, LoadProfile, EditDecks;
    Label? ProfileLabel;
    DataManager? DataManager;
    Theme? GUITheme;

    OpponentType? OpponentSelected = null;
    DeckChoice? DeckSelected = null;

    public override void _Ready()
    {
        GUITheme = GD.Load<Theme>("res://Resources/Themes/GUITheme.tres");
        Deck = GetNode<VBoxNodeContainer>($"%{nameof(Deck)}");
        Opponent = GetNode<VBoxNodeContainer>($"%{nameof(Opponent)}");

        PlayButton = GetNode<Button>($"%{nameof(PlayButton)}");
        PlayButton.Pressed += PlayPressed;

        OpponentChoiceGroup = new();
        SetButtonGroup(OpponentChoiceGroup, Opponent.GetChildren<Button>());
        OpponentChoiceGroup.Pressed += OpponentChoicePressed;

        DataManager = DataManager.Get(this);
        DataManager.ProfileChanged += OnProfileChanged;

        ProfileLabel = GetNode<Label>($"%{nameof(ProfileLabel)}");
        NewProfile = GetNode<Button>($"%{nameof(NewProfile)}");
        NewProfile.Pressed += NewProfilePressed;
        LoadProfile = GetNode<Button>($"%{nameof(LoadProfile)}");
        LoadProfile.Pressed += LoadProfilePressed;
        EditDecks = GetNode<Button>($"%{nameof(EditDecks)}");
        EditDecks.Pressed += EditDecksPressed;

        OnProfileChanged();
    }

    public override void _ExitTree()
    {
        if(DataManager is not null) DataManager.ProfileChanged -= OnProfileChanged;
    }

    private void OnProfileChanged()
    {
        RenderDecks();
        Render();
    }

    #region Profiles
    private void NewProfilePressed()
    {
        ShowSlotSelector(SlotSelectorMode.SAVE, slot => DataManager!.CreateProfile(slot, $"Profile {slot.Number}"));
    }

    private void LoadProfilePressed()
    {
        ShowSlotSelector(SlotSelectorMode.LOAD, slot => DataManager!.LoadProfile(slot));
    }

    private void ShowSlotSelector(SlotSelectorMode mode, Action<SaveSlot> onSelected)
    {
        var dialog = RevGodotCoreSceneFactory.CreateSlotSelectorDialog(DataManager!.GetProfileSlots(), mode, DescribeSlot);
        dialog.Theme = GUITheme;
        dialog.MinSize = new Vector2I(900, 800);
        dialog.SlotSelected += onSelected;
        dialog.VisibilityChanged += () =>
        {
            if(!dialog.Visible) dialog.QueueFree();
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }

    private string DescribeSlot(SaveSlot slot)
    {
        if(!slot.IsOccupied) return SlotSelectorDialog.DefaultDescription(slot);
        var profile = DataManager!.ReadProfile(slot);
        return $"Slot {slot.Number} - {profile.Name} ({slot.LastModified:g})";
    }
    #endregion

    #region Deck editing
    private void EditDecksPressed()
    {
        if(!DataManager!.HasCardPool)
        {
            ShowMessage("No card pool yet, run DataGenerators/DataGenerator.tscn and press Generate Card Pools");
            return;
        }

        var guiManager = GUIManager.Get(this);
        var decks = DataManager.CurrentProfile!.Decks;
        var choices = decks
            .Select((deck, index) => new ChoiceDialog.Choice(DescribeDeck(deck), () => guiManager.OpenDeckBuilder(deck, index)))
            .Append(new ChoiceDialog.Choice("+ New Deck", ShowClanChoice));
        ChoiceDialog.Show(this, "Edit Deck", choices);
    }

    private void ShowClanChoice()
    {
        var guiManager = GUIManager.Get(this);
        var clans = CardRegistry.Game.Clans.Where(clan => DataManager!.Cards.GetCards(clan).Any());
        var choices = clans.Select(clan => new ChoiceDialog.Choice(clan.DisplayName, () =>
            guiManager.OpenDeckBuilder(new DeckInfo { Name = $"{clan.DisplayName} Deck", Clan = clan }, null)));
        ChoiceDialog.Show(this, "New Deck Clan", choices);
    }

    private string DescribeDeck(DeckInfo deck)
    {
        bool playable = DataManager!.ValidateDeck(deck).Count == 0;
        return $"{deck.Name} ({deck.Clan.DisplayName}) {deck.CardCount}/{VanguardDeckRules.DeckSize}{(playable ? string.Empty : " - incomplete")}";
    }

    private void ShowMessage(string message)
    {
        var dialog = new AcceptDialog { Title = "Duel Creatures", DialogText = message, Theme = GUITheme, MinSize = new Vector2I(900, 300) };
        dialog.DialogAutowrap = true;
        dialog.VisibilityChanged += () =>
        {
            if(!dialog.Visible) dialog.QueueFree();
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }
    #endregion

    #region Play
    /// <summary>The profile's decks, or the trial decks when no profile is loaded.</summary>
    private IEnumerable<DeckChoice> GetDeckChoices()
    {
        var profile = DataManager!.CurrentProfile;
        if(profile is null)
        {
            return DataManager.InitialDecks.Select(deck => new DeckChoice(deck.Name, () => deck.Deck, true));
        }
        if(!DataManager.HasCardPool)
        {
            return profile.Decks.Select(deck => new DeckChoice($"{deck.Name} - no card pool generated", () => throw new InvalidOperationException("No card pool"), false));
        }
        return profile.Decks.Select(deck => new DeckChoice(DescribeDeck(deck), () => DataManager.Cards.BuildDeck(deck), DataManager.ValidateDeck(deck).Count == 0));
    }

    private void RenderDecks()
    {
        var deckList = Deck!;
        foreach(var button in deckList.GetChildren<Button>())
        {
            deckList.RemoveChild(button);
            button.QueueFree();
        }

        DeckSelected = null;
        DeckChoiceGroup = new();
        foreach(var choice in GetDeckChoices())
        {
            var button = new Button
            {
                Text = choice.Label,
                Theme = GUITheme,
                ToggleMode = true,
                ButtonGroup = DeckChoiceGroup,
                Disabled = !choice.Playable,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            button.Pressed += () =>
            {
                DeckSelected = choice;
                Render();
            };
            deckList.AddChild(button);
        }
    }

    private void PlayPressed()
    {
        GUIManager.Get(this).PlayGame(DeckSelected!.Build(), OpponentSelected!.Value);
    }

    private void OpponentChoicePressed(BaseButton button)
    {
        GD.Print("Opponent Choice Pressed");
        switch(button.Name)
        {
            case "RoyalPaladinTrialDeckBot":
                OpponentSelected = OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK;
                break;
            case "KageroTrialDeckBot":
                OpponentSelected = OpponentType.RULE_BOT_KAGERO_TRIAL_DECK;
                break;
            default:
                throw new NotSupportedException($"Opponent Not Supported {button.Name}");
        }
        Render();
    }
    #endregion

    private void Render()
    {
        PlayButton!.Disabled = OpponentSelected is null || DeckSelected is null;

        var profile = DataManager!.CurrentProfile;
        ProfileLabel!.Text = profile is null
            ? "No Profile"
            : $"{profile.Name} - {profile.Decks.Count} decks, {profile.OwnedCards.Values.Sum()} cards";
        LoadProfile!.Disabled = !DataManager.GetProfileSlots().Any(slot => slot.IsOccupied);
        EditDecks!.Disabled = profile is null;
    }

    private void SetButtonGroup(ButtonGroup buttonGroup, List<Button> buttons)
    {
        foreach(var button in buttons)
        {
            button.ToggleMode = true;
            button.ButtonGroup = buttonGroup;
        }
    }
}
