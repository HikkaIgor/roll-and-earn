using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class WalletConnectScreen : MonoBehaviour
    {
        [SerializeField] private Button connectButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text titleLabel;

        private ScreenManager _screenManager;

        private void Awake()
        {
            _screenManager = FindAnyObjectByType<ScreenManager>();
        }

        private void OnEnable()
        {
            if (connectButton != null)
            {
                connectButton.onClick.RemoveAllListeners();
                connectButton.onClick.AddListener(OnConnectClicked);
                connectButton.interactable = true;
            }
            if (statusText != null) statusText.text = "Connect your wallet to begin";
            FontProvider.ApplyToText(titleLabel, true);
            FontProvider.ApplyToText(statusText, false);
        }

        private void OnDisable()
        {
            if (connectButton != null) connectButton.onClick.RemoveListener(OnConnectClicked);
            var solana = SolanaManager.Instance;
            if (solana != null) solana.OnWalletConnected -= OnWalletConnected;
        }

        private void OnConnectClicked()
        {
            Debug.Log("[WalletConnect] OnConnectClicked");
            var solana = SolanaManager.Instance;
            if (solana == null)
            {
                Debug.LogError("[WalletConnect] SolanaManager is null!");
                if (statusText != null) statusText.text = "SolanaManager not ready";
                return;
            }

            solana.OnWalletConnected -= OnWalletConnected;
            solana.OnWalletConnected += OnWalletConnected;
            if (connectButton != null) connectButton.interactable = false;
            if (statusText != null) statusText.text = "Connecting...";
            Debug.Log("[WalletConnect] Calling ConnectWalletAsync...");
            solana.ConnectWalletAsync().Forget();
        }

        private async void OnWalletConnected(string publicKey)
        {
            var solana = SolanaManager.Instance;
            if (solana == null) return;
            solana.OnWalletConnected -= OnWalletConnected;

            if (publicKey == null)
            {
                if (statusText != null) statusText.text = "Connection failed. Try again.";
                if (connectButton != null) connectButton.interactable = true;
                return;
            }

            if (statusText != null) statusText.text = $"Connected: {publicKey.Substring(0, 8)}...";

             SoundManager.Instance.PlayLogin();

             var anchorClient = FindAnyObjectByType<AnchorClient>();
             var player = new Solana.Unity.Wallet.PublicKey(publicKey);
             var rpc = solana.GetRpcClient();
             var profilePda = anchorClient.DerivePlayerProfilePda(player);

             try
             {
                 var acct = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                 if (acct?.Result?.Value?.Data != null)
                 {
                     var data = Convert.FromBase64String(acct.Result.Value.Data[0]);
                     solana.CurrentProfile = PlayerProfile.DeserializeFromAccount(data);
                 }
                 else
                 {
                     solana.CurrentProfile = null;
                 }
             }
             catch (Exception e)
             {
                 Debug.LogWarning($"[WalletConnect] Profile fetch error: {e.Message}");
                 solana.CurrentProfile = null;
             }

            if (_screenManager == null) _screenManager = FindAnyObjectByType<ScreenManager>();
            if (_screenManager == null) return;

            var profile = solana.CurrentProfile;
            if (profile != null && !string.IsNullOrEmpty(profile.characterMint) && profile.characterMint != "11111111111111111111111111111111")
                _screenManager.ShowScreen("MainHub");
            else
                _screenManager.ShowScreen("CharacterCreation");
        }
    }
}
