using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;

/// <summary>What kind of card it is: a monster's types, or a spell's / trap's property ("Quick Play Spell").</summary>
[Tool]
public partial class YuGiOhCardTypePart : CardValuePart
{
    [Export] public string SpellSuffix { get; set; } = "Spell";
    [Export] public string TrapSuffix { get; set; } = "Trap";

    protected override string? Format(CardBase card) => card switch
    {
        YuGiOhMonsterCard monster => string.Join(" / ", monster.Types.Select(type => Words(type))),
        YuGiOhSpellCard spell => $"{Words(spell.Property)} {SpellSuffix}",
        YuGiOhTrapCard trap => $"{Words(trap.Property)} {TrapSuffix}",
        _ => null,
    };
}
