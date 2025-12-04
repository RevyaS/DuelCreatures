
using System.Collections.Generic;
using System.Linq;
using ArC.CardGames.Components;
using ArC.CardGames.Predefined.Vanguard;

public partial class AIInputProvider : IVanguardPlayerInputProvider
{
    private List<CardBase> Mulligan(List<CardBase> handOriginal)
    {
        var hand = handOriginal.ToList();

        var g0 = hand.OfType<VanguardCard>().Where(c => c.Grade == 0).ToList();
        var g1 = hand.OfType<VanguardCard>().Where(c => c.Grade == 1).ToList();
        var g2 = hand.OfType<VanguardCard>().Where(c => c.Grade == 2).ToList();
        var g3 = hand.OfType<VanguardCard>().Where(c => c.Grade == 3).ToList();

        bool needG1 = g1.Count == 0;
        bool needG2 = g2.Count == 0;
        bool needG3 = g3.Count == 0;

        // Identify must keep (first ride chain piece of each)
        HashSet<CardBase> mustKeep = new();
        if (g1.Count > 0) mustKeep.Add(g1.First());
        if (g2.Count > 0) mustKeep.Add(g2.First());
        if (g3.Count > 0) mustKeep.Add(g3.First());

        var optional = hand.Where(c => !mustKeep.Contains(c)).ToList();

        // Sort optional from worst to best
        var ordered = optional
            .OrderByDescending(c => ((VanguardCard)c).Grade)
            .ToList();

        List<CardBase> toReturn = new();

        void MulliganOne()
        {
            if (ordered.Count == 0) return;
            var card = ordered[0];
            ordered.RemoveAt(0);
            toReturn.Add(card);
        }

        // Fix missing ride grades
        if (needG1) MulliganOne();
        if (needG2) MulliganOne();
        if (needG3) MulliganOne();

        // Remove extra high-grade cards
        foreach (var card in ordered)
        {
            var grade = ((VanguardCard)card).Grade;

            if (grade >= 2) // G2 or G3 excess
                toReturn.Add(card);
        }

        // Do not over-mulligan; keep max 3
        if (toReturn.Count > 3)
            toReturn = toReturn.Take(3).ToList();

        return toReturn;
    }
}