using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>A monster's Level, or an Xyz monster's Rank (labelled LevelLabel / RankLabel).</summary>
[Tool]
public partial class YuGiOhLevelPart : CardValuePart
{
    [Export] public string LevelLabel { get; set; } = "Level";
    [Export] public string RankLabel { get; set; } = "Rank";

    protected override string? Format(CardBase card)
    {
        switch(card)
        {
            case YuGiOhXYZMonsterCard xyz:
                Label = RankLabel;
                return xyz.Rank.ToString();
            case YuGiOhNormalMonsterCard monster:
                Label = LevelLabel;
                return monster.Level.ToString();
            default:
                return null;
        }
    }
}
