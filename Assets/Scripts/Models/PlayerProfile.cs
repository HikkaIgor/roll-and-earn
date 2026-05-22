using System;
using Solana.Unity.Wallet;

namespace RollAndEarn
{
    [Serializable]
    public class PlayerProfile
    {
        public string owner;
        public string characterMint;
        public byte level;
        public uint xp;
        public long lastActionTs;
        public long[] cooldownExpiries = new long[3];
        public string equippedWeapon;
        public string equippedArmor;
        public byte class_;
        public byte strength;
        public byte agility;
        public byte intelligence;
        public byte luck;
        public byte itemsMinted;
        public byte unclaimedSpecials;
        public long lastDailyClaimTs;
        public ushort dailyStreak;
        public byte weaponBonus;
        public byte armorBonus;
        public bool airdropClaimed;
        public byte bump;

        public static long ValidatorTime { get; set; }

        public bool HasEquippedWeapon => equippedWeapon != "11111111111111111111111111111111";
        public bool HasEquippedArmor => equippedArmor != "11111111111111111111111111111111";

        public byte TotalEquipmentBonus => (byte)(weaponBonus + armorBonus);

        public bool CanClaimDailyReward
        {
            get
            {
                if (lastDailyClaimTs == 0) return true;
                long now = ValidatorTime > 0 ? ValidatorTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return now >= lastDailyClaimTs + 86400;
            }
        }

        public long SecondsUntilDailyClaim
        {
            get
            {
                if (lastDailyClaimTs == 0) return 0;
                long now = ValidatorTime > 0 ? ValidatorTime : DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long remaining = (lastDailyClaimTs + 86400) - now;
                return remaining > 0 ? remaining : 0;
            }
        }

        public ulong NextDailyRewardAmount
        {
            get
            {
                ulong baseReward = 50_000_000_000;
                ulong streakBonus = (ulong)dailyStreak * 10_000_000_000;
                return Math.Min(baseReward + streakBonus, 120_000_000_000);
            }
        }

        public static PlayerProfile DeserializeFromAccount(byte[] data)
        {
            int offset = 8;
            var profile = new PlayerProfile();
            profile.owner = ReadPubkey(data, ref offset);
            profile.characterMint = ReadPubkey(data, ref offset);
            profile.level = data[offset++];
            profile.xp = BitConverter.ToUInt32(data, offset); offset += 4;
            profile.lastActionTs = BitConverter.ToInt64(data, offset); offset += 8;
            if (data.Length >= 194)
            {
                for (int i = 0; i < 3; i++)
                {
                    profile.cooldownExpiries[i] = BitConverter.ToInt64(data, offset); offset += 8;
                }
            }
            else
            {
                long singleCooldown = BitConverter.ToInt64(data, offset); offset += 8;
                profile.cooldownExpiries[0] = singleCooldown;
                profile.cooldownExpiries[1] = singleCooldown;
                profile.cooldownExpiries[2] = singleCooldown;
            }
            profile.equippedWeapon = ReadPubkey(data, ref offset);
            profile.equippedArmor = ReadPubkey(data, ref offset);
            profile.class_ = data[offset++];
            profile.strength = data[offset++];
            profile.agility = data[offset++];
            profile.intelligence = data[offset++];
            profile.luck = data[offset++];
            profile.itemsMinted = data[offset++];
            profile.unclaimedSpecials = data[offset++];
            profile.lastDailyClaimTs = BitConverter.ToInt64(data, offset); offset += 8;
            profile.dailyStreak = BitConverter.ToUInt16(data, offset); offset += 2;
            profile.weaponBonus = data[offset++];
            profile.armorBonus = data[offset++];
            profile.airdropClaimed = data[offset++] != 0;
            profile.bump = data[offset++];
            return profile;
        }

        public static int ExpectedDataLength => 178;
        public static int NewFormatDataLength => 194;

        private static string ReadPubkey(byte[] data, ref int offset)
        {
            var keyBytes = new byte[32];
            Array.Copy(data, offset, keyBytes, 0, 32);
            offset += 32;
            return new PublicKey(keyBytes).ToString();
        }
    }
}
