using System.Collections.Generic;
using Godot;


public partial class DuelCreaturesBoard : Control
{
    Label OppPhaseIndicator, PlayerPhaseIndicator;
    UnitCircle PlayerExtraLeft1, PlayerExtraLeft2, PlayerExtraRight1, PlayerExtraRight2,
        OppExtraLeft1, OppExtraLeft2, OppExtraRight1, OppExtraRight2,

        PlayerFrontLeft, PlayerBackLeft, PlayerBackCenter, PlayerFrontRight, PlayerBackRight,
        OppFrontLeft, OppBackLeft, OppBackCenter, OppFrontRight, OppBackRight;

    List<UnitCircle> AllExtraFields => [ 
        PlayerExtraLeft1, PlayerExtraLeft2, PlayerExtraRight1, PlayerExtraRight2,
        OppExtraLeft1, OppExtraLeft2, OppExtraRight1, OppExtraRight2
    ];

    List<UnitCircle> AllFields => [ 
        PlayerFrontLeft, PlayerBackLeft, PlayerBackCenter, PlayerFrontRight, PlayerBackRight,
        OppFrontLeft, OppBackLeft, OppBackCenter, OppFrontRight, OppBackRight
    ];

    private void SetComponents()
    {
        OppPhaseIndicator = GetNode<Label>($"%{nameof(OppPhaseIndicator)}");
        PlayerPhaseIndicator = GetNode<Label>($"%{nameof(PlayerPhaseIndicator)}");

        PlayerExtraLeft1 = GetNode<UnitCircle>($"%{nameof(PlayerExtraLeft1)}");
        PlayerExtraLeft2 = GetNode<UnitCircle>($"%{nameof(PlayerExtraLeft2)}");
        PlayerExtraRight1 = GetNode<UnitCircle>($"%{nameof(PlayerExtraRight1)}");
        PlayerExtraRight2 = GetNode<UnitCircle>($"%{nameof(PlayerExtraRight2)}");

        OppExtraLeft1 = GetNode<UnitCircle>($"%{nameof(OppExtraLeft1)}");
        OppExtraLeft2 = GetNode<UnitCircle>($"%{nameof(OppExtraLeft2)}");
        OppExtraRight1 = GetNode<UnitCircle>($"%{nameof(OppExtraRight1)}");
        OppExtraRight2 = GetNode<UnitCircle>($"%{nameof(OppExtraRight2)}");

        PlayerFrontLeft = GetNode<UnitCircle>($"%{nameof(PlayerFrontLeft)}");
        PlayerBackLeft = GetNode<UnitCircle>($"%{nameof(PlayerBackLeft)}");
        PlayerBackCenter = GetNode<UnitCircle>($"%{nameof(PlayerBackCenter)}");
        PlayerFrontRight = GetNode<UnitCircle>($"%{nameof(PlayerFrontRight)}");
        PlayerBackRight = GetNode<UnitCircle>($"%{nameof(PlayerBackRight)}");

        OppFrontLeft = GetNode<UnitCircle>($"%{nameof(OppFrontLeft)}");
        OppBackLeft = GetNode<UnitCircle>($"%{nameof(OppBackLeft)}");
        OppBackCenter = GetNode<UnitCircle>($"%{nameof(OppBackCenter)}");
        OppFrontRight = GetNode<UnitCircle>($"%{nameof(OppFrontRight)}");
        OppBackRight = GetNode<UnitCircle>($"%{nameof(OppBackRight)}");
    }
}
