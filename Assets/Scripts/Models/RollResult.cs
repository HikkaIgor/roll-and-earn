using System;

namespace RollAndEarn
{
    [Serializable]
    public class RollResult
    {
        public byte rollValue;
        public byte effectiveRoll;
        public byte adventureType;
        public ulong tokenAmount;
        public uint xpGained;
        public bool isSpecial;
        public string tier;
        public long cooldownExpiry;
        public string itemMintAddress;
        public byte equipmentBonus;
    }
}
