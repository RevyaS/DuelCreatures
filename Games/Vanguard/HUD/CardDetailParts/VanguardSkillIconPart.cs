using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;
using Godot;

/// <summary>The unit's skill icon: Boost, Intercept or Twin Drive; hidden for none.</summary>
[Tool]
public partial class VanguardSkillIconPart : CardValuePart
{
    protected override string? Format(CardBase card) => For<VanguardCard>(card, unit => unit.Skill == VanguardCardSkill.NONE ? null : Words(unit.Skill));
}
