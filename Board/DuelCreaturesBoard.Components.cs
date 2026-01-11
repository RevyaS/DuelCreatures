using System.Collections.Generic;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    AudioStreamPlayer SfxPlayer = null!;

    public PlayAreaComponent PlayerArea { get; private set; } = null!;
    public PlayAreaComponent OppArea { get; private set; } = null!;

    public CardLineStatic GuardZone { get; private set; } = null!; 

    public HandComponent PlayerHand { get; private set; } = null!;
    HandComponent OppHand = null!;

    List<HandComponent> AllHands => [ 
        PlayerHand, OppHand
    ];

    AttackIndicator PlayerLeftAttackRightLine = null!, PlayerLeftAttackCenterLine = null!, PlayerLeftAttackLeftLine = null!,
        PlayerCenterAttackRightLine = null!, PlayerCenterAttackCenterLine = null!, PlayerCenterAttackLeftLine = null!,
        PlayerRightAttackRightLine = null!, PlayerRightAttackCenterLine = null!, PlayerRightAttackLeftLine = null!,
        OppLeftAttackRightLine = null!, OppLeftAttackCenterLine = null!, OppLeftAttackLeftLine = null!,
        OppCenterAttackRightLine = null!, OppCenterAttackCenterLine = null!, OppCenterAttackLeftLine = null!,
        OppRightAttackRightLine = null!, OppRightAttackCenterLine = null!, OppRightAttackLeftLine = null!;

    private void SetComponents()
    {
        SfxPlayer = GetNode<AudioStreamPlayer>($"%{nameof(SfxPlayer)}");

        PlayerArea = GetNode<PlayAreaComponent>($"%{nameof(PlayerArea)}");
        OppArea = GetNode<PlayAreaComponent>($"%{nameof(OppArea)}");

        GuardZone = GetNode<CardLineStatic>($"%{nameof(GuardZone)}");

        PlayerHand = GetNode<HandComponent>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandComponent>($"%{nameof(OppHand)}");

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
