using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArC.CardGames.Components;
using ArC.CardGames.Vanguard;

/// <summary>
/// Picking the unit a trigger goes to (its power, its critical, the rear-guard it stands): on the board as it is, not
/// enlarged (EnlargesField off in the scene), the prompt between the opponent's board above and the player's below, so
/// the attack stays drawn and both sides can be read. The revealed trigger is on the board's trigger zone (a tap shows
/// it). A tap picks a unit (numbered), a tap on another moves the pick, OK answers (ConfirmSinglePick in the scene).
/// </summary>
public partial class VanguardTriggerSelectScreen : FieldSelectScreen
{
    /// <summary>The unit <paramref name="purpose"/>'s trigger goes to, of <paramref name="candidates"/>.</summary>
    public async Task<ICardSpace> ChooseTriggerTarget(TriggerQuestion purpose, IReadOnlyList<ICardSpace> candidates, string hint)
        => (await ChooseZones(candidates, 1, 1, hint, canCancel: false)).Single();
}
