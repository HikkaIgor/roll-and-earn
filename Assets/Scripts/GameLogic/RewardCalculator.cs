namespace RollAndEarn
{
    public static class RewardCalculator
    {
        private const ulong ForestLowReward = 5_000_000_000;
        private const ulong ForestMidReward = 15_000_000_000;
        private const ulong ForestHighReward = 30_000_000_000;
        private const uint ForestLowXP = 5;
        private const uint ForestMidXP = 15;
        private const uint ForestHighXP = 25;
        private const uint ForestSpecialXP = 35;

        private const ulong DungeonLowReward = 5_000_000_000;
        private const ulong DungeonMidReward = 25_000_000_000;
        private const ulong DungeonHighReward = 60_000_000_000;
        private const uint DungeonLowXP = 10;
        private const uint DungeonMidXP = 25;
        private const uint DungeonHighXP = 50;
        private const uint DungeonSpecialXP = 75;

        private const ulong DragonLowReward = 10_000_000_000;
        private const ulong DragonMidReward = 50_000_000_000;
        private const ulong DragonHighReward = 150_000_000_000;
        private const uint DragonLowXP = 20;
        private const uint DragonMidXP = 50;
        private const uint DragonHighXP = 100;
        private const uint DragonSpecialXP = 150;

        public static (ulong tokenAmount, uint xp, bool isSpecial, string tier) Calculate(byte adventureType, byte rollValue)
        {
            byte mid, high, special;

            switch (adventureType)
            {
                case 0:
                    mid = 6; high = 11; special = 20;
                    return CalculateTier(rollValue, mid, high, special,
                        ForestLowReward, ForestMidReward, ForestHighReward, ForestHighReward,
                        ForestLowXP, ForestMidXP, ForestHighXP, ForestSpecialXP);
                case 1:
                    mid = 5; high = 9; special = 18;
                    return CalculateTier(rollValue, mid, high, special,
                        DungeonLowReward, DungeonMidReward, DungeonHighReward, DungeonHighReward,
                        DungeonLowXP, DungeonMidXP, DungeonHighXP, DungeonSpecialXP);
                case 2:
                    mid = 4; high = 8; special = 16;
                    return CalculateTier(rollValue, mid, high, special,
                        DragonLowReward, DragonMidReward, DragonHighReward, DragonHighReward,
                        DragonLowXP, DragonMidXP, DragonHighXP, DragonSpecialXP);
                default:
                    return (0, 0, false, "None");
            }
        }

        private static (ulong tokenAmount, uint xp, bool isSpecial, string tier) CalculateTier(
            byte roll, byte mid, byte high, byte special,
            ulong lowR, ulong midR, ulong highR, ulong specR,
            uint lowXP, uint midXP, uint highXP, uint specXP)
        {
            if (roll >= special)
                return (specR, specXP, true, "Special");
            if (roll >= high)
                return (highR, highXP, false, "High");
            if (roll >= mid)
                return (midR, midXP, false, "Mid");
            return (lowR, lowXP, false, "Low");
        }
    }
}
