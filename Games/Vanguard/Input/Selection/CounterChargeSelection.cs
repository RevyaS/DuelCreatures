using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;
using ArC.CardGames.Skills;

/// <summary>
/// Counter Charge: an effect's "as many as possible" pick of the player's face-down damage. Unpicked cards show the
/// face-down indicator; a pick shows face up, a preview of the unflip. ArC has no type for this pick yet, so it's told by its quantity and the cards offered.
/// </summary>
public sealed class CounterChargeSelection(VanguardInput input) : VanguardCardSelection(input)
{
    public override bool Handles(Choosing<CardBase> question)
        => question.Purpose is Pick<CardBase>
           && question.Quantity.FewerWillDo
           && question.Offered.Count > 0
           && AllInOwnDamage(question)
           && question.Offered.All(card => !card.IsFaceUp);

    public override Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question)
        => Ask(question, VanguardChoices.Text.CounterCharge(question.Maximum));

    protected override FacingMark FacingOf(CardBase card, bool picked) => picked ? FacingMark.FaceUp : FacingMark.FaceDown;
}
