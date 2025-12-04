
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;

public partial class AIInputProvider : IVanguardPlayerInputProvider
{
    private IAttackPhaseAction DecideAtackPhaseAction(List<IAttackPhaseAction> actions)
    {
        return actions.OfType<EndAttackPhase>().First();
    }
}