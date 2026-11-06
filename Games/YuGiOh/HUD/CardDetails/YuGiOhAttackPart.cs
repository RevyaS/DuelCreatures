using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>A monster's printed ATK.</summary>
[Tool]
public partial class YuGiOhAttackPart : CardValuePart
{
    protected override string? Format(CardBase card) => For<YuGiOhMonsterCard>(card, monster => monster.Attack.ToString());
}
