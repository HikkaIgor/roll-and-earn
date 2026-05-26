using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class RewardPopup : MonoBehaviour
    {
        private GameObject _overlay;
        private GameObject _panel;
        private Image _panelGlow;
        private Image _panelBorder;
        private TMP_Text _titleText;
        private TMP_Text _rollText;
        private TMP_Text _tierText;
        private GameObject _rewardsGroup;
        private TMP_Text _tokenText;
        private TMP_Text _xpText;
        private TMP_Text _specialText;
        private TMP_Text _failText;
        private Button _closeBtn;
        private TMP_Text _closeLabel;
        private Image _closeBtnGlow;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var titleFont = FontProvider.TitleFont ?? FontProvider.DefaultFont;
            var bodyFont = FontProvider.BodyFont ?? FontProvider.DefaultFont;

            _overlay = Create("Overlay", transform);
            var overlayImg = _overlay.AddComponent<Image>();
            overlayImg.color = ThemeColors.Overlay;
            var overlayRect = _overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            var panelOuter = UIHelper.CreateAnchored("PanelOuter", _overlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var outerRect = panelOuter;
            outerRect.pivot = new Vector2(0.5f, 0.5f);
            outerRect.sizeDelta = new Vector2(Mathf.Min(520, Screen.width * 0.92f), 460);

            var glowRect = UIHelper.CreateOutset("PanelGlow", panelOuter, 6f);
            _panelGlow = glowRect.gameObject.AddComponent<Image>();
            _panelGlow.sprite = UIHelper.GetRoundedRect();
            _panelGlow.color = Color.clear;
            _panelGlow.type = Image.Type.Sliced;
            _panelGlow.raycastTarget = false;

            var borderRect = UIHelper.CreateStretch("PanelBorder", panelOuter, 0f);
            _panelBorder = borderRect.gameObject.AddComponent<Image>();
            _panelBorder.sprite = UIHelper.GetRoundedRect();
            _panelBorder.color = ThemeColors.GlassBorder;
            _panelBorder.type = Image.Type.Sliced;

            _panel = Create("Panel", borderRect);
            var panelImg = _panel.AddComponent<Image>();
            panelImg.sprite = UIHelper.GetRoundedRect();
            panelImg.color = ThemeColors.GlassPanel;
            panelImg.type = Image.Type.Sliced;
            var panelInnerRect = _panel.GetComponent<RectTransform>();
            panelInnerRect.anchorMin = Vector2.zero;
            panelInnerRect.anchorMax = Vector2.one;
            panelInnerRect.offsetMin = new Vector2(2, 2);
            panelInnerRect.offsetMax = new Vector2(-2, -2);

            UIHelper.CreateGradientBar(_panel.transform, new Color(1f, 0.722f, 0f, 0.15f),
                new Color(1f, 0.722f, 0f, 0f), 60f);

            var content = new GameObject("Content", typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            content.SetParent(_panel.transform, false);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 24, 24);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            _titleText = MakeText("Title", content, titleFont, 38, ThemeColors.Primary, TextAlignmentOptions.Center);
            _titleText.fontStyle = FontStyles.Bold;

            _rollText = MakeText("Roll", content, bodyFont, 22, ThemeColors.TextPrimary, TextAlignmentOptions.Center);

            var divider = Create("Divider", content);
            var divImg = divider.AddComponent<Image>();
            divImg.sprite = UIHelper.GetRoundedRect();
            divImg.color = new Color(0.35f, 0.28f, 0.55f, 0.3f);
            divImg.type = Image.Type.Sliced;
            var divLayout = divider.AddComponent<LayoutElement>();
            divLayout.preferredHeight = 2;

            _tierText = MakeText("Tier", content, titleFont, 30, ThemeColors.AccentGold, TextAlignmentOptions.Center);
            _tierText.fontStyle = FontStyles.Bold;

            _rewardsGroup = new GameObject("RewardsGroup", typeof(VerticalLayoutGroup));
            _rewardsGroup.GetComponent<RectTransform>().SetParent(content, false);
            var rwVlg = _rewardsGroup.GetComponent<VerticalLayoutGroup>();
            rwVlg.spacing = 6;
            rwVlg.childAlignment = TextAnchor.MiddleCenter;
            rwVlg.childControlWidth = true;
            rwVlg.childControlHeight = true;
            rwVlg.childForceExpandWidth = true;
            rwVlg.childForceExpandHeight = false;

            _tokenText = MakeText("Token", _rewardsGroup.GetComponent<RectTransform>(), bodyFont, 24, ThemeColors.Success, TextAlignmentOptions.Center);
            _tokenText.fontStyle = FontStyles.Bold;
            _xpText = MakeText("XP", _rewardsGroup.GetComponent<RectTransform>(), bodyFont, 22, ThemeColors.Secondary, TextAlignmentOptions.Center);
            _specialText = MakeText("Special", _rewardsGroup.GetComponent<RectTransform>(), titleFont, 20, ThemeColors.AccentGold, TextAlignmentOptions.Center);
            _specialText.fontStyle = FontStyles.Bold;

            _failText = MakeText("Fail", content, bodyFont, 20, ThemeColors.Error, TextAlignmentOptions.Center);

            var spacer = new GameObject("Spacer", typeof(LayoutElement));
            spacer.GetComponent<RectTransform>().SetParent(content, false);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1;

            var btnContainer = new GameObject("BtnContainer", typeof(RectTransform));
            btnContainer.GetComponent<RectTransform>().SetParent(content, false);
            var btnContainerLayout = btnContainer.AddComponent<LayoutElement>();
            btnContainerLayout.preferredHeight = 54;

            var btnGlowRect = UIHelper.CreateOutset("BtnGlow", btnContainer.transform, 4f);
            _closeBtnGlow = btnGlowRect.gameObject.AddComponent<Image>();
            _closeBtnGlow.sprite = UIHelper.GetRoundedRect();
            _closeBtnGlow.color = Color.clear;
            _closeBtnGlow.type = Image.Type.Sliced;
            _closeBtnGlow.raycastTarget = false;

            var btnBorderRect = UIHelper.CreateStretch("BtnBorder", btnContainer.transform, 0f);
            var btnBorderImg = btnBorderRect.gameObject.AddComponent<Image>();
            btnBorderImg.sprite = UIHelper.GetRoundedRect();
            btnBorderImg.color = ThemeColors.Primary;
            btnBorderImg.type = Image.Type.Sliced;

            var btnBgRect = UIHelper.CreateStretch("BtnBg", btnBorderRect.transform, 2f);
            var btnBgImg = btnBgRect.gameObject.AddComponent<Image>();
            btnBgImg.sprite = UIHelper.GetRoundedRect();
            btnBgImg.color = ThemeColors.Primary;
            btnBgImg.type = Image.Type.Sliced;

            _closeBtn = btnBgRect.gameObject.AddComponent<Button>();
            var btnBgRectParent = _closeBtn.GetComponent<RectTransform>();
            _closeBtn.targetGraphic = btnBgImg;

            _closeLabel = MakeText("CloseLabel", btnBgRectParent, titleFont, 20, new Color(0.05f, 0.04f, 0.10f), TextAlignmentOptions.Center);
            _closeLabel.text = "CONTINUE";
            _closeLabel.fontStyle = FontStyles.Bold;
            _closeBtn.onClick.AddListener(Hide);

            _overlay.SetActive(false);
        }

        private static GameObject Create(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.GetComponent<RectTransform>().SetParent(parent, false);
            return go;
        }

        private static TMP_Text MakeText(string name, RectTransform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.text = "";
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = size + 10;
            return tmp;
        }

        public void Show(RollResult result)
        {
            if (_titleText == null) return;

            _titleText.text = "VICTORY!";
            _titleText.color = ThemeColors.Primary;

            if (_panelBorder != null) _panelBorder.color = new Color(1f, 0.722f, 0f, 0.5f);
            if (_panelGlow != null) _panelGlow.color = new Color(1f, 0.722f, 0f, 0.12f);

            string bonusStr = result.equipmentBonus > 0 ? $" (+{result.equipmentBonus} equip)" : "";
            if (_rollText != null)
            {
                _rollText.text = $"D20: {result.rollValue}{bonusStr} = {result.effectiveRoll}";
                _rollText.gameObject.SetActive(true);
            }

            string tier = result.tier ?? "Low";
            if (_tierText != null)
            {
                _tierText.text = tier.ToUpper();
                _tierText.color = tier switch
                {
                    "Special" => ThemeColors.AccentGold,
                    "High" => ThemeColors.Primary,
                    "Mid" => ThemeColors.Secondary,
                    _ => ThemeColors.TextSecondary
                };
                _tierText.gameObject.SetActive(true);
            }

            if (_tokenText != null) _tokenText.text = $"+{result.tokenAmount / 1e9:F2} ROLAND";
            if (_xpText != null) _xpText.text = $"+{result.xpGained} XP";
            if (_rewardsGroup != null) _rewardsGroup.gameObject.SetActive(true);

            if (_specialText != null)
            {
                if (result.isSpecial)
                {
                    _specialText.text = "SPECIAL ITEM FOUND!";
                    _specialText.gameObject.SetActive(true);
                }
                else
                {
                    _specialText.gameObject.SetActive(false);
                }
            }

            if (_failText != null) _failText.gameObject.SetActive(false);
            if (_closeLabel != null) _closeLabel.text = "CONTINUE";
            if (_closeBtnGlow != null) _closeBtnGlow.color = new Color(1f, 0.722f, 0f, 0.10f);

            SoundManager.Instance.PlayReward();
            if (_overlay != null) _overlay.SetActive(true);
        }

        public void ShowFailure(string reason)
        {
            if (_titleText == null) return;
            _titleText.text = "DEFEAT";
            _titleText.color = ThemeColors.Error;

            if (_panelBorder != null) _panelBorder.color = new Color(0.937f, 0.267f, 0.267f, 0.5f);
            if (_panelGlow != null) _panelGlow.color = new Color(0.937f, 0.267f, 0.267f, 0.10f);

            if (_rollText != null) _rollText.gameObject.SetActive(false);
            if (_tierText != null) _tierText.gameObject.SetActive(false);
            if (_rewardsGroup != null) _rewardsGroup.gameObject.SetActive(false);
            if (_specialText != null) _specialText.gameObject.SetActive(false);

            if (_failText != null)
            {
                _failText.text = reason ?? "Unknown error";
                _failText.gameObject.SetActive(true);
            }

            if (_closeLabel != null) _closeLabel.text = "TRY AGAIN";
            if (_closeBtnGlow != null) _closeBtnGlow.color = new Color(0.937f, 0.267f, 0.267f, 0.10f);

            SoundManager.Instance.PlayFail();
            if (_overlay != null) _overlay.SetActive(true);
        }

        public void Hide()
        {
            _overlay.SetActive(false);
        }

        public bool IsVisible => _overlay != null && _overlay.activeSelf;
    }
}
