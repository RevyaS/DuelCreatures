using ArC.CardGames.Hololive;
using static ArC.CardGames.Hololive.HololiveCardsFactory;

public static class HololiveDeckFactory
{
    public static HololiveDeckBuilder ExosisterTacticalTryDeck => HololiveDeckBuilder.Create()
        // Oshi
        .SetOshi(Members.TodorokiHajime.TodorokiHajime_HSDO5_001)

        // Members
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_002, 6)
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_003, 4)
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_004, 2)
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_005, 4)
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_006, 4)
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_007, 2)
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_008, 2)
        .AddMembers(Members.TodorokiHajime.TodorokiHajime_HSDO5_009, 2)

        .AddMembers(Members.JuufuuteiRaden.JuufuuteiRaden_HSDO5_010, 2)
        .AddMembers(Members.IchijouRirika.IchijouRirika_HSDO5_011, 2)
        .AddMembers(Members.HiodoshiAo.HiodoshiAo_HSDO5_012, 2)
        .AddMembers(Members.OtonoseKanade.OtonoseKanade_HSDO5_013, 2)

        // Supports
        .AddSupports(Supports.ReGLOSS, 4)
        .AddSupports(Supports.Banpen, 2)
        .AddSupports(Supports.HarusakiNodoka, 4)
        .AddSupports(Supports.ManagerChan, 2)
        .AddSupports(Supports.NormalPC, 2)
        .AddSupports(Supports.SoThatMakesYouMyEnemy, 2)

        // Cheers
        .AddCheers(Cheers.White.HY01_001, 20);
}