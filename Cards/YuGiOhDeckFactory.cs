using ArC.CardGames.YuGiOh;
using static ArC.CardGames.YuGiOh.Cards.YuGiOhCardsFactory;

public static class YuGiOhDeckFactory
{
    public static YuGiOhDeckBuilder ExosisterTacticalTryDeck => YuGiOhDeckBuilder.Create()
        // Monsters
        .AddCards(Exosister.ExosisterMartha, 3)
        .AddCards(Exosister.ExosisterElis, 3)
        .AddCards(Exosister.ExosisterStella, 2)
        .AddCards(Exosister.ExosisterSophia, 2)
        .AddCards(Exosister.ExosisterIrene, 1)
        .AddCards(Spirit.Aratama, 2)
        .AddCards(Spirit.Sakitama, 2)

        // Spells
        .AddCards(Exosister.ExosisterPax, 3)
        .AddCards(Exosister.ExosisterArment, 1)
        .AddCards(Exosister.ExosisterCarpedivem, 1)

        // Traps
        .AddCards(Exosister.ExosisterReturnia, 2)
        .AddCards(Exosister.ExosisterVadis, 2)

        // Staple Spells
        .AddCards(Generic.TripleTacticsTalent, 2)
        .AddCards(Generic.CalledByTheGrave, 2)
        .AddCards(Generic.CrossoutDesignator, 1)

        // Staple Traps
        .AddCards(Generic.SolemnJudgement, 2)
        .AddCards(Generic.InfiniteImpermanence, 2)

        // Staple Monsters
        .AddCards(Generic.MaxxC, 2)
        .AddCards(Generic.AshBlossomAndJoyousSpring, 2)
        .AddCards(Generic.NibiruThePrimalBeing, 1)
        .AddCards(Generic.DimensionShifter, 1)
        .AddCards(Generic.GhostOgreAndSnowRabbit, 1)
        
        // XYZ Monsters
        .AddCards(Exosister.ExosistersMagnifica, 2)
        .AddCards(Exosister.ExosisterMikailis, 3)
        .AddCards(Exosister.ExosisterKaspitell, 2)
        .AddCards(Exosister.ExosisterAsophiel, 1)
        .AddCards(Exosister.ExosisterGibrine, 1)
        .AddCards(Generic.DivineArsenalAA_ZEUS_SkyThunder, 1);
}