using System;
using Cysharp.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using UnityEngine;

namespace RollAndEarn
{
    public class TokenManager : MonoBehaviour
    {
        public event Action<ulong> OnBalanceUpdated;
        public ulong RawBalance { get; private set; }
        public ulong RolandBalance { get; private set; }

        private GameConfig gameConfig;

        private GameConfig GetConfig() => gameConfig ??= GameConfigProvider.Instance != null ? GameConfigProvider.Instance.Config : null;

        public async UniTask<ulong> GetRolandBalanceAsync()
        {
            Debug.Log("[TokenManager] GetRolandBalanceAsync START");
            try
            {
                var rpc = SolanaManager.Instance.GetRpcClient();
                var owner = new PublicKey(SolanaManager.Instance.PublicKey);
                var mint = new PublicKey(GetConfig().rolandMintAddress);

                var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, mint);
                Debug.Log($"[TokenManager] ATA: {ata}");

                var acctInfo = await rpc.GetAccountInfoAsync(ata, Solana.Unity.Rpc.Types.Commitment.Processed);

                RawBalance = 0;
                RolandBalance = 0;

                if (acctInfo?.Result?.Value?.Data == null)
                {
                    Debug.Log("[TokenManager] No ROLAND token account found (null data)");
                    OnBalanceUpdated?.Invoke(0);
                    return 0;
                }

                var data = Convert.FromBase64String(acctInfo.Result.Value.Data[0]);
                Debug.Log($"[TokenManager] ATA data length: {data.Length}");

                if (data.Length < 72)
                {
                    Debug.LogWarning("[TokenManager] ATA data too short");
                    OnBalanceUpdated?.Invoke(0);
                    return 0;
                }

                RawBalance = BitConverter.ToUInt64(data, 64);
                RolandBalance = RawBalance / 1_000_000_000;
                Debug.Log($"[TokenManager] Balance: raw={RawBalance}, roland={RolandBalance}");

                OnBalanceUpdated?.Invoke(RolandBalance);
                return RolandBalance;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TokenManager] GetRolandBalanceAsync ERROR: {e}");
                RawBalance = 0;
                RolandBalance = 0;
                OnBalanceUpdated?.Invoke(0);
                return 0;
            }
        }

        public async UniTask RefreshBalanceAsync()
        {
            await GetRolandBalanceAsync();
        }

        public async UniTask<ulong> GetTreasuryBalanceAsync(AnchorClient anchorClient)
        {
            try
            {
                var rpc = SolanaManager.Instance.GetRpcClient();
                var mint = new PublicKey(GetConfig().rolandMintAddress);
                var treasuryPda = anchorClient.DeriveTreasuryPda();
                var treasuryAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(treasuryPda, mint);

                var acctInfo = await rpc.GetAccountInfoAsync(treasuryAta, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acctInfo?.Result?.Value?.Data == null)
                {
                    Debug.LogWarning($"[TokenManager] Treasury ATA not found: {treasuryAta}");
                    return 0;
                }

                var data = Convert.FromBase64String(acctInfo.Result.Value.Data[0]);
                if (data.Length < 72) return 0;

                ulong raw = BitConverter.ToUInt64(data, 64);
                ulong roland = raw / 1_000_000_000;
                Debug.Log($"[TokenManager] Treasury balance: {roland} ROLAND (raw={raw})");
                return roland;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TokenManager] GetTreasuryBalanceAsync ERROR: {e}");
                return 0;
            }
        }
    }
}
