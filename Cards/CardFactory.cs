using ArC.CardGames.Predefined.Vanguard.Skill;

namespace ArC.CardGames.Predefined.Vanguard.Cards;

public static class DuelMaidensCardsFactory
{
    public static class RoyalPaladin
    {
        // Stardust Trumpeteer
        public static VanguardCard StardustTrumpeteer => new VanguardCard(0, "Stardust Trumpeteer", 6000, 1, 10000, VanguardCardSkill.BOOST, VanguardTrigger.NONE, []);
        // public static VanguardCard Barcgal => new VanguardCard(0, "Barcgal", 4000, 1, 15000, VanguardCardSkill.BOOST, VanguardTrigger.NONE, []);
        public static VanguardCard FutureKnightLlew => new VanguardCard(0, "Future Knight, Llew", 4000, 1, 10000, VanguardCardSkill.BOOST, VanguardTrigger.CRITICAL, []);
        public static VanguardCard DevotingJewelKnightTabitha => new VanguardCard(0, "Devoting Jewel Knight, Tabitha", 4000, 1, 5000, VanguardCardSkill.BOOST, VanguardTrigger.DRAW, []);
        // public static VanguardCard ArdentJewelKnightPolli => new VanguardCard(0, "Ardent Jewel Knight, Polli", 5000, 1, 15000, VanguardCardSkill.BOOST, VanguardTrigger.HEAL, []);

        // Lake Maiden Lien
        public static VanguardCard SailorGuardianMichiru => new VanguardCard(1, "Sailor Guardian, Michiru", 7000, 1, 5000, VanguardCardSkill.BOOST, VanguardTrigger.NONE, []);
        public static VanguardCard LittleSageMarron => new VanguardCard(1, "Little Sage, Marron", 8000, 1, 10000, VanguardCardSkill.BOOST, VanguardTrigger.NONE, []);
        public static VanguardCard KnightOfSilenceGallatin => new VanguardCard(2, "Knight of Silence, Gallatin", 10000, 1, 5000, VanguardCardSkill.NONE, VanguardTrigger.NONE, []);
        public static VanguardCard BlasterBlade => new VanguardCard(2, "Blaster Blade", 9000, 1, 5000, VanguardCardSkill.NONE, VanguardTrigger.NONE, [
            new VanguardCriticalEffectContinuousSkill(VanguardSkillCardLocation.VANGUARD, new OccupiedRearguards(4), 1),
            new VanguardAutomaticSkill(VanguardSkillCardLocation.VANGUARD | VanguardSkillCardLocation.REARGUARD, 
                new OnPlaceTiming(), new VanguardSkillCost { CounterBlast = 1, SoulBlast = 1 }, [
                new RetireFrontRow()
            ])
        ]);
        public static VanguardCard KingOfKnightsAlfred => new VanguardCard(3, "King of Knights, Alfred", 10000, 1, 0, VanguardCardSkill.TWIN_DRIVE, VanguardTrigger.NONE, [
            new VanguardActivationSkill(VanguardSkillCardLocation.VANGUARD, new VanguardSkillCost { CounterBlast = 2 }, [
                new SuperiorCallFromDeck(0, 2)
            ])
        ]);
    }

    public static class OracleThinkTank
    {
        public static VanguardCard LozengeMagus => new VanguardCard(0, "Lozenge Magus", 3000, 1, 15000, VanguardCardSkill.BOOST, VanguardTrigger.HEAL, []);
        public static VanguardCard OracleGuardianNike => new VanguardCard(0, "Oracle Guardian, Nike", 5000, 1, 15000, VanguardCardSkill.BOOST, VanguardTrigger.CRITICAL, []);
        public static VanguardCard OracleGuardianGemini => new VanguardCard(1, "Oracle Guardian, Gemini", 8000, 1, 10000, VanguardCardSkill.BOOST, VanguardTrigger.NONE, []);
        public static VanguardCard BattleSisterMocha => new VanguardCard(2, "Battle Sister, Mocha", 8000, 1, 5000, VanguardCardSkill.NONE, VanguardTrigger.NONE, []);
        public static VanguardCard MikoOfSpiritualLightKinuka => new VanguardCard(2, "Miko of Spiritual Light, Kinuka", 8000, 1, 5000, VanguardCardSkill.NONE, VanguardTrigger.NONE, []);
        public static VanguardCard CEOAmaterasu => new VanguardCard(3, "CEO Amaterasu", 13000, 1, 0, VanguardCardSkill.TWIN_DRIVE, VanguardTrigger.NONE, []);
    }
}