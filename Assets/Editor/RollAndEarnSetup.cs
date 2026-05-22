using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RollAndEarn
{
    public static class RollAndEarnSetup
    {
        private const string AssetPath = "Assets/Resources/RollAndEarn";
        private const string SpritePath = "Assets/Resources/RollAndEarn/Sprites";
        private const string ROLAND_MINT = "G47BKqhe57LFL8hSL4MwSJ545d5EL6XFgAy9nkoHNjbB";
        private const string PROGRAM_ID = "GZFENGPA9g1rcvUHUTBY5HoEgmFjJyJoBhLHT9A2wQ8T";

        [MenuItem("RollAndEarn/Setup Everything")]
        public static void SetupEverything()
        {
            CreateScriptableObjects();
            GenerateSprites();
            SetupScene();
            Debug.Log("[RollAndEarn] Full setup complete!");
        }

        [MenuItem("RollAndEarn/Create ScriptableObjects")]
        public static void CreateScriptableObjects()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(AssetPath);
            CreateCharacterClasses();
            CreateAdventureConfigs();
            CreateGameConfig();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("RollAndEarn/Generate Sprites")]
        public static void GenerateSprites()
        {
            EnsureFolder(SpritePath);
            CreateTtgSprite("DiceFace1", new Color(0.06f, 0.08f, 0.12f), new Color(0.29f, 0.87f, 0.50f), 128, 128);
            CreateTtgSprite("DiceFace2", new Color(0.06f, 0.08f, 0.12f), new Color(0.38f, 0.65f, 0.98f), 128, 128);
            CreateTtgSprite("DiceFace3", new Color(0.06f, 0.08f, 0.12f), new Color(0.29f, 0.87f, 0.50f), 128, 128);
            CreateTtgSprite("DiceFace4", new Color(0.06f, 0.08f, 0.12f), new Color(1f, 0.84f, 0f), 128, 128);
            CreateTtgSprite("DiceFace5", new Color(0.06f, 0.08f, 0.12f), new Color(0.6f, 0.35f, 0.9f), 128, 128);
            CreateTtgSprite("DiceFace6", new Color(0.06f, 0.08f, 0.12f), new Color(0.92f, 0.32f, 0.22f), 128, 128);

            CreateTtgSprite("WarriorCard", new Color(0.08f, 0.03f, 0.02f), new Color(0.4f, 0.1f, 0.08f), 200, 280);
            CreateTtgSprite("RogueCard", new Color(0.02f, 0.06f, 0.03f), new Color(0.06f, 0.28f, 0.12f), 200, 280);
            CreateTtgSprite("MageCard", new Color(0.03f, 0.03f, 0.08f), new Color(0.1f, 0.12f, 0.35f), 200, 280);
            CreateTtgSprite("ItemPlaceholder", new Color(0.05f, 0.06f, 0.09f), new Color(0.16f, 0.2f, 0.28f), 100, 140);
            CreateTtgSprite("AdventureBg", new Color(0.05f, 0.06f, 0.1f), new Color(0.16f, 0.2f, 0.28f), 400, 200);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RollAndEarn] Sprites generated!");
        }

        [MenuItem("RollAndEarn/Setup Scene")]
        public static void SetupScene()
        {
            var existingCanvas = Object.FindObjectOfType<Canvas>();
            if (existingCanvas != null) Object.DestroyImmediate(existingCanvas.gameObject);
            var oldCore = GameObject.Find("___CoreSystems");
            if (oldCore != null) Object.DestroyImmediate(oldCore);

            var config = Resources.Load<GameConfig>("RollAndEarn/GameConfig");
            var canvasObj = CreateCanvas();
            CreateCoreSystems(config, canvasObj.transform);
            CreateAllScreens(canvasObj.transform);
            CreateBottomNav(canvasObj.transform);
            Debug.Log("[RollAndEarn] Scene setup complete!");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path);
                string name = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void CreateTtgSprite(string name, Color baseColor, Color borderColor, int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float gradientT = (float)y / h;
                    Color grad = Color.Lerp(baseColor * 1.15f, baseColor * 0.85f, gradientT);
                    bool isBorder = x < 1 || x >= w - 1 || y < 1 || y >= h - 1;
                    pixels[y * w + x] = isBorder ? borderColor : grad;
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            string filePath = $"{SpritePath}/{name}.png";
            System.IO.File.WriteAllBytes(filePath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritePath}/{name}.png");

        #region ScriptableObjects

        private static void CreateCharacterClasses()
        {
            CreateCharClass("Warrior", 0, 10, 6, 4, 5, "+10 to combat rolls");
            CreateCharClass("Rogue", 1, 5, 10, 6, 8, "+10 to trap rolls");
            CreateCharClass("Mage", 2, 4, 5, 10, 6, "+10 to magic rolls");
        }

        private static void CreateCharClass(string name, byte type, byte str, byte agi, byte intel, byte luck, string bonus)
        {
            var path = $"{AssetPath}/{name}.asset";
            var so = AssetDatabase.LoadAssetAtPath<CharacterClassSO>(path);
            if (so == null) { so = ScriptableObject.CreateInstance<CharacterClassSO>(); AssetDatabase.CreateAsset(so, path); }
            so.className = name; so.classType = type; so.baseStrength = str;
            so.baseAgility = agi; so.baseIntelligence = intel; so.baseLuck = luck;
            so.rollBonusDescription = bonus;
            var sprite = LoadSprite($"{name}Card");
            if (sprite != null) so.artSprite = sprite;
            EditorUtility.SetDirty(so);
        }

        private static void CreateAdventureConfigs()
        {
            CreateAdv("EnchantedForest", 0, 0, 300, "A mystical forest path",
                5_000_000_000, 15_000_000_000, 30_000_000_000, 30_000_000_000,
                5, 15, 25, 35, 6, 11, 20);
            CreateAdv("DarkDungeon", 1, 10_000_000_000, 900, "A cursed dungeon",
                5_000_000_000, 25_000_000_000, 60_000_000_000, 60_000_000_000,
                10, 25, 50, 75, 5, 9, 18);
            CreateAdv("DragonsLair", 2, 50_000_000_000, 3600, "Face the dragon!",
                10_000_000_000, 50_000_000_000, 150_000_000_000, 150_000_000_000,
                20, 50, 100, 150, 4, 8, 16);
        }

        private static void CreateAdv(string name, byte type, ulong cost, int cd, string desc,
            ulong lowR, ulong midR, ulong highR, ulong specR,
            uint lowXP, uint midXP, uint highXP, uint specXP,
            byte midS, byte highS, byte specS)
        {
            var path = $"{AssetPath}/{name}.asset";
            var so = AssetDatabase.LoadAssetAtPath<AdventureConfigSO>(path);
            if (so == null) { so = ScriptableObject.CreateInstance<AdventureConfigSO>(); AssetDatabase.CreateAsset(so, path); }
            so.adventureName = name; so.adventureType = type; so.cost = cost; so.cooldownSeconds = cd;
            so.description = desc; so.lowReward = lowR; so.midReward = midR; so.highReward = highR; so.specialReward = specR;
            so.lowXp = lowXP; so.midXp = midXP; so.highXp = highXP; so.specialXp = specXP;
            so.midStart = midS; so.highStart = highS; so.specialStart = specS;
            var sprite = LoadSprite("AdventureBg");
            if (sprite != null) so.artSprite = sprite;
            EditorUtility.SetDirty(so);
        }

        private static void CreateGameConfig()
        {
            var path = $"{AssetPath}/GameConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (config == null) { config = ScriptableObject.CreateInstance<GameConfig>(); AssetDatabase.CreateAsset(config, path); }
            config.rpcEndpoint = "https://api.devnet.solana.com";
            config.websocketEndpoint = "wss://api.devnet.solana.com";
            config.rolandMintAddress = ROLAND_MINT;
            config.programId = PROGRAM_ID;
            config.ipfsGateway = "https://ipfs.io/ipfs/";
            config.faucetAmount = 100_000_000_000;
            var warrior = AssetDatabase.LoadAssetAtPath<CharacterClassSO>($"{AssetPath}/Warrior.asset");
            var rogue = AssetDatabase.LoadAssetAtPath<CharacterClassSO>($"{AssetPath}/Rogue.asset");
            var mage = AssetDatabase.LoadAssetAtPath<CharacterClassSO>($"{AssetPath}/Mage.asset");
            var forest = AssetDatabase.LoadAssetAtPath<AdventureConfigSO>($"{AssetPath}/EnchantedForest.asset");
            var dungeon = AssetDatabase.LoadAssetAtPath<AdventureConfigSO>($"{AssetPath}/DarkDungeon.asset");
            var dragon = AssetDatabase.LoadAssetAtPath<AdventureConfigSO>($"{AssetPath}/DragonsLair.asset");
            if (warrior && rogue && mage) config.CharacterClasses = new[] { warrior, rogue, mage };
            if (forest && dungeon && dragon) config.Adventures = new[] { forest, dungeon, dragon };
            EditorUtility.SetDirty(config);
        }

        #endregion

        #region Canvas & Core

        private static GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("Background");
            bg.transform.SetParent(canvasObj.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bg.AddComponent<CanvasRenderer>();
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = ThemeColors.Background;
            bgImg.raycastTarget = false;

            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(canvasObj.transform, false);
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            return canvasObj;
        }

        private static void CreateCoreSystems(GameConfig config, Transform canvasParent)
        {
            var systemsObj = new GameObject("___CoreSystems");
            systemsObj.transform.SetParent(canvasParent, false);
            try
            {
                var w3 = new GameObject("Web3"); w3.transform.SetParent(systemsObj.transform, false);
                var web3 = w3.AddComponent<Solana.Unity.SDK.Web3>();
                var web3SO = new SerializedObject(web3);
                web3SO.FindProperty("rpcCluster").intValue = 1;
                web3SO.FindProperty("customRpc").stringValue = "https://api.devnet.solana.com";
                web3SO.FindProperty("webSocketsRpc").stringValue = "wss://api.devnet.solana.com";
                web3SO.FindProperty("autoConnectOnStartup").boolValue = false;
                web3SO.ApplyModifiedProperties();
            }
            catch (System.Exception e) { Debug.LogWarning("[RollAndEarn] Web3: " + e.Message); }
            try { var sm = new GameObject("SolanaManager"); sm.transform.SetParent(systemsObj.transform, false); sm.AddComponent<SolanaManager>(); } catch { }
            try
            {
                var prov = new GameObject("GameConfigProvider"); prov.transform.SetParent(systemsObj.transform, false);
                var p = prov.AddComponent<GameConfigProvider>();
                if (config) { var so = new SerializedObject(p); so.FindProperty("config").objectReferenceValue = config; so.ApplyModifiedProperties(); }
            }
            catch { }
            try { var ac = new GameObject("AnchorClient"); ac.transform.SetParent(systemsObj.transform, false); ac.AddComponent<AnchorClient>(); } catch { }
            try { var tm = new GameObject("TokenManager"); tm.transform.SetParent(systemsObj.transform, false); tm.AddComponent<TokenManager>(); } catch { }
            try { var nm = new GameObject("NFTManager"); nm.transform.SetParent(systemsObj.transform, false); nm.AddComponent<NFTManager>(); } catch { }
            try { var am = new GameObject("AdventureManager"); am.transform.SetParent(systemsObj.transform, false); am.AddComponent<AdventureManager>(); } catch { }
            try { var cm = new GameObject("CooldownManager"); cm.transform.SetParent(systemsObj.transform, false); cm.AddComponent<CooldownManager>(); } catch { }
        }

        #endregion

        #region Shared UI Helpers

        private static GameObject CreateScreen(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = ThemeColors.Panel;
            img.raycastTarget = true;
            return go;
        }

        private static VerticalLayoutGroup AddVerticalLayout(GameObject go, int padding, int spacing, int top, int bottom)
        {
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = spacing;
            vlg.padding = new RectOffset(padding, padding, top, bottom);
            return vlg;
        }

        private static GameObject MakeButton(Transform parent, string name, string text, float height, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.94f, 0.97f);
            colors.pressedColor = new Color(0.75f, 0.8f, 0.87f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minWidth = 160;
            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(go.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(10, 4); txtRect.offsetMax = new Vector2(-10, -4);
            txtGo.AddComponent<CanvasRenderer>();
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ThemeColors.TextPrimary;
            return go;
        }

        private static GameObject MakeHeroButton(Transform parent, string name, string text, float height)
        {
            var border = new GameObject(name);
            border.transform.SetParent(parent, false);
            var borderRect = border.AddComponent<RectTransform>();
            border.AddComponent<CanvasRenderer>();
            border.AddComponent<Image>().color = ThemeColors.Primary;
            var le = border.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minWidth = 200;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(border.transform, false);
            var innerRect = inner.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero; innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(1, 1); innerRect.offsetMax = new Vector2(-1, -1);
            inner.AddComponent<CanvasRenderer>();
            inner.AddComponent<Image>().color = ThemeColors.ButtonBg;

            var btn = border.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.98f, 0.96f);
            colors.pressedColor = new Color(0.85f, 0.92f, 0.88f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var txtGo = new GameObject("Label");
            txtGo.transform.SetParent(inner.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = new Vector2(10, 4); txtRect.offsetMax = new Vector2(-10, -4);
            txtGo.AddComponent<CanvasRenderer>();
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ThemeColors.Primary;
            tmp.fontStyle = FontStyles.Bold;
            return border;
        }

        private static GameObject MakeLabel(Transform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize;
            tmp.alignment = alignment; tmp.color = color;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = fontSize + 14;
            return go;
        }

        private static GameObject MakeSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            return go;
        }

        private static GameObject MakeCardView(Transform parent, string name, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            var borderImg = go.AddComponent<Image>();
            borderImg.color = ThemeColors.CardBorder;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;

            var inner = new GameObject("Inner");
            inner.transform.SetParent(go.transform, false);
            var innerRect = inner.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero; innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(1, 1); innerRect.offsetMax = new Vector2(-1, -1);
            inner.AddComponent<CanvasRenderer>();
            inner.AddComponent<Image>().color = ThemeColors.CardInner;

            var artGo = new GameObject("Art");
            artGo.transform.SetParent(inner.transform, false);
            var artRect = artGo.AddComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0, 0.5f); artRect.anchorMax = new Vector2(1, 1);
            artRect.offsetMin = Vector2.zero; artRect.offsetMax = Vector2.zero;
            artGo.AddComponent<CanvasRenderer>();
            var artImg = artGo.AddComponent<Image>();
            artImg.color = Color.clear; artImg.preserveAspect = true;

            var nameGo = new GameObject("NameText");
            nameGo.transform.SetParent(inner.transform, false);
            var nameRect = nameGo.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.35f); nameRect.anchorMax = new Vector2(1, 0.55f);
            nameRect.offsetMin = new Vector2(8, 0); nameRect.offsetMax = new Vector2(-8, 0);
            nameGo.AddComponent<CanvasRenderer>();
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 16; nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = ThemeColors.TextPrimary;

            var statsGo = new GameObject("StatsText");
            statsGo.transform.SetParent(inner.transform, false);
            var statsRect = statsGo.AddComponent<RectTransform>();
            statsRect.anchorMin = Vector2.zero; statsRect.anchorMax = new Vector2(1, 0.35f);
            statsRect.offsetMin = new Vector2(8, 4); statsRect.offsetMax = new Vector2(-8, -2);
            statsGo.AddComponent<CanvasRenderer>();
            var statsTmp = statsGo.AddComponent<TextMeshProUGUI>();
            statsTmp.fontSize = 13; statsTmp.alignment = TextAlignmentOptions.Center;
            statsTmp.color = ThemeColors.TextSecondary;

            var cardView = go.AddComponent<CardView>();
            var cvSO = new SerializedObject(cardView);
            cvSO.FindProperty("artImage").objectReferenceValue = artImg;
            cvSO.FindProperty("nameText").objectReferenceValue = nameTmp;
            cvSO.FindProperty("statsText").objectReferenceValue = statsTmp;
            cvSO.FindProperty("borderImage").objectReferenceValue = borderImg;
            cvSO.ApplyModifiedProperties();
            return go;
        }

        private static GameObject MakeSurfacePanel(Transform parent, string name, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<Image>().color = ThemeColors.Surface;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleHeight = 1;
            return go;
        }

        #endregion

        #region Screens

        private static void CreateAllScreens(Transform canvasParent)
        {
            var screensParent = new GameObject("Screens");
            screensParent.transform.SetParent(canvasParent, false);
            var screensRect = screensParent.AddComponent<RectTransform>();
            screensRect.anchorMin = Vector2.zero; screensRect.anchorMax = new Vector2(1f, 1f);
            screensRect.pivot = new Vector2(0.5f, 0.5f);
            screensRect.offsetMin = new Vector2(0, 60); screensRect.offsetMax = Vector2.zero;

            var smGo = new GameObject("ScreenManager");
            smGo.transform.SetParent(canvasParent, false);
            var sm = smGo.AddComponent<ScreenManager>();

            var wcGo = BuildWalletConnectScreen(screensParent.transform);
            var ccGo = BuildCharacterCreationScreen(screensParent.transform);
            var hubGo = BuildMainHubScreen(screensParent.transform);
            var advGo = BuildAdventureScreen(screensParent.transform);
            var invGo = BuildInventoryScreen(screensParent.transform);
            var walGo = BuildWalletProfileScreen(screensParent.transform);

            wcGo.SetActive(false); ccGo.SetActive(false); hubGo.SetActive(false);
            advGo.SetActive(false); invGo.SetActive(false); walGo.SetActive(false);

            var smSO = new SerializedObject(sm);
            smSO.FindProperty("walletConnectScreen").objectReferenceValue = wcGo;
            smSO.FindProperty("characterCreationScreen").objectReferenceValue = ccGo;
            smSO.FindProperty("mainHubScreen").objectReferenceValue = hubGo;
            smSO.FindProperty("adventureScreen").objectReferenceValue = advGo;
            smSO.FindProperty("inventoryScreen").objectReferenceValue = invGo;
            smSO.FindProperty("walletProfileScreen").objectReferenceValue = walGo;
            smSO.ApplyModifiedProperties();
        }

        #endregion

        #region WalletConnect

        private static GameObject BuildWalletConnectScreen(Transform parent)
        {
            var go = CreateScreen("WalletConnectScreen", parent);
            var vlg = AddVerticalLayout(go, 80, 20, 120, 80);

            MakeSpacer(go.transform, 40);
            var titleGo = MakeLabel(go.transform, "Title", "ROLL & EARN", 48, ThemeColors.Primary);
            titleGo.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            MakeSpacer(go.transform, 8);
            MakeLabel(go.transform, "Subtitle", "A Fantasy Adventure on Solana", 20, ThemeColors.TextSecondary);
            MakeSpacer(go.transform, 60);
            MakeLabel(go.transform, "Status", "Connect your wallet to begin", 18, ThemeColors.TextMuted);
            MakeSpacer(go.transform, 30);
            var btnGo = MakeHeroButton(go.transform, "ConnectBtn", "CONNECT WALLET", 56);

            var script = go.AddComponent<WalletConnectScreen>();
            var so = new SerializedObject(script);
            so.FindProperty("connectButton").objectReferenceValue = btnGo.GetComponent<Button>();
            so.FindProperty("statusText").objectReferenceValue = go.transform.Find("Status").GetComponent<TMP_Text>();
            so.FindProperty("titleLabel").objectReferenceValue = go.transform.Find("Title").GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();
            return go;
        }

        #endregion

        #region CharacterCreation

        private static GameObject BuildCharacterCreationScreen(Transform parent)
        {
            var go = CreateScreen("CharacterCreationScreen", parent);
            var vlg = AddVerticalLayout(go, 60, 12, 40, 40);

            MakeLabel(go.transform, "Title", "Create Your Character", 32, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            MakeSpacer(go.transform, 10);

            var nameInputGo = new GameObject("NameInput");
            nameInputGo.transform.SetParent(go.transform, false);
            nameInputGo.AddComponent<RectTransform>();
            nameInputGo.AddComponent<CanvasRenderer>();
            var nameInputBg = nameInputGo.AddComponent<Image>();
            nameInputBg.color = ThemeColors.InputFieldBg;
            var nameInput = nameInputGo.AddComponent<TMP_InputField>();
            nameInputGo.AddComponent<LayoutElement>().preferredHeight = 48;
            var ph = MakeLabel(nameInputGo.transform, "Placeholder", "Enter character name...", 16, ThemeColors.TextMuted, TextAlignmentOptions.Left);
            ph.GetComponent<RectTransform>().anchorMin = Vector2.zero; ph.GetComponent<RectTransform>().anchorMax = Vector2.one;
            ph.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0); ph.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);
            var txt = MakeLabel(nameInputGo.transform, "Text", "", 16, ThemeColors.TextPrimary, TextAlignmentOptions.Left);
            txt.GetComponent<RectTransform>().anchorMin = Vector2.zero; txt.GetComponent<RectTransform>().anchorMax = Vector2.one;
            txt.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0); txt.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);
            nameInput.textViewport = txt.GetComponent<RectTransform>();
            nameInput.textComponent = txt.GetComponent<TMP_Text>();
            nameInput.placeholder = ph.GetComponent<TMP_Text>();

            MakeSpacer(go.transform, 8);
            MakeLabel(go.transform, "ClassLabel", "Choose your class:", 18, ThemeColors.TextSecondary);

            var classBtns = new GameObject("ClassButtons");
            classBtns.transform.SetParent(go.transform, false);
            classBtns.AddComponent<RectTransform>();
            classBtns.AddComponent<LayoutElement>().preferredHeight = 110;
            var hlg = classBtns.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.spacing = 12;
            hlg.padding = new RectOffset(20, 20, 5, 5);

            Color[] classColors = { ThemeColors.ClassWarrior, ThemeColors.ClassRogue, ThemeColors.ClassMage };
            string[] classNames = { "Warrior", "Rogue", "Mage" };
            Button[] buttons = new Button[3];
            TMP_Text[] statTexts = new TMP_Text[3];

            for (int i = 0; i < 3; i++)
            {
                var cardGo = new GameObject($"Class_{i}");
                cardGo.transform.SetParent(classBtns.transform, false);
                cardGo.AddComponent<RectTransform>();
                cardGo.AddComponent<CanvasRenderer>();
                cardGo.AddComponent<Image>().color = classColors[i];
                cardGo.AddComponent<LayoutElement>().preferredHeight = 100;
                buttons[i] = cardGo.AddComponent<Button>();
                var btnColors = buttons[i].colors;
                btnColors.normalColor = Color.white;
                btnColors.highlightedColor = new Color(0.92f, 0.96f, 0.94f);
                btnColors.pressedColor = new Color(0.8f, 0.9f, 0.85f);
                buttons[i].colors = btnColors;

                var nameLbl = new GameObject("Name");
                nameLbl.transform.SetParent(cardGo.transform, false);
                var nRect = nameLbl.AddComponent<RectTransform>();
                nRect.anchorMin = new Vector2(0, 0.6f); nRect.anchorMax = new Vector2(1, 1);
                nRect.offsetMin = new Vector2(4, 0); nRect.offsetMax = new Vector2(-4, 0);
                nameLbl.AddComponent<CanvasRenderer>();
                var nTmp = nameLbl.AddComponent<TextMeshProUGUI>();
                nTmp.text = classNames[i]; nTmp.fontSize = 16; nTmp.alignment = TextAlignmentOptions.Center;
                nTmp.color = ThemeColors.TextPrimary; nTmp.fontStyle = FontStyles.Bold;

                var statsLbl = new GameObject("Stats");
                statsLbl.transform.SetParent(cardGo.transform, false);
                var sRect = statsLbl.AddComponent<RectTransform>();
                sRect.anchorMin = Vector2.zero; sRect.anchorMax = new Vector2(1, 0.6f);
                sRect.offsetMin = new Vector2(4, 4); sRect.offsetMax = new Vector2(-4, -2);
                statsLbl.AddComponent<CanvasRenderer>();
                statTexts[i] = statsLbl.AddComponent<TextMeshProUGUI>();
                statTexts[i].fontSize = 11; statTexts[i].alignment = TextAlignmentOptions.Center;
                statTexts[i].color = ThemeColors.TextSecondary;
            }

            MakeSpacer(go.transform, 8);
            var previewGo = MakeCardView(go.transform, "PreviewCard", 130);
            MakeSpacer(go.transform, 8);

            var errGo = MakeLabel(go.transform, "ErrorText", "", 14, ThemeColors.Error);
            var createBtnGo = MakeHeroButton(go.transform, "CreateBtn", "CREATE CHARACTER", 52);

            var script = go.AddComponent<CharacterCreationScreen>();
            var so = new SerializedObject(script);
            var cbp = so.FindProperty("classButtons");
            cbp.arraySize = 3;
            for (int i = 0; i < 3; i++) cbp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            so.FindProperty("nameInput").objectReferenceValue = nameInput;
            so.FindProperty("createButton").objectReferenceValue = createBtnGo.GetComponent<Button>();
            so.FindProperty("previewCard").objectReferenceValue = previewGo.GetComponent<CardView>();
            so.FindProperty("errorText").objectReferenceValue = errGo.GetComponent<TMP_Text>();
            var stp = so.FindProperty("classStatTexts");
            stp.arraySize = 3;
            for (int i = 0; i < 3; i++) stp.GetArrayElementAtIndex(i).objectReferenceValue = statTexts[i];
            so.ApplyModifiedProperties();
            return go;
        }

        #endregion

        #region MainHub

        private static GameObject BuildMainHubScreen(Transform parent)
        {
            var go = CreateScreen("MainHubScreen", parent);
            var vlg = AddVerticalLayout(go, 40, 10, 30, 30);

            MakeLabel(go.transform, "BalanceLabel", "ROLAND BALANCE", 12, ThemeColors.TextMuted);
            var balGo = MakeLabel(go.transform, "BalanceText", "0.00 ROLAND", 28, ThemeColors.Primary);
            balGo.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            var solGo = MakeLabel(go.transform, "SolBalanceText", "0.0000 SOL", 14, ThemeColors.TextSecondary);

            var charCard = MakeCardView(go.transform, "CharacterCard", 170);

            var lvlRow = new GameObject("LevelRow");
            lvlRow.transform.SetParent(go.transform, false);
            lvlRow.AddComponent<RectTransform>();
            lvlRow.AddComponent<LayoutElement>().preferredHeight = 28;
            var lvlHlg = lvlRow.AddComponent<HorizontalLayoutGroup>();
            lvlHlg.childControlWidth = true; lvlHlg.childForceExpandWidth = true; lvlHlg.spacing = 10;

            var lvlGo = MakeLabel(lvlRow.transform, "LevelText", "Level 1", 20, ThemeColors.TextPrimary);
            lvlGo.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            var xpGo = MakeLabel(lvlRow.transform, "XPText", "XP: 0/200", 16, ThemeColors.TextSecondary);

            var sliderGo = new GameObject("XPBar");
            sliderGo.transform.SetParent(go.transform, false);
            sliderGo.AddComponent<RectTransform>();
            sliderGo.AddComponent<CanvasRenderer>();
            sliderGo.AddComponent<Image>().color = ThemeColors.XPBarBg;
            sliderGo.AddComponent<LayoutElement>().preferredHeight = 16;
            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = CreateSliderFill(sliderGo.transform);
            slider.handleRect = null;
            slider.value = 0; slider.maxValue = 1;

            MakeSpacer(go.transform, 8);

            var btnRow1 = new GameObject("BtnRow1");
            btnRow1.transform.SetParent(go.transform, false);
            btnRow1.AddComponent<RectTransform>();
            btnRow1.AddComponent<LayoutElement>().preferredHeight = 46;
            var hlg1 = btnRow1.AddComponent<HorizontalLayoutGroup>();
            hlg1.childControlWidth = true; hlg1.childForceExpandWidth = true; hlg1.spacing = 12;

            var advBtn = MakeButton(btnRow1.transform, "AdvBtn", "Adventure", 46, ThemeColors.SurfaceElevated);
            var invBtn = MakeButton(btnRow1.transform, "InvBtn", "Inventory", 46, ThemeColors.SurfaceElevated);

            var btnRow2 = new GameObject("BtnRow2");
            btnRow2.transform.SetParent(go.transform, false);
            btnRow2.AddComponent<RectTransform>();
            btnRow2.AddComponent<LayoutElement>().preferredHeight = 46;
            var hlg2 = btnRow2.AddComponent<HorizontalLayoutGroup>();
            hlg2.childControlWidth = true; hlg2.childForceExpandWidth = true; hlg2.spacing = 12;

            var walBtn = MakeButton(btnRow2.transform, "WalBtn", "Wallet", 46, ThemeColors.SurfaceElevated);
            var lvlUpBtn = MakeButton(btnRow2.transform, "LevelUpBtn", "LEVEL UP", 46, ThemeColors.LevelUpBtn);
            lvlUpBtn.GetComponentInChildren<TMP_Text>().color = ThemeColors.TextPrimary;

            var lvlUpCostGo = MakeLabel(go.transform, "LevelUpCost", "", 12, ThemeColors.TextMuted);
            var airBtn = MakeButton(go.transform, "DailyRewardBtn", "CLAIM DAILY REWARD", 42, ThemeColors.DailyBtn);
            airBtn.GetComponentInChildren<TMP_Text>().color = ThemeColors.TextPrimary;
            airBtn.GetComponentInChildren<TMP_Text>().fontSize = 18;
            var dailyRewardLabel = MakeLabel(go.transform, "DailyRewardText", "", 12, ThemeColors.TextMuted);

            var faucetBtn = MakeButton(go.transform, "FaucetBtn", "CLAIM 200 ROLAND (FREE)", 42, ThemeColors.DailyBtn);
            faucetBtn.GetComponentInChildren<TMP_Text>().color = ThemeColors.TextPrimary;
            faucetBtn.GetComponentInChildren<TMP_Text>().fontSize = 16;

            var equipBonusGo = MakeLabel(go.transform, "EquipBonus", "", 14, ThemeColors.Primary);

            var script = go.AddComponent<MainHubScreen>();
            var so = new SerializedObject(script);
            so.FindProperty("characterCard").objectReferenceValue = charCard.GetComponent<CardView>();
            so.FindProperty("balanceText").objectReferenceValue = balGo.GetComponent<TMP_Text>();
            so.FindProperty("solBalanceText").objectReferenceValue = solGo.GetComponent<TMP_Text>();
            so.FindProperty("levelText").objectReferenceValue = lvlGo.GetComponent<TMP_Text>();
            so.FindProperty("xpText").objectReferenceValue = xpGo.GetComponent<TMP_Text>();
            so.FindProperty("xpBar").objectReferenceValue = slider;
            so.FindProperty("adventureButton").objectReferenceValue = advBtn.GetComponent<Button>();
            so.FindProperty("inventoryButton").objectReferenceValue = invBtn.GetComponent<Button>();
            so.FindProperty("walletButton").objectReferenceValue = walBtn.GetComponent<Button>();
            so.FindProperty("dailyRewardButton").objectReferenceValue = airBtn.GetComponent<Button>();
            so.FindProperty("dailyRewardText").objectReferenceValue = dailyRewardLabel.GetComponent<TMP_Text>();
            so.FindProperty("levelUpButton").objectReferenceValue = lvlUpBtn.GetComponent<Button>();
            so.FindProperty("levelUpCostText").objectReferenceValue = lvlUpCostGo.GetComponent<TMP_Text>();
            so.FindProperty("faucetButton").objectReferenceValue = faucetBtn.GetComponent<Button>();
            so.FindProperty("equipmentBonusText").objectReferenceValue = equipBonusGo.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();
            return go;
        }

        private static RectTransform CreateSliderFill(Transform parent)
        {
            var fill = new GameObject("Fill");
            fill.transform.SetParent(parent, false);
            var rect = fill.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            fill.AddComponent<CanvasRenderer>();
            fill.AddComponent<Image>().color = ThemeColors.XPBarFill;
            return rect;
        }

        #endregion

        #region Adventure

        private static GameObject BuildAdventureScreen(Transform parent)
        {
            var go = CreateScreen("AdventureScreen", parent);
            var vlg = AddVerticalLayout(go, 50, 10, 30, 30);

            MakeLabel(go.transform, "Title", "Choose Your Adventure", 28, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            MakeSpacer(go.transform, 6);

            string[] advNames = { "Enchanted Forest (Free)", "Dark Dungeon (10 ROLAND)", "Dragon's Lair (50 ROLAND)" };
            Color[] advColors = { ThemeColors.AdvForest, ThemeColors.AdvDungeon, ThemeColors.AdvDragon };
            Button[] advButtons = new Button[3];
            TMP_Text[] cdTexts = new TMP_Text[3];

            for (int i = 0; i < 3; i++)
            {
                var cardGo = new GameObject($"Adv_{i}");
                cardGo.transform.SetParent(go.transform, false);
                cardGo.AddComponent<RectTransform>();
                cardGo.AddComponent<CanvasRenderer>();
                cardGo.AddComponent<Image>().color = advColors[i];
                cardGo.AddComponent<LayoutElement>().preferredHeight = 60;
                advButtons[i] = cardGo.AddComponent<Button>();
                var btnColors = advButtons[i].colors;
                btnColors.normalColor = Color.white;
                btnColors.highlightedColor = new Color(0.95f, 0.97f, 0.96f);
                btnColors.pressedColor = new Color(0.85f, 0.9f, 0.88f);
                advButtons[i].colors = btnColors;

                var contentGo = new GameObject("Content");
                contentGo.transform.SetParent(cardGo.transform, false);
                var cRect = contentGo.AddComponent<RectTransform>();
                cRect.anchorMin = Vector2.zero; cRect.anchorMax = Vector2.one;
                cRect.offsetMin = new Vector2(12, 4); cRect.offsetMax = new Vector2(-12, -4);

                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(contentGo.transform, false);
                var nRect = nameGo.AddComponent<RectTransform>();
                nRect.anchorMin = new Vector2(0, 0.45f); nRect.anchorMax = new Vector2(1, 1);
                nRect.offsetMin = Vector2.zero; nRect.offsetMax = Vector2.zero;
                nameGo.AddComponent<CanvasRenderer>();
                var nTmp = nameGo.AddComponent<TextMeshProUGUI>();
                nTmp.text = advNames[i]; nTmp.fontSize = 16; nTmp.alignment = TextAlignmentOptions.Center;
                nTmp.color = ThemeColors.TextPrimary; nTmp.fontStyle = FontStyles.Bold;

                var cdGo = new GameObject("Cooldown");
                cdGo.transform.SetParent(contentGo.transform, false);
                var cdRect = cdGo.AddComponent<RectTransform>();
                cdRect.anchorMin = Vector2.zero; cdRect.anchorMax = new Vector2(1, 0.45f);
                cdRect.offsetMin = Vector2.zero; cdRect.offsetMax = Vector2.zero;
                cdGo.AddComponent<CanvasRenderer>();
                cdTexts[i] = cdGo.AddComponent<TextMeshProUGUI>();
                cdTexts[i].text = "Ready!"; cdTexts[i].fontSize = 13; cdTexts[i].alignment = TextAlignmentOptions.Center;
                cdTexts[i].color = ThemeColors.Success;
            }

            MakeSpacer(go.transform, 12);

            var rollBtn = MakeHeroButton(go.transform, "RollBtn", "ROLL D20!", 56);
            rollBtn.GetComponentInChildren<TMP_Text>().fontSize = 24;

            var statusGo = MakeLabel(go.transform, "RollStatus", "", 16, ThemeColors.TextSecondary);
            var equipBonusGo = MakeLabel(go.transform, "EquipBonus", "", 14, ThemeColors.Primary);

            var diceGo = new GameObject("DiceArea");
            diceGo.transform.SetParent(go.transform, false);
            var diceRect = diceGo.AddComponent<RectTransform>();
            diceRect.anchorMin = new Vector2(0.3f, 0); diceRect.anchorMax = new Vector2(0.7f, 1);
            diceRect.offsetMin = Vector2.zero; diceRect.offsetMax = Vector2.zero;
            var diceLe = diceGo.AddComponent<LayoutElement>();
            diceLe.preferredHeight = 120;
            diceLe.flexibleHeight = 0;

            var diceResultGo = new GameObject("DiceResult");
            diceResultGo.transform.SetParent(diceGo.transform, false);
            var drRect = diceResultGo.AddComponent<RectTransform>();
            drRect.anchorMin = new Vector2(0.5f, 1f); drRect.anchorMax = new Vector2(0.5f, 1f);
            drRect.pivot = new Vector2(0.5f, 0);
            drRect.sizeDelta = new Vector2(200, 60);
            drRect.anchoredPosition = new Vector2(0, 10);
            diceResultGo.AddComponent<CanvasRenderer>();
            var diceResultTmp = diceResultGo.AddComponent<TextMeshProUGUI>();
            diceResultTmp.fontSize = 52; diceResultTmp.alignment = TextAlignmentOptions.Center;
            diceResultTmp.color = ThemeColors.Primary; diceResultTmp.fontStyle = FontStyles.Bold;

            var d20Dice = diceGo.AddComponent<D20Dice>();
            var d20SO = new SerializedObject(d20Dice);
            d20SO.FindProperty("resultText").objectReferenceValue = diceResultTmp;
            d20SO.ApplyModifiedProperties();

            var rewardPopupGo = BuildRewardPopup(go.transform);
            var unclaimedGo = MakeLabel(go.transform, "UnclaimedText", "", 16, ThemeColors.Primary);
            var claimBtn = MakeButton(go.transform, "ClaimItemBtn", "CLAIM ITEM", 46, ThemeColors.ClaimBtn);
            claimBtn.GetComponentInChildren<TMP_Text>().color = ThemeColors.TextPrimary;
            claimBtn.SetActive(false);

            var script = go.AddComponent<AdventureScreen>();
            var so = new SerializedObject(script);
            var abp = so.FindProperty("adventureButtons");
            abp.arraySize = 3;
            for (int i = 0; i < 3; i++) abp.GetArrayElementAtIndex(i).objectReferenceValue = advButtons[i];
            var cdp = so.FindProperty("adventureCooldownTexts");
            cdp.arraySize = 3;
            for (int i = 0; i < 3; i++) cdp.GetArrayElementAtIndex(i).objectReferenceValue = cdTexts[i];
            so.FindProperty("rollButton").objectReferenceValue = rollBtn.GetComponent<Button>();
            so.FindProperty("rollStatusText").objectReferenceValue = statusGo.GetComponent<TMP_Text>();
            so.FindProperty("d20Dice").objectReferenceValue = d20Dice;
            so.FindProperty("equipmentBonusText").objectReferenceValue = equipBonusGo.GetComponent<TMP_Text>();
            so.FindProperty("rewardPopup").objectReferenceValue = rewardPopupGo.GetComponent<RewardPopup>();
            so.FindProperty("claimItemButton").objectReferenceValue = claimBtn.GetComponent<Button>();
            so.FindProperty("unclaimedItemsText").objectReferenceValue = unclaimedGo.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();
            return go;
        }

        private static GameObject BuildRewardPopup(Transform parent)
        {
            var overlay = new GameObject("RewardPopup");
            overlay.transform.SetParent(parent, false);
            var rect = overlay.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            overlay.AddComponent<CanvasRenderer>();
            overlay.AddComponent<Image>().color = ThemeColors.Overlay;
            overlay.SetActive(false);

            var panelBorder = new GameObject("PanelBorder");
            panelBorder.transform.SetParent(overlay.transform, false);
            var pbRect = panelBorder.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.2f, 0.3f); pbRect.anchorMax = new Vector2(0.8f, 0.7f);
            pbRect.offsetMin = Vector2.zero; pbRect.offsetMax = Vector2.zero;
            panelBorder.AddComponent<CanvasRenderer>();
            panelBorder.AddComponent<Image>().color = ThemeColors.PanelBorder;

            var panel = new GameObject("Panel");
            panel.transform.SetParent(panelBorder.transform, false);
            var pRect = panel.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one;
            pRect.offsetMin = new Vector2(1, 1); pRect.offsetMax = new Vector2(-1, -1);
            panel.AddComponent<CanvasRenderer>();
            panel.AddComponent<Image>().color = ThemeColors.Surface;

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.spacing = 8;
            vlg.padding = new RectOffset(20, 20, 20, 20);

            var titleGo = MakeLabel(panel.transform, "Title", "SUCCESS!", 32, ThemeColors.Primary);
            titleGo.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            MakeLabel(panel.transform, "RollVal", "Roll: 0", 28, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            MakeLabel(panel.transform, "Tier", "Low", 22, ThemeColors.Primary);
            MakeLabel(panel.transform, "Tokens", "+0 ROLAND", 20, ThemeColors.Primary);
            MakeLabel(panel.transform, "XP", "+0 XP", 18, ThemeColors.TextSecondary);
            var specGo = MakeLabel(panel.transform, "Special", "SPECIAL!", 22, ThemeColors.AccentGold);
            specGo.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            specGo.SetActive(false);
            var failGo = MakeLabel(panel.transform, "FailureReason", "", 18, ThemeColors.Error);
            failGo.SetActive(false);
            var closeBtn = MakeButton(panel.transform, "CloseBtn", "CLOSE", 42, ThemeColors.SurfaceElevated);

            var popup = overlay.AddComponent<RewardPopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("popupPanel").objectReferenceValue = overlay;
            so.FindProperty("titleText").objectReferenceValue = titleGo.GetComponent<TMP_Text>();
            so.FindProperty("rollValueText").objectReferenceValue = panel.transform.Find("RollVal").GetComponent<TMP_Text>();
            so.FindProperty("tierText").objectReferenceValue = panel.transform.Find("Tier").GetComponent<TMP_Text>();
            so.FindProperty("tokenAmountText").objectReferenceValue = panel.transform.Find("Tokens").GetComponent<TMP_Text>();
            so.FindProperty("xpGainedText").objectReferenceValue = panel.transform.Find("XP").GetComponent<TMP_Text>();
            so.FindProperty("specialIndicator").objectReferenceValue = specGo.GetComponent<TMP_Text>();
            so.FindProperty("failureReasonText").objectReferenceValue = failGo.GetComponent<TMP_Text>();
            so.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            so.ApplyModifiedProperties();
            return overlay;
        }

        #endregion

        #region Inventory

        private static GameObject BuildInventoryScreen(Transform parent)
        {
            var go = CreateScreen("InventoryScreen", parent);
            var vlg = AddVerticalLayout(go, 40, 10, 30, 30);

            MakeLabel(go.transform, "Title", "Inventory", 28, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            MakeSpacer(go.transform, 6);

            MakeLabel(go.transform, "WeaponLabel", "Equipped Weapon:", 16, ThemeColors.TextSecondary);
            var weaponCard = MakeCardView(go.transform, "WeaponSlot", 85);
            var uwBtn = MakeButton(go.transform, "UnequipWeapon", "Unequip Weapon", 32, ThemeColors.UnequipBtn);
            uwBtn.GetComponentInChildren<TMP_Text>().fontSize = 14;
            uwBtn.SetActive(false);

            MakeLabel(go.transform, "ArmorLabel", "Equipped Armor:", 16, ThemeColors.TextSecondary);
            var armorCard = MakeCardView(go.transform, "ArmorSlot", 85);
            var uaBtn = MakeButton(go.transform, "UnequipArmor", "Unequip Armor", 32, ThemeColors.UnequipBtn);
            uaBtn.GetComponentInChildren<TMP_Text>().fontSize = 14;
            uaBtn.SetActive(false);

            MakeSpacer(go.transform, 8);
            MakeLabel(go.transform, "ItemsLabel", "Your Items:", 18, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

            var grid = new GameObject("ItemGrid");
            grid.transform.SetParent(go.transform, false);
            grid.AddComponent<RectTransform>();
            grid.AddComponent<CanvasRenderer>();
            grid.AddComponent<Image>().color = ThemeColors.GridBg;
            var le = grid.AddComponent<LayoutElement>();
            le.preferredHeight = 200; le.flexibleHeight = 1;
            var gl = grid.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(110, 130);
            gl.spacing = new Vector2(8, 8);
            gl.childAlignment = TextAnchor.MiddleCenter;
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 5;

            var statusGo = MakeLabel(go.transform, "StatusText", "", 14, ThemeColors.TextMuted);
            var bonusSummaryGo = MakeLabel(go.transform, "BonusSummary", "", 14, ThemeColors.Primary);

            var script = go.AddComponent<InventoryScreen>();
            var so = new SerializedObject(script);
            so.FindProperty("weaponSlot").objectReferenceValue = weaponCard.GetComponent<CardView>();
            so.FindProperty("armorSlot").objectReferenceValue = armorCard.GetComponent<CardView>();
            so.FindProperty("unequipWeaponButton").objectReferenceValue = uwBtn.GetComponent<Button>();
            so.FindProperty("unequipArmorButton").objectReferenceValue = uaBtn.GetComponent<Button>();
            so.FindProperty("itemGrid").objectReferenceValue = grid.transform;
            so.FindProperty("statusText").objectReferenceValue = statusGo.GetComponent<TMP_Text>();
            so.FindProperty("bonusSummaryText").objectReferenceValue = bonusSummaryGo.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();
            return go;
        }

        #endregion

        #region WalletProfile

        private static GameObject BuildWalletProfileScreen(Transform parent)
        {
            var go = CreateScreen("WalletProfileScreen", parent);
            var vlg = AddVerticalLayout(go, 40, 10, 30, 30);

            MakeLabel(go.transform, "Title", "Wallet Profile", 28, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
            MakeSpacer(go.transform, 6);

            var pkGo = MakeLabel(go.transform, "PublicKey", "Not connected", 13, ThemeColors.TextMuted);
            var balGo = MakeLabel(go.transform, "Balance", "0.00 ROLAND", 26, ThemeColors.Primary);
            balGo.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

            MakeSpacer(go.transform, 8);
            MakeLabel(go.transform, "NFTLabel", "Your NFT Collection:", 18, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

            var nftGrid = new GameObject("NFTGrid");
            nftGrid.transform.SetParent(go.transform, false);
            nftGrid.AddComponent<RectTransform>();
            nftGrid.AddComponent<CanvasRenderer>();
            nftGrid.AddComponent<Image>().color = ThemeColors.GridBg;
            var nftLe = nftGrid.AddComponent<LayoutElement>();
            nftLe.preferredHeight = 180;
            var nftGl = nftGrid.AddComponent<GridLayoutGroup>();
            nftGl.cellSize = new Vector2(90, 110);
            nftGl.spacing = new Vector2(6, 6);
            nftGl.childAlignment = TextAnchor.MiddleCenter;
            nftGl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            nftGl.constraintCount = 5;

            MakeSpacer(go.transform, 8);
            MakeLabel(go.transform, "TxLabel", "Recent Transactions:", 18, ThemeColors.TextPrimary).GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

            var txHistory = new GameObject("TxHistory");
            txHistory.transform.SetParent(go.transform, false);
            txHistory.AddComponent<RectTransform>();
            txHistory.AddComponent<CanvasRenderer>();
            txHistory.AddComponent<Image>().color = ThemeColors.GridBg;
            var txLe = txHistory.AddComponent<LayoutElement>();
            txLe.preferredHeight = 150;
            var txVlg = txHistory.AddComponent<VerticalLayoutGroup>();
            txVlg.childControlWidth = true; txVlg.childControlHeight = false;
            txVlg.childForceExpandWidth = true; txVlg.spacing = 4;
            txVlg.padding = new RectOffset(10, 10, 10, 10);

            var statusGo = MakeLabel(go.transform, "StatusText", "", 14, ThemeColors.TextMuted);

            var script = go.AddComponent<WalletProfileScreen>();
            var so = new SerializedObject(script);
            so.FindProperty("publicKeyText").objectReferenceValue = pkGo.GetComponent<TMP_Text>();
            so.FindProperty("balanceText").objectReferenceValue = balGo.GetComponent<TMP_Text>();
            so.FindProperty("nftCollectionGrid").objectReferenceValue = nftGrid.transform;
            so.FindProperty("transactionHistory").objectReferenceValue = txHistory.transform;
            so.FindProperty("statusText").objectReferenceValue = statusGo.GetComponent<TMP_Text>();
            so.ApplyModifiedProperties();
            return go;
        }

        #endregion

        #region BottomNav

        private static void CreateBottomNav(Transform canvasParent)
        {
            var navGo = new GameObject("BottomNavBar");
            navGo.transform.SetParent(canvasParent, false);
            var navRect = navGo.AddComponent<RectTransform>();
            navRect.anchorMin = new Vector2(0, 0); navRect.anchorMax = new Vector2(1, 0);
            navRect.pivot = new Vector2(0.5f, 0);
            navRect.offsetMin = Vector2.zero; navRect.offsetMax = new Vector2(0, 56);
            navGo.AddComponent<CanvasRenderer>();
            navGo.AddComponent<Image>().color = ThemeColors.NavBg;
            var hlg = navGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.spacing = 4;
            hlg.padding = new RectOffset(8, 8, 6, 6);

            string[] labels = { "Hub", "Adventure", "Inventory", "Wallet", "Login" };
            var buttons = new Button[5];
            for (int i = 0; i < labels.Length; i++)
            {
                var b = MakeButton(navGo.transform, $"Nav_{labels[i]}", labels[i], 42, ThemeColors.ButtonBg);
                b.GetComponent<LayoutElement>().preferredHeight = 42;
                b.GetComponentInChildren<TMP_Text>().fontSize = 14;
                buttons[i] = b.GetComponent<Button>();
            }

            var nb = navGo.AddComponent<BottomNavBar>();
            var nbSO = new SerializedObject(nb);
            var bp = nbSO.FindProperty("navButtons");
            if (bp != null)
            {
                bp.arraySize = 5;
                for (int i = 0; i < 5; i++) bp.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
            }
            nbSO.FindProperty("activeColor").colorValue = ThemeColors.Primary;
            nbSO.FindProperty("inactiveColor").colorValue = ThemeColors.Unselected;
            nbSO.FindProperty("disabledColor").colorValue = ThemeColors.Disabled;
            nbSO.ApplyModifiedProperties();
        }

        #endregion
    }
}
