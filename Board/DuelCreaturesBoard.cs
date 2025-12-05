using System;
using System.Collections.Generic;
using ArC.CardGames;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class DuelCreaturesBoard : Control
{
    Button EndPhaseButton = null!;

    VanguardGameSession _gameSession = null!;
    VanguardPlayerProfile player1 => _gameSession.Game.Player1;
    VanguardPlayerProfile player2 => _gameSession.Game.Player2;

    string mulliganPhase = "Mulligan Phase";
    string ridePhase = "Ride Phase";
    string mainPhase = "Main Phase";
    string attackPhase = "Attack Phase";

    public override void _Ready()
    {
        EndPhaseButton = GetNode<Button>($"%{nameof(EndPhaseButton)}");
        EndPhaseButton.Pressed += OnEndPhaseButtonPressed;
        SetComponents();
        PlayerHand.CardPressed += OnHandCardPressed;
        PlayerVanguard.CardDropped += OnPlayerVanguardCardDropped;
        PlayerFrontLeft.CardDropped += (card) => OnPlayerRearguardCardDropped(PlayerFrontLeft, card);
        PlayerBackLeft.CardDropped += (card) => OnPlayerRearguardCardDropped(PlayerBackLeft, card);
        PlayerBackCenter.CardDropped += (card) => OnPlayerRearguardCardDropped(PlayerBackCenter, card);
        PlayerFrontRight.CardDropped += (card) => OnPlayerRearguardCardDropped(PlayerFrontRight, card);
        PlayerBackRight.CardDropped += (card) => OnPlayerRearguardCardDropped(PlayerBackRight, card);

        PlayerRearguards.ForEach((rearguard) =>
        {
            rearguard.RearguardCardDragging += OnPlayerRearGuardDragged;
            rearguard.RearguardCardDragCancelled += OnPlayerRearGuardCardDragCancelled;
        });
    }

    private void OnPlayerRearGuardCardDragCancelled(UnitCircleComponent component1, CardBaseComponent component2)
    {
        PlayerRearGuardCardDragCancelled?.Invoke(component1, component2);
    }

    private void OnPlayerRearGuardDragged(UnitCircleComponent component1, CardBaseComponent component2)
    {
        PlayerRearGuardDragged?.Invoke(component1, component2);
    }

    private void OnPlayerRearguardCardDropped(UnitCircleComponent unitCircle, Card card)
    {
        CardDroppedToPlayerRearguard?.Invoke(unitCircle, card);
    }

    private void OnPlayerVanguardCardDropped(Card card)
    {
        PlayerVanguardCardDropped?.Invoke(card);
    }

    private void OnEndPhaseButtonPressed()
    {
        EndPhasePressed?.Invoke();
    }

    public void ShowEndPhaseButton()
    {
        EndPhaseButton.Show();
    }

    public void HideEndPhaseButton()
    {
        EndPhaseButton.Hide();
    }

    private void OnHandCardPressed(Card card)
    {
        HandCardPressed?.Invoke(card);
    }

    public void ApplySession(VanguardGameSession gameSession)
    {
        _gameSession = gameSession;

        Reset();

        OnPhaseChanged(gameSession.CurrentPhase);
        SetupEventBus(gameSession.EventBus);

        var game = gameSession.Game;
        PlayerHand.BindHand(game.Board.Player1Area.Hand);
        OppHand.BindHand(game.Board.Player2Area.Hand);
        PlayerVanguard.BindUnitCircle(game.Board.Player1Area.Vanguard);
        PlayerFrontLeft.BindUnitCircle(game.Board.Player1Area.FrontLeft);
        PlayerBackLeft.BindUnitCircle(game.Board.Player1Area.BackLeft);
        PlayerBackCenter.BindUnitCircle(game.Board.Player1Area.BackCenter);
        PlayerFrontRight.BindUnitCircle(game.Board.Player1Area.FrontRight);
        PlayerBackRight.BindUnitCircle(game.Board.Player1Area.BackRight);


        OppVanguard.BindUnitCircle(game.Board.Player2Area.Vanguard);
    }

    private void SetupEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;

        PlayerHand.SetEventBus(eventBus);
        OppHand.SetEventBus(eventBus);
        PlayerVanguard.SetEventBus(eventBus);
        PlayerFrontLeft.SetEventBus(eventBus);
        PlayerBackLeft.SetEventBus(eventBus);
        PlayerBackCenter.SetEventBus(eventBus);
        PlayerFrontRight.SetEventBus(eventBus);
        PlayerBackRight.SetEventBus(eventBus);

        OppVanguard.SetEventBus(eventBus);
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
                SetPhaseIndicatorToCurrentPlayer(ridePhase);
                break;
            case MainPhase:
                SetPhaseIndicatorToCurrentPlayer(mainPhase);
                break;
            case AttackPhase:
                SetPhaseIndicatorToCurrentPlayer(attackPhase);
                break;
        };
    }

    private void SetPhaseIndicatorToCurrentPlayer(string message)
    {
        if (_gameSession.currentPlayer == player1)
        {
            OppPhaseIndicator.Text = string.Empty;
            PlayerPhaseIndicator.Text = message;
        }
        if (_gameSession.currentPlayer == player2)
        {
            PlayerPhaseIndicator.Text = string.Empty;
            OppPhaseIndicator.Text = message;
        }
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

    public void EnablePlayerVanguardDropping()
    {
        PlayerVanguard.Droppable = true;
    }
    public void DisablePlayerVanguardDropping()
    {
        PlayerVanguard.Droppable = false;
    }

    public UnitCircleComponent GetPlayerOppositeRearguard(UnitCircleComponent rearguard)
    {
        if(ReferenceEquals(PlayerFrontLeft, rearguard)) return PlayerBackLeft;
        if(ReferenceEquals(PlayerBackLeft, rearguard)) return PlayerFrontLeft;
        if(ReferenceEquals(PlayerFrontRight, rearguard)) return PlayerBackRight;
        if(ReferenceEquals(PlayerBackRight, rearguard)) return PlayerFrontRight;
        throw new InvalidOperationException();
    }

    public void EnablePlayerRearguardDropping()
    {
        EnablePlayerRearguardDropping(PlayerRearguards);
    }

    public void EnablePlayerRearguardDropping(List<UnitCircleComponent> rearguards)
    {
        rearguards.ForEach(rg => rg.Droppable = true);
    }

    public void DisablePlayerRearguardDropping()
    {
        PlayerFrontLeft.Droppable = false;
        PlayerBackLeft.Droppable = false;
        PlayerBackCenter.Droppable = false;
        PlayerFrontRight.Droppable = false;
        PlayerBackRight.Droppable = false;
    }

    public void EnablePlayerRearguardDragging()
    {
        PlayerFrontLeft.Draggable = true;
        PlayerBackLeft.Draggable = true;
        PlayerBackCenter.Draggable = true;
        PlayerFrontRight.Draggable = true;
        PlayerBackRight.Draggable = true;
    }
    public void DisablePlayerRearguardDragging()
    {
        DisablePlayerRearguardDragging(PlayerRearguards);
    }

    public void DisablePlayerRearguardDragging(List<UnitCircleComponent> unitCircleComponents)
    {
        unitCircleComponents.ForEach(rg => rg.Draggable = false);
    }

    public void EnablePlayerHandDragging()
    {
        PlayerHand.Draggable = true;
    }
    public void DisablePlayerHandDragging()
    {
        PlayerHand.Draggable = false;
    }

    public event Action<Card>? HandCardPressed;
    public event Action<Card>? PlayerVanguardCardDropped;
    public event Action? EndPhasePressed;
    public event Action<UnitCircleComponent, Card>? CardDroppedToPlayerRearguard;
    public event Action<UnitCircleComponent, CardBaseComponent>? PlayerRearGuardDragged;
    public event Action<UnitCircleComponent, CardBaseComponent>? PlayerRearGuardCardDragCancelled;
}
