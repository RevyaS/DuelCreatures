using System;
using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>
/// A YuGiOh card: its frame colour by kind, and on a monster its Level (or an Xyz monster's Rank), ATK and DEF, in the
/// scene's face texts (%Level, %Attack, %Defense); spells and traps hide them.
/// </summary>
[Tool]
public partial class YuGiOhCardComponent : Card
{
    /// <summary>Before an Xyz monster's Rank, instead of the Level text's prefix.</summary>
    [Export] public string RankPrefix { get; set; } = "☆";

    CardFaceText Level = null!, Attack = null!, Defense = null!;

    protected override void FindNodes()
    {
        base.FindNodes();
        Level = GetNode<CardFaceText>($"%{nameof(Level)}");
        Attack = GetNode<CardFaceText>($"%{nameof(Attack)}");
        Defense = GetNode<CardFaceText>($"%{nameof(Defense)}");
    }

    public override void LoadCard(CardBase card)
    {
        Color color = card switch
        {
            YuGiOhXYZMonsterCard => Color.Color8(8, 10, 10),
            YuGiOhNormalMonsterCard => Color.Color8(182, 84, 44),
            YuGiOhSpellCard => Color.Color8(1, 146, 129),
            YuGiOhTrapCard => Color.Color8(170, 24, 118),
            _ => throw new NotImplementedException($"{card.Name} type color is not setup")
        };
        FaceColorValue = color;
        base.LoadCard(card);
    }

    protected override void ShowFace(CardBase card)
    {
        switch(card)
        {
            case YuGiOhXYZMonsterCard xyz: Level.Show(xyz.Rank, RankPrefix); break;
            case YuGiOhNormalMonsterCard monster: Level.Show(monster.Level); break;
            default: Level.Show((string?)null); break;
        }
        var stats = card as YuGiOhMonsterCard;
        Attack.Show(stats?.Attack);
        Defense.Show(stats?.Defense);
    }
}
