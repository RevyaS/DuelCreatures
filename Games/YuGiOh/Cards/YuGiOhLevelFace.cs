using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>A monster's Level after the Prefix, or an Xyz monster's Rank after the <see cref="RankPrefix"/>; hidden on spells and traps.</summary>
[Tool]
public partial class YuGiOhLevelFace : CardFaceText
{
    [Export] public string RankPrefix { get; set; } = "☆";

    public YuGiOhLevelFace()
    {
        Prefix = "★";
    }

    protected override string? Format(CardBase card) => card switch
    {
        YuGiOhXYZMonsterCard xyz => xyz.Rank.ToString(),
        YuGiOhNormalMonsterCard monster => monster.Level.ToString(),
        _ => null,
    };

    protected override string PrefixFor(CardBase card) => card is YuGiOhXYZMonsterCard ? RankPrefix : Prefix;
}
