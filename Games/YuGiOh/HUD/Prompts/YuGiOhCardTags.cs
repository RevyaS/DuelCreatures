using System;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.YuGiOh;
using Godot;
using HandSpace = ArC.CardGames.Predefined.Common.Hand;

/// <summary>
/// YuGiOh's card tags (YuGiOhCardTags.tres): one per place a card offered to pick can be in. <see cref="For"/> finds the
/// place holding a card in a play area and gives its tag.
/// </summary>
[GlobalClass]
public partial class YuGiOhCardTags : CardIndicators
{
    [Export] public CardTag Hand { get; set; } = null!;
    [Export] public CardTag Deck { get; set; } = null!;
    [Export] public CardTag ExtraDeck { get; set; } = null!;
    [Export] public CardTag Graveyard { get; set; } = null!;
    [Export] public CardTag Banished { get; set; } = null!;
    [Export] public CardTag MonsterZone { get; set; } = null!;
    [Export] public CardTag SpellTrapZone { get; set; } = null!;
    [Export] public CardTag FieldZone { get; set; } = null!;
    /// <summary>An Xyz Material, attached to a monster on the field.</summary>
    [Export] public CardTag Material { get; set; } = null!;

    /// <summary>The tag of the place in <paramref name="area"/> holding <paramref name="card"/>, or null when none of its places does.</summary>
    public CardTag? For(CardBase card, YuGiOhPlayArea area)
    {
        EnsureComplete();
        var zone = area.Zones.FirstOrDefault(zone => zone is ICardSource source && source.Holds(card));
        if(zone is not null) return For(zone, area);
        return area.AttachedZones.Any(attached => Refs.Has(attached.Cards, card)) ? Material : null;
    }

    private CardTag For(ICardSpace zone, YuGiOhPlayArea area) => zone switch
    {
        HandSpace => Hand,
        _ when ReferenceEquals(zone, area.ExtraMonsterDeck) => ExtraDeck,
        _ when ReferenceEquals(zone, area.Deck) => Deck,
        ArC.CardGames.YuGiOh.Graveyard => Graveyard,
        BanishZone => Banished,
        ArC.CardGames.YuGiOh.MonsterZone => MonsterZone,
        ArC.CardGames.YuGiOh.FieldZone => FieldZone,
        SpellAndTrapZone => SpellTrapZone,
        _ => throw new NotSupportedException($"{nameof(YuGiOhCardTags)} {ResourcePath} has no tag for a {zone.GetType().Name}"),
    };
}
