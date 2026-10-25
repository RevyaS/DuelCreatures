using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Vanguard;
using ArC.Common.Utils;
using Godot;

public partial class MainScreen : Control
{
    VBoxNodeContainer? Deck, Opponent;
    ButtonGroup? DeckChoiceGroup, OpponentChoiceGroup;
    Button? PlayButton;
    Button? NewProfile, LoadProfile, EditDecks, Gacha;
    Label? ProfileLabel, Tickets;
    DataManager? DataManager;
    Theme? GUITheme;

    OpponentType? OpponentSelected = null;
    DeckChoice? DeckSelected = null;

    public override void _Ready()
    {
        Tickets = GetNode<Label>($"%{nameof(Tickets)}");
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
        Gacha = GetNode<Button>($"%{nameof(Gacha)}");
        Gacha.Pressed += GachaPressed;


        // Random is default
        OpponentSelected = SessionHelper.SelectRandomOpponent();

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
            .Select((deck, index) => new ChoiceDialog.Choice(DataManager!.DescribeDeck(deck), () => guiManager.OpenDeckBuilder(deck, index)))
            .Append(new ChoiceDialog.Choice("+ New Deck", ShowClanChoice));
        ChoiceDialog.Show(this, "Edit Deck", choices);
    }

    private void GachaPressed()
    {
        if(!DataManager!.HasCardPool)
        {
            ShowMessage("No card pool yet, run DataGenerators/DataGenerator.tscn and press Generate Card Pools");
            return;
        }
        GUIManager.Get(this).OpenGacha();
    }

    private void ShowClanChoice()
    {
        var guiManager = GUIManager.Get(this);
        var clans = CardRegistry.Game.Clans.Where(clan => DataManager!.Cards.GetCards(clan).Any());
        var choices = clans.Select(clan => new ChoiceDialog.Choice(clan.DisplayName, () =>
            guiManager.OpenDeckBuilder(new DeckInfo { Name = $"{clan.DisplayName} Deck", Clan = clan }, null)));
        ChoiceDialog.Show(this, "New Deck Clan", choices);
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
        foreach(var choice in DataManager!.GetDeckChoices())
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
            case "OTTTrialDeckBot":
                OpponentSelected = OpponentType.RULE_BOT_OTT_TRIAL_DECK;
                break;
            case "NovaGrapplerTrialDeckBot":
                OpponentSelected = OpponentType.RULE_BOT_NOVA_GRAPPLER_TRIAL_DECK;
                break;
            case "Random":
                OpponentSelected = SessionHelper.SelectRandomOpponent();
                break;
            default:
                throw new NotSupportedException($"Opponent Not Supported {button.Name}");
        }
        Render();
    }
    #endregion

    private void Render()
    {
        PlayButton!.Disabled = DeckSelected is null;

        var profile = DataManager!.CurrentProfile;
        ProfileLabel!.Text = profile is null
            ? "No Profile"
            : $"{profile.Name} - {profile.Decks.Count} decks, {profile.OwnedCards.Values.Sum()} cards";
        LoadProfile!.Disabled = !DataManager.GetProfileSlots().Any(slot => slot.IsOccupied);
        EditDecks!.Disabled = profile is null;
        Gacha!.Disabled = profile is null;

        Tickets!.Visible = profile is not null;
        if(profile is not null)
        {
            Tickets.Text = $"Tickets: {profile.Resources.Tickets}";
        }
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
