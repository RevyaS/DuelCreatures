using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>Normal or trigger unit, and which trigger (labelled NormalUnitLabel / TriggerUnitLabel).</summary>
[Tool]
public partial class VanguardUnitTypePart : CardValuePart
{
    [Export] public string NormalUnitLabel { get; set; } = "Normal Unit";
    [Export] public string TriggerUnitLabel { get; set; } = "Trigger Unit";

    protected override string? Format(CardBase card) => For<VanguardCard>(card, unit =>
    {
        Label = unit.Trigger == VanguardTrigger.NONE ? NormalUnitLabel : TriggerUnitLabel;
        return unit.Trigger == VanguardTrigger.NONE ? string.Empty : Words(unit.Trigger);
    });
}
