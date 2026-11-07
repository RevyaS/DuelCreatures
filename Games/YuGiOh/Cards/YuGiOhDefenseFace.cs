using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>A monster's printed DEF; hidden on spells and traps.</summary>
[Tool]
public partial class YuGiOhDefenseFace : CardFaceText
{
    public YuGiOhDefenseFace()
    {
    }

    protected override string? Format(CardBase card) => card is YuGiOhMonsterCard monster ? monster.Defense.ToString() : null;
}
