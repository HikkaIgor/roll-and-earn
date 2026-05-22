using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollAndEarn
{
    public class ScreenManager : MonoBehaviour
    {
        public event Action<string> OnScreenChanged;

        [SerializeField] private GameObject walletConnectScreen;
        [SerializeField] private GameObject characterCreationScreen;
        [SerializeField] private GameObject mainHubScreen;
        [SerializeField] private GameObject adventureScreen;
        [SerializeField] private GameObject inventoryScreen;
        [SerializeField] private GameObject walletProfileScreen;

        private Dictionary<string, GameObject> screens;
        private string currentScreen;

        private void Awake()
        {
            screens = new Dictionary<string, GameObject>
            {
                { "WalletConnect", walletConnectScreen },
                { "CharacterCreation", characterCreationScreen },
                { "MainHub", mainHubScreen },
                { "Adventure", adventureScreen },
                { "Inventory", inventoryScreen },
                { "WalletProfile", walletProfileScreen },
            };
        }

        private void Start()
        {
            ShowScreen("WalletConnect");
        }

        public void ShowScreen(string screenName)
        {
            HideAllScreens();
            if (screens != null && screens.TryGetValue(screenName, out var screen) && screen != null)
            {
                screen.SetActive(true);
                currentScreen = screenName;
                OnScreenChanged?.Invoke(screenName);
            }
        }

        public void HideAllScreens()
        {
            if (screens == null) return;
            foreach (var screen in screens.Values)
                if (screen != null) screen.SetActive(false);
            currentScreen = null;
        }

        public string GetCurrentScreen() => currentScreen;
    }
}
