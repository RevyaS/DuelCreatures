using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;
using ArC.CardGames.Vanguard;

/// <summary>
/// A heal trigger (<see cref="TriggerQuestion.Heal"/>): one card of the player's damage zone. The face-down cards show the
/// face-down indicator so the player sees which are flipped; picking one changes nothing, the card leaves as it is.
/// </summary>
public sealed class HealSelection(VanguardInput input) : VanguardCardSelection(input)
{
    public override bool Handles(Choosing<CardBase> question) => question.Purpose is TriggerQuestion.Heal;
    public override Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question) => Ask(question, VanguardChoices.Text.Heal);

    protected override FacingMark FacingOf(CardBase card, bool picked) => card.IsFaceUp ? FacingMark.None : FacingMark.FaceDown;
}
