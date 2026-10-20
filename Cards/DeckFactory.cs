using ArC.CardGames.Setup;
using static ArC.CardGames.Predefined.Vanguard.Cards.VanguardCardsFactory;

public static class DeckFactory
{
    public static DeckBuilder RoyalPaladinTrialDeck => DeckBuilder.Create()
        .AddCards(RoyalPaladin.Flogal, 4)
        .AddCards(RoyalPaladin.WeaponsDealerGovannon, 4)
        .AddCards(RoyalPaladin.YggdrasilMaidenElaine, 4)
        .AddCards(RoyalPaladin.BringerOfGoodLuckEpona, 4)
        .AddCards(RoyalPaladin.KnightOfRoseMorgana, 4)
        .AddCards(RoyalPaladin.StarlightUnicorn, 4)
        .AddCards(RoyalPaladin.Wingal, 2)
        .AddCards(RoyalPaladin.LittleSageMarron, 4)
        .AddCards(RoyalPaladin.CovenantKnightRandolf, 4)
        .AddCards(RoyalPaladin.KnightOfTheHarpTristan, 3)
        .AddCards(RoyalPaladin.BlasterBlade, 1)
        .AddCards(RoyalPaladin.KnightOfSilenceGallatin, 4)
        .AddCards(RoyalPaladin.SolitaryKnightGancelot, 2)
        .AddCards(RoyalPaladin.KnightOfConvictionBors, 1)
        .AddCards(RoyalPaladin.CrimsonButterflyBrigitte, 4);

    public static DeckBuilder KageroTrialDeck => DeckBuilder.Create()
        .AddCards(Kagero.DemonicDragonMageRakshasa, 4)
        .AddCards(Kagero.DragonMonkGenjo, 4)
        .AddCards(Kagero.LizardSoldierGanlu, 4)
        .AddCards(Kagero.DragonDancerMonica, 4)
        .AddCards(Kagero.WyvernStrikeJarran, 2)
        .AddCards(Kagero.DemonicDragonMadonnaJoka, 2)
        .AddCards(Kagero.FlameOfHopeAermo, 4)
        .AddCards(Kagero.DragonMonkGojo, 2)
        .AddCards(Kagero.EmbodimentOfArmorBahr, 4)
        .AddCards(Kagero.WyvernStrikeTejas, 4)
        .AddCards(Kagero.BerserkDragon, 4)
        .AddCards(Kagero.DragonKnightNehalem, 4)
        .AddCards(Kagero.DemonicDragonBerserkerYaksha, 4)
        .AddCards(Kagero.DragonMonkGoku, 1)
        .AddCards(Kagero.DragonicOverlord, 2);
}