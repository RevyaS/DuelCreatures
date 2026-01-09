using System.Collections.Generic;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    public PlayAreaComponent PlayerArea { get; private set; } = null!;

    Label OppPhaseIndicator = null!;
    
    UnitCircleComponent OppExtraLeft1 = null!, OppExtraLeft2 = null!, OppExtraRight1 = null!, OppExtraRight2 = null!,
        OppVanguard = null!,
        OppFrontLeft = null!, OppBackLeft = null!, OppBackCenter = null!, OppFrontRight = null!, OppBackRight = null!;
    DamageZoneComponent OppDamageZone = null!;
    TriggerZoneComponent OppTriggerZone = null!;
    public CardLineStatic GuardZone { get; private set; } = null!; 

    public HandComponent PlayerHand { get; private set; } = null!;
    HandComponent OppHand = null!;

    DeckComponent OppDeck = null!;
    public DropZoneComponent OppDropZone { get; private set; } = null!;

    List<UnitCircleComponent> AllExtraFields => [ 
        OppExtraLeft1, OppExtraLeft2, OppExtraRight1, OppExtraRight2
    ];

    List<UnitCircleComponent> AllFields => [ 
        OppFrontLeft, OppBackLeft, OppBackCenter, OppFrontRight, OppBackRight
    ];

    List<CardLineStatic> AllDamageZones => [ 
        OppDamageZone
    ];

    List<HandComponent> AllHands => [ 
        PlayerHand, OppHand
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

    AttackIndicator PlayerLeftAttackRightLine = null!, PlayerLeftAttackCenterLine = null!, PlayerLeftAttackLeftLine = null!,
        PlayerCenterAttackRightLine = null!, PlayerCenterAttackCenterLine = null!, PlayerCenterAttackLeftLine = null!,
        PlayerRightAttackRightLine = null!, PlayerRightAttackCenterLine = null!, PlayerRightAttackLeftLine = null!,
        OppLeftAttackRightLine = null!, OppLeftAttackCenterLine = null!, OppLeftAttackLeftLine = null!,
        OppCenterAttackRightLine = null!, OppCenterAttackCenterLine = null!, OppCenterAttackLeftLine = null!,
        OppRightAttackRightLine = null!, OppRightAttackCenterLine = null!, OppRightAttackLeftLine = null!;

    private void SetComponents()
    {
        PlayerArea = GetNode<PlayAreaComponent>($"%{nameof(PlayerArea)}");

        OppPhaseIndicator = GetNode<Label>($"%{nameof(OppPhaseIndicator)}");
        GuardZone = GetNode<CardLineStatic>($"%{nameof(GuardZone)}");

        OppDeck = GetNode<DeckComponent>($"%{nameof(OppDeck)}");


        OppExtraLeft1 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraLeft1)}");
        OppExtraLeft2 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraLeft2)}");
        OppExtraRight1 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraRight1)}");
        OppExtraRight2 = GetNode<UnitCircleComponent>($"%{nameof(OppExtraRight2)}");


        OppFrontLeft = GetNode<UnitCircleComponent>($"%{nameof(OppFrontLeft)}");
        OppBackLeft = GetNode<UnitCircleComponent>($"%{nameof(OppBackLeft)}");
        OppBackCenter = GetNode<UnitCircleComponent>($"%{nameof(OppBackCenter)}");
        OppFrontRight = GetNode<UnitCircleComponent>($"%{nameof(OppFrontRight)}");
        OppBackRight = GetNode<UnitCircleComponent>($"%{nameof(OppBackRight)}");

        OppDamageZone = GetNode<DamageZoneComponent>($"%{nameof(OppDamageZone)}");

        PlayerHand = GetNode<HandComponent>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandComponent>($"%{nameof(OppHand)}");

        OppDropZone = GetNode<DropZoneComponent>($"%{nameof(OppDropZone)}");

        OppVanguard = GetNode<UnitCircleComponent>($"%{nameof(OppVanguard)}");

        OppTriggerZone = GetNode<TriggerZoneComponent>($"%{nameof(OppTriggerZone)}");


        PlayerLeftAttackRightLine = GetNode<AttackIndicator>($"%{nameof(PlayerLeftAttackRightLine)}");
        PlayerLeftAttackCenterLine = GetNode<AttackIndicator>($"%{nameof(PlayerLeftAttackCenterLine)}");
        PlayerLeftAttackLeftLine = GetNode<AttackIndicator>($"%{nameof(PlayerLeftAttackLeftLine)}");
        PlayerCenterAttackRightLine = GetNode<AttackIndicator>($"%{nameof(PlayerCenterAttackRightLine)}");
        PlayerCenterAttackCenterLine = GetNode<AttackIndicator>($"%{nameof(PlayerCenterAttackCenterLine)}");
        PlayerCenterAttackLeftLine = GetNode<AttackIndicator>($"%{nameof(PlayerCenterAttackLeftLine)}");
        PlayerRightAttackRightLine = GetNode<AttackIndicator>($"%{nameof(PlayerRightAttackRightLine)}");
        PlayerRightAttackCenterLine = GetNode<AttackIndicator>($"%{nameof(PlayerRightAttackCenterLine)}");
        PlayerRightAttackLeftLine = GetNode<AttackIndicator>($"%{nameof(PlayerRightAttackLeftLine)}");

        OppLeftAttackRightLine = GetNode<AttackIndicator>($"%{nameof(OppLeftAttackRightLine)}");
        OppLeftAttackCenterLine = GetNode<AttackIndicator>($"%{nameof(OppLeftAttackCenterLine)}");
        OppLeftAttackLeftLine = GetNode<AttackIndicator>($"%{nameof(OppLeftAttackLeftLine)}");
        OppCenterAttackRightLine = GetNode<AttackIndicator>($"%{nameof(OppCenterAttackRightLine)}");
        OppCenterAttackCenterLine = GetNode<AttackIndicator>($"%{nameof(OppCenterAttackCenterLine)}");
        OppCenterAttackLeftLine = GetNode<AttackIndicator>($"%{nameof(OppCenterAttackLeftLine)}");
        OppRightAttackRightLine = GetNode<AttackIndicator>($"%{nameof(OppRightAttackRightLine)}");
        OppRightAttackCenterLine = GetNode<AttackIndicator>($"%{nameof(OppRightAttackCenterLine)}");
        OppRightAttackLeftLine = GetNode<AttackIndicator>($"%{nameof(OppRightAttackLeftLine)}");
    }
}
