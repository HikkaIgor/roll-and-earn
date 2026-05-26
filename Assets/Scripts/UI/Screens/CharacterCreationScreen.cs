using System;
using Cysharp.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class CharacterCreationScreen : MonoBehaviour
    {
        [SerializeField] private Button[] classButtons = new Button[3];
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button createButton;
        [SerializeField] private CardView previewCard;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text[] classStatTexts = new TMP_Text[3];

        private AnchorClient _anchorClient;
        private ScreenManager _screenManager;
        private CharacterClassSO[] _classes;
        private int _selectedClassIndex = -1;

        private void Awake()
        {
            _anchorClient = FindAnyObjectByType<AnchorClient>();
            _screenManager = FindAnyObjectByType<ScreenManager>();
        }

        private void Start()
        {
            if (_anchorClient == null) _anchorClient = FindAnyObjectByType<AnchorClient>();
            if (_screenManager == null) _screenManager = FindAnyObjectByType<ScreenManager>();
            var cfg = GameConfigProvider.Instance != null ? GameConfigProvider.Instance.Config : null;
            if (cfg != null && cfg.CharacterClasses != null && cfg.CharacterClasses.Length > 0)
                _classes = cfg.CharacterClasses;
            else
                _classes = CreateDefaultClasses();

            for (int i = 0; i < classButtons.Length; i++)
            {
                if (classButtons[i] == null) continue;
                int idx = i;
                classButtons[i].onClick.AddListener(() => OnClassSelected(idx));

                if (i < _classes.Length && i < classStatTexts.Length && classStatTexts[i] != null)
                {
                    var c = _classes[i];
                    classStatTexts[i].text = $"STR:{c.baseStrength} AGI:{c.baseAgility}\nINT:{c.baseIntelligence} LCK:{c.baseLuck}";
                }
            }

            if (nameInput != null)
                nameInput.onValueChanged.AddListener(OnNameChanged);

            if (createButton != null)
            {
                createButton.onClick.AddListener(OnCreateClicked);
                createButton.interactable = false;
            }

            if (errorText != null) errorText.text = string.Empty;
            _selectedClassIndex = -1;

            foreach (var st in classStatTexts) FontProvider.ApplyToText(st, false);
            FontProvider.ApplyToText(errorText, false);
        }

        private CharacterClassSO[] CreateDefaultClasses()
        {
            return new CharacterClassSO[]
            {
                CreateClass("Warrior", 0, 10, 6, 4, 5, "+10 to combat rolls"),
                CreateClass("Rogue", 1, 5, 10, 6, 8, "+10 to trap rolls"),
                CreateClass("Mage", 2, 4, 5, 10, 6, "+10 to magic rolls")
            };
        }

        private CharacterClassSO CreateClass(string name, byte type, byte str, byte agi, byte intel, byte luck, string bonus)
        {
            var so = ScriptableObject.CreateInstance<CharacterClassSO>();
            so.className = name;
            so.classType = type;
            so.baseStrength = str;
            so.baseAgility = agi;
            so.baseIntelligence = intel;
            so.baseLuck = luck;
            so.rollBonusDescription = bonus;
            return so;
        }

        private void OnDisable()
        {
            if (classButtons != null)
                for (int i = 0; i < classButtons.Length; i++)
                    if (classButtons[i] != null) classButtons[i].onClick.RemoveAllListeners();
            if (createButton != null) createButton.onClick.RemoveListener(OnCreateClicked);
            if (nameInput != null) nameInput.onValueChanged.RemoveListener(OnNameChanged);
        }

        private void OnNameChanged(string newName)
        {
            UpdateCreateButton();
            if (_selectedClassIndex >= 0 && _selectedClassIndex < _classes.Length)
                OnClassSelected(_selectedClassIndex);
        }

        private void UpdateCreateButton()
        {
            if (createButton == null) return;
            bool hasName = !string.IsNullOrEmpty(nameInput?.text?.Trim());
            bool hasClass = _selectedClassIndex >= 0;
            createButton.interactable = hasName && hasClass;
        }

        private void OnClassSelected(int index)
        {
            if (_classes == null || _classes.Length == 0) return;
            _selectedClassIndex = index;

            for (int i = 0; i < classButtons.Length; i++)
            {
                if (classButtons[i] == null) continue;
                var colors = classButtons[i].colors;
                colors.normalColor = i == index ? ThemeColors.Selected : ThemeColors.Unselected;
                classButtons[i].colors = colors;
            }

            if (index >= 0 && index < _classes.Length)
            {
                var cls = _classes[index];
                string heroName = !string.IsNullOrEmpty(nameInput?.text?.Trim()) ? nameInput.text.Trim() : cls.className;
                var data = new CharacterData
                {
                    characterName = heroName,
                    className = cls.className,
                    classType = cls.classType,
                    strength = cls.baseStrength,
                    agility = cls.baseAgility,
                    intelligence = cls.baseIntelligence,
                    luck = cls.baseLuck,
                    level = 1,
                    xp = 0,
                    xpToNextLevel = 200
                };
                if (previewCard != null)
                    previewCard.DisplayCharacter(data);
            }

            UpdateCreateButton();
        }

        private async void OnCreateClicked()
        {
            string name = nameInput != null ? nameInput.text.Trim() : "";

            if (string.IsNullOrEmpty(name) || name.Length > 32)
            {
                if (errorText != null) errorText.text = "Name must be 1-32 characters";
                return;
            }

            if (_selectedClassIndex < 0)
            {
                if (errorText != null) errorText.text = "Select a class";
                return;
            }

            if (createButton != null) createButton.interactable = false;
            if (errorText != null) errorText.text = "Creating character on blockchain...";

            try
            {
                byte classType = (byte)_selectedClassIndex;
                string player = SolanaManager.Instance.PublicKey;
                var playerPk = new Solana.Unity.Wallet.PublicKey(player);

                var rpc = SolanaManager.Instance.GetRpcClient();
                var balResult = await rpc.GetBalanceAsync(player, Commitment.Processed);
                ulong sol = balResult.Result?.Value ?? 0;
                if (sol < 50_000_000)
                {
                    Debug.LogWarning($"[CharCreate] Low SOL ({sol / 1_000_000_000f:F4}), requesting airdrop...");
                    await SolanaManager.Instance.EnsureSolBalanceAsync();
                }

                var characterMintPda = _anchorClient.DeriveCharacterMintPda(playerPk);
                var mintAcct = await rpc.GetAccountInfoAsync(characterMintPda, Commitment.Processed);
                bool mintExists = mintAcct?.Result?.Value?.Data != null;

                TransactionInstruction[] ixs;
                if (mintExists)
                {
                    Debug.Log($"[CharCreate] Mint exists, fetching existing profile...");
                    var existingProfile = await SolanaManager.Instance.FetchProfileAsync(_anchorClient);
                    if (existingProfile != null && existingProfile.characterMint != "11111111111111111111111111111111")
                    {
                        Debug.Log($"[CharCreate] Profile already exists: class={existingProfile.class_}, level={existingProfile.level}");
                        if (_screenManager != null) _screenManager.ShowScreen("MainHub");
                        return;
                    }

                    Debug.Log($"[CharCreate] Profile empty or invalid, sending recreate_profile: class={classType}");
                    ixs = new TransactionInstruction[]
                    {
                        _anchorClient.BuildRecreateProfileInstruction(playerPk, classType),
                    };
                }
                else
                {
                    Debug.Log($"[CharCreate] No mint, using create_character: name={name}, class={classType}");
                    var cfg = GameConfigProvider.Instance?.Config;
                    var uri = classType switch
                    {
                        0 => cfg?.warriorMetadataUri ?? "",
                        1 => cfg?.rogueMetadataUri ?? "",
                        2 => cfg?.mageMetadataUri ?? "",
                        _ => ""
                    };
                    ixs = new[] { _anchorClient.BuildCreateCharacterInstruction(playerPk, name, "RAE_HERO", uri, classType) };
                }

                var txSig = await _anchorClient.SendTransactionAsync(ixs);
                Debug.Log($"[CharCreate] Transaction sent: {txSig}");

                await UniTask.Delay(4000);

                Debug.Log("[CharCreate] Fetching profile (up to 5 attempts)...");
                var profile = await SolanaManager.Instance.FetchProfileAsync(_anchorClient);
                if (profile != null)
                {
                    Debug.Log($"[CharCreate] Profile loaded: class={profile.class_}, level={profile.level}, mint={profile.characterMint}");
                }
                else
                {
                    Debug.LogError("[CharCreate] Profile not found. Check tx on explorer.");
                    if (errorText != null) errorText.text = $"Tx sent but profile not found. Tx: {txSig}";
                }

                if (_screenManager != null) _screenManager.ShowScreen("MainHub");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharCreate] Error: {ex.Message}");
                if (errorText != null) errorText.text = $"Error: {ex.Message}";
                if (createButton != null) createButton.interactable = true;
            }
        }
    }
}
