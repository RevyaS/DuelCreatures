using System;
using ArC.CardGames.Components;
using Godot;
using DeckSpace = ArC.CardGames.Predefined.Common.Deck;
using HandSpace = ArC.CardGames.Predefined.Common.Hand;
using VanguardZones = ArC.CardGames.Vanguard;

/// <summary>
/// Vanguard's card tags (VanguardCardTags.tres): one per place a card offered to pick can be in, and the roles a prompt
/// gives a card (<see cref="Intercept"/>, set by the guard screen). Each rear-guard circle has its own tag, so a unit
/// offered to pick shows which circle it's on. <see cref="For"/> maps a zone to its place's tag.
/// </summary>
[GlobalClass]
public partial class VanguardCardTags : CardIndicators
{
    [Export] public CardTag Hand { get; set; } = null!;
    [Export] public CardTag Soul { get; set; } = null!;
    [Export] public CardTag DropZone { get; set; } = null!;
    [Export] public CardTag Deck { get; set; } = null!;
    [Export] public CardTag DamageZone { get; set; } = null!;
    [Export] public CardTag FrontLeft { get; set; } = null!;
    [Export] public CardTag FrontRight { get; set; } = null!;
    [Export] public CardTag BackLeft { get; set; } = null!;
    [Export] public CardTag BackCenter { get; set; } = null!;
    [Export] public CardTag BackRight { get; set; } = null!;
    [Export] public CardTag Vanguard { get; set; } = null!;
    /// <summary>A front rear-guard offered to intercept.</summary>
    [Export] public CardTag Intercept { get; set; } = null!;

    /// <summary>
    /// The tag of the place <paramref name="zone"/> (of <paramref name="area"/>) is; a zone with no tag here throws,
    /// naming it.
    /// </summary>
    public CardTag For(ICardSpace zone, VanguardZones.VanguardPlayArea area)
    {
        EnsureComplete();
        return zone switch
        {
            HandSpace => Hand,
            VanguardZones.Soul => Soul,
            VanguardZones.DropZone => DropZone,
            DeckSpace => Deck,
            VanguardZones.DamageZone => DamageZone,
            VanguardZones.RearGuard circle => RearGuardTag(circle, area),
            VanguardZones.Vanguard => Vanguard,
            _ => throw new NotSupportedException($"{nameof(VanguardCardTags)} {ResourcePath} has no tag for a {zone.GetType().Name}"),
        };
    }

    // The tag of the circle the rear-guard is, of its play area
    private CardTag RearGuardTag(VanguardZones.RearGuard circle, VanguardZones.VanguardPlayArea area)
    {
        if(ReferenceEquals(circle, area.FrontLeft)) return FrontLeft;
        if(ReferenceEquals(circle, area.FrontRight)) return FrontRight;
        if(ReferenceEquals(circle, area.BackLeft)) return BackLeft;
        if(ReferenceEquals(circle, area.BackCenter)) return BackCenter;
        if(ReferenceEquals(circle, area.BackRight)) return BackRight;
        throw new InvalidOperationException($"{circle} isn't a rear-guard circle of the play area it was found in");
    }
}
