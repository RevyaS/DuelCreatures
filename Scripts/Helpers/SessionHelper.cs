using System.Collections.Generic;
using ArC.Common.Utils;

public static class SessionHelper
{
    static OpponentType[] selectableOpponents = [ 
        OpponentType.RULE_BOT_ROYAL_PALADIN_TRIAL_DECK,  
        OpponentType.RULE_BOT_KAGERO_TRIAL_DECK
    ];
    
    public static OpponentType SelectRandomOpponent()
    {
        return RNGUtils.SelectRandom(selectableOpponents);
    }

}