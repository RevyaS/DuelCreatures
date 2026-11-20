using System;
using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

[Tool]
public partial class YuGiOhCardComponent : Card
{
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
}