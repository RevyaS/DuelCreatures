
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Predefined.Common;
using ArC.CardGames.Predefined.Vanguard;

public partial class AIInputProvider : IVanguardPlayerInputProvider
{
    private IMainPhaseAction DecideMainPhaseAction(List<IMainPhaseAction> actions)
    {
        return actions.OfType<EndMainPhase>().First();
    }
}