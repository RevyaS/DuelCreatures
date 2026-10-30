using System;
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using DuelCreatures.Data;
using Godot;

[Tool]
public partial class PlayAreaComponent : Control, IPlayAreaBindable, IEventBusUtilizer
{
    Label PhaseIndicator = null!;
    QuarterTurnContainer PhaseIndicatorTurn = null!, DeckTurn = null!, DropZoneTurn = null!;
    public UnitCircleComponent Vanguard { get; private set; } = null!;
    public UnitCircleComponent BackCenter { get; private set; } = null!;
    public UnitCircleComponent FrontLeft { get; private set; } = null!;
    public UnitCircleComponent BackLeft { get; private set; } = null!;
    public UnitCircleComponent FrontRight { get; private set; } = null!;
    public UnitCircleComponent BackRight { get; private set; } = null!;

    UnitCircleComponent ExtraLeft1 = null!, ExtraLeft2 = null!, ExtraRight1 = null!, ExtraRight2 = null!;

    public DamageZoneComponent DamageZone { get; private set; } = null!; 
    public TriggerZoneComponent TriggerZone { get; private set; } = null!;
    DeckComponent Deck = null!;
    public DropZoneComponent DropZone { get; private set; } = null!;
    AttackIndicator LeftBoostLine = null!, CenterBoostLine = null!, RightBoostLine = null!;

    List<UnitCircleComponent> AllExtraFields => [ 
        ExtraLeft1, ExtraLeft2, ExtraRight1, ExtraRight2,
    ];

    List<UnitCircleComponent> BackRowRearguards => [
        BackLeft, BackCenter, BackRight
    ];

    List<UnitCircleComponent> FrontRowRearguards => [
        FrontLeft, FrontRight
    ];

    public List<UnitCircleComponent> FrontRowCircles => [
        ..FrontRowRearguards, Vanguard
    ];
    public List<UnitCircleComponent> Rearguards => [
        ..FrontRowRearguards, ..BackRowRearguards
    ];

    public List<UnitCircleComponent> Circles => [
        ..Rearguards, Vanguard
    ];

    public VanguardPlayArea PlayArea { get; private set; }= null!;

    private bool _flippedAppearance = false;
    [Export]
    public bool FlippedAppearance
    {
        get => _flippedAppearance;
        set
        {
            _flippedAppearance = value;
            Render();
        }
    }

    private SleeveInfo _sleeveInfo = null!;
    [Export]
    public SleeveInfo SleeveInfo
    {
        get => _sleeveInfo;
        set
        {
            _sleeveInfo = value;
            Render();
        }
    }

    private void Render()
    {
        if(!IsInsideTree()) return;
        Circles.ForEach(x => x.FlippedAppearance = FlippedAppearance);
        AllExtraFields.ForEach(x => x.FlippedAppearance = FlippedAppearance);
        var turns = FlippedAppearance ? QuarterTurnContainer.QuarterTurn.Deg180 : QuarterTurnContainer.QuarterTurn.Deg0;
        PhaseIndicatorTurn.Turns = turns;
        DeckTurn.Turns = turns;
        DropZoneTurn.Turns = turns;
        Deck.SleeveInfo = SleeveInfo;
        DamageZone.SleeveInfo = SleeveInfo;
    }

    public override void _Ready()
    {
        PhaseIndicator = GetNode<Label>($"%{nameof(PhaseIndicator)}");
        PhaseIndicatorTurn = GetNode<QuarterTurnContainer>($"%{nameof(PhaseIndicatorTurn)}");
        DeckTurn = GetNode<QuarterTurnContainer>($"%{nameof(DeckTurn)}");
        DropZoneTurn = GetNode<QuarterTurnContainer>($"%{nameof(DropZoneTurn)}");
        Deck = GetNode<DeckComponent>($"%{nameof(Deck)}");

        ExtraLeft1 = GetNode<UnitCircleComponent>($"%{nameof(ExtraLeft1)}");
        ExtraLeft2 = GetNode<UnitCircleComponent>($"%{nameof(ExtraLeft2)}");
        ExtraRight1 = GetNode<UnitCircleComponent>($"%{nameof(ExtraRight1)}");
        ExtraRight2 = GetNode<UnitCircleComponent>($"%{nameof(ExtraRight2)}");
    
        FrontLeft = GetNode<UnitCircleComponent>($"%{nameof(FrontLeft)}");
        BackLeft = GetNode<UnitCircleComponent>($"%{nameof(BackLeft)}");
        BackCenter = GetNode<UnitCircleComponent>($"%{nameof(BackCenter)}");
        FrontRight = GetNode<UnitCircleComponent>($"%{nameof(FrontRight)}");
        BackRight = GetNode<UnitCircleComponent>($"%{nameof(BackRight)}");

        Vanguard = GetNode<UnitCircleComponent>($"%{nameof(Vanguard)}");

        DamageZone = GetNode<DamageZoneComponent>($"%{nameof(DamageZone)}");
        DropZone = GetNode<DropZoneComponent>($"%{nameof(DropZone)}");
        TriggerZone = GetNode<TriggerZoneComponent>($"%{nameof(TriggerZone)}");

        LeftBoostLine = GetNode<AttackIndicator>($"%{nameof(LeftBoostLine)}");
        CenterBoostLine = GetNode<AttackIndicator>($"%{nameof(CenterBoostLine)}");
        RightBoostLine = GetNode<AttackIndicator>($"%{nameof(RightBoostLine)}");

        Render();
    }

