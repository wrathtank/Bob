#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace BobsPetroleum.Editor
{
    /// <summary>
    /// PREFAB CREATOR - One-click prefab creation!
    /// Creates all the prefabs you need with correct components.
    ///
    /// Window > Bob's Petroleum > Prefab Creator
    /// </summary>
    public class PrefabCreator : EditorWindow
    {
        private string prefabFolder = "Assets/Prefabs";
        private Vector2 scrollPos;

        [MenuItem("Window/Bob's Petroleum/Prefab Creator")]
        public static void ShowWindow()
        {
            GetWindow<PrefabCreator>("Prefab Creator");
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            EditorGUILayout.LabelField("Bob's Petroleum - Prefab Creator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create pre-configured prefabs with one click!\nAll prefabs have the correct components attached.", MessageType.Info);

            EditorGUILayout.Space(10);

            // Folder selection
            EditorGUILayout.LabelField("Output Folder:", EditorStyles.boldLabel);
            prefabFolder = EditorGUILayout.TextField(prefabFolder);
            if (GUILayout.Button("Create Folder Structure"))
            {
                CreateFolderStructure();
            }

            EditorGUILayout.Space(20);

            // ═══════════════════════════════════════════════
            // CORE PREFABS
            // ═══════════════════════════════════════════════
            EditorGUILayout.LabelField("═══ CORE PREFABS ═══", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Player Prefab", GUILayout.Height(30)))
            {
                CreatePlayerPrefab();
            }

            if (GUILayout.Button("Create Bob Character Prefab", GUILayout.Height(30)))
            {
                CreateBobPrefab();
            }

            if (GUILayout.Button("Create Spawn Tube Prefab", GUILayout.Height(30)))
            {
                CreateSpawnTubePrefab();
            }

            if (GUILayout.Button("Create Home Spot Prefab", GUILayout.Height(30)))
            {
                CreateHomeSpotPrefab();
            }

            EditorGUILayout.Space(20);

            // ═══════════════════════════════════════════════
            // UI PREFABS
            // ═══════════════════════════════════════════════
            EditorGUILayout.LabelField("═══ UI PREFABS ═══", EditorStyles.boldLabel);

            if (GUILayout.Button("Create HUD Canvas", GUILayout.Height(30)))
            {
                CreateHUDCanvas();
            }

            if (GUILayout.Button("Create Main Menu Canvas", GUILayout.Height(30)))
            {
                CreateMainMenuCanvas();
            }

            if (GUILayout.Button("Create Pause Menu Canvas", GUILayout.Height(30)))
            {
                CreatePauseMenuCanvas();
            }

            if (GUILayout.Button("Create Loading Screen Canvas", GUILayout.Height(30)))
            {
                CreateLoadingScreenCanvas();
            }

            EditorGUILayout.Space(20);

            // ═══════════════════════════════════════════════
            // ECONOMY PREFABS
            // ═══════════════════════════════════════════════
            EditorGUILayout.LabelField("═══ ECONOMY PREFABS ═══", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Cash Register Prefab", GUILayout.Height(30)))
            {
                CreateCashRegisterPrefab();
            }

            if (GUILayout.Button("Create Shop Prefab", GUILayout.Height(30)))
            {
                CreateShopPrefab();
            }

            EditorGUILayout.Space(20);

            // ═══════════════════════════════════════════════
            // ENVIRONMENT PREFABS
            // ═══════════════════════════════════════════════
            EditorGUILayout.LabelField("═══ ENVIRONMENT PREFABS ═══", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Water Plane Prefab", GUILayout.Height(30)))
            {
                CreateWaterPlanePrefab();
            }

            if (GUILayout.Button("Create Subway Station Prefab", GUILayout.Height(30)))
            {
                CreateSubwayStationPrefab();
            }

            EditorGUILayout.Space(20);

            // ═══════════════════════════════════════════════
            // MANAGER PREFABS
            // ═══════════════════════════════════════════════
            EditorGUILayout.LabelField("═══ MANAGER PREFABS ═══", EditorStyles.boldLabel);

            if (GUILayout.Button("Create All Managers Prefab", GUILayout.Height(30)))
            {
                CreateAllManagersPrefab();
            }

            EditorGUILayout.Space(20);

            // ═══════════════════════════════════════════════
            // ALL IN ONE
            // ═══════════════════════════════════════════════
            EditorGUILayout.LabelField("═══ CREATE ALL ═══", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Creates all prefabs at once!", MessageType.Info);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE ALL PREFABS", GUILayout.Height(40)))
            {
                CreateAllPrefabs();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        #region Folder Structure

        private void CreateFolderStructure()
        {
            CreateFolder("Assets", "Prefabs");
            CreateFolder("Assets/Prefabs", "Core");
            CreateFolder("Assets/Prefabs", "UI");
            CreateFolder("Assets/Prefabs", "Economy");
            CreateFolder("Assets/Prefabs", "Environment");
            CreateFolder("Assets/Prefabs", "Managers");

            AssetDatabase.Refresh();
            Debug.Log("[PrefabCreator] Folder structure created!");
        }

        private void CreateFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        #endregion

        #region Core Prefabs

        private void CreatePlayerPrefab()
        {
            GameObject player = new GameObject("Player");

            // Add components
            player.AddComponent<CharacterController>();
            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerInventory>();
            player.AddComponent<Player.DeathRespawnSystem>();
            player.AddComponent<Player.SkinManager>();

            // Add Camera
            GameObject camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            Camera cam = camHolder.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camHolder.AddComponent<AudioListener>();

            // Add Audio
            player.AddComponent<AudioSource>();

            // Set layer/tag
            player.tag = "Player";

            SavePrefab(player, "Core/Player");
            Debug.Log("[PrefabCreator] Created Player prefab!");
        }

        private void CreateBobPrefab()
        {
            GameObject bob = new GameObject("Bob");

            // Placeholder mesh
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.name = "BobMesh";
            mesh.transform.SetParent(bob.transform);
            DestroyImmediate(mesh.GetComponent<Collider>());

            // Add components
            bob.AddComponent<Core.BobCharacter>();
            bob.AddComponent<Animation.SimpleAnimationPlayer>();
            bob.AddComponent<AudioSource>();
            bob.AddComponent<CapsuleCollider>();

            // Create health bar UI (world space)
            GameObject canvas = new GameObject("HealthBarCanvas");
            canvas.transform.SetParent(bob.transform);
            canvas.transform.localPosition = new Vector3(0, 2.5f, 0);
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            canvas.AddComponent<CanvasScaler>();

            GameObject healthBar = new GameObject("HealthBar");
            healthBar.transform.SetParent(canvas.transform);
            healthBar.AddComponent<Image>();

            SavePrefab(bob, "Core/Bob");
            Debug.Log("[PrefabCreator] Created Bob prefab!");
        }

        private void CreateSpawnTubePrefab()
        {
            GameObject tube = new GameObject("SpawnTube");

            // Placeholder mesh
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = "TubeMesh";
            mesh.transform.SetParent(tube.transform);
            mesh.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            DestroyImmediate(mesh.GetComponent<Collider>());

            // Door
            GameObject door = new GameObject("Door");
            door.transform.SetParent(tube.transform);
            door.AddComponent<Animator>();

            // Spawn point
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(tube.transform);
            spawnPoint.transform.localPosition = new Vector3(0, 0, 1.5f);

            // Add collider
            tube.AddComponent<BoxCollider>();

            SavePrefab(tube, "Core/SpawnTube");
            Debug.Log("[PrefabCreator] Created SpawnTube prefab!");
        }

        private void CreateHomeSpotPrefab()
        {
            GameObject homeSpot = new GameObject("HomeSpot");

            // Add component
            homeSpot.AddComponent<Player.HomeSpot>();

            // Placeholder mesh (bed)
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "BedMesh";
            mesh.transform.SetParent(homeSpot.transform);
            mesh.transform.localScale = new Vector3(2f, 0.5f, 1f);
            DestroyImmediate(mesh.GetComponent<Collider>());

            // Spawn point
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(homeSpot.transform);
            spawnPoint.transform.localPosition = new Vector3(0, 0.5f, 0);

            // Interaction collider
            var col = homeSpot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.5f, 1.5f, 1.5f);

            SavePrefab(homeSpot, "Core/HomeSpot");
            Debug.Log("[PrefabCreator] Created HomeSpot prefab!");
        }

        #endregion

        #region UI Prefabs

        private void CreateHUDCanvas()
        {
            GameObject canvas = CreateUICanvas("HUD_Canvas");
            var hudManager = canvas.AddComponent<UI.HUDManager>();

            // Create HUD elements
            GameObject panel = CreateUIPanel(canvas.transform, "HUDPanel");

            // Health bar
            GameObject healthBar = CreateHealthBar(panel.transform, "PlayerHealthBar", new Vector2(-350, 200));
            hudManager.healthBarFill = healthBar.transform.Find("Fill").GetComponent<Image>();

            // Money
            GameObject money = CreateText(panel.transform, "MoneyText", "$0", new Vector2(350, 200));
            hudManager.moneyText = money.GetComponent<TMP_Text>();

            // Bob health
            GameObject bobHealth = CreateHealthBar(panel.transform, "BobHealthBar", new Vector2(0, 200));
            hudManager.bobHealthBarFill = bobHealth.transform.Find("Fill").GetComponent<Image>();

            // Crosshair
            GameObject crosshair = CreateImage(panel.transform, "Crosshair", new Vector2(0, 0), new Vector2(20, 20));
            hudManager.crosshair = crosshair.GetComponent<Image>();

            // Interaction prompt
            GameObject prompt = CreateText(panel.transform, "InteractionPrompt", "[E] Interact", new Vector2(0, -100));
            hudManager.interactionPromptText = prompt.GetComponent<TMP_Text>();
            hudManager.interactionPromptContainer = prompt;

            SavePrefab(canvas, "UI/HUD_Canvas");
            Debug.Log("[PrefabCreator] Created HUD Canvas prefab!");
        }

        private void CreateMainMenuCanvas()
        {
            GameObject canvas = CreateUICanvas("MainMenu_Canvas");
            var menuManager = canvas.AddComponent<UI.MainMenuManager>();

            // Create panels
            GameObject mainPanel = CreateUIPanel(canvas.transform, "MainMenuPanel");
            menuManager.mainMenuPanel = mainPanel;

            // Title
            CreateText(mainPanel.transform, "TitleText", "BOB'S PETROLEUM", new Vector2(0, 200), 60);

            // Buttons
            var playBtn = CreateButton(mainPanel.transform, "PlayButton", "PLAY", new Vector2(0, 50));
            menuManager.playButton = playBtn.GetComponent<Button>();

            var settingsBtn = CreateButton(mainPanel.transform, "SettingsButton", "SETTINGS", new Vector2(0, -25));
            menuManager.settingsButton = settingsBtn.GetComponent<Button>();

            var quitBtn = CreateButton(mainPanel.transform, "QuitButton", "QUIT", new Vector2(0, -100));
            menuManager.quitButton = quitBtn.GetComponent<Button>();

            // Settings panel (hidden by default)
            GameObject settingsPanel = CreateUIPanel(canvas.transform, "SettingsPanel");
            settingsPanel.SetActive(false);
            menuManager.settingsPanel = settingsPanel;

            SavePrefab(canvas, "UI/MainMenu_Canvas");
            Debug.Log("[PrefabCreator] Created Main Menu Canvas prefab!");
        }

        private void CreatePauseMenuCanvas()
        {
            GameObject canvas = CreateUICanvas("PauseMenu_Canvas");
            var pauseManager = canvas.AddComponent<UI.PauseMenuManager>();

            GameObject panel = CreateUIPanel(canvas.transform, "PausePanel");
            pauseManager.pausePanel = panel;
            panel.SetActive(false); // Hidden by default

            // Title
            CreateText(panel.transform, "TitleText", "PAUSED", new Vector2(0, 150), 48);

            // Buttons
            var resumeBtn = CreateButton(panel.transform, "ResumeButton", "RESUME", new Vector2(0, 50));
            pauseManager.resumeButton = resumeBtn.GetComponent<Button>();

            var settingsBtn = CreateButton(panel.transform, "SettingsButton", "SETTINGS", new Vector2(0, -25));
            pauseManager.settingsButton = settingsBtn.GetComponent<Button>();

            var mainMenuBtn = CreateButton(panel.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -100));
            pauseManager.mainMenuButton = mainMenuBtn.GetComponent<Button>();

            var quitBtn = CreateButton(panel.transform, "QuitButton", "QUIT", new Vector2(0, -175));
            pauseManager.quitButton = quitBtn.GetComponent<Button>();

            SavePrefab(canvas, "UI/PauseMenu_Canvas");
            Debug.Log("[PrefabCreator] Created Pause Menu Canvas prefab!");
        }

        private void CreateLoadingScreenCanvas()
        {
            GameObject canvas = CreateUICanvas("LoadingScreen_Canvas");

            GameObject panel = CreateUIPanel(canvas.transform, "LoadingPanel");

            // Background
            var bg = panel.GetComponent<Image>();
            bg.color = Color.black;

            // Loading text
            CreateText(panel.transform, "LoadingText", "Loading...", new Vector2(0, 50), 36);

            // Progress bar
            GameObject progressBg = CreateImage(panel.transform, "ProgressBarBG", new Vector2(0, -50), new Vector2(400, 30));
            progressBg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            GameObject progressFill = CreateImage(progressBg.transform, "ProgressBarFill", Vector2.zero, new Vector2(400, 30));
            var fillImg = progressFill.GetComponent<Image>();
            fillImg.color = Color.green;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            // Tip text
            CreateText(panel.transform, "TipText", "Tip: Feed Bob hamburgers to revive him!", new Vector2(0, -150), 18);

            panel.SetActive(false); // Hidden by default

            SavePrefab(canvas, "UI/LoadingScreen_Canvas");
            Debug.Log("[PrefabCreator] Created Loading Screen Canvas prefab!");
        }

        #endregion

        #region Economy Prefabs

        private void CreateCashRegisterPrefab()
        {
            GameObject register = new GameObject("CashRegister");

            // Placeholder mesh
            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "RegisterMesh";
            mesh.transform.SetParent(register.transform);
            mesh.transform.localScale = new Vector3(0.6f, 0.4f, 0.4f);
            DestroyImmediate(mesh.GetComponent<Collider>());

            // Add components
            register.AddComponent<Economy.CashRegister>();
            register.AddComponent<AudioSource>();

            // Interaction collider
            var col = register.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1f, 1f, 1f);

            SavePrefab(register, "Economy/CashRegister");
            Debug.Log("[PrefabCreator] Created CashRegister prefab!");
        }

        private void CreateShopPrefab()
        {
            GameObject shop = new GameObject("Shop");

            // Add components
            shop.AddComponent<Economy.ShopManager>();
            shop.AddComponent<Economy.ShopSystem>();

            // Counter
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.SetParent(shop.transform);
            counter.transform.localScale = new Vector3(3f, 1f, 1f);
            counter.transform.localPosition = new Vector3(0, 0.5f, 0);

            // NPC spot
            GameObject npcSpot = new GameObject("NPCSpot");
            npcSpot.transform.SetParent(shop.transform);
            npcSpot.transform.localPosition = new Vector3(0, 0, -1f);

            // Customer spot
            GameObject customerSpot = new GameObject("CustomerSpot");
            customerSpot.transform.SetParent(shop.transform);
            customerSpot.transform.localPosition = new Vector3(0, 0, 1f);

            SavePrefab(shop, "Economy/Shop");
            Debug.Log("[PrefabCreator] Created Shop prefab!");
        }

        #endregion

        #region Environment Prefabs

        private void CreateWaterPlanePrefab()
        {
            GameObject water = new GameObject("WaterPlane");

            // Create plane
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "WaterMesh";
            plane.transform.SetParent(water.transform);
            plane.transform.localScale = new Vector3(10, 1, 10);

            // Add water component
            water.AddComponent<Environment.WaterSystem>();

            // Note: User needs to assign Water shader material

            SavePrefab(water, "Environment/WaterPlane");
            Debug.Log("[PrefabCreator] Created WaterPlane prefab! Remember to assign Water shader material.");
        }

        private void CreateSubwayStationPrefab()
        {
            GameObject station = new GameObject("SubwayStation");

            // Platform
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.SetParent(station.transform);
            platform.transform.localScale = new Vector3(10f, 0.5f, 3f);

            // Entrance
            GameObject entrance = new GameObject("Entrance");
            entrance.transform.SetParent(station.transform);
            entrance.transform.localPosition = new Vector3(0, 0, 2f);

            // Add components
            station.AddComponent<Systems.SubwayStation>();

            SavePrefab(station, "Environment/SubwayStation");
            Debug.Log("[PrefabCreator] Created SubwayStation prefab!");
        }

        #endregion

        #region Manager Prefabs

        private void CreateAllManagersPrefab()
        {
            GameObject managers = new GameObject("---MANAGERS---");

            // Core
            CreateChildManager(managers, "GameManager", typeof(Core.GameManager));
            CreateChildManager(managers, "GameStateManager", typeof(Core.GameStateManager));
            CreateChildManager(managers, "GameFlowController", typeof(Core.GameFlowController));
            CreateChildManager(managers, "CloneSpawnSystem", typeof(Core.CloneSpawnSystem));
            CreateChildManager(managers, "SceneLoader", typeof(Core.SceneLoader));

            // Audio
            CreateChildManager(managers, "AudioManager", typeof(Audio.AudioManager));

            // Systems
            CreateChildManager(managers, "DialogueSystem", typeof(Systems.DialogueSystem));
            CreateChildManager(managers, "FastTravelSystem", typeof(Systems.FastTravelSystem));
            CreateChildManager(managers, "HorrorEventsSystem", typeof(Systems.HorrorEventsSystem));

            // UI
            CreateChildManager(managers, "TutorialManager", typeof(UI.TutorialManager));

            // Networking (optional)
            CreateChildManager(managers, "NetworkGameManager", typeof(Networking.NetworkGameManager));
            CreateChildManager(managers, "SupabaseSaveSystem", typeof(Networking.SupabaseSaveSystem));

            SavePrefab(managers, "Managers/AllManagers");
            Debug.Log("[PrefabCreator] Created AllManagers prefab!");
        }

        private void CreateChildManager(GameObject parent, string name, System.Type componentType)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            child.AddComponent(componentType);
        }

        #endregion

        #region UI Helpers

        private GameObject CreateUICanvas(string name)
        {
            GameObject canvas = new GameObject(name);
            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private GameObject CreateUIPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.8f);

            return panel;
        }

        private GameObject CreateText(Transform parent, string name, string text, Vector2 position, int fontSize = 24)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 50);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private GameObject CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f);

            go.AddComponent<Button>();

            // Button text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private GameObject CreateImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            go.AddComponent<Image>();

            return go;
        }

        private GameObject CreateHealthBar(Transform parent, string name, Vector2 position)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);

            var rect = bar.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(200, 20);

            var bgImg = bar.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f);

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);

            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            var fillImg = fill.AddComponent<Image>();
            fillImg.color = Color.green;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            return bar;
        }

        #endregion

        #region Save Prefab

        private void SavePrefab(GameObject obj, string subPath)
        {
            string fullPath = $"{prefabFolder}/{subPath}.prefab";

            // Ensure directory exists
            string dir = System.IO.Path.GetDirectoryName(fullPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            // Save
            PrefabUtility.SaveAsPrefabAsset(obj, fullPath);
            DestroyImmediate(obj);

            AssetDatabase.Refresh();
        }

        #endregion

        #region Create All

        private void CreateAllPrefabs()
        {
            CreateFolderStructure();

            // Core
            CreatePlayerPrefab();
            CreateBobPrefab();
            CreateSpawnTubePrefab();
            CreateHomeSpotPrefab();

            // UI
            CreateHUDCanvas();
            CreateMainMenuCanvas();
            CreatePauseMenuCanvas();
            CreateLoadingScreenCanvas();

            // Economy
            CreateCashRegisterPrefab();
            CreateShopPrefab();

            // Environment
            CreateWaterPlanePrefab();
            CreateSubwayStationPrefab();

            // Managers
            CreateAllManagersPrefab();

            Debug.Log("═══════════════════════════════════════════════════════════");
            Debug.Log("  ALL PREFABS CREATED!");
            Debug.Log("  Check Assets/Prefabs folder");
            Debug.Log("═══════════════════════════════════════════════════════════");

            EditorUtility.DisplayDialog("Prefabs Created!",
                "All prefabs have been created in Assets/Prefabs!\n\n" +
                "Next steps:\n" +
                "1. Replace placeholder meshes with your models\n" +
                "2. Assign animations\n" +
                "3. Set up materials\n" +
                "4. Drag prefabs into your scene",
                "Got it!");
        }

        #endregion
    }
}
#endif
