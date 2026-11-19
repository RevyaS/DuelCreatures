using Godot;

/// <summary>
/// YuGiOh's card details: its parts read YuGiOh cards. Skills read as their names (SkillKindInterpreter) until the
/// rewritten ArC's skills have text. Every effect the player activates has a button; a Set card is activated with the
/// details' Activate command and a spell from the hand by dropping it on a zone (YuGiOhMainPhaseStrategy).
/// </summary>
public partial class YuGiOhCardDetails : CardDetailsView
{
}