    public void Reset()
    {
        // Disable Extra fields
        foreach(var extrafield in AllExtraFields)
        {
            extrafield.Hide();
        }

        foreach(var field in Rearguards)
        {
            field.ClearCard();
        }
  
        DamageZone.ClearCards();
        DropZone.ClearCard();

        // Set Vanguards
        TriggerZone.ClearCard();
    }

    public void SetVanguard(VanguardCard card)
    {
        Vanguard.SetCard(card);
    }

    public void Bind(VanguardPlayArea playArea)
    {
        PlayArea = playArea;

        Vanguard.Bind(playArea.Vanguard);
        FrontLeft.Bind(playArea.FrontLeft);
        BackLeft.Bind(playArea.BackLeft);
        BackCenter.Bind(playArea.BackCenter);
        FrontRight.Bind(playArea.FrontRight);
        BackRight.Bind(playArea.BackRight);

        Deck.Bind(playArea.Deck);
        DamageZone.Bind(playArea.DamageZone);
        DropZone.Bind(playArea.DropZone);
        TriggerZone.Bind(playArea.TriggerZone);
    }

    // Cards with no known previous location come from here (draws, checks, superior calls)
    public ICardLocation DeckLocation => Deck;

    PresentationContext? presentation = null;

    public void SetEventBus(VanguardEventBus eventBus, PresentationContext presentation)
    {
        this.presentation = presentation;
        eventBus.CardAddedToSoul += OnCardAddedToSoul;
        TriggerZone.DeckLocation = Deck;
        Circles.ForEach(circle => circle.DeckLocation = Deck);

        Vanguard.SetEventBus(eventBus, presentation);
        FrontLeft.SetEventBus(eventBus, presentation);
        BackLeft.SetEventBus(eventBus, presentation);
        BackCenter.SetEventBus(eventBus, presentation);
        FrontRight.SetEventBus(eventBus, presentation);
        BackRight.SetEventBus(eventBus, presentation);

        Deck.SetEventBus(eventBus, presentation);
        DamageZone.SetEventBus(eventBus, presentation);
        DropZone.SetEventBus(eventBus, presentation);
        TriggerZone.SetEventBus(eventBus, presentation);
    }

    // Soul Charge: from the deck to under the vanguard. A ridden vanguard going to the soul
    // stays where it is, so it doesn't fly (CardMotion skips moves within one location).
    private void OnCardAddedToSoul(Soul soul, CardBase card)
    {
        if(!ReferenceEquals(soul, PlayArea.Soul)) return;
        presentation.PresentArrival(card, CardMove.ToSoul, Vanguard, Deck, () => { }, faceUp: true);
    }

    public void RecalculateStats()
    {
        Circles.ForEach(circle => circle.UpdateStats());
    }

    public void SetIndicatorText(string text)
    {
        PhaseIndicator.Text = text;
    }

    public UnitCircleComponent GetOppositeCircle(UnitCircleComponent circle)
    {
        if(ReferenceEquals(FrontLeft, circle)) return BackLeft;
        if(ReferenceEquals(BackLeft, circle)) return FrontLeft;
        if(ReferenceEquals(FrontRight, circle)) return BackRight;
        if(ReferenceEquals(BackRight, circle)) return FrontRight;
        if(ReferenceEquals(Vanguard, circle)) return BackCenter;
        if(ReferenceEquals(BackCenter, circle)) return Vanguard;
        throw new InvalidOperationException();
    }

    public bool IsCardInUnitCircle(VanguardCard card)
    {
        return Circles.Any(x => ReferenceEquals(x.CurrentCard?.CurrentCard, card));
    }

