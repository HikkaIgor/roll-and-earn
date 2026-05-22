using UnityEngine;

namespace RollAndEarn
{
    public static class ThemeColors
    {
        public static readonly Color Background = new(0.051f, 0.043f, 0.102f);
        public static readonly Color Panel = new(0.082f, 0.063f, 0.153f, 0.95f);
        public static readonly Color PanelBorder = new(0.22f, 0.18f, 0.35f, 0.5f);

        public static readonly Color Primary = new(1f, 0.722f, 0f);
        public static readonly Color PrimaryDim = new(0.75f, 0.54f, 0f, 0.5f);
        public static readonly Color Secondary = new(0.66f, 0.33f, 0.97f);
        public static readonly Color SecondaryDim = new(0.50f, 0.25f, 0.73f, 0.5f);

        public static readonly Color Cyan = new(0.38f, 0.65f, 0.98f);
        public static readonly Color CyanDim = new(0.2f, 0.35f, 0.55f, 0.5f);
        public static readonly Color CyanGlow = new(0.38f, 0.65f, 0.98f, 0.08f);

        public static readonly Color Gold = new(1f, 0.722f, 0f);
        public static readonly Color GoldDim = new(0.75f, 0.54f, 0f, 0.5f);
        public static readonly Color GoldGlow = new(1f, 0.722f, 0f, 0.08f);

        public static readonly Color AccentGold = new(1f, 0.84f, 0f);

        public static readonly Color TextPrimary = new(0.97f, 0.95f, 1f);
        public static readonly Color TextSecondary = new(0.65f, 0.62f, 0.75f);
        public static readonly Color TextMuted = new(0.40f, 0.37f, 0.50f);

        public static readonly Color ButtonBg = new(0.10f, 0.08f, 0.18f, 0.95f);
        public static readonly Color ButtonHover = new(0.16f, 0.12f, 0.28f, 0.98f);
        public static readonly Color ButtonPressed = new(0.06f, 0.04f, 0.12f, 1f);

        public static readonly Color Success = new(0.063f, 0.725f, 0.506f);
        public static readonly Color Error = new(0.937f, 0.267f, 0.267f);
        public static readonly Color Warning = new(0.98f, 0.82f, 0.18f);

        public static readonly Color Surface = new(0.082f, 0.063f, 0.153f, 0.9f);
        public static readonly Color SurfaceElevated = new(0.14f, 0.11f, 0.24f, 0.95f);

        public static readonly Color CardBg = new(0.075f, 0.059f, 0.145f, 0.92f);
        public static readonly Color CardBorder = new(0.22f, 0.18f, 0.35f, 0.4f);
        public static readonly Color CardInner = new(0.055f, 0.043f, 0.11f, 0.95f);

        public static readonly Color Overlay = new(0.02f, 0.01f, 0.05f, 0.82f);

        public static readonly Color Selected = new(1f, 0.722f, 0f, 0.85f);
        public static readonly Color Unselected = new(0.10f, 0.08f, 0.18f, 0.8f);
        public static readonly Color Disabled = new(0.06f, 0.05f, 0.10f, 0.6f);

        public static readonly Color AdvForest = new(0.03f, 0.28f, 0.18f, 0.65f);
        public static readonly Color AdvDungeon = new(0.20f, 0.08f, 0.35f, 0.65f);
        public static readonly Color AdvDragon = new(0.40f, 0.06f, 0.06f, 0.65f);

        public static readonly Color ClassWarrior = new(0.55f, 0.08f, 0.08f, 0.6f);
        public static readonly Color ClassRogue = new(0.03f, 0.32f, 0.16f, 0.6f);
        public static readonly Color ClassMage = new(0.14f, 0.08f, 0.42f, 0.6f);

        public static readonly Color GridBg = new(0.04f, 0.03f, 0.07f, 0.5f);
        public static readonly Color InputFieldBg = new(0.06f, 0.047f, 0.12f, 0.95f);
        public static readonly Color NavBg = new(0.035f, 0.025f, 0.08f, 0.98f);

        public static readonly Color XPBarBg = new(0.06f, 0.047f, 0.12f);
        public static readonly Color XPBarFill = new(1f, 0.722f, 0f);
        public static readonly Color LevelUpBtn = new(0.66f, 0.33f, 0.97f, 0.85f);
        public static readonly Color DailyBtn = new(0.063f, 0.725f, 0.506f, 0.85f);
        public static readonly Color ClaimBtn = new(0.66f, 0.33f, 0.97f, 0.85f);
        public static readonly Color UnequipBtn = new(0.40f, 0.08f, 0.08f, 0.7f);

        public static readonly Color DiceBg = new(0.06f, 0.047f, 0.12f, 0.95f);
        public static readonly Color DiceBorder = new(1f, 0.722f, 0f, 0.6f);
        public static readonly Color DiceGlow = new(1f, 0.722f, 0f, 0.15f);
        public static readonly Color DiceCritical = new(1f, 0.84f, 0f);
        public static readonly Color DiceEpic = new(0.75f, 0.35f, 1f);
        public static readonly Color DiceRare = new(0.38f, 0.65f, 1f);
        public static readonly Color DiceCommon = new(0.55f, 0.58f, 0.64f);

        public static readonly Color GlassPanel = new(0.10f, 0.08f, 0.20f, 0.88f);
        public static readonly Color GlassBorder = new(0.35f, 0.28f, 0.55f, 0.45f);
        public static readonly Color GlowLegendary = new(1f, 0.722f, 0f, 0.18f);
        public static readonly Color GlowEpic = new(0.66f, 0.33f, 0.97f, 0.15f);
        public static readonly Color GlowRare = new(0.38f, 0.65f, 1f, 0.12f);

        public static Color GetAdventureColor(byte type) => type switch
        {
            0 => new Color(0.063f, 0.725f, 0.506f),
            1 => new Color(0.66f, 0.33f, 0.97f),
            2 => new Color(0.937f, 0.267f, 0.267f),
            _ => TextSecondary
        };

        public static Color GetRarityColor(string rarity) => rarity switch
        {
            "Common" => new Color(0.58f, 0.60f, 0.67f),
            "Uncommon" => new Color(0.063f, 0.725f, 0.506f),
            "Rare" => new Color(0.23f, 0.51f, 0.96f),
            "Epic" => new Color(0.66f, 0.33f, 0.97f),
            "Legendary" => new Color(1f, 0.722f, 0f),
            _ => TextSecondary
        };

        public static Color GetRarityColor(ItemData.Rarity rarity) => rarity switch
        {
            ItemData.Rarity.Common => new Color(0.58f, 0.60f, 0.67f),
            ItemData.Rarity.Uncommon => new Color(0.063f, 0.725f, 0.506f),
            ItemData.Rarity.Rare => new Color(0.23f, 0.51f, 0.96f),
            ItemData.Rarity.Epic => new Color(0.66f, 0.33f, 0.97f),
            ItemData.Rarity.Legendary => new Color(1f, 0.722f, 0f),
            _ => TextSecondary
        };

        public static Color GetRarityGlow(ItemData.Rarity rarity) => rarity switch
        {
            ItemData.Rarity.Rare => GlowRare,
            ItemData.Rarity.Epic => GlowEpic,
            ItemData.Rarity.Legendary => GlowLegendary,
            _ => Color.clear
        };

        public static Color GetRarityGlow(string rarity) => rarity switch
        {
            "Rare" => GlowRare,
            "Epic" => GlowEpic,
            "Legendary" => GlowLegendary,
            _ => Color.clear
        };
    }
}
