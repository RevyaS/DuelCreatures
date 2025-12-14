using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class DuelCreaturesBoard : Control
{
    Button LeftButton = null!;

    VanguardGameSession _gameSession = null!;
    VanguardPlayerProfile player1 => _gameSession.Game.Player1;
    VanguardPlayerProfile player2 => _gameSession.Game.Player2;

    string mulliganPhase = "Mulligan Phase";
    string ridePhase = "Ride Phase";
    string mainPhase = "Main Phase";
    string attackPhase = "Attack Phase";

    public override void _Ready()
    {
        LeftButton = GetNode<Button>($"%{nameof(LeftButton)}");
        LeftButton.Pressed += OnLeftButtonPressed;
        SetComponents();

        PlayerCircles.ForEach((circle) =>
        {
            circle.CardDropped += (card) => OnPlayerRearguardCardDropped(circle, card);
            circle.ScreenDragging += OnPlayerCircleScreenDragged;
            circle.ScreenDragRelease += OnPlayerCircleScreenDragRelease;
            circle.CardPressed += OnUnitCircleCardPressed;
        });

        OpponentCircles.ForEach((circle) =>
        {
            circle.CardPressed += OnUnitCircleCardPressed;
        });

        PlayerFrontRowCircles.ForEach((circle) =>
        {
            circle.Hovering += OnPlayerCircleHovering;
            circle.HoverReleased += OnPlayerCircleHoverReleased;
        });

        OppFrontRowCircles.ForEach((circle) =>
        {
            circle.Hovering += OnOppCircleHovering;
            circle.HoverReleased += OnOppCircleHoverReleased;
        });

        PlayerRearguards.ForEach((rearguard) =>
        {
            rearguard.RearguardCardDragging += OnPlayerRearGuardDragged;
            rearguard.RearguardCardDragCancelled += OnPlayerRearGuardCardDragCancelled;
        });

        PlayerHand.CardPressed += OnHandCardPressed;

        PlayerDamageZone.CardPressed += OnDamageZoneCardPressed;
        OppDamageZone.CardPressed += OnDamageZoneCardPressed;
    }

    private void OnDamageZoneCardPressed(Card card)
    {
        DamageZoneCardPressed?.Invoke(card);
    }

    private void OnUnitCircleCardPressed(Card card)
    {
        UnitCircleCardPressed?.Invoke(card);
    }

    private void OnOppCircleHoverReleased(UnitCircleComponent component)
    {
        OppCircleHoverReleased?.Invoke(component);
    }

    private void OnPlayerCircleHoverReleased(UnitCircleComponent component)
    {
        PlayerCircleHoverReleased?.Invoke(component);
    }

    private void OnPlayerCircleScreenDragRelease(UnitCircleComponent component)
    {
        PlayerCircleScreenDragReleased?.Invoke(component);
    }

    private void OnOppCircleHovering(UnitCircleComponent component)
    {
        OppCircleHovering?.Invoke(component);
    }

    private void OnPlayerCircleHovering(UnitCircleComponent component)
    {
        PlayerCircleHovering?.Invoke(component);
    }

    private void OnPlayerCircleScreenDragged(UnitCircleComponent component)
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

    private void OnLeftButtonPressed()
    {
        LeftButtonPressed?.Invoke();
    }

    public void ShowLeftButton(string caption)
    {
        LeftButton.Text = caption;
        LeftButton.Show();
    }

    public void HideLeftButton()
    {
        LeftButton.Hide();
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
        OppFrontLeft.BindUnitCircle(game.Board.Player2Area.FrontLeft);
        OppBackLeft.BindUnitCircle(game.Board.Player2Area.BackLeft);
        OppBackCenter.BindUnitCircle(game.Board.Player2Area.BackCenter);
        OppFrontRight.BindUnitCircle(game.Board.Player2Area.FrontRight);
        OppBackRight.BindUnitCircle(game.Board.Player2Area.BackRight);

        PlayerDamageZone.BindDamageZone(game.Board.Player1Area.DamageZone);
        OppDamageZone.BindDamageZone(game.Board.Player2Area.DamageZone);

        PlayerDropZone.BindDropZone(game.Board.Player1Area.DropZone);
        OppDropZone.BindDropZone(game.Board.Player2Area.DropZone);
    }

    private void SetupEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
        eventBus.AttackEnded += OnAttackEnded;
        eventBus.OnDamageChecked += OnDamageChecked;
        eventBus.OnDriveChecked += OnDriveChecked;
        eventBus.CardAssignedToUnitCircle += OnCardAssignedToUnitCircle;

        PlayerHand.SetEventBus(eventBus);
        OppHand.SetEventBus(eventBus);
        PlayerVanguard.SetEventBus(eventBus);
        PlayerFrontLeft.SetEventBus(eventBus);
        PlayerBackLeft.SetEventBus(eventBus);
        PlayerBackCenter.SetEventBus(eventBus);
        PlayerFrontRight.SetEventBus(eventBus);
        PlayerBackRight.SetEventBus(eventBus);

        OppVanguard.SetEventBus(eventBus);
        OppFrontLeft.SetEventBus(eventBus);
        OppBackLeft.SetEventBus(eventBus);
        OppBackCenter.SetEventBus(eventBus);
        OppFrontRight.SetEventBus(eventBus);
        OppBackRight.SetEventBus(eventBus);

        PlayerDamageZone.SetEventBus(eventBus);
        OppDamageZone.SetEventBus(eventBus);

        PlayerDropZone.SetEventBus(eventBus);
        OppDropZone.SetEventBus(eventBus);
    }

    private Task OnCardAssignedToUnitCircle(UnitCircle unitCircle)
    {
        RecalculateStats();
        return Task.CompletedTask;
    }

    private void RecalculateStats()
    {
        PlayerCircles.ForEach(circle => circle.UpdateStats());
    }

    private async Task OnDriveChecked(VanguardPlayArea area, VanguardCard card)
    {
        await TriggerCheckCore(area, card);
    }

    private async Task OnDamageChecked(VanguardPlayArea area, VanguardCard card)
    {
        await TriggerCheckCore(area, card);
    }

    private async Task TriggerCheckCore(VanguardPlayArea area, VanguardCard card)
    {
        Card cardComponent = SceneFactory.CreateVanguardCard(card);
        bool isPlayer1 = ReferenceEquals(area, _gameSession.Game.Board.Player1Area);
        if(isPlayer1)
        {
            PlayerTriggerZone.AddCard(cardComponent);
        } else
        {
            OppTriggerZone.AddCard(cardComponent);
        }

        // Wait for a bit
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

        if(isPlayer1)
        {
            PlayerTriggerZone.ClearCard();
        } else
        {
            OppTriggerZone.ClearCard();
        }
    }

    private Task OnAttackEnded()
    {
        GuardZone.ClearCards();
        HideAttackLines();
        HideBoostLines();
        return Task.CompletedTask;
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
        if (_gameSession.CurrentPlayerIsPlayer1)
        {
            OppPhaseIndicator.Text = string.Empty;
            PlayerPhaseIndicator.Text = message;
        } else
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

        GuardZone.ClearCards();

        // Set Vanguards
        PlayerVanguard.SetCard((VanguardCard)player1.Vanguard);
        OppVanguard.SetCard((VanguardCard)player2.Vanguard);

        PlayerTriggerZone.ClearCard();
        OppTriggerZone.ClearCard();
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

    public UnitCircleComponent GetPlayerUnitCircleComponent(UnitCircle circle)
    {
        return PlayerCircles.First(x => ReferenceEquals(x.UnitCircle, circle));
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

    public void EnablePlayerHandDropping()
    {
        PlayerHand.Droppable = true;
    }
    public void DisablePlayerHandDropping()
    {
        PlayerHand.Droppable = false;
    }

    public void EnableGuardDragging()
    {
        GuardZone.Draggable = true;
    }
    public void DisableGuardDragging()
    {
        GuardZone.Draggable = false;
    }

    public void EnableGuardDropping()
    {
        GuardZone.Droppable = true;
    }
    public void DisableGuardDropping()
    {
        GuardZone.Droppable = false;
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
        OppFrontRowCircles.ForEach((circle) => circle.Hoverable = true);
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

    public void ShowAttackLine(UnitCircleComponent attacker, UnitCircleComponent target)
    {
        if(ReferenceEquals(PlayerFrontLeft, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerLeftAttackLeftLine.Show();
        if(ReferenceEquals(PlayerFrontLeft, attacker) && ReferenceEquals(OppVanguard, target)) PlayerLeftAttackCenterLine.Show();
        if(ReferenceEquals(PlayerFrontLeft, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerLeftAttackRightLine.Show();
        if(ReferenceEquals(PlayerVanguard, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerCenterAttackLeftLine.Show();
        if(ReferenceEquals(PlayerVanguard, attacker) && ReferenceEquals(OppVanguard, target)) PlayerCenterAttackCenterLine.Show();
        if(ReferenceEquals(PlayerVanguard, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerCenterAttackRightLine.Show();
        if(ReferenceEquals(PlayerFrontRight, attacker) && ReferenceEquals(OppFrontLeft, target)) PlayerRightAttackLeftLine.Show();
        if(ReferenceEquals(PlayerFrontRight, attacker) && ReferenceEquals(OppVanguard, target)) PlayerRightAttackCenterLine.Show();
        if(ReferenceEquals(PlayerFrontRight, attacker) && ReferenceEquals(OppFrontRight, target)) PlayerRightAttackRightLine.Show();
    }

    public void HideAttackLines()
    {
        PlayerLeftAttackLeftLine.Hide();
        PlayerLeftAttackCenterLine.Hide();
        PlayerLeftAttackRightLine.Hide();
        PlayerCenterAttackLeftLine.Hide();
        PlayerCenterAttackCenterLine.Hide();
        PlayerCenterAttackRightLine.Hide();
        PlayerRightAttackLeftLine.Hide();
        PlayerRightAttackCenterLine.Hide();
        PlayerRightAttackRightLine.Hide();
    }

    public event Action<Card>? HandCardPressed;
    public event Action<Card>? UnitCircleCardPressed;
    public event Action<Card>? DamageZoneCardPressed;
    public event Action<Card>? PlayerVanguardCardDropped;
    public event Action? LeftButtonPressed;
    public event Action<UnitCircleComponent, Card>? CardDroppedToPlayerRearguard;
    public event Action<UnitCircleComponent, CardBaseComponent>? PlayerRearGuardDragged;
    public event Action<UnitCircleComponent, CardBaseComponent>? PlayerRearGuardCardDragCancelled;
    public event Action<UnitCircleComponent>? PlayerCircleScreenDragged;
    public event Action<UnitCircleComponent>? PlayerCircleScreenDragReleased;
    public event Action<UnitCircleComponent>? PlayerCircleHovering;
    public event Action<UnitCircleComponent>? PlayerCircleHoverReleased;
    public event Action<UnitCircleComponent>? OppCircleHovering;
    public event Action<UnitCircleComponent>? OppCircleHoverReleased;
}
