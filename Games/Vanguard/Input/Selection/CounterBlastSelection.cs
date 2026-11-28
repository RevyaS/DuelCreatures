using System.Collections.Generic;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Setup;
using ArC.CardGames.Skills;
using ArC.CardGames.Vanguard;

/// <summary>
/// Counter Blast (a <see cref="CounterBlastCost"/>'s pick): the face-up damage. Unpicked cards show nothing; a pick
/// shows the face-down indicator, a preview of the flip.
/// </summary>
public sealed class CounterBlastSelection(VanguardInput input) : VanguardCardSelection(input)
{
    public override bool Handles(Choosing<CardBase> question) => question.Purpose is CounterBlastCost;

    public override Task<IReadOnlyList<CardBase>> Choose(Choosing<CardBase> question)
        => Ask(question, VanguardChoices.Text.CounterBlast(((CounterBlastCost)question.Purpose).Amount));

    protected override FacingMark FacingOf(CardBase card, bool picked) => picked ? FacingMark.FaceDown : FacingMark.None;
}
