using UnityEngine;

namespace RollAndEarn
{
    [CreateAssetMenu(menuName = "RollAndEarn/AdventureConfig")]
    public class AdventureConfigSO : ScriptableObject
    {
        public string adventureName;
        public byte adventureType;
        public ulong cost;
        public int cooldownSeconds;
        public string description;
        public ulong lowReward;
        public ulong midReward;
        public ulong highReward;
        public ulong specialReward;
        public uint lowXp;
        public uint midXp;
        public uint highXp;
        public uint specialXp;
        public byte midStart;
        public byte highStart;
        public byte specialStart;
        public Sprite artSprite;

        public ulong CostRaw => cost;
        public ulong MaxTokenReward => highReward;
    }
}
