using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image artImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image glowImage;

        private static Sprite _defaultCharacterSprite;
        private static Sprite _defaultItemSprite;
        private static Sprite _defaultAdventureSprite;

        public static void SetDefaultSprites(Sprite character, Sprite item, Sprite adventure)
        {
            _defaultCharacterSprite = character;
            _defaultItemSprite = item;
            _defaultAdventureSprite = adventure;
        }

        private void Awake()
        {
            if (artImage == null || nameText == null || statsText == null || borderImage == null)
                CreateRuntimeUI();
        }

        private void CreateRuntimeUI()
        {
            var glowRect = UIHelper.CreateOutset("Glow", transform, 4f);
            glowImage = glowRect.gameObject.AddComponent<Image>();
            glowImage.sprite = UIHelper.GetRoundedRect();
            glowImage.color = Color.clear;
            glowImage.type = Image.Type.Sliced;
            glowImage.raycastTarget = false;

            var borderRect = UIHelper.CreateStretch("Border", transform, 0f);
            borderImage = borderRect.gameObject.AddComponent<Image>();
            borderImage.sprite = UIHelper.GetRoundedRect();
            borderImage.color = ThemeColors.CardBorder;
            borderImage.type = Image.Type.Sliced;

            var innerRect = UIHelper.CreateStretch("Inner", borderRect, 2f);
            var innerImg = innerRect.gameObject.AddComponent<Image>();
            innerImg.sprite = UIHelper.GetRoundedRect();
            innerImg.color = ThemeColors.CardBg;
            innerImg.type = Image.Type.Sliced;

            var artRect = UIHelper.CreateAnchored("Art", innerRect,
                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.95f));
            artImage = artRect.gameObject.AddComponent<Image>();
            artImage.sprite = UIHelper.GetRoundedRect();
            artImage.color = new Color(0.04f, 0.03f, 0.08f);
            artImage.type = Image.Type.Sliced;
            artImage.preserveAspect = true;

            var nameRect = UIHelper.CreateAnchored("Name", innerRect,
                new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.36f));
            nameText = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = 14;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = ThemeColors.TextPrimary;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            var statsRect = UIHelper.CreateAnchored("Stats", innerRect,
                new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.17f));
            statsText = statsRect.gameObject.AddComponent<TextMeshProUGUI>();
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.fontSize = 11;
            statsText.color = ThemeColors.TextSecondary;
            statsText.textWrappingMode = TextWrappingModes.NoWrap;
            statsText.overflowMode = TextOverflowModes.Ellipsis;
        }

        public void DisplayCharacter(CharacterData data)
        {
            if (artImage != null)
            {
                artImage.sprite = data.artSprite != null ? data.artSprite : _defaultCharacterSprite;
                artImage.color = data.artSprite != null ? Color.white : GetClassColor(data.classType);
                artImage.type = data.artSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            }
            if (nameText != null)
                nameText.text = $"<b>{data.characterName}</b>";
            if (statsText != null)
                statsText.text = $"Lv {data.level}  |  STR {data.strength}  AGI {data.agility}\nINT {data.intelligence}  LCK {data.luck}  |  XP {data.xp}/{data.xpToNextLevel}";
            SetBorderColor(ThemeColors.Primary);
            SetGlowColor(ThemeColors.GoldGlow);
        }

        public void DisplayItem(ItemData data)
        {
            if (artImage != null)
            {
                artImage.sprite = data.artSprite != null ? data.artSprite : _defaultItemSprite;
                artImage.color = data.artSprite != null ? Color.white : ThemeColors.GetRarityColor(data.rarity);
                artImage.type = data.artSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            }
            if (nameText != null)
                nameText.text = $"<b>{data.itemName}</b>";
            if (statsText != null)
                statsText.text = $"{data.type}  |  {data.rarity}\n{data.statBonus}  |  Roll +{data.rollBonus}";
            SetBorderColor(ThemeColors.GetRarityColor(data.rarity));
            SetGlowColor(ThemeColors.GetRarityGlow(data.rarity));
        }

        public void DisplayAdventure(AdventureConfigSO data)
        {
            if (artImage != null)
            {
                artImage.sprite = data.artSprite != null ? data.artSprite : _defaultAdventureSprite;
                artImage.color = data.artSprite != null ? Color.white : ThemeColors.GetAdventureColor(data.adventureType);
                artImage.type = data.artSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            }
            if (nameText != null)
                nameText.text = $"<b>{data.adventureName}</b>";
            if (statsText != null)
            {
                string costStr = data.cost == 0 ? "Free" : $"{data.cost / 1e9:F0} ROLAND";
                int cdMin = data.cooldownSeconds / 60;
                int cdSec = data.cooldownSeconds % 60;
                string cdStr = cdMin > 0 ? $"{cdMin}m" : $"{cdSec}s";
                statsText.text = $"Cost: {costStr}  |  CD: {cdStr}\n{data.description}";
            }
            SetBorderColor(ThemeColors.GetAdventureColor(data.adventureType));
            SetGlowColor(Color.clear);
        }

        public void Clear()
        {
            if (artImage != null) { artImage.sprite = UIHelper.GetRoundedRect(); artImage.color = Color.clear; artImage.type = Image.Type.Sliced; }
            if (nameText != null) nameText.text = string.Empty;
            if (statsText != null) statsText.text = string.Empty;
            SetBorderColor(ThemeColors.CardBorder);
            SetGlowColor(Color.clear);
        }

        private void SetBorderColor(Color color)
        {
            if (borderImage != null) borderImage.color = color;
        }

        private void SetGlowColor(Color color)
        {
            if (glowImage != null) glowImage.color = color;
        }

        private static Color GetClassColor(byte classType) => classType switch
        {
            0 => ThemeColors.ClassWarrior,
            1 => ThemeColors.ClassRogue,
            2 => ThemeColors.ClassMage,
            _ => ThemeColors.Surface
        };
    }
}
