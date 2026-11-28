using System;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;

/// <summary>
/// What Vanguard's card selection strategies share: the card selection screen (ScreenCardSelection), each card tagged
/// with the zone holding it (VanguardCardTags). What sets the strategies apart is the facing indicator over each card.
/// </summary>
public abstract class VanguardCardSelection(VanguardInput input) : ScreenCardSelection(input.CardSelect)
{
    protected VanguardInput Input => input;

    /// <summary>Whether every offered card is in the player's own damage zone.</summary>
    protected bool AllInOwnDamage(Choosing<CardBase> question)
        => question.Offered.All(card => Refs.Has(input.PlayArea.DamageZone.Cards, card));

    // The tag of the zone holding a card offered to pick, either player's
    protected override CardTag TagOf(CardBase card)
    {
        foreach(var area in new[] { input.PlayArea, input.OpponentPlayArea })
        {
            var zone = area.Zones.FirstOrDefault(zone => zone is ICardSource source && source.Holds(card));
            if(zone is not null) return input.CardTags.For(zone, area);
        }
        throw new InvalidOperationException($"{card.Name} was offered to pick, but no zone of either play area holds it");
    }
}
