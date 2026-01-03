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
    Stack<string> playerPhaseIndicatorStack = new();
    Stack<string> oppPhaseIndicatorStack = new();

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
            circle.CardPressed += (card) => OnPlayerUnitCircleCardPressed(circle, card);
            circle.Selected += OnPlayerCircleSelected;
            circle.Deselected += OnPlayerCircleDeselected;
        });

        OpponentCircles.ForEach((circle) =>
        {
            circle.CardPressed += OnUnitCircleCardPressed;
            circle.Selected += OnOppUnitCircleSelected;
            circle.Deselected += OnOppUnitCircleDeselected;
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

        PlayerVanguard.LongPressed += OnPlayerVanguardLongPressed;
        PlayerHand.CardPressed += OnHandCardPressed;

        PlayerDamageZone.CardPressed += OnDamageZoneCardPressed;
        OppDamageZone.CardPressed += OnDamageZoneCardPressed;
    }

    private void OnPlayerCircleDeselected(UnitCircleComponent component)
    {
        PlayerCircleDeselected?.Invoke(component);
    }

    private void OnPlayerCircleSelected(UnitCircleComponent component)
    {
        PlayerCircleSelected?.Invoke(component);
    }

    private void OnPlayerUnitCircleCardPressed(UnitCircleComponent unitCircle, Card card)
    {
        PlayerUnitCircleCardPressed?.Invoke(unitCircle, card);
    }

    private void OnOppUnitCircleDeselected(UnitCircleComponent component)
    {
        OppCircleDeselected?.Invoke(component);
    }

    private void OnOppUnitCircleSelected(UnitCircleComponent component)
    {
        OppCircleSelected?.Invoke(component);
    }

    private void OnPlayerVanguardLongPressed(UnitCircleComponent _)
    {
        PlayerSoulPressed?.Invoke();
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
        PlayerHand.Bind(game.Board.Player1Area.Hand);
        OppHand.Bind(game.Board.Player2Area.Hand);
        PlayerVanguard.Bind(game.Board.Player1Area.Vanguard);
        PlayerFrontLeft.Bind(game.Board.Player1Area.FrontLeft);
        PlayerBackLeft.Bind(game.Board.Player1Area.BackLeft);
        PlayerBackCenter.Bind(game.Board.Player1Area.BackCenter);
        PlayerFrontRight.Bind(game.Board.Player1Area.FrontRight);
        PlayerBackRight.Bind(game.Board.Player1Area.BackRight);

        PlayerDeck.Bind(game.Board.Player1Area.Deck);
        OppDeck.Bind(game.Board.Player2Area.Deck);

        OppVanguard.Bind(game.Board.Player2Area.Vanguard);
        OppFrontLeft.Bind(game.Board.Player2Area.FrontLeft);
        OppBackLeft.Bind(game.Board.Player2Area.BackLeft);
        OppBackCenter.Bind(game.Board.Player2Area.BackCenter);
        OppFrontRight.Bind(game.Board.Player2Area.FrontRight);
        OppBackRight.Bind(game.Board.Player2Area.BackRight);

        PlayerDamageZone.Bind(game.Board.Player1Area.DamageZone);
        OppDamageZone.Bind(game.Board.Player2Area.DamageZone);

        PlayerDropZone.Bind(game.Board.Player1Area.DropZone);
        OppDropZone.Bind(game.Board.Player2Area.DropZone);

        PlayerTriggerZone.Bind(game.Board.Player1Area.TriggerZone);
        OppTriggerZone.Bind(game.Board.Player2Area.TriggerZone);
    }

    private void SetupEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
        eventBus.AttackEnded += OnAttackEnded;
        eventBus.CardAssignedToUnitCircle += OnCardAssignedToUnitCircle;

        PlayerHand.SetEventBus(eventBus);
        OppHand.SetEventBus(eventBus);
        PlayerVanguard.SetEventBus(eventBus);
        PlayerFrontLeft.SetEventBus(eventBus);
        PlayerBackLeft.SetEventBus(eventBus);
        PlayerBackCenter.SetEventBus(eventBus);
        PlayerFrontRight.SetEventBus(eventBus);
        PlayerBackRight.SetEventBus(eventBus);

        PlayerDeck.SetEventBus(eventBus);
        OppDeck.SetEventBus(eventBus);

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

        PlayerTriggerZone.SetEventBus(eventBus);
        OppTriggerZone.SetEventBus(eventBus);
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

    private Task OnAttackEnded()
    {
        GuardZone.ClearCards();
        HideAttackLines();
        HideBoostLines();
        RecalculateStats();
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
        if(oppPhaseIndicatorStack.Count > 0)
        {
            oppPhaseIndicatorStack.Pop();
        }
        if(playerPhaseIndicatorStack.Count > 0)
        {
            playerPhaseIndicatorStack.Pop();
        }
        if (_gameSession.CurrentPlayerIsPlayer1)
        {
            playerPhaseIndicatorStack.Push(message);
        }
        else
        {
            oppPhaseIndicatorStack.Push(message);
        }
        RenderPhaseIndicators();
    }

    private void RenderPhaseIndicators()
    {
        OppPhaseIndicator.Text = oppPhaseIndicatorStack.Count == 0 ? string.Empty : oppPhaseIndicatorStack.Peek();
        PlayerPhaseIndicator.Text = playerPhaseIndicatorStack.Count == 0 ? string.Empty : playerPhaseIndicatorStack.Peek();
    }

    public async Task<T> DoFuncWithIndicatorAsync<T>(string indicator, Func<Task<T>> action)
    {
        PushPlayerPhaseIndicatorText(indicator);
        var val = await action();
        PopPlayerPhaseIndicatorText();
        return val;
    }

    public void PushPlayerPhaseIndicatorText(string message)
    {
        playerPhaseIndicatorStack.Push(message);
        RenderPhaseIndicators();
    }

    public void PopPlayerPhaseIndicatorText()
    {
        playerPhaseIndicatorStack.Pop();
        RenderPhaseIndicators();
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

    public bool IsCardInPlayerUnitCircle(VanguardCard card)
    {
        return PlayerCircles.Any(x => ReferenceEquals(x.CurrentCard?.CurrentCard, card));
    }

    public UnitCircleComponent GetPlayerUnitCircleComponent(VanguardCard card)
    {
        return PlayerCircles.First(x => ReferenceEquals(x.CurrentCard?.CurrentCard, card));
    }

    public UnitCircleComponent GetPlayerUnitCircleComponent(UnitCircle circle)
    {
        return PlayerCircles.First(x => ReferenceEquals(x.UnitCircle, circle));
    }

    public bool IsBackRow(UnitCircleComponent unitCircle)
    {
        return PlayerBackRowRearguards.Contains(unitCircle, ReferenceEqualityComparer.Instance);
    }

    public bool IsFrontRow(UnitCircleComponent unitCircle)
    {
        return PlayerFrontRowCircles.Contains(unitCircle, ReferenceEqualityComparer.Instance);
    }

    public event Action<Card>? HandCardPressed;
    public event Action<UnitCircleComponent, Card>? PlayerUnitCircleCardPressed;
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
    public event Action<UnitCircleComponent>? OppCircleSelected;
    public event Action<UnitCircleComponent>? OppCircleDeselected;
    public event Action<UnitCircleComponent>? PlayerCircleSelected;
    public event Action<UnitCircleComponent>? PlayerCircleDeselected;
    public event Action? PlayerSoulPressed;
}
