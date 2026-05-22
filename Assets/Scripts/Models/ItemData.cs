using System;
using System.Security.Cryptography;
using UnityEngine;

namespace RollAndEarn
{
    [Serializable]
    public class ItemData
    {
        public enum ItemType { Weapon, Armor, Artifact }
        public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

        public string itemName;
        public ItemType type;
        public Rarity rarity;
        public string statBonus;
        public byte rollBonus;
        public string imageUri;
        public Sprite artSprite;
        public string mintAddress;

        private static readonly float[] RarityChances = { 0.25f, 0.30f, 0.25f, 0.15f, 0.05f };

        public static byte ComputeBonusFromMint(string mintAddress)
        {
            try
            {
                byte[] mintBytes = new Solana.Unity.Wallet.PublicKey(mintAddress).KeyBytes;
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(mintBytes);
                return (byte)((hash[0] % 5) + 1);
            }
            catch
            {
                return 1;
            }
        }

        public static Rarity DetermineRarityFromMint(string mintAddress)
        {
            try
            {
                byte[] mintBytes = new Solana.Unity.Wallet.PublicKey(mintAddress).KeyBytes;
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(mintBytes);
                byte val = hash[1];
                if (val < 5) return Rarity.Legendary;
                if (val < 20) return Rarity.Epic;
                if (val < 45) return Rarity.Rare;
                if (val < 75) return Rarity.Uncommon;
                return Rarity.Common;
            }
            catch
            {
                return Rarity.Common;
            }
        }

        public static ItemType DetermineTypeFromMint(string mintAddress)
        {
            try
            {
                byte[] mintBytes = new Solana.Unity.Wallet.PublicKey(mintAddress).KeyBytes;
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(mintBytes);
                return hash[2] % 2 == 0 ? ItemType.Weapon : ItemType.Armor;
            }
            catch
            {
                return ItemType.Weapon;
            }
        }

        public static ItemData FromMintAddress(string mintAddress)
        {
            var data = new ItemData();
            data.mintAddress = mintAddress;
            data.rollBonus = ComputeBonusFromMint(mintAddress);
            data.rarity = DetermineRarityFromMint(mintAddress);
            data.type = DetermineTypeFromMint(mintAddress);
            data.itemName = $"{data.rarity} {data.type} +{data.rollBonus}";
            data.statBonus = $"+{data.rollBonus} to rolls";
            return data;
        }

        public static ItemData FromNftMetadata(NftMetadata meta, string mint = "")
        {
            var data = new ItemData();
            data.itemName = meta.name;
            data.mintAddress = mint;
            foreach (var attr in meta.attributes)
            {
                switch (attr.trait_type)
                {
                    case "Type":
                        data.type = attr.value switch
                        {
                            "Weapon" => ItemType.Weapon,
                            "Armor" => ItemType.Armor,
                            "Artifact" => ItemType.Artifact,
                            _ => ItemType.Weapon
                        };
                        break;
                    case "Rarity":
                        data.rarity = attr.value switch
                        {
                            "Common" => Rarity.Common,
                            "Uncommon" => Rarity.Uncommon,
                            "Rare" => Rarity.Rare,
                            "Epic" => Rarity.Epic,
                            "Legendary" => Rarity.Legendary,
                            _ => Rarity.Common
                        };
                        break;
                    case "Stat Bonus":
                        data.statBonus = attr.value;
                        break;
                    case "Roll Bonus":
                        if (byte.TryParse(attr.value, out var rb)) data.rollBonus = rb;
                        break;
                }
            }
            if (data.rollBonus == 0 && !string.IsNullOrEmpty(mint))
                data.rollBonus = ComputeBonusFromMint(mint);
            data.imageUri = meta.image;
            return data;
        }

        public static Color GetRarityColor(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common => ThemeColors.TextSecondary,
                Rarity.Uncommon => ThemeColors.Success,
                Rarity.Rare => ThemeColors.Secondary,
                Rarity.Epic => new Color(0.6f, 0.35f, 0.9f),
                Rarity.Legendary => ThemeColors.AccentGold,
                _ => ThemeColors.TextSecondary
            };
        }
    }
}
