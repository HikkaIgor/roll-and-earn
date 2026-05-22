using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class AdventureScreen : MonoBehaviour
    {
        [SerializeField] private Button[] adventureButtons = new Button[3];
        [SerializeField] private TMP_Text[] adventureCooldownTexts = new TMP_Text[3];
        [SerializeField] private Button rollButton;
        [SerializeField] private TMP_Text rollStatusText;
        [SerializeField] private D20Dice d20Dice;
        [SerializeField] private RewardPopup rewardPopup;
        [SerializeField] private Button claimItemButton;
        [SerializeField] private TMP_Text unclaimedItemsText;
        [SerializeField] private TMP_Text equipmentBonusText;

        private TMP_Text _balanceText;

        private TokenManager _tokenManager;
        private AnchorClient _anchorClient;
        private AdventureManager _adventureManager;
        private CooldownManager _cooldownManager;
        private AdventureConfigSO[] _adventures;
        private int _selectedAdventureIndex = 0;

        private void Awake()
        {
            _tokenManager = FindAnyObjectByType<TokenManager>();
            _anchorClient = FindAnyObjectByType<AnchorClient>();
            _adventureManager = FindAnyObjectByType<AdventureManager>();
            _cooldownManager = FindAnyObjectByType<CooldownManager>();
        }

        private void Start()
        {
            if (_tokenManager == null) _tokenManager = FindAnyObjectByType<TokenManager>();
            if (_anchorClient == null) _anchorClient = FindAnyObjectByType<AnchorClient>();
            if (_adventureManager == null) _adventureManager = FindAnyObjectByType<AdventureManager>();
            if (_cooldownManager == null) _cooldownManager = FindAnyObjectByType<CooldownManager>();
            var cfg = GameConfigProvider.Instance != null ? GameConfigProvider.Instance.Config : null;
            if (cfg != null) _adventures = cfg.Adventures;

            for (int i = 0; i < adventureButtons.Length; i++)
            {
                if (adventureButtons[i] == null) continue;
                int idx = i;
                adventureButtons[i].onClick.AddListener(() => OnAdventureSelected(idx));
            }

            if (claimItemButton != null) claimItemButton.gameObject.SetActive(false);

            CreateBalanceDisplay();

            if (d20Dice == null)
                d20Dice = D20Dice.Create(transform);
        }

        private void CreateBalanceDisplay()
        {
            var container = new GameObject("BalanceDisplay", typeof(RectTransform));
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.SetParent(transform, false);
            containerRect.anchorMin = new Vector2(1f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            containerRect.anchoredPosition = new Vector2(-16, -16);
            containerRect.sizeDelta = new Vector2(220, 44);

            var glowRect = UIHelper.CreateOutset("Glow", container.transform, 3f);
            var glowImg = glowRect.gameObject.AddComponent<Image>();
            glowImg.sprite = UIHelper.GetRoundedRect();
            glowImg.color = new Color(1f, 0.722f, 0f, 0.08f);
            glowImg.type = Image.Type.Sliced;
            glowImg.raycastTarget = false;

            var borderRect = UIHelper.CreateStretch("Border", container.transform, 0f);
            var borderImg = borderRect.gameObject.AddComponent<Image>();
            borderImg.sprite = UIHelper.GetRoundedRect();
            borderImg.color = new Color(0.35f, 0.28f, 0.55f, 0.4f);
            borderImg.type = Image.Type.Sliced;

            var bgRect = UIHelper.CreateStretch("Bg", borderRect, 1f);
            var bgImg = bgRect.gameObject.AddComponent<Image>();
            bgImg.sprite = UIHelper.GetRoundedRect();
            bgImg.color = new Color(0.10f, 0.08f, 0.20f, 0.92f);
            bgImg.type = Image.Type.Sliced;

            var textGo = new GameObject("BalanceText", typeof(RectTransform));
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.SetParent(bgRect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 4);
            textRect.offsetMax = new Vector2(-10, -4);

            _balanceText = textGo.AddComponent<TextMeshProUGUI>();
            var font = TMP_Settings.defaultFontAsset;
            if (font != null) _balanceText.font = font;
            _balanceText.fontSize = 18;
            _balanceText.fontStyle = FontStyles.Bold;
            _balanceText.color = ThemeColors.AccentGold;
            _balanceText.alignment = TextAlignmentOptions.MidlineRight;
            _balanceText.text = "ROLAND: --";
        }

        private void OnEnable()
        {
            rollButton?.onClick.AddListener(OnRollClicked);
            claimItemButton?.onClick.AddListener(OnClaimItemClicked);
            if (_tokenManager != null)
                _tokenManager.OnBalanceUpdated += OnBalanceUpdated;
            var profile = SolanaManager.Instance?.CurrentProfile;
            if (profile != null && _cooldownManager != null)
                _cooldownManager.SyncFromProfile(profile);
            RefreshAdventures();
            RefreshUnclaimed();
            UpdateEquipmentBonus();
            RefreshBalance();
            SyncValidatorTimeAsync().Forget();
        }

        private async UniTaskVoid SyncValidatorTimeAsync()
        {
            try
            {
                var rpc = SolanaManager.Instance.GetRpcClient();
                var slotResp = await rpc.GetBlockHeightAsync(Solana.Unity.Rpc.Types.Commitment.Processed);
                if (slotResp?.Result != null)
                {
                    var timeResp = await rpc.GetBlockTimeAsync(slotResp.Result);
                    if (timeResp?.Result != null)
                        PlayerProfile.ValidatorTime = (long)timeResp.Result;
                }
            }
            catch { }
        }

        private void OnDisable()
        {
            rollButton?.onClick.RemoveAllListeners();
            claimItemButton?.onClick.RemoveAllListeners();
            if (_tokenManager != null)
                _tokenManager.OnBalanceUpdated -= OnBalanceUpdated;
        }

        private void OnBalanceUpdated(ulong raw)
        {
            if (_balanceText != null)
                _balanceText.text = $"ROLAND: {raw / 1e9:F2}";
        }

        private void RefreshBalance()
        {
            if (_tokenManager != null && _balanceText != null)
                _balanceText.text = $"ROLAND: {_tokenManager.RawBalance / 1e9:F2}";
        }

        private void Update()
        {
            for (int i = 0; i < adventureCooldownTexts.Length; i++)
            {
                if (adventureCooldownTexts[i] == null || _cooldownManager == null) continue;
                if (_cooldownManager.IsOnCooldown((byte)i))
                {
                    int rem = (int)_cooldownManager.GetRemainingSeconds((byte)i);
                    adventureCooldownTexts[i].text = $"CD: {rem / 60:D1}:{rem % 60:D2}";
                    adventureCooldownTexts[i].color = ThemeColors.Error;
                }
                else
                {
                    adventureCooldownTexts[i].text = "Ready!";
                    adventureCooldownTexts[i].color = ThemeColors.Success;
                }
            }
        }

        private void RefreshAdventures()
        {
            if (_adventures == null) return;
            for (int i = 0; i < adventureButtons.Length && i < _adventures.Length; i++)
            {
                if (adventureButtons[i] == null) continue;
                var adv = _adventures[i];
                var label = adventureButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    string costStr = adv.cost == 0 ? "Free" : $"{adv.cost / 1e9:F0} ROLAND";
                    label.text = $"{adv.adventureName}\n{costStr} | {adv.description}";
                }

                var colors = adventureButtons[i].colors;
                colors.normalColor = i == _selectedAdventureIndex ? ThemeColors.Selected : ThemeColors.Unselected;
                adventureButtons[i].colors = colors;
            }
        }

        private void OnAdventureSelected(int index)
        {
            _selectedAdventureIndex = index;
            RefreshAdventures();
        }

        private void UpdateEquipmentBonus()
        {
            if (equipmentBonusText == null) return;
            var profile = SolanaManager.Instance?.CurrentProfile;
            if (profile != null && profile.TotalEquipmentBonus > 0)
            {
                equipmentBonusText.text = $"Equipment Bonus: +{profile.TotalEquipmentBonus}";
                equipmentBonusText.color = ThemeColors.Primary;
            }
            else
            {
                equipmentBonusText.text = "";
            }
        }

        private async void OnRollClicked()
        {
            if (_adventures == null || _selectedAdventureIndex < 0 || _selectedAdventureIndex >= _adventures.Length) return;

            var config = _adventures[_selectedAdventureIndex];
            byte adventureType = (byte)_selectedAdventureIndex;

            if (_cooldownManager != null && _cooldownManager.IsOnCooldown(adventureType))
            {
                if (rollStatusText != null) rollStatusText.text = "Adventure on cooldown!";
                return;
            }

            if (_tokenManager.RawBalance < config.CostRaw)
            {
                if (rollStatusText != null) rollStatusText.text = "Not enough ROLAND!";
                return;
            }

            rollButton.interactable = false;
            if (rollStatusText != null) rollStatusText.text = "Rolling D20...";

            RollResult result = null;
            try
            {
                result = await _adventureManager.ExecuteRollAsync(adventureType);

                if (_cooldownManager != null && SolanaManager.Instance?.CurrentProfile != null)
                    _cooldownManager.SyncFromProfile(SolanaManager.Instance.CurrentProfile);
            }
            catch (Exception e)
            {
                if (rollStatusText != null) rollStatusText.text = $"Roll failed: {e.Message}";
                Debug.LogError($"[Adventure] Roll tx error: {e.Message}");
                if (rewardPopup != null)
                    rewardPopup.ShowFailure(e.Message);
                rollButton.interactable = true;
                return;
            }

            try
            {
                if (d20Dice != null)
                    await d20Dice.PlayRollAsync(result.rollValue);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Adventure] Dice animation error: {e.Message}");
            }

            try
            {
                if (rewardPopup != null)
                    rewardPopup.Show(result);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Adventure] Popup error: {e.Message}");
            }

            try
            {
                if (rollStatusText != null)
                {
                    string tierStr = result.tier ?? "Unknown";
                    string bonusStr = result.equipmentBonus > 0
                        ? $" (+{result.equipmentBonus} equip)"
                        : "";
                    rollStatusText.text = $"Rolled {result.rollValue}{bonusStr} = {result.effectiveRoll} — {tierStr}!";
                }

                RefreshUnclaimed();
                UpdateEquipmentBonus();
                RefreshBalance();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Adventure] Post-roll UI error: {e.Message}");
            }

            rollButton.interactable = true;
        }

        private void RefreshUnclaimed()
        {
            var profile = SolanaManager.Instance?.CurrentProfile;
            if (profile == null) return;
            byte unclaimed = profile.unclaimedSpecials;
            if (unclaimedItemsText != null)
                unclaimedItemsText.text = unclaimed > 0 ? $"Unclaimed items: {unclaimed}" : "";
            if (claimItemButton != null)
                claimItemButton.gameObject.SetActive(unclaimed > 0);
        }

        private async void OnClaimItemClicked()
        {
            if (claimItemButton != null) claimItemButton.interactable = false;
            try
            {
                var profile = SolanaManager.Instance.CurrentProfile;
                if (profile == null || profile.unclaimedSpecials == 0)
                {
                    RefreshUnclaimed();
                    if (rollStatusText != null) rollStatusText.text = "No items to claim.";
                    return;
                }

                var player = new Solana.Unity.Wallet.PublicKey(SolanaManager.Instance.PublicKey);
                byte itemsMinted = profile.itemsMinted;

                string itemName = $"RAE Item #{itemsMinted}";
                string symbol = "RAE_ITEM";
                string uri = "";

                var ix = _anchorClient.BuildClaimItemInstruction(player, itemName, symbol, uri, itemsMinted);
                await _anchorClient.SendTransactionAsync(new[] { ix });

                await UniTask.Delay(1500);

                var profilePda = _anchorClient.DerivePlayerProfilePda(player);
                var rpc = SolanaManager.Instance.GetRpcClient();
                var acct = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acct?.Result?.Value?.Data != null)
                {
                    var data = Convert.FromBase64String(acct.Result.Value.Data[0]);
                    SolanaManager.Instance.CurrentProfile = PlayerProfile.DeserializeFromAccount(data);
                }

                RefreshUnclaimed();
                if (rollStatusText != null) rollStatusText.text = "Item claimed! Check your inventory.";

                var invScreen = FindAnyObjectByType<InventoryScreen>();
                if (invScreen != null) invScreen.ReloadInventory();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Adventure] Claim error: {e.Message}");
                if (rollStatusText != null) rollStatusText.text = $"Claim error: {e.Message}";
                if (claimItemButton != null) claimItemButton.interactable = true;
            }
        }
    }
}
