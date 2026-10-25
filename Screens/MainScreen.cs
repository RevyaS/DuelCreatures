using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class MainScreen : Control
{
    VBoxNodeContainer? Deck, Opponent;
    ButtonGroup? DeckChoiceGroup, OpponentChoiceGroup;
    Button? PlayButton;
    Button? NewProfile, LoadProfile, EditDecks;
    Label? ProfileLabel;
    DataManager? DataManager;

    OpponentType? OpponentSelected = null;
    PlayerDeck? DeckSelected = null;

    public override void _Ready()
    {
        Deck = GetNode<VBoxNodeContainer>($"%{nameof(Deck)}");
        Opponent = GetNode<VBoxNodeContainer>($"%{nameof(Opponent)}");

        PlayButton = GetNode<Button>($"%{nameof(PlayButton)}");
        PlayButton.Pressed += PlayPressed;

        DeckChoiceGroup = new();
        SetButtonGroup(DeckChoiceGroup, Deck.GetChildren<Button>());
        DeckChoiceGroup.Pressed += DeckChoicePressed;

        OpponentChoiceGroup = new();
        SetButtonGroup(OpponentChoiceGroup, Opponent.GetChildren<Button>());
        OpponentChoiceGroup.Pressed += OpponentChoicePressed;

        DataManager = DataManager.Get(this);
        DataManager.ProfileChanged += Render;

        ProfileLabel = GetNode<Label>($"%{nameof(ProfileLabel)}");
        NewProfile = GetNode<Button>($"%{nameof(NewProfile)}");
        NewProfile.Pressed += NewProfilePressed;
        LoadProfile = GetNode<Button>($"%{nameof(LoadProfile)}");
        LoadProfile.Pressed += LoadProfilePressed;
        EditDecks = GetNode<Button>($"%{nameof(EditDecks)}");

        Render();
    }

    public override void _ExitTree()
    {
        if(DataManager is not null) DataManager.ProfileChanged -= Render;
    }

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
        dialog.Theme = GD.Load<Theme>("res://Resources/Themes/GUITheme.tres");
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

    private void PlayPressed()
    {
        var guiManager = GetTree().Root.GetNode<GUIManager>(nameof(GUIManager));
        guiManager.PlayGame(DeckSelected!.Value, OpponentSelected!.Value);
    }

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

    private void DeckChoicePressed(BaseButton button)
    {
        GD.Print("Deck Choice Pressed");
        switch(button.Name)
        {
            case "RoyalPaladinTrialDeck":
                DeckSelected = PlayerDeck.ROYAL_PALADIN_TRIAL_DECK;
                break;
            case "KageroTrialDeck":
                DeckSelected = PlayerDeck.KAGERO_TRIAL_DECK;
                break;
            case "OTTTrialDeck":
                DeckSelected = PlayerDeck.OTT_TRIAL_DECK;
                break;
            case "NovaGrapplerTrialDeck":
                DeckSelected = PlayerDeck.NOVA_GRAPPLER_TRIAL_DECK;
                break;
            default:
                throw new NotSupportedException($"Opponent Not Supported {button.Name}");
        }
        Render();
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
