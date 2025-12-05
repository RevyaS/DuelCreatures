using System;
using System.Collections.Generic;
using System.Linq;
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

        PlayerCircles.ForEach((circle) =>
        {
            circle.CardDropped += (card) => OnPlayerRearguardCardDropped(circle, card);
            circle.ScreenDragging += OnPlayerRearGuardScreenDragged;
        });

        PlayerFrontRowCircles.ForEach((circle) =>
        {
            circle.Hovering += OnPlayerCircleHovering;
        });

        OppFrontRowCircles.ForEach((circle) =>
        {
            circle.Hovering += OnOppCircleHovering;
        });

        PlayerRearguards.ForEach((rearguard) =>
        {
            rearguard.RearguardCardDragging += OnPlayerRearGuardDragged;
            rearguard.RearguardCardDragCancelled += OnPlayerRearGuardCardDragCancelled;
        });
    }

    private void OnOppCircleHovering(UnitCircleComponent component)
    {
        OppCircleHovering?.Invoke(component);
    }

    private void OnPlayerCircleHovering(UnitCircleComponent component)
    {
        PlayerCircleHovering?.Invoke(component);
    }

    private void OnPlayerRearGuardScreenDragged(UnitCircleComponent component)
    {
        PlayerCircleScreenDragged?.Invoke(component);
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

    public UnitCircleComponent GetPlayerOppositeCircle(UnitCircleComponent circle)
    {
        if(ReferenceEquals(PlayerFrontLeft, circle)) return PlayerBackLeft;
        if(ReferenceEquals(PlayerBackLeft, circle)) return PlayerFrontLeft;
        if(ReferenceEquals(PlayerFrontRight, circle)) return PlayerBackRight;
        if(ReferenceEquals(PlayerBackRight, circle)) return PlayerFrontRight;
        if(ReferenceEquals(PlayerVanguard, circle)) return PlayerBackCenter;
        if(ReferenceEquals(PlayerBackCenter, circle)) return PlayerVanguard;
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

    public bool IsBackRow(UnitCircleComponent unitCircle)
    {
        return PlayerBackRowCircles.Contains(unitCircle, ReferenceEqualityComparer.Instance);
    }

    public bool IsFrontRow(UnitCircleComponent unitCircle)
    {
        return PlayerFrontRowCircles.Contains(unitCircle, ReferenceEqualityComparer.Instance);
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

    public void EnablePlayerUnitCircleScreenDragging()
    {
        PlayerVanguard.ScreenDraggable = true;
        PlayerFrontLeft.ScreenDraggable = true;
        PlayerBackLeft.ScreenDraggable = true;
        PlayerBackCenter.ScreenDraggable = true;
        PlayerFrontRight.ScreenDraggable = true;
        PlayerBackRight.ScreenDraggable = true;
    }
    public void DisablePlayerUnitCircleScreenDragging()
    {
        PlayerVanguard.ScreenDraggable = false;
        PlayerFrontLeft.ScreenDraggable = false;
        PlayerBackLeft.ScreenDraggable = false;
        PlayerBackCenter.ScreenDraggable = false;
        PlayerFrontRight.ScreenDraggable = false;
        PlayerBackRight.ScreenDraggable = false;
    }

    public void EnablePlayerFrontRowUnitCircleHovering()
    {
        PlayerFrontRowCircles.ForEach((circle) => circle.Hoverable = true);
    }

    public void DisablePlayerFrontRowUnitCircleHovering()
    {
        PlayerFrontRowCircles.ForEach((circle) => circle.ScreenDraggable = false);
    }

    public void EnableOppFrontRowUnitCircleHovering()
    {
        OppFrontRowCircles.ForEach((circle) => circle.ScreenDraggable = true);
    }

    public void DisableOppFrontRowUnitCircleHovering()
    {
        OppFrontRowCircles.ForEach((circle) => circle.ScreenDraggable = false);
    }

    public void ShowBoostLine(UnitCircleComponent unitCircleComponent)
    {
        if(ReferenceEquals(PlayerFrontLeft, unitCircleComponent)) PlayerLeftBoostLine.Show();
        if(ReferenceEquals(PlayerVanguard, unitCircleComponent)) PlayerCenterBoostLine.Show();
        if(ReferenceEquals(PlayerFrontRight, unitCircleComponent)) PlayerRightBoostLine.Show();
    }

    public void HideBoostLines()
    {
        PlayerLeftBoostLine.Hide();
        PlayerCenterBoostLine.Hide();
        PlayerRightBoostLine.Hide();
    }

    public event Action<Card>? HandCardPressed;
    public event Action<Card>? PlayerVanguardCardDropped;
    public event Action? EndPhasePressed;
    public event Action<UnitCircleComponent, Card>? CardDroppedToPlayerRearguard;
    public event Action<UnitCircleComponent, CardBaseComponent>? PlayerRearGuardDragged;
    public event Action<UnitCircleComponent, CardBaseComponent>? PlayerRearGuardCardDragCancelled;
    public event Action<UnitCircleComponent>? PlayerCircleScreenDragged;
    public event Action<UnitCircleComponent>? PlayerCircleHovering;
    public event Action<UnitCircleComponent>? OppCircleHovering;
}
