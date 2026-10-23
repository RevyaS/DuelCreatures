using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;
using Godot;

public partial class DuelCreaturesBoard : Control
{
    Button LeftButton = null!;

    VanguardGameSession _gameSession = null!;
    PresentationContext presentation = null!;
    PresentationContext playerPresentation = null!;
    PresentationContext oppPresentation = null!;

    // Whose guards the guard zone is showing for the current battle
    BoardSide guardingSide = BoardSide.None;
    // Cards on ArC's guardian circle this battle. ArC sends them to the drop zone right
    // before AttackEnded; the board flies them there itself once the battle ends.
    readonly HashSet<CardBase> guardians = new(ReferenceEqualityComparer.Instance);
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

        // Hands live outside the play areas, so they take their side's sleeve from there
        PlayerHand.SleeveInfo = PlayerArea.SleeveInfo;
        OppHand.SleeveInfo = OppArea.SleeveInfo;

        PlayerArea.Circles.ForEach((circle) =>
        {
            circle.CardDropped += (card) => OnPlayerRearguardCardDropped(circle, card);
            circle.ScreenDragging += OnPlayerCircleScreenDragged;
            circle.ScreenDragRelease += OnPlayerCircleScreenDragRelease;
            circle.CardPressed += (card) => OnPlayerUnitCircleCardPressed(circle, card);
            circle.Selected += OnPlayerCircleSelected;
            circle.Deselected += OnPlayerCircleDeselected;
        });

        OppArea.Circles.ForEach((circle) =>
        {
            circle.CardPressed += OnUnitCircleCardPressed;
            circle.Selected += OnOppUnitCircleSelected;
            circle.Deselected += OnOppUnitCircleDeselected;
        });

        PlayerArea.FrontRowCircles.ForEach((circle) =>
        {
            circle.Hovering += OnPlayerCircleHovering;
            circle.HoverReleased += OnPlayerCircleHoverReleased;
        });

        // Every opponent circle: some attackers may target the back row (e.g. Tejas)
        OppArea.Circles.ForEach((circle) =>
        {
            circle.Hovering += OnOppCircleHovering;
            circle.HoverReleased += OnOppCircleHoverReleased;
        });

        PlayerArea.Rearguards.ForEach((rearguard) =>
        {
            rearguard.RearguardCardDragging += OnPlayerRearGuardDragged;
            rearguard.RearguardCardDragCancelled += OnPlayerRearGuardCardDragCancelled;
        });

        PlayerArea.Vanguard.LongPressed += OnPlayerVanguardLongPressed;
        OppArea.Vanguard.LongPressed += OnOppVanguardLongPressed;
        PlayerHand.CardPressed += OnHandCardPressed;

        PlayerArea.DamageZone.CardPressed += OnDamageZoneCardPressed;
        OppArea.DamageZone.CardPressed += OnDamageZoneCardPressed;

        // Public information both players can inspect
        PlayerArea.TriggerZone.CardPressed += OnPublicCardPressed;
        OppArea.TriggerZone.CardPressed += OnPublicCardPressed;
        GuardZone.CardPressed += OnPublicCardPressed;
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

    private void OnOppVanguardLongPressed(UnitCircleComponent _)
    {
        OppSoulPressed?.Invoke();
    }

