using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.Networking;

namespace RollAndEarn
{
    public class SolanaManager : MonoBehaviour
    {
        public static SolanaManager Instance { get; private set; }

        public event Action<string> OnWalletConnected;
        public event Action OnWalletDisconnected;

        public string PublicKey { get; private set; }
        public bool IsConnected { get; private set; }
        public PlayerProfile CurrentProfile { get; set; }
        public double SolBalance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        public async UniTaskVoid ConnectWalletAsync()
        {
            if (Web3.Instance == null)
            {
                Debug.LogError("[RollAndEarn] Web3 instance not found.");
                OnWalletConnected?.Invoke(null);
                return;
            }

            Debug.Log("[RollAndEarn] Connecting wallet...");
            Account account;

            if (Application.isEditor || Application.platform != RuntimePlatform.WebGLPlayer)
            {
                Debug.Log("[RollAndEarn] Using in-game wallet");
                var existing = await Web3.Instance.LoginInGameWallet("rollandearn");
                account = existing ?? await Web3.Instance.CreateAccount(null, "rollandearn");
            }
            else
            {
                Debug.Log("[RollAndEarn] Using wallet adapter");
                account = await Web3.Instance.LoginWalletAdapter();
            }

            if (account == null)
            {
                Debug.LogError("[RollAndEarn] Wallet connection returned null");
                OnWalletConnected?.Invoke(null);
                return;
            }

            PublicKey = account.PublicKey.ToString();
            IsConnected = true;
            Debug.Log($"[RollAndEarn] Wallet connected: {PublicKey}");

            bool hasSol = await EnsureSolBalanceAsync();
            if (!hasSol)
            {
                Debug.LogError("[RollAndEarn] Could not get SOL. Transactions will fail.");
            }

            var anchorClient = FindAnyObjectByType<AnchorClient>();
            if (anchorClient != null)
            {
                await EnsureGameInitializedAsync(anchorClient);
            }

            OnWalletConnected?.Invoke(PublicKey);
        }

        public async UniTask EnsureGameInitializedAsync(AnchorClient anchorClient)
        {
            try
            {
                var rpc = GetRpcClient();
                var gamePda = anchorClient.DeriveGamePda();
                var gameAcct = await rpc.GetAccountInfoAsync(gamePda, Commitment.Processed);

                if (gameAcct?.Result?.Value?.Data != null)
                {
                    Debug.Log($"[RollAndEarn] Game already initialized at {gamePda}");
                    return;
                }

                Debug.Log("[RollAndEarn] Game not initialized. Initializing on-chain...");

                var player = new PublicKey(PublicKey);
                var rewardMint = new PublicKey(GameConfigProvider.Instance.Config.rolandMintAddress);

                var initGameIx = anchorClient.BuildInitGameInstruction(player, rewardMint);
                Debug.Log("[RollAndEarn] Sending init_game transaction...");
                var initGameSig = await anchorClient.SendTransactionAsync(new[] { initGameIx });
                Debug.Log($"[RollAndEarn] init_game tx: {initGameSig}");
                await UniTask.Delay(3000);

                var treasuryPda = anchorClient.DeriveTreasuryPda();
                var treasuryAcct = await rpc.GetAccountInfoAsync(treasuryPda, Commitment.Processed);
                if (treasuryAcct?.Result?.Value?.Data != null)
                {
                    Debug.Log("[RollAndEarn] Treasury already initialized");
                    return;
                }

                var initTreasuryIx = anchorClient.BuildInitTreasuryInstruction(player, rewardMint);
                Debug.Log("[RollAndEarn] Sending init_treasury transaction...");
                var initTreasurySig = await anchorClient.SendTransactionAsync(new[] { initTreasuryIx });
                Debug.Log($"[RollAndEarn] init_treasury tx: {initTreasurySig}");
                await UniTask.Delay(3000);

                await FundTreasuryAsync(anchorClient);

                Debug.Log("[RollAndEarn] Game fully initialized!");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RollAndEarn] Game init error: {e.Message}");
            }
        }

