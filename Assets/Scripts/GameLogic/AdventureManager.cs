using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Solana.Unity.Wallet;
using UnityEngine;

namespace RollAndEarn
{
    public class AdventureManager : MonoBehaviour
    {
        private AdventureConfigSO[] _adventures;
        private GameConfig _gameConfig;
        private AnchorClient _anchorClient;
        private CooldownManager _cooldownManager;
        private TokenManager _tokenManager;

        private void Awake()
        {
            _anchorClient = FindAnyObjectByType<AnchorClient>();
            _cooldownManager = FindAnyObjectByType<CooldownManager>();
            _tokenManager = FindAnyObjectByType<TokenManager>();
        }

        private GameConfig GetConfig()
        {
            if (_gameConfig == null) _gameConfig = GameConfigProvider.Instance != null ? GameConfigProvider.Instance.Config : null;
            return _gameConfig;
        }

        private void Start()
        {
            var cfg = GetConfig();
            if (cfg != null) _adventures = cfg.Adventures;
        }

        public List<AdventureConfigSO> GetAvailableAdventures()
        {
            var available = new List<AdventureConfigSO>();
            if (_adventures == null) return available;
            for (int i = 0; i < _adventures.Length; i++)
            {
                if (_cooldownManager != null && _cooldownManager.IsOnCooldown((byte)i))
                    continue;
                available.Add(_adventures[i]);
            }
            return available;
        }

        public RollResult CalculatePreviewReward(AdventureConfigSO config, byte rollValue)
        {
            var (tokenAmount, xp, isSpecial, tier) = RewardCalculator.Calculate(config.adventureType, rollValue);
            return new RollResult
            {
                rollValue = rollValue,
                adventureType = config.adventureType,
                tokenAmount = tokenAmount,
                xpGained = xp,
                isSpecial = isSpecial,
                tier = tier
            };
        }

        public async UniTask<RollResult> ExecuteRollAsync(byte adventureType)
        {
            var player = new PublicKey(SolanaManager.Instance.PublicKey);
            var rewardMint = new PublicKey(GetConfig().rolandMintAddress);
            var profilePda = _anchorClient.DerivePlayerProfilePda(player);
            var rpc = SolanaManager.Instance.GetRpcClient();

            ulong balanceBefore = _tokenManager.RawBalance;
            byte equipBonus = 0;

            byte unclaimedBefore = 0;
            uint xpBefore = 0;
            try
            {
                var acctBefore = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acctBefore?.Result?.Value?.Data != null)
                {
                    var dataBefore = Convert.FromBase64String(acctBefore.Result.Value.Data[0]);
                    var profileBefore = PlayerProfile.DeserializeFromAccount(dataBefore);
                    unclaimedBefore = profileBefore.unclaimedSpecials;
                    xpBefore = profileBefore.xp;
                    equipBonus = profileBefore.TotalEquipmentBonus;
                    SolanaManager.Instance.CurrentProfile = profileBefore;
                }
            }
            catch { }

            var createAta = AnchorClient.CreateAssociatedTokenAccountIdempotent(player, player, rewardMint);
            var ix = _anchorClient.BuildRollActionInstruction(player, rewardMint, adventureType);
            var txSignature = await _anchorClient.SendTransactionAsync(new[] { createAta, ix });

            await UniTask.Delay(1500);

            await _tokenManager.RefreshBalanceAsync();
            ulong balanceAfter = _tokenManager.RawBalance;
            ulong rewardAmount = 0;
            if (balanceAfter > balanceBefore) rewardAmount = balanceAfter - balanceBefore;

            uint xpGained = 0;
            byte unclaimedAfter = unclaimedBefore;
            long cooldownExpiry = 0;
            try
            {
                var acctAfter = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acctAfter?.Result?.Value?.Data != null)
                {
                    var dataAfter = Convert.FromBase64String(acctAfter.Result.Value.Data[0]);
                    var profileAfter = PlayerProfile.DeserializeFromAccount(dataAfter);
                    unclaimedAfter = profileAfter.unclaimedSpecials;
                    if (profileAfter.xp > xpBefore) xpGained = profileAfter.xp - xpBefore;
                    cooldownExpiry = profileAfter.cooldownExpiries[adventureType];
                    equipBonus = profileAfter.TotalEquipmentBonus;
                    SolanaManager.Instance.CurrentProfile = profileAfter;
                }
            }
            catch { }

            bool isSpecial = unclaimedAfter > unclaimedBefore;
            byte effectiveRoll = EstimateEffectiveRoll(adventureType, rewardAmount, isSpecial);
            byte baseRoll = (byte)Mathf.Max(1, effectiveRoll - equipBonus);
            var (_, _, _, tier) = RewardCalculator.Calculate(adventureType, effectiveRoll);

            if (_cooldownManager != null && SolanaManager.Instance?.CurrentProfile != null)
                _cooldownManager.SyncFromProfile(SolanaManager.Instance.CurrentProfile);

            return new RollResult
            {
                rollValue = baseRoll,
                effectiveRoll = effectiveRoll,
                adventureType = adventureType,
                tokenAmount = rewardAmount,
                xpGained = xpGained,
                isSpecial = isSpecial,
                tier = tier,
                cooldownExpiry = cooldownExpiry,
                equipmentBonus = equipBonus
            };
        }

        private byte EstimateEffectiveRoll(byte adventureType, ulong rewardAmount, bool isSpecial)
        {
            if (isSpecial)
                return (byte)UnityEngine.Random.Range(20, 26);

            var cfg = GetConfig();
            if (cfg == null || adventureType >= cfg.Adventures.Length) return 10;
            var adv = cfg.Adventures[adventureType];

            if (rewardAmount >= adv.highReward)
                return (byte)UnityEngine.Random.Range(adv.highStart, adv.specialStart);
            if (rewardAmount >= adv.midReward)
                return (byte)UnityEngine.Random.Range(adv.midStart, adv.highStart);
            return (byte)UnityEngine.Random.Range(1, adv.midStart);
        }
    }
}
