using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>A monster's printed ATK; hidden on spells and traps.</summary>
[Tool]
public partial class YuGiOhAttackFace : CardFaceText
{
    public YuGiOhAttackFace()
    {
    }

    protected override string? Format(CardBase card) => card is YuGiOhMonsterCard monster ? monster.Attack.ToString() : null;
}
