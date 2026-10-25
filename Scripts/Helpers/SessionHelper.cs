using System.Collections.Generic;
using ArC.CardGames.Predefined.Vanguard;
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

    /// <summary>Fresh copies of the deck's cards, so a replay doesn't reuse the card instances (and their state) of the last game.</summary>
    public static VanguardDeckBuilder CopyDeck(VanguardDeckBuilder deck)
    {
        var copy = VanguardDeckBuilder.Create().SetStartingVanguard(deck.StartingVanguard with { });
        foreach(var card in deck.GetCards())
        {
            copy.AddCards(card);
        }
        return copy;
    }

}