using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class BottomNavBar : MonoBehaviour
    {
        [SerializeField] private Button[] navButtons = new Button[5];
        [SerializeField] private TMP_Text[] navLabels = new TMP_Text[5];
        [SerializeField] private Color activeColor = new Color(1f, 0.722f, 0f);
        [SerializeField] private Color inactiveColor = new Color(0.40f, 0.37f, 0.50f, 0.8f);
        [SerializeField] private Color disabledColor = new Color(0.12f, 0.10f, 0.20f, 0.6f);

        private ScreenManager _screenManager;
        private Image[] _buttonBackgrounds;
        private static readonly string[] ScreenNames = { "MainHub", "Adventure", "Inventory", "WalletProfile", "WalletConnect" };

        private void Awake()
        {
            _screenManager = FindAnyObjectByType<ScreenManager>();
            _buttonBackgrounds = new Image[navButtons.Length];
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] != null)
                    _buttonBackgrounds[i] = navButtons[i].GetComponent<Image>();
            }
        }

        private void Start()
        {
            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;
                int idx = i;
                navButtons[i].onClick.AddListener(() => Navigate(idx));
            }
        }

        private void Update()
        {
            UpdateInteractable();
        }

        private bool IsReady()
        {
            if (SolanaManager.Instance == null || !SolanaManager.Instance.IsConnected) return false;
            var profile = SolanaManager.Instance.CurrentProfile;
            return profile != null
                && !string.IsNullOrEmpty(profile.characterMint)
                && profile.characterMint != "11111111111111111111111111111111";
        }

        private void UpdateInteractable()
        {
            bool ready = IsReady();

            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;
                navButtons[i].interactable = ready;

                if (_buttonBackgrounds[i] != null)
                {
                    _buttonBackgrounds[i].color = ready
                        ? new Color(0.10f, 0.08f, 0.18f, 0.9f)
                        : new Color(0.06f, 0.05f, 0.10f, 0.6f);
                }

                if (i < navLabels.Length && navLabels[i] != null)
                {
                    navLabels[i].color = ready ? inactiveColor : disabledColor;
                }
            }
        }

        private void Navigate(int index)
        {
            if (index < 0 || index >= ScreenNames.Length) return;
            if (!IsReady()) return;

            if (_screenManager != null) _screenManager.ShowScreen(ScreenNames[index]);
            SetActive(index);
        }

        public void SetActive(int index)
        {
            bool ready = IsReady();

            for (int i = 0; i < navButtons.Length; i++)
            {
                if (navButtons[i] == null) continue;

                bool isActive = i == index;

                if (_buttonBackgrounds[i] != null)
                {
                    _buttonBackgrounds[i].color = isActive
                        ? new Color(0.14f, 0.11f, 0.26f, 0.95f)
                        : new Color(0.10f, 0.08f, 0.18f, 0.9f);
                }

                if (i < navLabels.Length && navLabels[i] != null)
                {
                    navLabels[i].color = isActive
                        ? activeColor
                        : (ready ? inactiveColor : disabledColor);
                    navLabels[i].fontStyle = isActive ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }
    }
}
