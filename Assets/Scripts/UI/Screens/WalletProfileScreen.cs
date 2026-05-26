using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class WalletProfileScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text publicKeyText;
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private Transform nftCollectionGrid;
        [SerializeField] private Transform transactionHistory;
        [SerializeField] private TMP_Text statusText;

        private TokenManager _tokenManager;
        private NFTManager _nftManager;

        private void Awake()
        {
            _tokenManager = FindAnyObjectByType<TokenManager>();
            _nftManager = FindAnyObjectByType<NFTManager>();
        }

        private void OnEnable()
        {
            FontProvider.ApplyToText(publicKeyText, false);
            FontProvider.ApplyToText(balanceText, true);
            FontProvider.ApplyToText(statusText, false);
            RefreshAsync().Forget();
        }

        private async UniTask RefreshAsync()
        {
            if (SolanaManager.Instance == null) return;
            if (statusText != null) statusText.text = "Loading...";
            try
            {
                if (publicKeyText != null)
                    publicKeyText.text = SolanaManager.Instance.PublicKey;

                await _tokenManager.RefreshBalanceAsync();
                ulong rolandBalance = _tokenManager.RolandBalance;
                if (balanceText != null)
                    balanceText.text = $"{rolandBalance:F2} ROLAND";

                var nfts = await _nftManager.GetNftsByOwnerAsync(SolanaManager.Instance.PublicKey);
                PopulateNFTGrid(nfts);

                await LoadTransactionHistoryAsync();
                if (statusText != null) statusText.text = "";
            }
            catch (Exception e)
            {
                if (statusText != null) statusText.text = $"Error: {e.Message}";
            }
        }

        private void PopulateNFTGrid(List<NftMetadata> nfts)
        {
            if (nftCollectionGrid == null) return;
            foreach (Transform child in nftCollectionGrid)
                Destroy(child.gameObject);

            foreach (var nft in nfts)
            {
                var obj = new GameObject("NFT_Card");
                obj.transform.SetParent(nftCollectionGrid, false);
                obj.AddComponent<RectTransform>();
                obj.AddComponent<CanvasRenderer>();
                var img = obj.AddComponent<Image>();
                img.color = ThemeColors.CardBg;
                var card = obj.AddComponent<CardView>();
                var itemData = new ItemData { itemName = nft.name, imageUri = nft.image };
                card.DisplayItem(itemData);
            }
        }

        private async UniTask LoadTransactionHistoryAsync()
        {
            if (transactionHistory == null) return;
            foreach (Transform child in transactionHistory)
                Destroy(child.gameObject);

            try
            {
                var signatures = await SolanaManager.Instance.GetRecentSignaturesAsync(10);
                foreach (var sig in signatures)
                {
                    var obj = new GameObject("TxEntry");
                    obj.transform.SetParent(transactionHistory, false);
                    obj.AddComponent<RectTransform>();
                    obj.AddComponent<CanvasRenderer>();
                    var txt = obj.AddComponent<TextMeshProUGUI>();
                    txt.text = sig.Substring(0, Math.Min(16, sig.Length)) + "...";
                    txt.fontSize = 14;
                    txt.color = ThemeColors.Secondary;
                    FontProvider.ApplyToText(txt, false);
                    var le = obj.AddComponent<LayoutElement>();
                    le.preferredHeight = 22;
                }
            }
            catch { }
        }
    }
}
