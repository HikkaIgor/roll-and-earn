using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class InventoryScreen : MonoBehaviour
    {
        [SerializeField] private CardView weaponSlot;
        [SerializeField] private CardView armorSlot;
        [SerializeField] private Button unequipWeaponButton;
        [SerializeField] private Button unequipArmorButton;
        [SerializeField] private Transform itemGrid;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text bonusSummaryText;

        private NFTManager _nftManager;
        private AnchorClient _anchorClient;
        private List<GameObject> _itemCardObjects = new();
        private List<(string mint, ItemData data)> _items = new();
        private bool _gridInitialized;

        private void Awake()
        {
            _nftManager = FindAnyObjectByType<NFTManager>();
            _anchorClient = FindAnyObjectByType<AnchorClient>();
        }

        private void Start()
        {
            if (_nftManager == null) _nftManager = FindAnyObjectByType<NFTManager>();
            if (_anchorClient == null) _anchorClient = FindAnyObjectByType<AnchorClient>();
            EnsureGridLayout();
            FontProvider.ApplyToText(statusText, false);
            FontProvider.ApplyToText(bonusSummaryText, true);
        }

        private void EnsureGridLayout()
        {
            if (_gridInitialized || itemGrid == null) return;
            _gridInitialized = true;

            var gridTransform = itemGrid as RectTransform;
            if (gridTransform == null) return;

            var parent = gridTransform.parent;
            if (parent == null) return;
            var parentRect = parent as RectTransform;
            if (parentRect == null) return;

            bool hasScroll = parentRect.GetComponent<ScrollRect>() != null;
            if (!hasScroll)
            {
                var scrollRect = parentRect.gameObject.AddComponent<ScrollRect>();
                if (parentRect.GetComponent<Image>() == null)
                {
                    var viewport = parentRect.gameObject.AddComponent<Image>();
                    viewport.color = Color.clear;
                }
                if (parentRect.GetComponent<Mask>() == null)
                    parentRect.gameObject.AddComponent<Mask>().showMaskGraphic = false;

                scrollRect.content = gridTransform;
                scrollRect.viewport = parentRect;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Elastic;
                scrollRect.elasticity = 0.1f;
            }

            var existingLayout = gridTransform.GetComponent<GridLayoutGroup>();
            if (existingLayout == null)
            {
                var gridLayout = gridTransform.gameObject.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(150, 100);
                gridLayout.spacing = new Vector2(10, 10);
                gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
                gridLayout.childAlignment = TextAnchor.UpperCenter;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 8;
            }

            var gridRect = gridTransform;
            gridRect.sizeDelta = new Vector2(gridRect.sizeDelta.x, 100);

            if (bonusSummaryText != null)
            {
                var bsRect = bonusSummaryText.rectTransform;
                bsRect.sizeDelta = new Vector2(bsRect.sizeDelta.x, 40);
            }
        }

        private void OnEnable()
        {
            unequipWeaponButton?.onClick.AddListener(OnUnequipWeapon);
            unequipArmorButton?.onClick.AddListener(OnUnequipArmor);
            LoadInventoryAsync().Forget();
        }

        private void OnDisable()
        {
            unequipWeaponButton?.onClick.RemoveAllListeners();
            unequipArmorButton?.onClick.RemoveAllListeners();
        }

        public void ReloadInventory()
        {
            LoadInventoryAsync().Forget();
        }

        private async UniTaskVoid LoadInventoryAsync()
        {
            if (statusText != null) statusText.text = "Loading inventory...";
            try
            {
                string owner = SolanaManager.Instance?.PublicKey;
                if (string.IsNullOrEmpty(owner)) return;

                var nfts = await _nftManager.GetItemNftsAsync(owner);

                var rpc = SolanaManager.Instance.GetRpcClient();
                var player = new Solana.Unity.Wallet.PublicKey(owner);
                var profilePda = _anchorClient.DerivePlayerProfilePda(player);
                var acct = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acct?.Result?.Value?.Data != null)
                {
                    var data = Convert.FromBase64String(acct.Result.Value.Data[0]);
                    SolanaManager.Instance.CurrentProfile = PlayerProfile.DeserializeFromAccount(data);
                }

                PopulateGrid(nfts);
                UpdateEquipmentSlots();
                UpdateBonusSummary();
                if (statusText != null) statusText.text = nfts.Count > 0 ? $"Items: {nfts.Count}" : "No items yet";
            }
            catch (Exception e)
            {
                if (statusText != null) statusText.text = $"Error: {e.Message}";
            }
        }

        private void PopulateGrid(List<NftMetadata> nfts)
        {
            ClearGrid();
            _items.Clear();

            for (int i = 0; i < nfts.Count; i++)
            {
                var nft = nfts[i];
                bool hasAttrs = nft.attributes != null && nft.attributes.Count > 0;
                var itemData = hasAttrs
                    ? ItemData.FromNftMetadata(nft, nft.mint)
                    : ItemData.FromMintAddress(nft.mint);

                if (itemData.rollBonus == 0)
                    itemData.rollBonus = ItemData.ComputeBonusFromMint(nft.mint);

                CreateItemCard(itemData, i);
                _items.Add((nft.mint, itemData));
            }
        }

        private void CreateItemCard(ItemData itemData, int index)
        {
            EnsureGridLayout();

            var cardObj = new GameObject($"Item_{index}");
            cardObj.transform.SetParent(itemGrid, false);
            var rect = cardObj.AddComponent<RectTransform>();
            cardObj.AddComponent<CanvasRenderer>();

            var rarityColor = ThemeColors.GetRarityColor(itemData.rarity);
            var rarityGlow = ThemeColors.GetRarityGlow(itemData.rarity);

            var glowRect = UIHelper.CreateOutset("Glow", cardObj.transform, 3f);
            var glowImg = glowRect.gameObject.AddComponent<Image>();
            glowImg.sprite = UIHelper.GetRoundedRectSmall();
            glowImg.color = rarityGlow;
            glowImg.type = Image.Type.Sliced;
            glowImg.raycastTarget = false;

            var borderObj = UIHelper.CreateStretch("Border", cardObj.transform, 0f);
            var borderImg = borderObj.gameObject.AddComponent<Image>();
            borderImg.sprite = UIHelper.GetRoundedRectSmall();
            borderImg.color = rarityColor;
            borderImg.type = Image.Type.Sliced;

            var innerObj = UIHelper.CreateStretch("Inner", borderObj, 2f);
            var innerImg = innerObj.gameObject.AddComponent<Image>();
            innerImg.sprite = UIHelper.GetRoundedRectSmall();
            innerImg.color = ThemeColors.CardBg;
            innerImg.type = Image.Type.Sliced;

            var headerRect = UIHelper.CreateAnchored("Header", innerObj,
                new Vector2(0f, 0.65f), new Vector2(1f, 0.98f));
            var headerImg = headerRect.gameObject.AddComponent<Image>();
            headerImg.sprite = UIHelper.GetRoundedRectSmall();
            headerImg.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.15f);
            headerImg.type = Image.Type.Sliced;

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(innerObj, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.06f, 0.58f);
            nameRect.anchorMax = new Vector2(0.94f, 0.95f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = 11;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = ThemeColors.TextPrimary;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.text = itemData.itemName;
            FontProvider.ApplyToText(nameText, true);

            var statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(innerObj, false);
            var statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.06f, 0.05f);
            statsRect.anchorMax = new Vector2(0.94f, 0.55f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            var statsText = statsObj.AddComponent<TextMeshProUGUI>();
            statsText.alignment = TextAlignmentOptions.Center;
            statsText.fontSize = 9;
            statsText.color = ThemeColors.TextSecondary;
            statsText.textWrappingMode = TextWrappingModes.NoWrap;
            statsText.overflowMode = TextOverflowModes.Ellipsis;
            statsText.text = $"{itemData.type} | +{itemData.rollBonus} rolls\n{itemData.rarity}";

            var btn = cardObj.AddComponent<Button>();
            int idx = index;
            btn.onClick.AddListener(() => OnItemClicked(idx));

            _itemCardObjects.Add(cardObj);
        }

        private void ClearGrid()
        {
            foreach (var obj in _itemCardObjects)
                if (obj != null) Destroy(obj);
            _itemCardObjects.Clear();
        }

        private void OnItemClicked(int index)
        {
            var profile = SolanaManager.Instance.CurrentProfile;
            if (profile == null || index >= _items.Count) return;

            EquipItemAsync(index).Forget();
        }

        private async UniTaskVoid EquipItemAsync(int index)
        {
            try
            {
                var (mint, data) = _items[index];
                var player = new Solana.Unity.Wallet.PublicKey(SolanaManager.Instance.PublicKey);
                var itemMint = new Solana.Unity.Wallet.PublicKey(mint);
                var itemTokenAccount = Solana.Unity.Programs.AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(player, itemMint);

                byte itemType = data.type == ItemData.ItemType.Armor ? (byte)1 : (byte)0;
                var ix = _anchorClient.BuildEquipItemInstruction(player, itemMint, itemTokenAccount, itemType);
                await _anchorClient.SendTransactionAsync(new[] { ix });

                SoundManager.Instance.PlayEquip();
                await UniTask.Delay(1000);

                var rpc = SolanaManager.Instance.GetRpcClient();
                var profilePda = _anchorClient.DerivePlayerProfilePda(player);
                var acct = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acct?.Result?.Value?.Data != null)
                {
                    var acctData = Convert.FromBase64String(acct.Result.Value.Data[0]);
                    SolanaManager.Instance.CurrentProfile = PlayerProfile.DeserializeFromAccount(acctData);
                }

                UpdateEquipmentSlots();
                UpdateBonusSummary();
                if (statusText != null) statusText.text = $"Equipped {data.itemName} (+{data.rollBonus} to rolls)!";
            }
            catch (Exception e)
            {
                if (statusText != null) statusText.text = $"Error: {e.Message}";
            }
        }

        private void UpdateEquipmentSlots()
        {
            var profile = SolanaManager.Instance.CurrentProfile;
            if (profile == null) return;

            if (profile.HasEquippedWeapon)
            {
                var weaponData = ItemData.FromMintAddress(profile.equippedWeapon);
                if (weaponSlot != null) weaponSlot.DisplayItem(weaponData);
                if (unequipWeaponButton != null) unequipWeaponButton.gameObject.SetActive(true);
            }
            else
            {
                if (weaponSlot != null) weaponSlot.Clear();
                if (unequipWeaponButton != null) unequipWeaponButton.gameObject.SetActive(false);
            }

            if (profile.HasEquippedArmor)
            {
                var armorData = ItemData.FromMintAddress(profile.equippedArmor);
                if (armorSlot != null) armorSlot.DisplayItem(armorData);
                if (unequipArmorButton != null) unequipArmorButton.gameObject.SetActive(true);
            }
            else
            {
                if (armorSlot != null) armorSlot.Clear();
                if (unequipArmorButton != null) unequipArmorButton.gameObject.SetActive(false);
            }
        }

        private void UpdateBonusSummary()
        {
            if (bonusSummaryText == null) return;
            var profile = SolanaManager.Instance.CurrentProfile;
            if (profile != null && profile.TotalEquipmentBonus > 0)
            {
                bonusSummaryText.text = $"Total Roll Bonus: +{profile.TotalEquipmentBonus} (Weapon: +{profile.weaponBonus}, Armor: +{profile.armorBonus})";
                bonusSummaryText.color = ThemeColors.Primary;
            }
            else
            {
                bonusSummaryText.text = "No equipment bonus active";
                bonusSummaryText.color = ThemeColors.TextMuted;
            }
        }

        private async void OnUnequipWeapon()
        {
            await UnequipAsync(0);
        }

        private async void OnUnequipArmor()
        {
            await UnequipAsync(1);
        }

        private async UniTask UnequipAsync(byte itemType)
        {
            try
            {
                var player = new Solana.Unity.Wallet.PublicKey(SolanaManager.Instance.PublicKey);
                var ix = _anchorClient.BuildUnequipItemInstruction(player, itemType);
                await _anchorClient.SendTransactionAsync(new[] { ix });

                SoundManager.Instance.PlayUnequip();

                await UniTask.Delay(1000);

                var rpc = SolanaManager.Instance.GetRpcClient();
                var profilePda = _anchorClient.DerivePlayerProfilePda(player);
                var acct = await rpc.GetAccountInfoAsync(profilePda, Solana.Unity.Rpc.Types.Commitment.Processed);
                if (acct?.Result?.Value?.Data != null)
                {
                    var data = Convert.FromBase64String(acct.Result.Value.Data[0]);
                    SolanaManager.Instance.CurrentProfile = PlayerProfile.DeserializeFromAccount(data);
                }

                UpdateEquipmentSlots();
                UpdateBonusSummary();
                if (statusText != null) statusText.text = itemType == 0 ? "Weapon unequipped" : "Armor unequipped";
            }
            catch (Exception e)
            {
                if (statusText != null) statusText.text = $"Error: {e.Message}";
            }
        }
    }
}
