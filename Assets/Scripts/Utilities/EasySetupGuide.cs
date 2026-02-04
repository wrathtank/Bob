using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// EASY SETUP GUIDE - Shows what you're missing!
    /// Add this to your scene and it tells you what to do.
    ///
    /// USAGE:
    /// 1. Add this component to any GameObject in your scene
    /// 2. Press Play
    /// 3. Look at the Console for setup instructions
    /// 4. Follow the instructions!
    ///
    /// This component auto-detects missing systems and tells you
    /// exactly what to add and where to drag things.
    /// </summary>
    [AddComponentMenu("Bob's Petroleum/Setup/Easy Setup Guide")]
    public class EasySetupGuide : MonoBehaviour
    {
        [Header("=== SETUP GUIDE ===")]
        [Tooltip("Show setup instructions when Play starts")]
        public bool showOnStart = true;

        [Tooltip("Show in Game View (not just Console)")]
        public bool showInGameView = true;

        [Tooltip("Auto-create missing core managers")]
        public bool autoCreateManagers = true;

        [Header("=== SCENE REFERENCES ===")]
        [Tooltip("Your main camera (usually auto-found)")]
        public Camera mainCamera;

        [Tooltip("Your player prefab")]
        public GameObject playerPrefab;

        [Tooltip("Your Bob character")]
        public GameObject bobCharacter;

        // Setup status
        private List<SetupItem> setupItems = new List<SetupItem>();
        private bool setupComplete = false;
        private Rect windowRect = new Rect(20, 20, 400, 500);

        private void Start()
        {
            if (showOnStart)
            {
                RunSetupCheck();
            }
        }

        /// <summary>
        /// Check what's set up and what's missing.
        /// </summary>
        public void RunSetupCheck()
        {
            setupItems.Clear();

            Debug.Log("╔════════════════════════════════════════════════════════════╗");
            Debug.Log("║        BOB'S PETROLEUM - SETUP GUIDE                       ║");
            Debug.Log("╚════════════════════════════════════════════════════════════╝");
            Debug.Log("");

            // Check Core Systems
            CheckCoreManagers();

            // Check Player
            CheckPlayerSetup();

            // Check Bob
            CheckBobSetup();

            // Check UI
            CheckUISetup();

            // Check Economy
            CheckEconomySetup();

            // Check Environment
            CheckEnvironmentSetup();

            // Summary
            PrintSummary();

            // Auto-create if enabled
            if (autoCreateManagers)
            {
                AutoCreateMissingManagers();
            }
        }

        #region System Checks

        private void CheckCoreManagers()
        {
            Debug.Log("── CORE MANAGERS ──────────────────────────────────────────");

            // GameManager
            var gameManager = FindObjectOfType<Core.GameManager>();
            AddSetupItem("GameManager", gameManager != null,
                "Controls day/night, scoring, game state",
                "Create empty GameObject > Add GameManager component");

            // GameStateManager
            var gameState = FindObjectOfType<Core.GameStateManager>();
            AddSetupItem("GameStateManager", gameState != null,
                "Handles game states (Playing, Paused, GameOver)",
                "Create empty GameObject > Add GameStateManager component");

            // GameFlowController
            var flowController = FindObjectOfType<Core.GameFlowController>();
            AddSetupItem("GameFlowController", flowController != null,
                "THE GLUE - connects all systems together",
                "Create empty GameObject > Add GameFlowController component");

            // SceneLoader
            var sceneLoader = FindObjectOfType<Core.SceneLoader>();
            AddSetupItem("SceneLoader", sceneLoader != null,
                "Loading screens between scenes",
                "Create empty GameObject > Add SceneLoader component");

            Debug.Log("");
        }

        private void CheckPlayerSetup()
        {
            Debug.Log("── PLAYER SETUP ───────────────────────────────────────────");

            // Camera
            mainCamera = mainCamera ?? Camera.main;
            AddSetupItem("Main Camera", mainCamera != null,
                "Player camera for FPS view",
                "Create Camera > Tag as 'MainCamera'");

            // Player Controller
            var playerController = FindObjectOfType<Player.PlayerController>();
            AddSetupItem("PlayerController", playerController != null,
                "FPS movement and input",
                "Create Player prefab > Add PlayerController, CharacterController");

            // Player Inventory
            var inventory = FindObjectOfType<Player.PlayerInventory>();
            AddSetupItem("PlayerInventory", inventory != null,
                "Tracks money, items, hamburgers",
                "Add PlayerInventory to Player");

            // Death/Respawn
            var deathSystem = FindObjectOfType<Player.DeathRespawnSystem>();
            AddSetupItem("DeathRespawnSystem", deathSystem != null,
                "Handles death and respawn",
                "Add DeathRespawnSystem to Player");

            // Clone Spawn
            var cloneSpawn = FindObjectOfType<Core.CloneSpawnSystem>();
            AddSetupItem("CloneSpawnSystem", cloneSpawn != null,
                "Spawns players from clone tubes",
                "Create CloneSpawnSystem > Assign spawn points");

            Debug.Log("");
        }

        private void CheckBobSetup()
        {
            Debug.Log("── BOB CHARACTER ──────────────────────────────────────────");

            var bob = FindObjectOfType<Core.BobCharacter>();
            AddSetupItem("BobCharacter", bob != null,
                "The dying owner you must save!",
                "Create Bob model > Add BobCharacter component");

            if (bob != null)
            {
                bool hasAnimator = bob.GetComponent<Animator>() != null ||
                                   bob.GetComponent<Animation.SimpleAnimationPlayer>() != null;
                AddSetupItem("Bob Animator", hasAnimator,
                    "Animations for Bob",
                    "Add Animator or SimpleAnimationPlayer to Bob");

                bool hasAudio = bob.GetComponent<AudioSource>() != null;
                AddSetupItem("Bob AudioSource", hasAudio,
                    "For Bob's dialogue",
                    "Add AudioSource to Bob");
            }

            Debug.Log("");
        }

        private void CheckUISetup()
        {
            Debug.Log("── UI SYSTEMS ─────────────────────────────────────────────");

            // HUD
            var hud = FindObjectOfType<UI.HUDManager>();
            AddSetupItem("HUDManager", hud != null,
                "Displays health, money, notifications",
                "Create Canvas > Add HUDManager > Wire up UI elements");

            // Main Menu
            var mainMenu = FindObjectOfType<UI.MainMenuManager>();
            AddSetupItem("MainMenuManager", mainMenu != null,
                "Main menu with Play, Settings, Quit",
                "Create Menu Canvas > Add MainMenuManager");

            // Settings
            var settings = FindObjectOfType<UI.SettingsManager>();
            AddSetupItem("SettingsManager", settings != null,
                "Audio, graphics, controls settings",
                "Create Settings panel > Add SettingsManager");

            // Pause Menu
            var pauseMenu = FindObjectOfType<UI.PauseMenuManager>();
            AddSetupItem("PauseMenuManager", pauseMenu != null,
                "Pause menu with Resume, Quit",
                "Create Pause panel > Add PauseMenuManager");

            // Tutorial
            var tutorial = FindObjectOfType<UI.TutorialManager>();
            AddSetupItem("TutorialManager", tutorial != null,
                "Tutorial for new players",
                "Create Tutorial panel > Add TutorialManager");

            Debug.Log("");
        }

        private void CheckEconomySetup()
        {
            Debug.Log("── ECONOMY SYSTEMS ────────────────────────────────────────");

            var cashRegister = FindObjectOfType<Economy.CashRegister>();
            AddSetupItem("CashRegister", cashRegister != null,
                "Where customers pay - earn money!",
                "Create Register model > Add CashRegister");

            var shopManager = FindObjectOfType<Economy.ShopManager>();
            AddSetupItem("ShopManager", shopManager != null,
                "Controls shop open/close, NPC",
                "Create Shop area > Add ShopManager");

            var shopSystem = FindObjectOfType<Economy.ShopSystem>();
            AddSetupItem("ShopSystem", shopSystem != null,
                "Buy weapons, pets, upgrades",
                "Create Shop UI > Add ShopSystem");

            Debug.Log("");
        }

        private void CheckEnvironmentSetup()
        {
            Debug.Log("── ENVIRONMENT ────────────────────────────────────────────");

            // Ground/Floor
            var ground = GameObject.FindGameObjectWithTag("Ground");
            if (ground == null) ground = FindObjectOfType<Terrain>()?.gameObject;
            AddSetupItem("Ground/Terrain", ground != null,
                "Something to walk on!",
                "Create Terrain or Plane > Tag as 'Ground'");

            // Lighting
            var light = FindObjectOfType<Light>();
            AddSetupItem("Directional Light", light != null,
                "Scene lighting",
                "Create Directional Light");

            // Water (optional)
            var water = FindObjectOfType<Environment.WaterSystem>();
            AddSetupItem("Water (optional)", water != null,
                "Wavy water surfaces",
                "Create Plane > Add WaterSystem > Apply Water shader",
                isRequired: false);

            Debug.Log("");
        }

        #endregion

        #region Setup Item Management

        private void AddSetupItem(string name, bool isComplete, string description, string howToFix, bool isRequired = true)
        {
            string status = isComplete ? "✓" : (isRequired ? "✗" : "○");
            string color = isComplete ? "green" : (isRequired ? "red" : "yellow");

            SetupItem item = new SetupItem
            {
                name = name,
                isComplete = isComplete,
                isRequired = isRequired,
                description = description,
                howToFix = howToFix
            };
            setupItems.Add(item);

            if (isComplete)
            {
                Debug.Log($"  <color=green>✓</color> {name}");
            }
            else if (isRequired)
            {
                Debug.Log($"  <color=red>✗</color> {name} - MISSING!");
                Debug.Log($"      → {howToFix}");
            }
            else
            {
                Debug.Log($"  <color=yellow>○</color> {name} (optional)");
            }
        }

        private void PrintSummary()
        {
            int total = 0;
            int complete = 0;
            int required = 0;
            int requiredComplete = 0;

            foreach (var item in setupItems)
            {
                total++;
                if (item.isComplete) complete++;
                if (item.isRequired)
                {
                    required++;
                    if (item.isComplete) requiredComplete++;
                }
            }

            setupComplete = requiredComplete >= required;

            Debug.Log("══════════════════════════════════════════════════════════════");
            Debug.Log($"  SETUP STATUS: {complete}/{total} systems ready");
            Debug.Log($"  REQUIRED: {requiredComplete}/{required}");

            if (setupComplete)
            {
                Debug.Log("  <color=green>★ CORE SETUP COMPLETE! Ready to play! ★</color>");
            }
            else
            {
                Debug.Log($"  <color=red>⚠ Missing {required - requiredComplete} required systems</color>");
                Debug.Log("  Follow the instructions above to complete setup.");
            }
            Debug.Log("══════════════════════════════════════════════════════════════");
        }

        #endregion

        #region Auto-Create Managers

        private void AutoCreateMissingManagers()
        {
            bool created = false;

            // Create Managers parent
            GameObject managers = GameObject.Find("---MANAGERS---");
            if (managers == null)
            {
                managers = new GameObject("---MANAGERS---");
                created = true;
            }

            // GameManager
            if (FindObjectOfType<Core.GameManager>() == null)
            {
                var go = new GameObject("GameManager");
                go.transform.SetParent(managers.transform);
                go.AddComponent<Core.GameManager>();
                Debug.Log("[SetupGuide] Created GameManager");
                created = true;
            }

            // GameStateManager
            if (FindObjectOfType<Core.GameStateManager>() == null)
            {
                var go = new GameObject("GameStateManager");
                go.transform.SetParent(managers.transform);
                go.AddComponent<Core.GameStateManager>();
                Debug.Log("[SetupGuide] Created GameStateManager");
                created = true;
            }

            // GameFlowController
            if (FindObjectOfType<Core.GameFlowController>() == null)
            {
                var go = new GameObject("GameFlowController");
                go.transform.SetParent(managers.transform);
                go.AddComponent<Core.GameFlowController>();
                Debug.Log("[SetupGuide] Created GameFlowController");
                created = true;
            }

            if (created)
            {
                Debug.Log("[SetupGuide] Auto-created missing managers! Check the Hierarchy.");
            }
        }

        #endregion

        #region In-Game Display

        private void OnGUI()
        {
            if (!showInGameView || setupItems.Count == 0) return;

            windowRect = GUILayout.Window(12345, windowRect, DrawSetupWindow, "Bob's Petroleum Setup Guide");
        }

        private void DrawSetupWindow(int windowID)
        {
            GUILayout.Label(setupComplete ? "✓ Setup Complete!" : "⚠ Setup Incomplete",
                new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold });

            GUILayout.Space(10);

            // Scrollable list
            GUILayout.BeginVertical(GUI.skin.box);

            foreach (var item in setupItems)
            {
                if (!item.isComplete && item.isRequired)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("✗", GUILayout.Width(20));
                    GUILayout.Label(item.name, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                    GUILayout.EndHorizontal();

                    GUILayout.Label($"  → {item.howToFix}", new GUIStyle(GUI.skin.label) { wordWrap = true });
                    GUILayout.Space(5);
                }
            }

            if (setupComplete)
            {
                GUILayout.Label("All required systems are set up!", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic });
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            if (GUILayout.Button("Refresh Check"))
            {
                RunSetupCheck();
            }

            if (GUILayout.Button("Hide This Window"))
            {
                showInGameView = false;
            }

            GUI.DragWindow();
        }

        #endregion

        private struct SetupItem
        {
            public string name;
            public bool isComplete;
            public bool isRequired;
            public string description;
            public string howToFix;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EasySetupGuide))]
    public class EasySetupGuideEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Run Setup Check Now", GUILayout.Height(30)))
            {
                ((EasySetupGuide)target).RunSetupCheck();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Open Full Setup Wizard"))
            {
                EditorApplication.ExecuteMenuItem("Window/Bob's Petroleum/Setup Wizard");
            }

            if (GUILayout.Button("Open Setup Checklist"))
            {
                EditorApplication.ExecuteMenuItem("Window/Bob's Petroleum/Setup Checklist");
            }
        }
    }
#endif
}
