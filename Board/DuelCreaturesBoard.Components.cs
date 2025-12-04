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
    CardLineStatic PlayerDamageZone = null!, OppDamageZone = null!;

    public HandComponent PlayerHand { get; private set; } = null!;
    HandComponent OppHand = null!;

    CardVerticalStack PlayerDeck = null!, OppDeck = null!,
        PlayerDropZone = null!, OppDropZone = null!;

    List<UnitCircleComponent> AllExtraFields => [ 
        PlayerExtraLeft1, PlayerExtraLeft2, PlayerExtraRight1, PlayerExtraRight2,
        OppExtraLeft1, OppExtraLeft2, OppExtraRight1, OppExtraRight2
    ];

    List<UnitCircleComponent> AllFields => [ 
        PlayerFrontLeft, PlayerBackLeft, PlayerBackCenter, PlayerFrontRight, PlayerBackRight,
        OppFrontLeft, OppBackLeft, OppBackCenter, OppFrontRight, OppBackRight
    ];

    List<CardLineStatic> AllDamageZones => [ 
        PlayerDamageZone, OppDamageZone
    ];

    List<HandComponent> AllHands => [ 
        PlayerHand, OppHand
    ];

    private void SetComponents()
    {
        OppPhaseIndicator = GetNode<Label>($"%{nameof(OppPhaseIndicator)}");
        PlayerPhaseIndicator = GetNode<Label>($"%{nameof(PlayerPhaseIndicator)}");

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

        PlayerDamageZone = GetNode<CardLineStatic>($"%{nameof(PlayerDamageZone)}");
        OppDamageZone = GetNode<CardLineStatic>($"%{nameof(OppDamageZone)}");

        PlayerHand = GetNode<HandComponent>($"%{nameof(PlayerHand)}");
        OppHand = GetNode<HandComponent>($"%{nameof(OppHand)}");

        PlayerDropZone = GetNode<CardVerticalStack>($"%{nameof(PlayerDropZone)}");
        OppDropZone = GetNode<CardVerticalStack>($"%{nameof(OppDropZone)}");

        PlayerVanguard = GetNode<UnitCircleComponent>($"%{nameof(PlayerVanguard)}");
        OppVanguard = GetNode<UnitCircleComponent>($"%{nameof(OppVanguard)}");
    }
}
