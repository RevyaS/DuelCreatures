using System;
using ArC.CardGames.Components;
using Godot;
using DeckSpace = ArC.CardGames.Predefined.Common.Deck;
using HandSpace = ArC.CardGames.Predefined.Common.Hand;
using VanguardZones = ArC.CardGames.Vanguard;

/// <summary>
/// Vanguard's card tags (VanguardCardTags.tres): one per place a card offered to pick can be in, and the roles a prompt
/// gives a card (<see cref="Intercept"/>, set by the guard screen). <see cref="For"/> maps a zone to its place's tag.
/// </summary>
[GlobalClass]
public partial class VanguardCardTags : CardIndicators
{
    [Export] public CardTag Hand { get; set; } = null!;
    [Export] public CardTag Soul { get; set; } = null!;
    [Export] public CardTag DropZone { get; set; } = null!;
    [Export] public CardTag Deck { get; set; } = null!;
    [Export] public CardTag DamageZone { get; set; } = null!;
    [Export] public CardTag RearGuard { get; set; } = null!;
    [Export] public CardTag Vanguard { get; set; } = null!;
    /// <summary>A front rear-guard offered to intercept.</summary>
    [Export] public CardTag Intercept { get; set; } = null!;

    /// <summary>The tag of the place <paramref name="zone"/> is; a zone with no tag here throws, naming it.</summary>
    public CardTag For(ICardSpace zone)
    {
        EnsureComplete();
        return zone switch
        {
            HandSpace => Hand,
            VanguardZones.Soul => Soul,
            VanguardZones.DropZone => DropZone,
            DeckSpace => Deck,
            VanguardZones.DamageZone => DamageZone,
            VanguardZones.RearGuard => RearGuard,
            VanguardZones.Vanguard => Vanguard,
            _ => throw new NotSupportedException($"{nameof(VanguardCardTags)} {ResourcePath} has no tag for a {zone.GetType().Name}"),
        };
    }
}
