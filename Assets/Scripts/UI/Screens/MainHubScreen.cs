using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class MainHubScreen : MonoBehaviour
    {
        [SerializeField] private CardView characterCard;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text solBalanceText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text xpText;
        [SerializeField] private Slider xpBar;
        [SerializeField] private Button adventureButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button walletButton;
        [SerializeField] private Button dailyRewardButton;
        [SerializeField] private TMP_Text dailyRewardText;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private TMP_Text levelUpCostText;
        [SerializeField] private Button faucetButton;
        [SerializeField] private TMP_Text equipmentBonusText;

        private TokenManager _tokenManager;
        private AnchorClient _anchorClient;
        private ScreenManager _screenManager;
        private CharacterClassSO[] _classes;

        private void Awake()
        {
            _tokenManager = FindAnyObjectByType<TokenManager>();
            _anchorClient = FindAnyObjectByType<AnchorClient>();
            _screenManager = FindAnyObjectByType<ScreenManager>();
        }

        private void Start()
        {
            if (_screenManager == null) _screenManager = FindAnyObjectByType<ScreenManager>();
            if (_anchorClient == null) _anchorClient = FindAnyObjectByType<AnchorClient>();
            if (_tokenManager == null) _tokenManager = FindAnyObjectByType<TokenManager>();
            if (xpBar != null) xpBar.interactable = false;
            var cfg = GameConfigProvider.Instance != null ? GameConfigProvider.Instance.Config : null;
            if (cfg != null) _classes = cfg.CharacterClasses;
        }

        private void OnEnable()
        {
            adventureButton?.onClick.AddListener(() => _screenManager?.ShowScreen("Adventure"));
            inventoryButton?.onClick.AddListener(() => _screenManager?.ShowScreen("Inventory"));
            walletButton?.onClick.AddListener(() => _screenManager?.ShowScreen("WalletProfile"));
            dailyRewardButton?.onClick.AddListener(OnDailyRewardClicked);
            levelUpButton?.onClick.AddListener(OnLevelUpClicked);
            faucetButton?.onClick.AddListener(OnFaucetClicked);
            RefreshAsync().Forget();
        }

        private void OnDisable()
        {
            adventureButton?.onClick.RemoveAllListeners();
            inventoryButton?.onClick.RemoveAllListeners();
            walletButton?.onClick.RemoveAllListeners();
            dailyRewardButton?.onClick.RemoveAllListeners();
            levelUpButton?.onClick.RemoveAllListeners();
            faucetButton?.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            var profile = SolanaManager.Instance?.CurrentProfile;
            if (profile == null) return;

            if (dailyRewardButton != null)
                dailyRewardButton.interactable = profile.CanClaimDailyReward;

            if (dailyRewardText != null)
            {
                if (profile.CanClaimDailyReward)
                {
                    ulong nextReward = profile.NextDailyRewardAmount / 1_000_000_000;
                    dailyRewardText.text = $"Daily Reward: {nextReward} ROLAND\nStreak: {profile.dailyStreak} days";
                }
                else
                {
                    long secs = profile.SecondsUntilDailyClaim;
                    int h = (int)(secs / 3600);
                    int m = (int)((secs % 3600) / 60);
                    int s = (int)(secs % 60);
                    dailyRewardText.text = $"Next reward in: {h:D2}:{m:D2}:{s:D2}\nStreak: {profile.dailyStreak} days";
                }
            }
        }

        private async UniTask RefreshAsync()
        {
            try
            {
                await _tokenManager.RefreshBalanceAsync();
                await SolanaManager.Instance.RefreshSolBalanceAsync();
                await FetchProfileAsync();
                UpdateUI();
            }
            catch (Exception e) { Debug.LogWarning($"[MainHub] Refresh error: {e.Message}"); }
        }

        private async UniTask FetchProfileAsync()
        {
            try
            {
                var player = new Solana.Unity.Wallet.PublicKey(SolanaManager.Instance.PublicKey);
                var profilePda = _anchorClient.DerivePlayerProfilePda(player);
                var rpc = SolanaManager.Instance.GetRpcClient();
                var acct = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acct?.Result?.Value?.Data != null)
                {
                    var data = Convert.FromBase64String(acct.Result.Value.Data[0]);
                    SolanaManager.Instance.CurrentProfile = PlayerProfile.DeserializeFromAccount(data);
                }

                var slotResp = await rpc.GetBlockHeightAsync(Solana.Unity.Rpc.Types.Commitment.Processed);
                if (slotResp?.Result != null)
                {
                    var timeResp = await rpc.GetBlockTimeAsync(slotResp.Result);
                    if (timeResp?.Result != null)
                    {
                        PlayerProfile.ValidatorTime = (long)timeResp.Result;
                        Debug.Log($"[MainHub] Validator time: {PlayerProfile.ValidatorTime}");
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning($"[MainHub] FetchProfile error: {e.Message}"); }
        }

        private void UpdateUI()
        {
            var profile = SolanaManager.Instance?.CurrentProfile;
            if (profile == null) return;

            if (levelText != null) levelText.text = $"Level {profile.level}";
            uint xpToNext = (uint)(profile.level + 1) * 100;
            if (xpText != null) xpText.text = $"XP: {profile.xp} / {xpToNext}";
            if (xpBar != null) xpBar.value = (float)profile.xp / xpToNext;

            string className = "Unknown";
            if (_classes != null && profile.class_ < _classes.Length)
                className = _classes[profile.class_].className;

            var charData = new CharacterData
            {
                characterName = className,
                className = className,
                classType = profile.class_,
                strength = profile.strength,
                agility = profile.agility,
                intelligence = profile.intelligence,
                luck = profile.luck,
                level = profile.level,
                xp = profile.xp,
                xpToNextLevel = xpToNext
            };
            if (characterCard != null) characterCard.DisplayCharacter(charData);

            ulong raw = _tokenManager.RawBalance;
            ulong roland = _tokenManager.RolandBalance;
            if (balanceText != null) balanceText.text = $"{roland:F2} ROLAND";
            if (solBalanceText != null) solBalanceText.text = $"{SolanaManager.Instance.SolBalance:F4} SOL";

            if (levelUpButton != null)
            {
                uint xpNeeded = (uint)profile.level * 100;
                ulong costRoland = (ulong)profile.level * 50_000_000_000;
                bool canLevel = profile.xp >= xpNeeded && raw >= costRoland;
                levelUpButton.interactable = canLevel;
                if (levelUpCostText != null)
                    levelUpCostText.text = $"Cost: {xpNeeded} XP + {costRoland / 1e9:F0} ROLAND";
            }

            if (faucetButton != null)
            {
                bool canClaim = profile != null && !profile.airdropClaimed;
                faucetButton.gameObject.SetActive(canClaim);
            }

            if (equipmentBonusText != null)
            {
                byte bonus = profile.TotalEquipmentBonus;
                equipmentBonusText.text = bonus > 0
                    ? $"Equipment Bonus: +{bonus} to D20 rolls"
                    : "";
                equipmentBonusText.color = ThemeColors.Primary;
            }
        }

        private async void OnDailyRewardClicked()
        {
            if (dailyRewardButton != null) dailyRewardButton.interactable = false;
            try
            {
                var treasuryBalance = await _tokenManager.GetTreasuryBalanceAsync(_anchorClient);
                if (treasuryBalance < 50)
                {
                    Debug.LogError($"[MainHub] Treasury has only {treasuryBalance} ROLAND — need at least 50 for daily reward. Refund via: wsl bash -c 'spl-token transfer <ROLAND_MINT> 500000 <TREASURY_ATA> --url http://localhost:8899'");
                    if (dailyRewardButton != null) dailyRewardButton.interactable = true;
                    return;
                }

                Debug.Log("[MainHub] Claiming daily reward...");
                var player = new Solana.Unity.Wallet.PublicKey(SolanaManager.Instance.PublicKey);
                var rewardMint = new Solana.Unity.Wallet.PublicKey(GameConfigProvider.Instance.Config.rolandMintAddress);
                var createAta = AnchorClient.CreateAssociatedTokenAccountIdempotent(player, player, rewardMint);
                var ix = _anchorClient.BuildClaimDailyRewardInstruction(player, rewardMint);
                await _anchorClient.SendTransactionAsync(new[] { createAta, ix });
                Debug.Log("[MainHub] Daily reward tx sent, waiting...");
                await UniTask.Delay(2000);
                await RefreshAsync();
                Debug.Log("[MainHub] Daily reward claimed");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainHub] Daily reward error: {e.Message}");
                if (dailyRewardButton != null) dailyRewardButton.interactable = true;
            }
        }

        private async void OnLevelUpClicked()
        {
            if (levelUpButton != null) levelUpButton.interactable = false;
            try
            {
                Debug.Log("[MainHub] Leveling up...");
                var player = new Solana.Unity.Wallet.PublicKey(SolanaManager.Instance.PublicKey);
                var rewardMint = new Solana.Unity.Wallet.PublicKey(GameConfigProvider.Instance.Config.rolandMintAddress);
                var createAta = AnchorClient.CreateAssociatedTokenAccountIdempotent(player, player, rewardMint);
                var ix = _anchorClient.BuildLevelUpInstruction(player, rewardMint);
                await _anchorClient.SendTransactionAsync(new[] { createAta, ix });
                Debug.Log("[MainHub] LevelUp tx sent, waiting...");
                await UniTask.Delay(2000);
                await RefreshAsync();
                Debug.Log("[MainHub] LevelUp complete");
            }
            catch (Exception e) { Debug.LogError($"[MainHub] LevelUp error: {e.Message}"); }
            UpdateUI();
        }

        private async void OnFaucetClicked()
        {
            if (faucetButton != null) faucetButton.interactable = false;
            try
            {
                await SolanaManager.Instance.ClaimRolandFaucetAsync(_anchorClient);
                await RefreshAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainHub] Faucet error: {e.Message}");
            }
        }
    }
}
