using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>A monster's printed DEF.</summary>
[Tool]
public partial class YuGiOhDefensePart : CardValuePart
{
    protected override string? Format(CardBase card) => For<YuGiOhMonsterCard>(card, monster => monster.Defense.ToString());
}
