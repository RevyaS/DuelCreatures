
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;
using static ArC.CardGames.Predefined.Vanguard.Cards.DuelMaidensCardsFactory;

public partial class AIInputProvider : IVanguardPlayerInputProvider
{
    private CardBase? SelectRideCard()
    {
        var currentVanguard = PlayArea.Vanguard.Card; // however you access it
        int requiredGrade = currentVanguard is VanguardCard v ? v.Grade + 1 : 1;

        // Cannot ride past grade 3
        if (requiredGrade > 3)
            return null;

        // Find all ride options
        var rideOptions = Hand.Cards
            .OfType<VanguardCard>()
            .Where(c => c.Grade == requiredGrade)
            .ToList();

        // No valid ride → skip
        if (rideOptions.Count == 0)
            return null;

        // If only one → easy pick
        if (rideOptions.Count == 1)
            return rideOptions[0];

        // Multiple cards → pick the best
        var result = ChooseBestRide(rideOptions);
        return result;
    }

    private CardBase ChooseBestRide(List<VanguardCard> options)
    {
        // Grade-based ranking
        // (Each list is only called when required grade == that value)
        return options.OrderByDescending(c => RidePriority(c)).First();
    }

    /// <summary>
    /// Assign a priority score for each card when riding.
    /// Higher score = preferred ride.
    /// </summary>
    private int RidePriority(VanguardCard card)
    {
        // Grade 1 priorities
        if (card == RoyalPaladin.LittleSageMarron) return 300;  // best G1
        if (card == RoyalPaladin.SailorGuardianMichiru) return 200;

        // Grade 2 priorities
        if (card == RoyalPaladin.BlasterBlade) return 500;
        if (card == RoyalPaladin.KnightOfSilenceGallatin) return 400;

        // Grade 3 priorities
        if (card == RoyalPaladin.KingOfKnightsAlfred) return 1000;

        // Generic fallback
        return 100;
    }
}