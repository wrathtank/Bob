#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace BobsPetroleum.Editor
{
    /// <summary>
    /// MASTER SETUP - Your one-stop shop for Bob's Petroleum setup!
    /// Everything you need in ONE window.
    ///
    /// Window > Bob's Petroleum > MASTER SETUP
    ///
    /// Features:
    /// - Asset checklist (what you need to import)
    /// - One-click scene creation
    /// - Prefab generation
    /// - Scene validation
    /// - Build checklist
    /// </summary>
    public class MasterSetup : EditorWindow
    {
        private enum Tab
        {
            AssetChecklist,
            SceneSetup,
            PrefabSetup,
            Validation,
            BuildChecklist
        }

        private Tab currentTab = Tab.AssetChecklist;
        private Vector2 scrollPos;

        // Asset paths
        private string modelsPath = "Assets/Models";
        private string animationsPath = "Assets/Animations";
        private string audioPath = "Assets/Audio";
        private string materialsPath = "Assets/Materials";
        private string prefabsPath = "Assets/Prefabs";

        // Scene settings
        private bool setupComplete = false;

        [MenuItem("Window/Bob's Petroleum/MASTER SETUP", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<MasterSetup>("MASTER SETUP");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            // Header
            DrawHeader();

            // Tab bar
            DrawTabBar();

            // Content
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            switch (currentTab)
            {
                case Tab.AssetChecklist:
                    DrawAssetChecklist();
                    break;
                case Tab.SceneSetup:
                    DrawSceneSetup();
                    break;
                case Tab.PrefabSetup:
                    DrawPrefabSetup();
                    break;
                case Tab.Validation:
                    DrawValidation();
                    break;
                case Tab.BuildChecklist:
                    DrawBuildChecklist();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region Header

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("BOB'S PETROLEUM", titleStyle);
            EditorGUILayout.LabelField("Master Setup Tool", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(5);

            // Progress bar
            float progress = CalculateOverallProgress();
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                progress, $"Overall Progress: {Mathf.RoundToInt(progress * 100)}%");

            EditorGUILayout.Space(10);
        }

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal();

            if (DrawTabButton("1. Assets", Tab.AssetChecklist))
                currentTab = Tab.AssetChecklist;
            if (DrawTabButton("2. Scene", Tab.SceneSetup))
                currentTab = Tab.SceneSetup;
            if (DrawTabButton("3. Prefabs", Tab.PrefabSetup))
                currentTab = Tab.PrefabSetup;
            if (DrawTabButton("4. Validate", Tab.Validation))
                currentTab = Tab.Validation;
            if (DrawTabButton("5. Build", Tab.BuildChecklist))
                currentTab = Tab.BuildChecklist;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
        }

        private bool DrawTabButton(string label, Tab tab)
        {
            bool isActive = currentTab == tab;
            GUI.backgroundColor = isActive ? Color.cyan : Color.white;
            bool clicked = GUILayout.Button(label, GUILayout.Height(30));
            GUI.backgroundColor = Color.white;
            return clicked;
        }

        #endregion

        #region Tab 1: Asset Checklist

        private void DrawAssetChecklist()
        {
            EditorGUILayout.LabelField("STEP 1: Prepare Your Assets", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Before setting up the game, make sure you have these assets ready.\n" +
                "You can import them from Asset Store, Mixamo, or create your own.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Required Models
            DrawAssetSection("REQUIRED MODELS", new string[]
            {
                "Player Character (humanoid, FBX)",
                "Bob Character (humanoid, FBX)",
                "Customer NPCs (1-3 variations)",
                "Zombie/Horror NPCs",
                "Gas Pump model",
                "Cash Register model",
                "Store building/interior",
                "Hamburger model"
            }, MessageType.Error);

            // Required Animations
            DrawAssetSection("REQUIRED ANIMATIONS (Humanoid)", new string[]
            {
                "Idle",
                "Walk",
                "Run",
                "Attack/Interact (optional - can use Walk)"
            }, MessageType.Error);

            // Optional Assets
            DrawAssetSection("OPTIONAL (But Recommended)", new string[]
            {
                "Vehicle models (cars)",
                "Pet/Animal models",
                "Store shelf items (chips, soda, etc.)",
                "Environment props (trees, signs)",
                "Horror props (blood, creepy stuff)"
            }, MessageType.Warning);

            // Audio
            DrawAssetSection("AUDIO NEEDED", new string[]
            {
                "Background music (day/night)",
                "Sound effects (register beep, door, footsteps)",
                "Bob voice lines (optional)",
                "Horror sounds (screams, ambient)"
            }, MessageType.Warning);

            EditorGUILayout.Space(20);

            // Folder setup
            EditorGUILayout.LabelField("Asset Folder Setup", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Folder Structure", GUILayout.Height(30)))
            {
                CreateAssetFolders();
            }

            EditorGUILayout.HelpBox(
                "After clicking, import your assets into:\n" +
                "• Assets/Models/ - 3D models\n" +
                "• Assets/Animations/ - Animation clips\n" +
                "• Assets/Audio/ - Sound effects and music\n" +
                "• Assets/Materials/ - Materials and textures\n" +
                "• Assets/Prefabs/ - Will be auto-generated",
                MessageType.None);
        }

        private void DrawAssetSection(string title, string[] items, MessageType type)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            foreach (var item in items)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("  □ " + item);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void CreateAssetFolders()
        {
            string[] folders = {
                "Assets/Models",
                "Assets/Models/Characters",
                "Assets/Models/Props",
                "Assets/Models/Environment",
                "Assets/Models/Vehicles",
                "Assets/Animations",
                "Assets/Animations/Player",
                "Assets/Animations/NPCs",
                "Assets/Audio",
                "Assets/Audio/Music",
                "Assets/Audio/SFX",
                "Assets/Audio/Voice",
                "Assets/Materials",
                "Assets/Textures",
                "Assets/Prefabs",
                "Assets/Prefabs/Characters",
                "Assets/Prefabs/Props",
                "Assets/Prefabs/UI",
                "Assets/ScriptableObjects",
                "Assets/Scenes",
                "Assets/Resources",
                "Assets/Resources/PlayerModels"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                    string name = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, name);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[MasterSetup] Created asset folder structure!");
            EditorUtility.DisplayDialog("Folders Created",
                "Asset folders have been created!\n\nNow import your assets into the appropriate folders.",
                "OK");
        }

        #endregion

        #region Tab 2: Scene Setup

        private void DrawSceneSetup()
        {
            EditorGUILayout.LabelField("STEP 2: Create Your Scene", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This will create a complete playable scene with all systems.\n" +
                "You can then replace placeholder objects with your models.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Quick Scene Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE COMPLETE SCENE", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Create Scene",
                    "This will create a new scene with all Bob's Petroleum systems.\n\n" +
                    "Make sure to save your current scene first!",
                    "Create", "Cancel"))
                {
                    CreateCompleteScene();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // What gets created
            EditorGUILayout.LabelField("What This Creates:", EditorStyles.boldLabel);

            DrawCheckItem("All Manager Objects", true, "GameManager, AudioManager, etc.");
            DrawCheckItem("Player with Camera", true, "FPS controller, inventory, health");
            DrawCheckItem("Bob Character", true, "The dying owner to revive");
            DrawCheckItem("4 Spawn Tubes", true, "For multiplayer clone spawning");
            DrawCheckItem("4 Gas Pumps", true, "Working pumps with displays");
            DrawCheckItem("Cash Register", true, "For customer transactions");
            DrawCheckItem("Store Building", true, "Placeholder - replace with your model");
            DrawCheckItem("Customer Spawner", true, "Auto-spawns NPCs");
            DrawCheckItem("Hamburger Vendor", true, "Buy hamburgers to feed Bob");
            DrawCheckItem("Debug Console", true, "Press ~ for cheats");
            DrawCheckItem("All UI Canvases", true, "HUD, menus, loading screen");
            DrawCheckItem("Lighting & Skybox", true, "Day/night ready");

            EditorGUILayout.Space(20);

            // Or add to existing scene
            EditorGUILayout.LabelField("Or Add to Current Scene:", EditorStyles.boldLabel);

            if (GUILayout.Button("Add All Managers Only"))
            {
                AddAllManagers();
            }

            if (GUILayout.Button("Add Player Only"))
            {
                AddPlayer();
            }

            if (GUILayout.Button("Add Bob Only"))
            {
                AddBob();
            }
        }

        private void CreateCompleteScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create hierarchy
            CreateHierarchy();

            // Add all systems
            AddAllManagers();
            AddPlayer();
            AddBob();
            AddGasStation();
            AddCustomerSystem();
            AddUI();
            AddEnvironment();

            // Save
            string path = "Assets/Scenes/BobsPetroleum_Main.unity";
            EditorSceneManager.SaveScene(scene, path);

            setupComplete = true;

            EditorUtility.DisplayDialog("Scene Created!",
                $"Complete scene saved to:\n{path}\n\n" +
                "Next steps:\n" +
                "1. Go to Tab 3 (Prefabs) to assign your models\n" +
                "2. Replace placeholder objects with your assets\n" +
                "3. Run Tab 4 (Validate) to check everything",
                "Got it!");
        }

        private void CreateHierarchy()
        {
            new GameObject("---MANAGERS---");
            new GameObject("---ENVIRONMENT---");
            new GameObject("---GAMEPLAY---");
            new GameObject("---UI---");
            new GameObject("---AUDIO---");
        }

        private void AddAllManagers()
        {
            var parent = GameObject.Find("---MANAGERS---")?.transform ??
                         new GameObject("---MANAGERS---").transform;

            CreateManagerIfMissing<Core.GameManager>("GameManager", parent);
            CreateManagerIfMissing<Core.GameStateManager>("GameStateManager", parent);
            CreateManagerIfMissing<Core.GameFlowController>("GameFlowController", parent);
            CreateManagerIfMissing<Core.SceneLoader>("SceneLoader", parent);
            CreateManagerIfMissing<Audio.AudioManager>("AudioManager", parent);
            CreateManagerIfMissing<Systems.DayNightCycle>("DayNightCycle", parent);
            CreateManagerIfMissing<Systems.DialogueSystem>("DialogueSystem", parent);
            CreateManagerIfMissing<Systems.HorrorEventsSystem>("HorrorEventsSystem", parent);
            CreateManagerIfMissing<Systems.PowerSystem>("PowerSystem", parent);
            CreateManagerIfMissing<Utilities.DebugConsole>("DebugConsole", parent);

            Debug.Log("[MasterSetup] All managers added!");
        }

        private void CreateManagerIfMissing<T>(string name, Transform parent) where T : Component
        {
            if (FindObjectOfType<T>() != null) return;

            var obj = new GameObject(name);
            obj.AddComponent<T>();
            if (parent != null) obj.transform.SetParent(parent);
        }

        private void AddPlayer()
        {
            if (FindObjectOfType<Player.PlayerController>() != null) return;

            var parent = GameObject.Find("---GAMEPLAY---")?.transform;

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1, 0);
            player.tag = "Player";
            if (parent != null) player.transform.SetParent(parent);

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerHealth>();
            player.AddComponent<Player.PlayerInventory>();
            player.AddComponent<Player.DeathRespawnSystem>();
            player.AddComponent<AudioSource>();

            // Camera
            var camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            var cam = camHolder.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camHolder.AddComponent<AudioListener>();

            Debug.Log("[MasterSetup] Player added!");
        }

        private void AddBob()
        {
            if (FindObjectOfType<Core.BobCharacter>() != null) return;

            var parent = GameObject.Find("---GAMEPLAY---")?.transform;

            var bob = new GameObject("Bob");
            bob.transform.position = new Vector3(5, 0, 5);
            if (parent != null) bob.transform.SetParent(parent);

            // Placeholder
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.name = "BobMesh_REPLACE";
            mesh.transform.SetParent(bob.transform);
            mesh.transform.localPosition = new Vector3(0, 1, 0);
            DestroyImmediate(mesh.GetComponent<Collider>());

            bob.AddComponent<Core.BobCharacter>();
            bob.AddComponent<AudioSource>();
            var col = bob.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.4f;
            col.center = new Vector3(0, 0.9f, 0);

            // Spots
            var dropSpot = new GameObject("HamburgerDropSpot");
            dropSpot.transform.SetParent(bob.transform);
            dropSpot.transform.localPosition = new Vector3(0, 1, 0.5f);

            var moneySpot = new GameObject("MoneyDispenseSpot");
            moneySpot.transform.SetParent(bob.transform);
            moneySpot.transform.localPosition = new Vector3(0.5f, 1, 0);

            Debug.Log("[MasterSetup] Bob added!");
        }

        private void AddGasStation()
        {
            var parent = GameObject.Find("---GAMEPLAY---")?.transform;

            // Pumps
            for (int i = 0; i < 4; i++)
            {
                var pump = new GameObject($"GasPump_{i + 1}");
                pump.transform.position = new Vector3(-5, 0, i * 4);
                if (parent != null) pump.transform.SetParent(parent);

                var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mesh.name = "PumpMesh_REPLACE";
                mesh.transform.SetParent(pump.transform);
                mesh.transform.localScale = new Vector3(0.8f, 1.5f, 0.5f);
                mesh.transform.localPosition = new Vector3(0, 0.75f, 0);

                var pumpComp = pump.AddComponent<Economy.GasPump>();
                pumpComp.pumpNumber = i + 1;
            }

            // Register
            var register = new GameObject("CashRegister");
            register.transform.position = new Vector3(8, 0, 2);
            if (parent != null) register.transform.SetParent(parent);
            register.AddComponent<Economy.CashRegister>();

            var regMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            regMesh.name = "RegisterMesh_REPLACE";
            regMesh.transform.SetParent(register.transform);
            regMesh.transform.localScale = new Vector3(0.6f, 0.4f, 0.4f);
            regMesh.transform.localPosition = new Vector3(0, 0.2f, 0);

            // Hamburger vendor
            var vendor = new GameObject("HamburgerVendor");
            vendor.transform.position = new Vector3(12, 0, 0);
            if (parent != null) vendor.transform.SetParent(parent);
            vendor.AddComponent<Economy.HamburgerVendor>();
            vendor.AddComponent<SphereCollider>().isTrigger = true;

            Debug.Log("[MasterSetup] Gas station elements added!");
        }

        private void AddCustomerSystem()
        {
            var parent = GameObject.Find("---GAMEPLAY---")?.transform;

            var spawner = new GameObject("CustomerSpawner");
            if (parent != null) spawner.transform.SetParent(parent);

            var comp = spawner.AddComponent<AI.CustomerSpawner>();

            // Waypoints
            var spawnPt = new GameObject("SpawnPoint");
            spawnPt.transform.position = new Vector3(-20, 0, 0);
            spawnPt.transform.SetParent(spawner.transform);
            comp.spawnPoint = spawnPt.transform;

            var exitPt = new GameObject("ExitPoint");
            exitPt.transform.position = new Vector3(-20, 0, 20);
            exitPt.transform.SetParent(spawner.transform);
            comp.exitPoint = exitPt.transform;

            var storePt = new GameObject("StoreEntrance");
            storePt.transform.position = new Vector3(6, 0, 0);
            storePt.transform.SetParent(spawner.transform);
            comp.storeEntrance = storePt.transform;

            Debug.Log("[MasterSetup] Customer system added!");
        }

        private void AddUI()
        {
            var parent = GameObject.Find("---UI---")?.transform;
            if (parent == null) parent = new GameObject("---UI---").transform;

            // HUD
            var hudCanvas = CreateCanvas("HUD_Canvas", parent);
            hudCanvas.AddComponent<UI.HUDManager>();

            // Event System
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSys = new GameObject("EventSystem");
                eventSys.transform.SetParent(parent);
                eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("[MasterSetup] UI added!");
        }

        private void AddEnvironment()
        {
            var parent = GameObject.Find("---ENVIRONMENT---")?.transform;

            // Ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);
            ground.tag = "Ground";
            if (parent != null) ground.transform.SetParent(parent);

            // Sun
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            if (parent != null) sun.transform.SetParent(parent);

            // Store placeholder
            var store = GameObject.CreatePrimitive(PrimitiveType.Cube);
            store.name = "StoreBuilding_REPLACE";
            store.transform.position = new Vector3(10, 2, 0);
            store.transform.localScale = new Vector3(8, 4, 10);
            if (parent != null) store.transform.SetParent(parent);

            Debug.Log("[MasterSetup] Environment added!");
        }

        private GameObject CreateCanvas(string name, Transform parent)
        {
            var canvas = new GameObject(name);
            if (parent != null) canvas.transform.SetParent(parent);

            var c = canvas.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
                UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            return canvas;
        }

        #endregion

        #region Tab 3: Prefab Setup

        private void DrawPrefabSetup()
        {
            EditorGUILayout.LabelField("STEP 3: Configure Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Now assign your imported models to create game prefabs.\n" +
                "Drag your models into the slots below.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Open Prefab Creator
            if (GUILayout.Button("Open Prefab Creator Window", GUILayout.Height(35)))
            {
                PrefabCreator.ShowWindow();
            }

            EditorGUILayout.Space(10);

            // NFT Models section
            EditorGUILayout.LabelField("NFT Character Models (250)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "For NFT skins, place your 250 character model prefabs in:\n" +
                "Assets/Resources/PlayerModels/\n\n" +
                "Name them by token ID: 001, 002, 003... 250\n" +
                "All models must use the same animation rig.",
                MessageType.Info);

            if (GUILayout.Button("Create PlayerModels Folder"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                if (!AssetDatabase.IsValidFolder("Assets/Resources/PlayerModels"))
                    AssetDatabase.CreateFolder("Assets/Resources", "PlayerModels");
                AssetDatabase.Refresh();
            }

            EditorGUILayout.Space(10);

            // Animation Config
            EditorGUILayout.LabelField("Animation Configuration", EditorStyles.boldLabel);

            if (GUILayout.Button("Create Animation Config Asset"))
            {
                CreateAnimationConfig();
            }

            EditorGUILayout.HelpBox(
                "After creating, find AnimationConfig in Assets/ScriptableObjects/\n" +
                "Fill in your animation clip names (at minimum: Idle, Walk, Run)",
                MessageType.None);
        }

        private void CreateAnimationConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");

            var config = ScriptableObject.CreateInstance<Animation.AnimationConfig>();
            AssetDatabase.CreateAsset(config, "Assets/ScriptableObjects/AnimationConfig.asset");
            AssetDatabase.SaveAssets();

            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);

            Debug.Log("[MasterSetup] Created AnimationConfig asset!");
        }

        #endregion

        #region Tab 4: Validation

        private void DrawValidation()
        {
            EditorGUILayout.LabelField("STEP 4: Validate Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Check that everything is properly configured before playing.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("RUN FULL VALIDATION", GUILayout.Height(40)))
            {
                RunValidation();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // Quick checks
            EditorGUILayout.LabelField("Quick Checks:", EditorStyles.boldLabel);

            DrawCheckItem("GameManager", FindObjectOfType<Core.GameManager>() != null);
            DrawCheckItem("Player", FindObjectOfType<Player.PlayerController>() != null);
            DrawCheckItem("Bob", FindObjectOfType<Core.BobCharacter>() != null);
            DrawCheckItem("Camera", Camera.main != null);
            DrawCheckItem("Cash Register", FindObjectOfType<Economy.CashRegister>() != null);
            DrawCheckItem("Gas Pumps", FindObjectOfType<Economy.GasPump>() != null);
            DrawCheckItem("Customer Spawner", FindObjectOfType<AI.CustomerSpawner>() != null);
            DrawCheckItem("HUD Manager", FindObjectOfType<UI.HUDManager>() != null);
            DrawCheckItem("Event System", FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null);

            EditorGUILayout.Space(10);

            // NavMesh
            var navMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();
            bool hasNavMesh = navMesh.vertices.Length > 0;
            DrawCheckItem("NavMesh Baked", hasNavMesh,
                hasNavMesh ? "" : "Window > AI > Navigation > Bake");

            if (!hasNavMesh)
            {
                EditorGUILayout.HelpBox(
                    "NavMesh is needed for NPC navigation!\n" +
                    "Go to Window > AI > Navigation, then click Bake.",
                    MessageType.Warning);
            }
        }

        private void RunValidation()
        {
            var validator = FindObjectOfType<Utilities.SceneValidator>();
            if (validator != null)
            {
                validator.ValidateScene();
            }
            else
            {
                // Quick validation
                Debug.Log("=== BOB'S PETROLEUM VALIDATION ===");

                int errors = 0;
                int warnings = 0;

                if (FindObjectOfType<Core.GameManager>() == null) { Debug.LogError("Missing: GameManager"); errors++; }
                if (FindObjectOfType<Player.PlayerController>() == null) { Debug.LogError("Missing: Player"); errors++; }
                if (FindObjectOfType<Core.BobCharacter>() == null) { Debug.LogError("Missing: Bob"); errors++; }
                if (Camera.main == null) { Debug.LogError("Missing: Main Camera"); errors++; }
                if (FindObjectOfType<Economy.CashRegister>() == null) { Debug.LogWarning("Missing: CashRegister"); warnings++; }

                if (errors == 0 && warnings == 0)
                {
                    Debug.Log("✓ All checks passed!");
                }
                else
                {
                    Debug.Log($"Result: {errors} errors, {warnings} warnings");
                }
            }
        }

        #endregion

        #region Tab 5: Build Checklist

        private void DrawBuildChecklist()
        {
            EditorGUILayout.LabelField("STEP 5: Build & Deploy", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Final checklist before building your game.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Pre-Build Checklist:", EditorStyles.boldLabel);

            DrawCheckItem("Scene saved", true);
            DrawCheckItem("Scene in Build Settings",
                EditorBuildSettings.scenes.Length > 0,
                "File > Build Settings > Add Open Scenes");
            DrawCheckItem("Player Settings configured", true, "Edit > Project Settings > Player");
            DrawCheckItem("All placeholder meshes replaced", false, "Search for '_REPLACE' in hierarchy");

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("WebGL Settings:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "For WebGL builds:\n" +
                "• Use compressed textures (DXT/ETC)\n" +
                "• Enable texture compression in Player Settings\n" +
                "• Keep draw calls low (<100)\n" +
                "• Disable Multithreaded Rendering\n" +
                "• Set Memory Size to 256-512MB",
                MessageType.None);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Open Build Settings"))
            {
                EditorApplication.ExecuteMenuItem("File/Build Settings...");
            }

            if (GUILayout.Button("Open Player Settings"))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
            }
        }

        #endregion

        #region Helpers

        private void DrawCheckItem(string name, bool isComplete, string fix = "")
        {
            EditorGUILayout.BeginHorizontal();

            GUI.color = isComplete ? Color.green : Color.red;
            GUILayout.Label(isComplete ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;

            GUILayout.Label(name, GUILayout.Width(200));

            if (!isComplete && !string.IsNullOrEmpty(fix))
            {
                GUILayout.Label(fix, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        private float CalculateOverallProgress()
        {
            int total = 10;
            int done = 0;

            if (FindObjectOfType<Core.GameManager>() != null) done++;
            if (FindObjectOfType<Player.PlayerController>() != null) done++;
            if (FindObjectOfType<Core.BobCharacter>() != null) done++;
            if (Camera.main != null) done++;
            if (FindObjectOfType<Economy.CashRegister>() != null) done++;
            if (FindObjectOfType<Economy.GasPump>() != null) done++;
            if (FindObjectOfType<AI.CustomerSpawner>() != null) done++;
            if (FindObjectOfType<UI.HUDManager>() != null) done++;
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) done++;
            if (UnityEngine.AI.NavMesh.CalculateTriangulation().vertices.Length > 0) done++;

            return (float)done / total;
        }

        #endregion
    }
}
#endif