    internal string GetUnitCircleComponentName(UnitCircleComponent unitCircleComponent)
    {
        if(ReferenceEquals(FrontLeft, unitCircleComponent)) return nameof(FrontLeft);
        if(ReferenceEquals(BackLeft, unitCircleComponent)) return nameof(BackLeft);
        if(ReferenceEquals(FrontRight, unitCircleComponent)) return nameof(FrontRight);
        if(ReferenceEquals(BackRight, unitCircleComponent)) return nameof(BackRight);
        if(ReferenceEquals(Vanguard, unitCircleComponent)) return nameof(Vanguard);
        if(ReferenceEquals(BackCenter, unitCircleComponent)) return nameof(BackCenter);
        throw new InvalidOperationException();
    }

    public UnitCircleComponent GetUnitCircleComponent(VanguardCard card)
    {
        return Circles.First(x => ReferenceEquals(x.CurrentCard?.CurrentCard, card));
    }
    public UnitCircleComponent GetUnitCircleComponent(UnitCircle circle)
    {
        return Circles.First(x => ReferenceEquals(x.UnitCircle, circle));
    }
    public UnitCircleComponent? GetUnitCircleComponentOrDefault(UnitCircle circle)
    {
        return Circles.FirstOrDefault(x => ReferenceEquals(x.UnitCircle, circle));
    }
    public bool IsBackRow(UnitCircleComponent unitCircle)
    {
        return BackRowRearguards.Contains(unitCircle, ReferenceEqualityComparer.Instance);
    }
    public bool IsFrontRow(UnitCircleComponent unitCircle)
    {
        return FrontRowCircles.Contains(unitCircle, ReferenceEqualityComparer.Instance);
    }

    public void DisableRearguardDropping()
    {
        Rearguards.ForEach(x =>
        {
            x.Droppable = false;
        });
    }

    public void EnableRearguardDropping()
    {
        Rearguards.ForEach(x =>
        {
            x.Droppable = true;
        });
    }

    public void EnableRearguardDragging()
    {
        Rearguards.ForEach(rg => rg.Draggable = true);
    }

    public void DisableRearguardDragging(List<UnitCircleComponent> unitCircleComponents)
    {
        unitCircleComponents.ForEach(rg => rg.Draggable = false);
    }

    public void EnableUnitCircleScreenDragging()
    {
        Circles.ForEach(x => x.ScreenDraggable = true);
    }
    public void DisableUnitCircleScreenDragging()
    {
        Circles.ForEach(x => x.ScreenDraggable = false);
    }

    public void EnableUnitCircleHovering(IEnumerable<UnitCircleComponent> circles)
    {
        foreach(var circle in circles)
        {
            circle.Hoverable = true;
        }
    }

    public void DisableUnitCircleHovering()
    {
        Circles.ForEach((circle) => circle.Hoverable = false);
    }

    public void EnableFrontRowUnitCircleHovering()
    {
        FrontRowCircles.ForEach((circle) => circle.Hoverable = true);
    }
    public void DisableFrontRowUnitCircleHovering()
    {
        FrontRowCircles.ForEach((circle) => circle.Hoverable = false);
    }

    public void ShowBoostLine(UnitCircleComponent unitCircleComponent)
    {
        if (ReferenceEquals(FrontLeft, unitCircleComponent)) LeftBoostLine.Show();
        if (ReferenceEquals(Vanguard, unitCircleComponent)) CenterBoostLine.Show();
        if (ReferenceEquals(FrontRight, unitCircleComponent)) RightBoostLine.Show();
    }

    public void HideBoostLines()
    {
        LeftBoostLine.Hide();
        CenterBoostLine.Hide();
        RightBoostLine.Hide();
    }

    public void EnableSelectUnitCircle(UnitSelector selector)
    {
        var selectedCircles = PlayArea.GetUnitCirclesBySelector(selector);
        selectedCircles.Select(circle => Circles.First(pc => ReferenceEquals(pc.UnitCircle, circle))).ToList()
            .ForEach((circle) => circle.Selectable = true);
    }

    public void DisableSelectUnitCircle()
    {
        Circles.ForEach((circle) => { 
            circle.ResetSelection();
            circle.Selectable = false;
        });
    }

    public void HideTargetIndicators()
    {
        Circles.ForEach(x => x.HideTargetIndicators());
    }

    public void PrintRearguardCardDraggableStates()
    {
        GD.Print($"{nameof(FrontLeft)}", FrontLeft.Draggable);
        GD.Print($"{nameof(BackLeft)}", BackLeft.Draggable);
        GD.Print($"{nameof(BackCenter)}", BackCenter.Draggable);
        GD.Print($"{nameof(FrontRight)}", FrontRight.Draggable);
        GD.Print($"{nameof(BackRight)}", BackRight.Draggable);
    }
}
