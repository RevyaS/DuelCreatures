using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;

/// <summary>
/// What Vanguard's card selection strategies share: asking on the card selection screen (CardSelectScreen), each card
/// tagged with the zone holding it. What sets the strategies apart is the facing indicator over each card
/// (<see cref="FacingOf"/>): how the card will face, given whether it's picked.
/// </summary>
public abstract class VanguardCardSelection(VanguardInput input) : ICardSelectionStrategy
{
    protected VanguardInput Input => input;

    public abstract bool Handles(Choosing<CardBase> question);
    public abstract Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question);

    /// <summary>The indicator over <paramref name="card"/>, picked or not. None by default.</summary>
    protected virtual FacingMark FacingOf(CardBase card, bool picked) => FacingMark.None;

    protected Task<IReadOnlyList<CardBase>> Ask(Choosing<CardBase> question, string hint)
        => input.CardSelect.ChooseCards(new CardSelectRequest(question.Offered, question.Minimum, question.Maximum, hint, question.CanCancel,
            card => input.CardTags.For(ZoneOf(card)), FacingOf));

    /// <summary>Whether every offered card is in the player's own damage zone.</summary>
    protected bool AllInOwnDamage(Choosing<CardBase> question)
        => question.Offered.All(card => Refs.Has(input.PlayArea.DamageZone.Cards, card));

    // The zone holding a card offered to pick, either player's
    private ICardSpace ZoneOf(CardBase card)
        => new PlayAreaBase[] { input.PlayArea, input.OpponentPlayArea }
            .SelectMany(area => area.Zones)
            .FirstOrDefault(zone => zone is ICardSource source && source.Holds(card))
            ?? throw new InvalidOperationException($"{card.Name} was offered to pick, but no zone of either play area holds it");
}
