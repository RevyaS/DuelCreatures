using System;
using ArC.CardGames;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    VanguardGameSession _gameSession;
    VanguardPlayerProfile player1 => _gameSession.Game.Player1;
    VanguardPlayerProfile player2 => _gameSession.Game.Player2;

    string mulliganPhase = "Mulligan Phase";
    string ridePhase = "Ride Phase";

    public override void _Ready()
    {
        SetComponents();
        PlayerHand.CardPressed += OnHandCardPressed;
    }

    private void OnHandCardPressed(Card card)
    {
        HandCardPressed(card);
    }

    public void ApplySession(VanguardGameSession gameSession)
    {
        _gameSession = gameSession;
        _gameSession.OnPhaseChanged += OnPhaseChanged;

        Reset();

        OnPhaseChanged(gameSession.CurrentPhase);
        SetupEventBus(gameSession.EventBus);

        var game = gameSession.Game;
        PlayerHand.BindHand(game.Board.Player1Area.Hand);
        OppHand.BindHand(game.Board.Player2Area.Hand);
    }

    private void SetupEventBus(VanguardEventBus eventBus)
    {
        PlayerHand.SetEventBus(eventBus);
        OppHand.SetEventBus(eventBus);
    }

    private void OnPhaseChanged(IPhase phase)
    {
        switch(phase)
        {
            case MulliganPhase:
                OppPhaseIndicator.Text = mulliganPhase;
                PlayerPhaseIndicator.Text = mulliganPhase;
                break;
            case RidePhase:
                if(_gameSession.currentPlayer == player1)
                {
                    OppPhaseIndicator.Text = string.Empty;
                    PlayerPhaseIndicator.Text = ridePhase;
                }
                if(_gameSession.currentPlayer == player2)
                {
                    PlayerPhaseIndicator.Text = string.Empty;
                    OppPhaseIndicator.Text = ridePhase;
                }
                break;
        };
    }

    public void Reset()
    {
        // Disable Extra fields
        foreach(var extrafield in AllExtraFields)
        {
            extrafield.Hide();
        }

        foreach(var field in AllFields)
        {
            field.ClearCard();
        }

        foreach(var damageZone in AllDamageZones)
        {
            damageZone.ClearCards();
        }

        foreach(var hand in AllHands)
        {
            hand.ClearCards();
        }

        PlayerDropZone.ClearCard();
        OppDropZone.ClearCard();

        // Set Vanguards
        PlayerVanguard.SetCard((VanguardCard)player1.Vanguard);
        OppVanguard.SetCard((VanguardCard)player2.Vanguard);
    }

    public event Action<Card> HandCardPressed;
}