    private void OnPublicCardPressed(Card card)
    {
        PublicCardPressed?.Invoke(card);
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

    public void ApplySession(VanguardGameSession gameSession, Presenter presenter)
    {
        _gameSession = gameSession;
        presentation = new(presenter, Sfx, BoardSide.Both);
        playerPresentation = presentation with { Side = BoardSide.Player };
        oppPresentation = presentation with { Side = BoardSide.Opponent };

        // Guards stay on the guard zone until the battle ends, even though the game
        // already put them in the drop zone
        presenter.AddHolder(card => guardians.Contains(card));

        Reset();

        OnPhaseChanged(gameSession.CurrentPhase);
        SetupEventBus(gameSession.EventBus);

        var game = gameSession.Game;

        PlayerArea.Bind(game.Board.Player1Area);
        OppArea.Bind(game.Board.Player2Area);

        PlayerHand.Bind(game.Board.Player1Area.Hand);
        OppHand.Bind(game.Board.Player2Area.Hand);
    }

    private void SetupEventBus(VanguardEventBus eventBus)
    {
        eventBus.PhaseChanged += OnPhaseChanged;
        eventBus.AttackEnded += OnAttackEnded;
        eventBus.OnGuardRequested += OnGuardRequested;
        eventBus.OnGuarded += OnGuarded;
        eventBus.CardAddedToHand += OnHandChanged;
        eventBus.CardTakenFromHand += OnHandChanged;
        eventBus.OnAttack += OnAttack;
        eventBus.CardAssignedToUnitCircle += OnCardAssignedToUnitCircle;


        PlayerHand.DrawSource = PlayerArea.DeckLocation;
        OppHand.DrawSource = OppArea.DeckLocation;

        PlayerArea.SetEventBus(eventBus, playerPresentation);
        OppArea.SetEventBus(eventBus, oppPresentation);

        PlayerHand.SetEventBus(eventBus, playerPresentation);
        OppHand.SetEventBus(eventBus, oppPresentation);
    }

    private Task OnAttack(UnitCircle attacker, UnitCircle target)
    {
        presentation.Present(() => ShowAttackLine(attacker, target));
        return Task.CompletedTask;
    }

    private Task OnCardAssignedToUnitCircle(UnitCircle unitCircle)
    {
        RecalculateStats();
        return Task.CompletedTask;
    }

    // Both sides: continuous effects (e.g. "+4000 while your hand has 4+ cards") change
    // without an effect event, so stats are refreshed whenever something they depend on does
    private void RecalculateStats()
    {
        PlayerArea.RecalculateStats();
        OppArea.RecalculateStats();
    }

    private void OnHandChanged(CardBase card, Hand hand)
    {
        RecalculateStats();
    }

    private Task OnGuardRequested(UnitCircle circle)
    {
        guardingSide = OppArea.GetUnitCircleComponentOrDefault(circle) is not null ? BoardSide.Opponent : BoardSide.Player;
        return Task.CompletedTask;
    }

    // ArC's guardian circle now holds the hand guards and the interceptors. (Not read from
    // OnInterceptorPlacedOnGuardianCircle: ArC awaits only that event's last subscriber,
    // which must stay its own skill binder so "when placed on (GC)" skills resolve.)
    private void OnGuarded(List<CardBase> guardCards, Hand hand)
    {
        // `hand` is the defender's; read the defender's guardian circle
        var defender = ReferenceEquals(hand, OppHand.Hand) ? OppArea.PlayArea : PlayerArea.PlayArea;
        var onGuardianCircle = defender.GuardZone.Cards.ToList();
        guardians.UnionWith(onGuardianCircle);

        // The player's own guards were already placed by the guard step's UI
        if(!ReferenceEquals(hand, OppHand.Hand)) return;

        // The opponent's guards and interceptors fly in, then stay readable
        foreach(var guardian in onGuardianCircle)
        {
            PresentOppGuardian(guardian);
        }

        oppPresentation.Present(async () =>
        {
            if(PresentationConfig.GuardReveal.Includes(BoardSide.Opponent) && PresentationConfig.GuardRevealHold > 0)
            {
                await ToSignal(GetTree().CreateTimer(PresentationConfig.GuardRevealHold), SceneTreeTimer.SignalName.Timeout);
            }
        });
    }

    private void PresentOppGuardian(CardBase guardian)
    {
        oppPresentation.PresentArrival(guardian, CardMove.ToGuardZone, GuardZone, null, () =>
        {
            if(!GuardZone.IsFull)
            {
                GuardZone.AddCard(SceneFactory.CreateVanguardCard((VanguardCard)guardian));
            }
        });
    }

    private Task OnAttackEnded()
    {
        // Guards and interceptors go to their owner's drop zone now that the battle is over
        var side = guardingSide;
        guardingSide = BoardSide.None;
        var context = side == BoardSide.Opponent ? oppPresentation : playerPresentation;
        var dropZone = side == BoardSide.Opponent ? OppArea.DropZone : PlayerArea.DropZone;
        var dropTop = dropZone.TopCardOfGame(includeHeld: true);
        // ArC already moved them to the drop zone (held back until now)
        guardians.Clear();

        presentation.Present(async () =>
        {
            // Read the guard zone when this plays: the opponent's guards are placed by earlier steps
            foreach(var guard in GuardZone.GetNonEmptyCards().Select(node => node.CurrentCard).ToList())
            {
                await context.ArriveAsync(guard, CardMove.GuardToDropZone, dropZone, GuardZone,
                    () => dropZone.ShowTopCard(dropTop));
            }

            GuardZone.ClearCards();
            HideAttackLines();
            HideBoostLines();
        });
        RecalculateStats();
        return Task.CompletedTask;
    }

    private void OnPhaseChanged(IPhase phase)
    {
        // Snapshot who's playing now; the step may play after the turn has moved on
        var currentPlayerIsPlayer1 = _gameSession.CurrentPlayerIsPlayer1;
        presentation.Present(() => ShowPhase(phase, currentPlayerIsPlayer1));
        // "During your turn" continuous effects change with the turn
        RecalculateStats();
    }

    private void ShowPhase(IPhase phase, bool currentPlayerIsPlayer1)
    {
        switch(phase)
        {
            case MulliganPhase:
                OppArea.SetIndicatorText(mulliganPhase);
                PlayerArea.SetIndicatorText(mulliganPhase);
                break;
            case RidePhase:
                SetPhaseIndicatorToCurrentPlayer(ridePhase, currentPlayerIsPlayer1);
                break;
            case MainPhase:
                SetPhaseIndicatorToCurrentPlayer(mainPhase, currentPlayerIsPlayer1);
                break;
            case AttackPhase:
                SetPhaseIndicatorToCurrentPlayer(attackPhase, currentPlayerIsPlayer1);
                break;
        };
    }

    private void SetPhaseIndicatorToCurrentPlayer(string message, bool currentPlayerIsPlayer1)
    {
        if(oppPhaseIndicatorStack.Count > 0)
        {
            oppPhaseIndicatorStack.Pop();
        }
        if(playerPhaseIndicatorStack.Count > 0)
        {
            playerPhaseIndicatorStack.Pop();
        }
        if (currentPlayerIsPlayer1)
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
        OppArea.SetIndicatorText(oppPhaseIndicatorStack.Count == 0 ? string.Empty : oppPhaseIndicatorStack.Peek());
        PlayerArea.SetIndicatorText(playerPhaseIndicatorStack.Count == 0 ? string.Empty : playerPhaseIndicatorStack.Peek());
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
        PlayerArea.Reset();
        OppArea.Reset();

        foreach(var hand in AllHands)
        {
            hand.ClearCards();
        }

        GuardZone.ClearCards();

        // Set Vanguards
        PlayerArea.Vanguard.SetCard((VanguardCard)player1.Vanguard);
        OppArea.Vanguard.SetCard((VanguardCard)player2.Vanguard);
    }


    public UnitCircleComponent GetPlayerOppositeCircle(UnitCircleComponent circle)
    {
        return PlayerArea.GetOppositeCircle(circle);
    }

    public bool IsCardInPlayerUnitCircle(VanguardCard card)
    {
        return PlayerArea.IsCardInUnitCircle(card);
    }

    public UnitCircleComponent GetPlayerUnitCircleComponent(VanguardCard card)
    {
        return PlayerArea.GetUnitCircleComponent(card);
    }

    public UnitCircleComponent GetPlayerUnitCircleComponent(UnitCircle circle)
    {
        return PlayerArea.GetUnitCircleComponent(circle);
    }
    public UnitCircleComponent? GetPlayerUnitCircleComponentOrDefault(UnitCircle circle)
    {
        return PlayerArea.GetUnitCircleComponentOrDefault(circle);
    }

    public UnitCircleComponent? GetOppUnitCircleComponentOrDefault(UnitCircle circle)
    {
        return OppArea.GetUnitCircleComponentOrDefault(circle);
    }
    public UnitCircleComponent GetUnitCircleComponent(UnitCircle circle)
    {
        var playerCircle = GetPlayerUnitCircleComponentOrDefault(circle);
        if(playerCircle is not null)
        {
            return playerCircle;
        }

        var oppCircle = GetOppUnitCircleComponentOrDefault(circle);
        if(oppCircle is not null)
        {
            return oppCircle;
        }

        throw new InvalidOperationException();
    }

    public bool IsBackRow(UnitCircleComponent unitCircle)
    {
        return PlayerArea.IsBackRow(unitCircle);
    }

    public bool IsFrontRow(UnitCircleComponent unitCircle)
    {
        return PlayerArea.IsFrontRow(unitCircle);
    }

    public event Action<Card>? HandCardPressed;
    public event Action<UnitCircleComponent, Card>? PlayerUnitCircleCardPressed;
    public event Action<Card>? UnitCircleCardPressed;
    public event Action<Card>? DamageZoneCardPressed;
    public event Action<Card>? PublicCardPressed;
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
    public event Action? OppSoulPressed;
}
