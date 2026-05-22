using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace RollAndEarn
{
    public class TokenBalanceDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text balanceText;

        private TokenManager _tokenManager;

        private void Awake()
        {
            _tokenManager = FindAnyObjectByType<TokenManager>();
        }

        private void OnEnable()
        {
            if (_tokenManager != null)
                _tokenManager.OnBalanceUpdated += OnBalanceUpdated;
            RefreshAsync();
        }

        private void OnDisable()
        {
            if (_tokenManager != null)
                _tokenManager.OnBalanceUpdated -= OnBalanceUpdated;
        }

        private void OnBalanceUpdated(ulong rolandBalance)
        {
            if (balanceText != null)
                balanceText.text = $"{rolandBalance:F2} ROLAND";
        }

        public async void RefreshAsync()
        {
            try { await _tokenManager.RefreshBalanceAsync(); }
            catch { }
        }
    }
}
