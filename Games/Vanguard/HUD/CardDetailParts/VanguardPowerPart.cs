using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;

[Tool]
public partial class VanguardPowerPart : CardValuePart
{
    protected override string? Format(CardBase card) => For<VanguardCard>(card, unit => unit.Power.ToString());
}
