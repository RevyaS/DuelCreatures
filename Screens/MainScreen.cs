using System;
using System.Collections.Generic;
using Godot;

public partial class MainScreen : Control
{
    VBoxNodeContainer? Deck, Opponent;
    ButtonGroup? DeckChoiceGroup, OpponentChoiceGroup;
    Button? PlayButton;

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

        Render();
    }

    private void PlayPressed()
    {
        var guiManager = GetTree().Root.GetNode<GUIManager>(nameof(GUIManager));
        guiManager.PlayGame(DeckSelected!.Value, OpponentSelected!.Value);
    }

    private void Render()
    {
        PlayButton!.Disabled = OpponentSelected is null || DeckSelected is null;
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