        public async UniTask<bool> EnsureSolBalanceAsync()
        {
            try
            {
                var rpc = GetRpcClient();
                var balance = await rpc.GetBalanceAsync(PublicKey, Commitment.Processed);
                ulong sol = balance.Result != null ? balance.Result.Value : 0;
                SolBalance = sol / 1_000_000_000.0;
                Debug.Log($"[RollAndEarn] SOL balance: {SolBalance:F4} SOL");

                if (sol >= 100_000_000)
                {
                    Debug.Log("[RollAndEarn] SOL balance sufficient");
                    return true;
                }

                Debug.Log("[RollAndEarn] Need SOL, trying airdrop methods...");

                var cfg = GameConfigProvider.Instance.Config;
                bool isLocal = cfg.rpcEndpoint.Contains("localhost") || cfg.rpcEndpoint.Contains("127.0.0.1");

                if (isLocal)
                {
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        Debug.Log($"[RollAndEarn] CLI airdrop attempt {attempt}/3...");
                        bool ok = await CliAirdropAsync(2_000_000_000);
                        if (ok)
                        {
                            await UniTask.Delay(3000);
                            var newBal = await rpc.GetBalanceAsync(PublicKey, Commitment.Processed);
                            if (newBal.Result != null && newBal.Result.Value >= 100_000_000)
                            {
                                Debug.Log($"[RollAndEarn] SOL received: {newBal.Result.Value / 1_000_000_000f:F4} SOL");
                                return true;
                            }
                        }
                        if (attempt < 3) await UniTask.Delay(2000);
                    }
                }
                else
                {
                    for (int attempt = 1; attempt <= 3; attempt++)
                    {
                        Debug.Log($"[RollAndEarn] RPC airdrop attempt {attempt}/3...");
                        var rpcResult = await rpc.RequestAirdropAsync(PublicKey, 2_000_000_000, Commitment.Processed);
                        if (rpcResult.WasSuccessful && !string.IsNullOrEmpty(rpcResult.Result))
                        {
                            Debug.Log($"[RollAndEarn] RPC airdrop sig: {rpcResult.Result}");
                            await UniTask.Delay(8000);
                            var newBal = await rpc.GetBalanceAsync(PublicKey, Commitment.Processed);
                            if (newBal.Result != null && newBal.Result.Value >= 100_000_000)
                            {
                                Debug.Log($"[RollAndEarn] SOL received: {newBal.Result.Value / 1_000_000_000f:F4} SOL");
                                return true;
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[RollAndEarn] RPC airdrop failed: error={rpcResult.ErrorData}");
                        }

                        Debug.Log("[RollAndEarn] Trying web faucet...");
                        bool webOk = await TryWebFaucetAsync();
                        if (webOk)
                        {
                            await UniTask.Delay(8000);
                            var newBal = await rpc.GetBalanceAsync(PublicKey, Commitment.Processed);
                            if (newBal.Result != null && newBal.Result.Value >= 100_000_000)
                            {
                                Debug.Log($"[RollAndEarn] SOL from web faucet: {newBal.Result.Value / 1_000_000_000f:F4} SOL");
                                return true;
                            }
                        }

                        if (attempt < 3) await UniTask.Delay(5000);
                    }
                }

                Debug.LogError("[RollAndEarn] All airdrop attempts failed.");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RollAndEarn] EnsureSolBalance error: {e.Message}");
                return false;
            }
        }

        private async UniTask<bool> CliAirdropAsync(ulong lamports)
        {
            try
            {
                var cfg = GameConfigProvider.Instance.Config;
                double sol = lamports / 1_000_000_000.0;
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "solana",
                    Arguments = $"airdrop {sol:F0} {PublicKey} --url {cfg.rpcEndpoint}",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                Debug.Log($"[RollAndEarn] Running: solana airdrop {sol:F0} {PublicKey} --url {cfg.rpcEndpoint}");

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null) return false;

                var cts = new System.Threading.CancellationTokenSource(15000);
                await UniTask.WaitUntil(() => process.HasExited, cancellationToken: cts.Token);
                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();
                Debug.Log($"[RollAndEarn] CLI airdrop output: {output}");
                if (!string.IsNullOrEmpty(err)) Debug.LogWarning($"[RollAndEarn] CLI airdrop stderr: {err}");

                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RollAndEarn] CLI airdrop error: {e.Message}");
                return false;
            }
        }

        private async UniTask<bool> TryWebFaucetAsync()
        {
            try
            {
                string json = $"{{\"address\":\"{PublicKey}\",\"amount\":2}}";
                using var request = new UnityWebRequest("https://faucet.solana.com/api/airdrop", "POST");
                byte[] body = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 15;
                await request.SendWebRequest().ToUniTask();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[RollAndEarn] Web faucet response: {request.downloadHandler.text}");
                    return true;
                }
                Debug.LogWarning($"[RollAndEarn] Web faucet failed: {request.error}");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RollAndEarn] Web faucet error: {e.Message}");
                return false;
            }
        }

        public void DisconnectWallet()
        {
            Web3.Instance.Logout();
            PublicKey = null;
            IsConnected = false;
            CurrentProfile = null;
            OnWalletDisconnected?.Invoke();
        }

        public IRpcClient GetRpcClient() => Web3.Rpc;

        public async UniTask RefreshSolBalanceAsync()
        {
            try
            {
                var rpc = GetRpcClient();
                var balance = await rpc.GetBalanceAsync(PublicKey, Commitment.Processed);
                if (balance.Result != null)
                    SolBalance = balance.Result.Value / 1_000_000_000.0;
            }
            catch { }
        }

        public async UniTask<string[]> GetRecentSignaturesAsync(int limit = 10)
        {
            var rpc = GetRpcClient();
            var signatures = await rpc.GetSignaturesForAddressAsync(
                PublicKey, (ulong)limit, null, null, Commitment.Processed);
            if (signatures.Result == null) return Array.Empty<string>();
            var result = new string[signatures.Result.Count];
            for (int i = 0; i < signatures.Result.Count; i++)
                result[i] = signatures.Result[i].Signature;
            return result;
        }

        public async UniTask<PlayerProfile> FetchProfileAsync(AnchorClient anchorClient)
        {
            if (string.IsNullOrEmpty(PublicKey)) return null;
            try
            {
                var player = new PublicKey(PublicKey);
                var profilePda = anchorClient.DerivePlayerProfilePda(player);
                var rpc = GetRpcClient();

                for (int attempt = 0; attempt < 5; attempt++)
                {
                    var acct = await rpc.GetAccountInfoAsync(profilePda, Commitment.Processed);
                    if (acct?.Result?.Value?.Data != null)
                    {
                        var data = Convert.FromBase64String(acct.Result.Value.Data[0]);
                        if (data.Length < PlayerProfile.ExpectedDataLength)
                        {
                            Debug.LogWarning($"[SolanaManager] Profile data too short ({data.Length} bytes, min {PlayerProfile.ExpectedDataLength})");
                            return null;
                        }
                        var profile = PlayerProfile.DeserializeFromAccount(data);
                        CurrentProfile = profile;
                        Debug.Log($"[SolanaManager] Profile fetched: class={profile.class_}, level={profile.level}, weaponBonus=+{profile.weaponBonus}, armorBonus=+{profile.armorBonus}");
                        return profile;
                    }
                    if (attempt < 4)
                    {
                        Debug.Log($"[SolanaManager] Profile not found, retry {attempt + 1}/5...");
                        await UniTask.Delay(3000);
                    }
                }
                Debug.LogWarning("[SolanaManager] Profile not found after 5 attempts");
            }
            catch (Exception e) { Debug.LogError($"[SolanaManager] FetchProfile error: {e.Message}"); }
            return null;
        }

        private async UniTask UpgradeProfileAsync(AnchorClient anchorClient, PublicKey player)
        {
            try
            {
                var closeIx = anchorClient.BuildForceCloseProfileInstruction(player);
                await anchorClient.SendTransactionAsync(new[] { closeIx });
                await UniTask.Delay(2000);

                byte class_ = CurrentProfile?.class_ ?? 0;
                var recreateIx = anchorClient.BuildRecreateProfileInstruction(player, class_);
                await anchorClient.SendTransactionAsync(new[] { recreateIx });
                await UniTask.Delay(2000);
                Debug.Log("[SolanaManager] Profile upgraded to new format");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SolanaManager] Profile upgrade failed: {e.Message}");
            }
        }

        public async UniTask ClaimRolandFaucetAsync(AnchorClient anchorClient)
        {
            try
            {
                if (CurrentProfile != null && CurrentProfile.airdropClaimed)
                {
                    Debug.Log("[SolanaManager] Airdrop already claimed");
                    return;
                }
                var player = new PublicKey(PublicKey);
                var rewardMint = new PublicKey(GameConfigProvider.Instance.Config.rolandMintAddress);
                var createAta = AnchorClient.CreateAssociatedTokenAccountIdempotent(player, player, rewardMint);
                var ix = anchorClient.BuildRequestAirdropInstruction(player, rewardMint);
                await anchorClient.SendTransactionAsync(new[] { createAta, ix });
                Debug.Log("[SolanaManager] ROLAND faucet claimed: 200 ROLAND");

                await UniTask.Delay(1500);
                var tokenManager = FindAnyObjectByType<TokenManager>();
                if (tokenManager != null) await tokenManager.RefreshBalanceAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SolanaManager] Faucet error: {e.Message}");
            }
        }

        private async UniTask FundTreasuryAsync(AnchorClient anchorClient)
        {
            try
            {
                var cfg = GameConfigProvider.Instance.Config;
                if (!cfg.rpcEndpoint.Contains("localhost") && !cfg.rpcEndpoint.Contains("127.0.0.1"))
                    return;

                var rewardMint = new PublicKey(cfg.rolandMintAddress);
                var treasuryPda = anchorClient.DeriveTreasuryPda();
                var treasuryAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(treasuryPda, rewardMint);

                Debug.Log($"[RollAndEarn] Treasury ATA: {treasuryAta}");

                var rpc = GetRpcClient();
                var acctInfo = await rpc.GetAccountInfoAsync(treasuryAta, Commitment.Processed);
                if (acctInfo?.Result?.Value?.Data == null)
                {
                    Debug.LogWarning("[RollAndEarn] Treasury ATA not found, skipping funding");
                    return;
                }

                Debug.Log("[RollAndEarn] Attempting to fund treasury via CLI...");

                string mintStr = cfg.rolandMintAddress;
                string ataStr = treasuryAta.ToString();

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wsl",
                    Arguments = $"bash -c \"export PATH=/home/alahf/.local/share/solana/install/releases/stable-437252fc4371f0050072cd4db44c295e3317e871/solana-release/bin:/home/alahf/.cargo/bin:\\$PATH && spl-token transfer {mintStr} 500000 {ataStr} --url http://localhost:8899\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using var process = System.Diagnostics.Process.Start(psi);
                if (process == null)
                {
                    Debug.LogWarning("[RollAndEarn] Could not start wsl spl-token. Run manually:\n" +
                        $"wsl bash -c 'spl-token transfer {mintStr} 500000 {ataStr} --url http://localhost:8899'");
                    return;
                }

                process.WaitForExit(30000);
                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                    Debug.Log($"[RollAndEarn] Treasury funded: {output}");
                else
                    Debug.LogWarning($"[RollAndEarn] spl-token error: {err}\nRun manually:\n" +
                        $"wsl bash -c 'spl-token transfer {mintStr} 500000 {ataStr} --url http://localhost:8899'");
            }
            catch (Exception e)
            {
                Debug.LogError($"[RollAndEarn] FundTreasury error: {e.Message}");
            }
        }
    }
}
