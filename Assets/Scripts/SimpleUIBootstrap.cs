using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RollAndEarn;
using Cysharp.Threading.Tasks;

public class SimpleUIBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject walletConnectScreen;
    [SerializeField] private GameObject characterCreationScreen;
    [SerializeField] private GameObject mainHubScreen;
    [SerializeField] private GameObject adventureScreen;
    [SerializeField] private GameObject inventoryScreen;
    [SerializeField] private GameObject walletProfileScreen;

    [SerializeField] private Button connectBtn;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text titleLabel;

    private void Awake()
    {
        SetupScreens();
        if (connectBtn != null)
            connectBtn.onClick.AddListener(OnConnect);
        ShowWalletConnect();
    }

    private void SetupScreens()
    {
        GameObject[] screens = { walletConnectScreen, characterCreationScreen, mainHubScreen, adventureScreen, inventoryScreen, walletProfileScreen };
        foreach (var s in screens)
            if (s != null) s.SetActive(false);
    }

    public void ShowWalletConnect() => ShowOnly(walletConnectScreen);
    public void ShowCharacterCreation() => ShowOnly(characterCreationScreen);
    public void ShowMainHub() => ShowOnly(mainHubScreen);
    public void ShowAdventure() => ShowOnly(adventureScreen);
    public void ShowInventory() => ShowOnly(inventoryScreen);
    public void ShowWalletProfile() => ShowOnly(walletProfileScreen);

    private void ShowOnly(GameObject target)
    {
        GameObject[] screens = { walletConnectScreen, characterCreationScreen, mainHubScreen, adventureScreen, inventoryScreen, walletProfileScreen };
        foreach (var s in screens)
            if (s != null) s.SetActive(s == target);
    }

    private void OnConnect()
    {
        if (statusLabel != null) statusLabel.text = "Connecting...";
        var solana = SolanaManager.Instance;
        if (solana != null)
        {
            solana.OnWalletConnected += OnConnected;
            solana.ConnectWalletAsync().Forget();
        }
    }

    private void OnConnected(string pk)
    {
        var solana = SolanaManager.Instance;
        solana.OnWalletConnected -= OnConnected;

        if (statusLabel != null) statusLabel.text = "Connected: " + pk.Substring(0, 8) + "...";

        RouteAfterConnectAsync().Forget();
    }

    private async UniTaskVoid RouteAfterConnectAsync()
    {
        var anchorClient = FindAnyObjectByType<AnchorClient>();
        var profile = await SolanaManager.Instance.FetchProfileAsync(anchorClient);
        if (profile != null)
        {
            ShowMainHub();
        }
        else
        {
            ShowCharacterCreation();
        }
    }
}
