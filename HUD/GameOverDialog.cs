using ArC.CardGames.Predefined.Vanguard;
using Godot;
using System;
using System.Linq;

public partial class GameOverDialog : PanelContainer
{
    Button PlayNextGame = null!, ChangeDeck = null!, MainMenu = null!;
    GUIManager GUIManager = null!;
    DataManager DataManager = null!;
    VanguardDeckBuilder CurrentPlayerDeck = null!;
    Label Result = null!, TicketGained = null!;

    public override void _Ready()
    {
        GUIManager = GUIManager.Get(this);
        DataManager = DataManager.Get(this);

        Result = GetNode<Label>($"%{nameof(Result)}");
        TicketGained = GetNode<Label>($"%{nameof(TicketGained)}");

        PlayNextGame = GetNode<Button>($"%{nameof(PlayNextGame)}");
        ChangeDeck = GetNode<Button>($"%{nameof(ChangeDeck)}");
        MainMenu = GetNode<Button>($"%{nameof(MainMenu)}");

        PlayNextGame.Pressed += OnPlayNextGamePressed;
        ChangeDeck.Pressed += OnChangeDeckPressed;
        MainMenu.Pressed += OnMainMenuPressed;
    }

    public void Initialize(VanguardDeckBuilder currentPlayerDeck)
    {
        CurrentPlayerDeck = currentPlayerDeck;
    }

    public void Set(bool playerWins, int damageDealt, GameOverContext gameOverContext)
    {
        Result.Text = playerWins ? "You Win!" : "You Lose :(";

        if(DataManager.CurrentProfile is not null)
        {
            var reward = playerWins ? Rewards.WIN_GAME_TICKETS : Rewards.LOSE_TICKETS_PER_DAMAGE * damageDealt;
            TicketGained.Text = $"Tickets: {reward}";
            TicketGained.Show();

            // Update Tickets
            DataManager.AddTicketToProfile(reward);
        } else
        {
            TicketGained.Hide();
        }
    }

    private void OnMainMenuPressed()
    {
        GUIManager.OpenMainScreen();
    }

    private void OnChangeDeckPressed()
    {
        var decks = DataManager.GetDeckChoices();
        var choices = decks
            .Select((deck, index) => new ChoiceDialog.Choice(deck.Label, () => GUIManager.PlayGame(deck.Build(), SessionHelper.SelectRandomOpponent())));
        ChoiceDialog.Show(this, "Select Deck", choices);
    }

    private void OnPlayNextGamePressed()
    {
        GUIManager.PlayGame(CurrentPlayerDeck, SessionHelper.SelectRandomOpponent());
    }
}
