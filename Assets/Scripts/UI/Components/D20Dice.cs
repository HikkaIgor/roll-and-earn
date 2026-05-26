using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class D20Dice : MonoBehaviour
    {
        [SerializeField] private float rollDuration = 2.0f;
        [SerializeField] private float settleDuration = 0.6f;

        private TMP_Text _diceText;
        private TMP_Text _subLabel;
        private Image _bgImage;
        private Image _glowImage;
        private Image _borderImage;
        private RectTransform _rectTransform;
        private bool _isRolling;
        private float _idleTime;

        private static readonly Color BgBase = new(0.06f, 0.047f, 0.12f, 0.95f);
        private static readonly Color BorderBase = new(1f, 0.722f, 0f, 0.70f);
        private static readonly Color GlowBase = new(1f, 0.722f, 0f, 0.18f);

        public static D20Dice Create(Transform parent)
        {
            var go = new GameObject("D20Dice", typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(200, 200);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 40);
            return go.AddComponent<D20Dice>();
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();

            if (_rectTransform.sizeDelta.magnitude < 1f)
                _rectTransform.sizeDelta = new Vector2(200, 200);

            BuildUI();
        }

        private void BuildUI()
        {
            Sprite diceSprite = LoadDiceSprite();

            var glow = CreateChild("Glow", transform);
            glow.localScale = Vector3.one * 1.15f;
            _glowImage = glow.gameObject.AddComponent<Image>();
            _glowImage.sprite = diceSprite;
            _glowImage.color = GlowBase;
            _glowImage.preserveAspect = true;
            _glowImage.raycastTarget = false;

            var border = CreateChild("Border", transform);
            border.localScale = Vector3.one * 1.05f;
            _borderImage = border.gameObject.AddComponent<Image>();
            _borderImage.sprite = diceSprite;
            _borderImage.color = BorderBase;
            _borderImage.preserveAspect = true;
            _borderImage.raycastTarget = false;

            var bg = CreateChild("Body", transform);
            _bgImage = bg.gameObject.AddComponent<Image>();
            _bgImage.sprite = diceSprite;
            _bgImage.color = BgBase;
            _bgImage.preserveAspect = true;
            _bgImage.raycastTarget = false;

            var textGo = new GameObject("DiceText", typeof(RectTransform));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20, -30);
            textRect.offsetMin = new Vector2(10, 20);
            textRect.offsetMax = new Vector2(-10, -10);

            _diceText = textGo.AddComponent<TextMeshProUGUI>();
            var font = FontProvider.TitleFont ?? TMP_Settings.defaultFontAsset;
            if (font != null) _diceText.font = font;
            _diceText.fontSize = 64;
            _diceText.fontStyle = FontStyles.Bold;
            _diceText.color = ThemeColors.Primary;
            _diceText.alignment = TextAlignmentOptions.Center;
            _diceText.text = "?";
            _diceText.raycastTarget = false;

            var subGo = new GameObject("SubLabel", typeof(RectTransform));
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.SetParent(transform, false);
            subRect.anchorMin = new Vector2(0, 0);
            subRect.anchorMax = new Vector2(1, 0.30f);
            subRect.offsetMin = new Vector2(4, 0);
            subRect.offsetMax = new Vector2(-4, 0);

            _subLabel = subGo.AddComponent<TextMeshProUGUI>();
            if (font != null) _subLabel.font = font;
            _subLabel.fontSize = 13;
            _subLabel.fontStyle = FontStyles.Normal;
            _subLabel.color = new Color(0.55f, 0.52f, 0.65f, 0.75f);
            _subLabel.alignment = TextAlignmentOptions.Center;
            _subLabel.text = "D20";
            _subLabel.raycastTarget = false;
        }

        private static Sprite LoadDiceSprite()
        {
            var sprites = Resources.LoadAll<Sprite>("RollAndEarn/Sprites/icosahedron");
            if (sprites != null && sprites.Length > 0)
                return sprites[0];
            Debug.LogWarning("[D20Dice] icosahedron sprite not found in Resources");
            return null;
        }

        private static RectTransform CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private void Update()
        {
            if (_isRolling) return;
            _idleTime += Time.deltaTime;

            float breathe = 1f + Mathf.Sin(_idleTime * 2f) * 0.012f;
            _rectTransform.localScale = Vector3.one * breathe;

            float glowPulse = 0.18f + Mathf.Sin(_idleTime * 1.5f) * 0.08f;
            if (_glowImage != null)
                _glowImage.color = new Color(_glowImage.color.r, _glowImage.color.g, _glowImage.color.b, glowPulse);

            float hue = (_idleTime * 0.08f) % 1f;
            Color shimmer = Color.HSVToRGB(hue, 0.4f, 1f);
            if (_borderImage != null)
                _borderImage.color = new Color(shimmer.r, shimmer.g, shimmer.b,
                    0.50f + Mathf.Sin(_idleTime * 3f) * 0.15f);
        }

        public async UniTask PlayRollAsync(byte result)
        {
            if (_isRolling) return;
            _isRolling = true;
            _idleTime = 0f;

            SoundManager.Instance.PlayDiceRoll();

            _bgImage.color = BgBase;
            _diceText.color = ThemeColors.Primary;
            if (_subLabel != null) _subLabel.text = "D20";
            if (_glowImage != null) _glowImage.color = GlowBase;
            if (_borderImage != null) _borderImage.color = BorderBase;

            float elapsed = 0f;
            float nextCycle = 0f;
            bool showedRolling = false;

            while (elapsed < rollDuration)
            {
                float t = elapsed / rollDuration;
                float cycleInterval = Mathf.Lerp(0.04f, 0.25f, t * t);
                nextCycle += Time.deltaTime;

                if (nextCycle >= cycleInterval)
                {
                    nextCycle = 0f;
                    byte randomFace = (byte)Random.Range(1, 21);
                    _diceText.text = randomFace.ToString();
                    float angle = Random.Range(-20f, 20f);
                    _rectTransform.localEulerAngles = new Vector3(0, 0, angle);
                    SoundManager.Instance.PlayDiceTick();
                }

                float scale = 1f + Mathf.Sin(elapsed * 12f) * 0.08f * (1f - t);
                _rectTransform.localScale = Vector3.one * scale;

                float flashHue = (elapsed * 3f) % 1f;
                Color flashColor = Color.HSVToRGB(flashHue, 0.7f, 0.9f);
                if (_glowImage != null)
                    _glowImage.color = new Color(flashColor.r, flashColor.g, flashColor.b,
                        0.30f + Mathf.Sin(elapsed * 8f) * 0.12f);

                if (t > 0.7f && !showedRolling)
                {
                    showedRolling = true;
                    if (_subLabel != null)
                    {
                        _subLabel.text = "ROLLING...";
                        _subLabel.color = new Color(0.66f, 0.33f, 0.97f, 0.9f);
                    }
                }

                await UniTask.Yield();
                elapsed += Time.deltaTime;
            }

            _diceText.text = result.ToString();
            Color targetColor = GetResultColor(result);
            SoundManager.Instance.PlayDiceResult(result);
            _bgImage.color = BgBase;
            if (_subLabel != null)
            {
                _subLabel.text = GetTierName(result);
                _subLabel.color = targetColor;
            }

            Color glowTarget = GetResultGlow(result);
            if (_glowImage != null)
                _glowImage.color = new Color(glowTarget.r, glowTarget.g, glowTarget.b, 0.40f);
            if (_borderImage != null)
                _borderImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.85f);

            elapsed = 0f;
            var startScale = _rectTransform.localScale;
            var startAngle = _rectTransform.localEulerAngles.z;
            while (elapsed < settleDuration)
            {
                float t = elapsed / settleDuration;
                float ease = 1f - Mathf.Pow(1f - t, 3);
                float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.10f;
                _rectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * overshoot, ease);
                _rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(startAngle, 0, ease));
                _diceText.color = Color.Lerp(ThemeColors.Primary, targetColor, ease);

                if (_glowImage != null)
                {
                    float glowA = Mathf.Lerp(0.40f, 0.55f, ease);
                    _glowImage.color = new Color(glowTarget.r, glowTarget.g, glowTarget.b, glowA);
                }
                await UniTask.Yield();
                elapsed += Time.deltaTime;
            }

            _rectTransform.localScale = Vector3.one * 1.10f;
            _rectTransform.localEulerAngles = Vector3.zero;
            _diceText.color = targetColor;

            float impactElapsed = 0f;
            while (impactElapsed < 0.3f)
            {
                float it = impactElapsed / 0.3f;
                float easeOut = 1f - Mathf.Pow(1f - it, 2);
                _rectTransform.localScale = Vector3.one * Mathf.Lerp(1.10f, 1f, easeOut);
                await UniTask.Yield();
                impactElapsed += Time.deltaTime;
            }
            _rectTransform.localScale = Vector3.one;

            if (result >= 16)
            {
                float shimmerTime = 0f;
                float shimmerDuration = 1.5f;
                while (shimmerTime < shimmerDuration)
                {
                    float st = shimmerTime / shimmerDuration;
                    float hue = (shimmerTime * 2f) % 1f;
                    Color holo = Color.HSVToRGB(hue, 0.6f, 1f);
                    float fadeAlpha = (1f - st) * 0.5f;
                    if (_glowImage != null)
                        _glowImage.color = new Color(holo.r, holo.g, holo.b, fadeAlpha + 0.12f);
                    if (_borderImage != null)
                        _borderImage.color = new Color(holo.r, holo.g, holo.b, fadeAlpha + 0.35f);
                    float pulse = 1f + Mathf.Sin(shimmerTime * 8f) * 0.04f * (1f - st);
                    _rectTransform.localScale = Vector3.one * pulse;
                    await UniTask.Yield();
                    shimmerTime += Time.deltaTime;
                }
            }
            else
            {
                float pulseElapsed = 0f;
                while (pulseElapsed < 0.8f)
                {
                    float pulse = 1f + Mathf.Sin(pulseElapsed * 6f) * 0.03f * (1f - pulseElapsed / 0.8f);
                    _rectTransform.localScale = Vector3.one * pulse;
                    await UniTask.Yield();
                    pulseElapsed += Time.deltaTime;
                }
            }

            _rectTransform.localScale = Vector3.one;
            _isRolling = false;
        }

        private static string GetTierName(byte roll)
        {
            if (roll >= 20) return "CRITICAL!";
            if (roll >= 16) return "EPIC";
            if (roll >= 11) return "RARE";
            if (roll >= 6) return "UNCOMMON";
            return "COMMON";
        }

        private static Color GetResultColor(byte roll)
        {
            if (roll >= 20) return ThemeColors.AccentGold;
            if (roll >= 16) return new Color(0.75f, 0.35f, 1f);
            if (roll >= 11) return new Color(0.38f, 0.65f, 1f);
            if (roll >= 6) return ThemeColors.Primary;
            return new Color(0.55f, 0.58f, 0.64f);
        }

        private static Color GetResultGlow(byte roll)
        {
            if (roll >= 20) return new Color(1f, 0.84f, 0f);
            if (roll >= 16) return new Color(0.6f, 0.3f, 1f);
            if (roll >= 11) return new Color(0.3f, 0.55f, 1f);
            if (roll >= 6) return new Color(0.2f, 0.75f, 0.4f);
            return new Color(0.4f, 0.42f, 0.48f);
        }
    }
}
