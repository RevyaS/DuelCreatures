using System.Collections.Generic;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    Label OppPhaseIndicator = null!, PlayerPhaseIndicator = null!;
    
    public UnitCircleComponent PlayerVanguard { get; private set; } = null!;
    public UnitCircleComponent PlayerBackCenter { get; private set; } = null!;

    UnitCircleComponent PlayerExtraLeft1 = null!, PlayerExtraLeft2 = null!, PlayerExtraRight1 = null!, PlayerExtraRight2 = null!,
        OppExtraLeft1 = null!, OppExtraLeft2 = null!, OppExtraRight1 = null!, OppExtraRight2 = null!,

        OppVanguard = null!,

        PlayerFrontLeft = null!, PlayerBackLeft = null!, PlayerFrontRight = null!, PlayerBackRight = null!,
        OppFrontLeft = null!, OppBackLeft = null!, OppBackCenter = null!, OppFrontRight = null!, OppBackRight = null!;
    public DamageZoneComponent PlayerDamageZone { get; private set; } = null!; 
    DamageZoneComponent OppDamageZone = null!;
    HorizontalCardArea PlayerTriggerZone = null!, OppTriggerZone = null!;
    public CardLineStatic GuardZone { get; private set; } = null!; 

    public HandComponent PlayerHand { get; private set; } = null!;
    HandComponent OppHand = null!;

    CardVerticalStack PlayerDeck = null!, OppDeck = null!;
    public DropZoneComponent PlayerDropZone { get; private set; } = null!;
    public DropZoneComponent OppDropZone { get; private set; } = null!;

    List<UnitCircleComponent> AllExtraFields => [ 
        PlayerExtraLeft1, PlayerExtraLeft2, PlayerExtraRight1, PlayerExtraRight2,
        OppExtraLeft1, OppExtraLeft2, OppExtraRight1, OppExtraRight2
    ];

    List<UnitCircleComponent> AllFields => [ 
        ..PlayerRearguards,
        OppFrontLeft, OppBackLeft, OppBackCenter, OppFrontRight, OppBackRight
    ];

    List<CardLineStatic> AllDamageZones => [ 
        PlayerDamageZone, OppDamageZone
    ];

    List<HandComponent> AllHands => [ 
        PlayerHand, OppHand
    ];


    List<UnitCircleComponent> PlayerBackRowRearguards => [
        PlayerBackLeft, PlayerBackCenter, PlayerBackRight
    ];

    List<UnitCircleComponent> PlayerFrontRowCircles => [
        ..PlayerFrontRowRearguards, PlayerVanguard
    ];

    List<UnitCircleComponent> PlayerFrontRowRearguards => [
        PlayerFrontLeft, PlayerFrontRight
    ];

    List<UnitCircleComponent> PlayerRearguards => [
        ..PlayerFrontRowRearguards, ..PlayerBackRowRearguards
    ];

    List<UnitCircleComponent> PlayerCircles => [
        ..PlayerRearguards, PlayerVanguard
    ];

    List<UnitCircleComponent> OpponentCircles => [
        ..OppFrontRowCircles, ..OppBackRowRearguards
    ];
    List<UnitCircleComponent> OppBackRowRearguards => [
        OppBackLeft, OppBackCenter, OppBackRight
    ];

    List<UnitCircleComponent> OppFrontRowRearguards => [
        OppFrontLeft, OppFrontRight
    ];
    List<UnitCircleComponent> OppFrontRowCircles => [
        OppVanguard, ..OppFrontRowRearguards
    ];

    Line2D PlayerLeftBoostLine = null!, PlayerCenterBoostLine = null!, PlayerRightBoostLine = null!,
        PlayerLeftAttackRightLine = null!, PlayerLeftAttackCenterLine = null!, PlayerLeftAttackLeftLine = null!,
        PlayerCenterAttackRightLine = null!, PlayerCenterAttackCenterLine = null!, PlayerCenterAttackLeftLine = null!,
        PlayerRightAttackRightLine = null!, PlayerRightAttackCenterLine = null!, PlayerRightAttackLeftLine = null!;

    private void SetComponents()
    {
        OppPhaseIndicator = GetNode<Label>($"%{nameof(OppPhaseIndicator)}");
        PlayerPhaseIndicator = GetNode<Label>($"%{nameof(PlayerPhaseIndicator)}");

        GuardZone = GetNode<CardLineStatic>($"%{nameof(GuardZone)}");

        PlayerExtraLeft1 = GetNode<UnitCircleComponent>($"%{nameof(PlayerExtraLeft1)}");
        PlayerExtraLeft2 = GetNode<UnitCircleComponent>($"%{nameof(PlayerExtraLeft2)}");
        PlayerExtraRight1 = GetNode<UnitCircleComponent>($"%{nameof(PlayerExtraRight1)}");
        PlayerExtraRight2 = GetNode<UnitCircleComponent>($"%{nameof(PlayerExtraRight2)}");

        OppExtraLeft1 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraLeft1)}");
        OppExtraLeft2 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraLeft2)}");
        OppExtraRight1 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraRight1)}");
        OppExtraRight2 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraRight2)}");

        PlayerFrontLeft = GetNode<UnitCircleComponent>($"%{nameof(PlayerFrontLeft)}");
        PlayerBackLeft = GetNode<UnitCircleComponent>($"%{nameof(PlayerBackLeft)}");
        PlayerBackCenter = GetNode<UnitCircleComponent>($"%{nameof(PlayerBackCenter)}");
        PlayerFrontRight = GetNode<UnitCircleComponent>($"%{nameof(PlayerFrontRight)}");
        PlayerBackRight = GetNode<UnitCircleComponent>($"%{nameof(PlayerBackRight)}");

        OppFrontLeft = GetNode<UnitCircleComponent>($"%{nameof(OppFrontLeft)}");
        OppBackLeft = GetNode<UnitCircleComponent>($"%{nameof(OppBackLeft)}");
        OppBackCenter = GetNode<UnitCircleComponent>($"%{nameof(OppBackCenter)}");
        OppFrontRight = GetNode<UnitCircleComponent>($"%{nameof(OppFrontRight)}");
        OppBackRight = GetNode<UnitCircleComponent>($"%{nameof(OppBackRight)}");

        PlayerDamageZone = GetNode<DamageZoneComponent>($"%{nameof(PlayerDamageZone)}");
        OppDamageZone = GetNode<DamageZoneComponent>($"%{nameof(OppDamageZone)}");

        PlayerHand = GetNode<HandComponent>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandComponent>($"%{nameof(OppHand)}");

        PlayerDropZone = GetNode<DropZoneComponent>($"%{nameof(PlayerDropZone)}");
        OppDropZone = GetNode<DropZoneComponent>($"%{nameof(OppDropZone)}");

        PlayerVanguard = GetNode<UnitCircleComponent>($"%{nameof(PlayerVanguard)}");
        OppVanguard = GetNode<UnitCircleComponent>($"%{nameof(OppVanguard)}");

        PlayerTriggerZone = GetNode<HorizontalCardArea>($"%{nameof(PlayerTriggerZone)}");
        OppTriggerZone = GetNode<HorizontalCardArea>($"%{nameof(OppTriggerZone)}");

        PlayerLeftBoostLine = GetNode<Line2D>($"%{nameof(PlayerLeftBoostLine)}");
        PlayerCenterBoostLine = GetNode<Line2D>($"%{nameof(PlayerCenterBoostLine)}");
        PlayerRightBoostLine = GetNode<Line2D>($"%{nameof(PlayerRightBoostLine)}");

        PlayerLeftAttackRightLine = GetNode<Line2D>($"%{nameof(PlayerLeftAttackRightLine)}");
        PlayerLeftAttackCenterLine = GetNode<Line2D>($"%{nameof(PlayerLeftAttackCenterLine)}");
        PlayerLeftAttackLeftLine = GetNode<Line2D>($"%{nameof(PlayerLeftAttackLeftLine)}");
        PlayerCenterAttackRightLine = GetNode<Line2D>($"%{nameof(PlayerCenterAttackRightLine)}");
        PlayerCenterAttackCenterLine = GetNode<Line2D>($"%{nameof(PlayerCenterAttackCenterLine)}");
        PlayerCenterAttackLeftLine = GetNode<Line2D>($"%{nameof(PlayerCenterAttackLeftLine)}");
        PlayerRightAttackRightLine = GetNode<Line2D>($"%{nameof(PlayerRightAttackRightLine)}");
        PlayerRightAttackCenterLine = GetNode<Line2D>($"%{nameof(PlayerRightAttackCenterLine)}");
        PlayerRightAttackLeftLine = GetNode<Line2D>($"%{nameof(PlayerRightAttackLeftLine)}");
    }
}
