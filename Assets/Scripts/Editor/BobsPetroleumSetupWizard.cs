#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

namespace BobsPetroleum.Editor
{
    /// <summary>
    /// One-click setup wizard for Bob's Petroleum.
    /// Window > Bob's Petroleum > Setup Wizard
    /// </summary>
    public class BobsPetroleumSetupWizard : EditorWindow
    {
        private Vector2 scrollPos;
        private bool showManagers = true;
        private bool showUI = true;
        private bool showPlayer = true;
        private bool showLayers = true;
        private bool showPrefabs = true;

        // Status tracking
        private Dictionary<string, bool> setupStatus = new Dictionary<string, bool>();

        [MenuItem("Window/Bob's Petroleum/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<BobsPetroleumSetupWizard>("Bob's Petroleum Setup");
            window.minSize = new Vector2(400, 500);
            window.RefreshStatus();
        }

        [MenuItem("Window/Bob's Petroleum/Quick Setup (All)")]
        public static void QuickSetupAll()
        {
            if (EditorUtility.DisplayDialog("Quick Setup",
                "This will create all managers and setup the scene. Continue?",
                "Yes, Setup Everything", "Cancel"))
            {
                CreateAllManagers();
                SetupLayers();
                CreateBasicUI();
                Debug.Log("[Bob's Petroleum] Quick setup complete! Check the scene.");
            }
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            GUILayout.Space(10);
            GUILayout.Label("Bob's Petroleum Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Label("One-click setup for your horror gas station game", EditorStyles.miniLabel);
            GUILayout.Space(10);

            // Quick Setup Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("🚀 QUICK SETUP (Create Everything)", GUILayout.Height(40)))
            {
                QuickSetupAll();
                RefreshStatus();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(20);

            // Managers Section
            showManagers = EditorGUILayout.Foldout(showManagers, "📦 Game Managers", true);
            if (showManagers)
            {
                EditorGUI.indentLevel++;
                DrawManagerButton("GameManager", typeof(Core.GameManager));
                DrawManagerButton("AudioManager", typeof(Audio.AudioManager));
                DrawManagerButton("DayNightCycle", typeof(Systems.DayNightCycle));
                DrawManagerButton("HorrorEventsSystem", typeof(Systems.HorrorEventsSystem));
                DrawManagerButton("RandomEventsSystem", typeof(Systems.RandomEventsSystem));
                DrawManagerButton("QuestSystem", typeof(Systems.QuestSystem));
                DrawManagerButton("GameBootstrapper", typeof(Core.GameBootstrapper));

                GUILayout.Space(5);
                if (GUILayout.Button("Create All Managers"))
                {
                    CreateAllManagers();
                    RefreshStatus();
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // UI Section
            showUI = EditorGUILayout.Foldout(showUI, "🖥️ UI Systems", true);
            if (showUI)
            {
                EditorGUI.indentLevel++;
                DrawManagerButton("HUDManager", typeof(UI.HUDManager));
                DrawManagerButton("PauseMenu", typeof(UI.PauseMenu));
                DrawManagerButton("MinimapSystem", typeof(UI.MinimapSystem));

                GUILayout.Space(5);
                if (GUILayout.Button("Create Basic UI Canvas"))
                {
                    CreateBasicUI();
                    RefreshStatus();
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // Player Section
            showPlayer = EditorGUILayout.Foldout(showPlayer, "🎮 Player", true);
            if (showPlayer)
            {
                EditorGUI.indentLevel++;
                DrawManagerButton("PlayerController", typeof(Player.PlayerController));
                DrawManagerButton("PlayerHealth", typeof(Player.PlayerHealth));
                DrawManagerButton("PlayerInventory", typeof(Player.PlayerInventory));
                DrawManagerButton("Flashlight", typeof(Player.Flashlight));

                GUILayout.Space(5);
                if (GUILayout.Button("Create Player"))
                {
                    CreatePlayer();
                    RefreshStatus();
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // Layers Section
            showLayers = EditorGUILayout.Foldout(showLayers, "🏷️ Layers & Tags", true);
            if (showLayers)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label("Required Layers:", EditorStyles.miniLabel);
                GUILayout.Label("  • Player, Enemy, NPC, Interactable", EditorStyles.miniLabel);
                GUILayout.Label("  • Ground, Buildings, Terrain", EditorStyles.miniLabel);

                if (GUILayout.Button("Setup Layers & Tags"))
                {
                    SetupLayers();
                }
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);

            // Prefabs Section
            showPrefabs = EditorGUILayout.Foldout(showPrefabs, "📁 Prefab Helpers", true);
            if (showPrefabs)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Create Zombie Prefab"))
                {
                    CreateZombiePrefab();
                }

                if (GUILayout.Button("Create Customer Prefab"))
                {
                    CreateCustomerPrefab();
                }

                if (GUILayout.Button("Create Gas Pump Prefab"))
                {
                    CreateGasPumpPrefab();
                }

                if (GUILayout.Button("Create Wild Animal Prefab"))
                {
                    CreateWildAnimalPrefab();
                }

                EditorGUI.indentLevel--;
            }

            GUILayout.Space(20);

            // Validation
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("🔍 Validate Scene Setup", GUILayout.Height(30)))
            {
                ValidateScene();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // Help link
            if (GUILayout.Button("📖 View Implementation Guide"))
            {
                Debug.Log(@"
=== BOB'S PETROLEUM IMPLEMENTATION GUIDE ===

1. QUICK START:
   - Click 'Quick Setup' to create all managers
   - Create Player object or use 'Create Player'
   - Add terrain and buildings

2. MANAGERS CREATED:
   - GameManager: Core game state, day counter, Bob's hunger
   - AudioManager: All game sounds
   - DayNightCycle: Automatic day/night transitions
   - HorrorEventsSystem: Scary events at night
   - QuestSystem: Daily tasks and quests

3. PLAYER SETUP:
   - PlayerController: First-person movement
   - PlayerHealth: Health, damage, death
   - PlayerInventory: Items and money
   - Flashlight: Battery-powered light

4. ADDING CONTENT:
   - Zombies: Add ZombieAI script to enemy
   - Customers: Add CustomerAI script to NPC
   - Gas Pumps: Add GasPump script to pump object
   - Items: Add Pickup script with item data

5. ANIMATIONS (Meshy/Mixamo):
   - Add SimpleAnimationPlayer to character
   - Assign clips in inspector
   - Call player.Play('Walk'), player.Play('Attack'), etc.

6. TESTING:
   - Press Play - everything auto-wires
   - G = Flashlight, M = Minimap, ESC = Pause
   - Day lasts ~5 min, then night with zombies

Done! Your horror gas station is ready.
");
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawManagerButton(string name, System.Type type)
        {
            EditorGUILayout.BeginHorizontal();

            bool exists = FindObjectOfType(type) != null;
            string status = exists ? "✓" : "✗";
            Color color = exists ? Color.green : Color.red;

            GUI.contentColor = color;
            GUILayout.Label(status, GUILayout.Width(20));
            GUI.contentColor = Color.white;

            GUILayout.Label(name, GUILayout.Width(150));

            GUI.enabled = !exists;
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateManager(name, type);
                RefreshStatus();
            }
            GUI.enabled = true;

            if (exists && GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = FindObjectOfType(type);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshStatus()
        {
            setupStatus.Clear();
            setupStatus["GameManager"] = FindObjectOfType<Core.GameManager>() != null;
            setupStatus["AudioManager"] = FindObjectOfType<Audio.AudioManager>() != null;
            setupStatus["Player"] = FindObjectOfType<Player.PlayerController>() != null;
            Repaint();
        }

        #region Creation Methods

        private static void CreateManager(string name, System.Type type)
        {
            GameObject obj = new GameObject(name);
            obj.AddComponent(type);
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            Selection.activeObject = obj;
            Debug.Log($"[Setup] Created {name}");
        }

        private static void CreateAllManagers()
        {
            // Create parent object
            GameObject managersParent = GameObject.Find("---MANAGERS---");
            if (managersParent == null)
            {
                managersParent = new GameObject("---MANAGERS---");
            }

            CreateIfMissing<Core.GameBootstrapper>("GameBootstrapper", managersParent.transform);
            CreateIfMissing<Core.GameManager>("GameManager", managersParent.transform);
            CreateIfMissing<Audio.AudioManager>("AudioManager", managersParent.transform);
            CreateIfMissing<Systems.DayNightCycle>("DayNightCycle", managersParent.transform);
            CreateIfMissing<Systems.HorrorEventsSystem>("HorrorEventsSystem", managersParent.transform);
            CreateIfMissing<Systems.RandomEventsSystem>("RandomEventsSystem", managersParent.transform);
            CreateIfMissing<Systems.QuestSystem>("QuestSystem", managersParent.transform);

            Debug.Log("[Setup] All managers created!");
        }

        private static T CreateIfMissing<T>(string name, Transform parent = null) where T : Component
        {
            T existing = FindObjectOfType<T>();
            if (existing != null) return existing;

            GameObject obj = new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent);
            T component = obj.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            return component;
        }

        private static void CreateBasicUI()
        {
            // Create Canvas if missing
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("GameCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // Create EventSystem if missing
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            // Create UI Managers under canvas
            CreateIfMissing<UI.HUDManager>("HUDManager", canvas.transform);
            CreateIfMissing<UI.PauseMenu>("PauseMenu", canvas.transform);
            CreateIfMissing<UI.MinimapSystem>("MinimapSystem", canvas.transform);

            Debug.Log("[Setup] UI created!");
        }

        private static void CreatePlayer()
        {
            if (FindObjectOfType<Player.PlayerController>() != null)
            {
                Debug.Log("[Setup] Player already exists!");
                return;
            }

            // Create player hierarchy
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.layer = LayerMask.NameToLayer("Player");

            // Add CharacterController
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Add components
            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerHealth>();
            player.AddComponent<Player.PlayerInventory>();

            // Create camera
            GameObject camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            Camera cam = camHolder.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.nearClipPlane = 0.1f;
            camHolder.AddComponent<AudioListener>();

            // Create flashlight
            GameObject flashlight = new GameObject("Flashlight");
            flashlight.transform.SetParent(camHolder.transform);
            flashlight.transform.localPosition = Vector3.zero;
            flashlight.AddComponent<Player.Flashlight>();

            // Position player
            player.transform.position = new Vector3(0, 1, 0);

            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            Selection.activeObject = player;

            Debug.Log("[Setup] Player created with camera and flashlight!");
        }

        private static void SetupLayers()
        {
            Debug.Log(@"[Setup] Please manually add these layers in Edit > Project Settings > Tags and Layers:

LAYERS (User Layer 8+):
  8: Player
  9: Enemy
  10: NPC
  11: Interactable
  12: Ground
  13: Buildings
  14: Terrain
  15: Water

TAGS:
  - Player (built-in)
  - Enemy
  - NPC
  - Interactable
  - GasPump
  - Customer
  - Zombie
  - Item
  - SpawnPoint
  - PlayerSpawn
");
        }

        private static void CreateZombiePrefab()
        {
            GameObject zombie = new GameObject("Zombie");

            // Add capsule collider for physics
            CapsuleCollider col = zombie.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0, 1f, 0);

            // Add NavMeshAgent
            var agent = zombie.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 2f;

            // Add components
            zombie.AddComponent<NPC.ZombieAI>();
            zombie.AddComponent<Animator>();
            zombie.AddComponent<Animation.SimpleAnimationPlayer>();

            // Create as prefab
            SaveAsPrefab(zombie, "Zombie");
        }

        private static void CreateCustomerPrefab()
        {
            GameObject customer = new GameObject("Customer");

            CapsuleCollider col = customer.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.4f;
            col.center = new Vector3(0, 0.9f, 0);

            var agent = customer.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 2f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 1f;

            customer.AddComponent<AI.CustomerAI>();
            customer.AddComponent<Animator>();
            customer.AddComponent<Animation.SimpleAnimationPlayer>();

            SaveAsPrefab(customer, "Customer");
        }

        private static void CreateGasPumpPrefab()
        {
            GameObject pump = new GameObject("GasPump");

            BoxCollider col = pump.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 2f, 0.5f);
            col.center = new Vector3(0, 1f, 0);

            pump.AddComponent<Economy.GasPump>();

            // Add interaction point
            GameObject interactPoint = new GameObject("InteractPoint");
            interactPoint.transform.SetParent(pump.transform);
            interactPoint.transform.localPosition = new Vector3(1f, 0, 0);

            SaveAsPrefab(pump, "GasPump");
        }

        private static void CreateWildAnimalPrefab()
        {
            GameObject animal = new GameObject("WildAnimal");

            CapsuleCollider col = animal.AddComponent<CapsuleCollider>();
            col.height = 1f;
            col.radius = 0.3f;
            col.center = new Vector3(0, 0.5f, 0);

            var agent = animal.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 4f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;

            animal.AddComponent<NPC.WanderingAnimalAI>();
            animal.AddComponent<Animator>();
            animal.AddComponent<Animation.SimpleAnimationPlayer>();

            SaveAsPrefab(animal, "WildAnimal");
        }

        private static void SaveAsPrefab(GameObject obj, string name)
        {
            // Ensure prefab directory exists
            string prefabPath = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string path = $"{prefabPath}/{name}.prefab";

            // Save as prefab
            bool success;
            PrefabUtility.SaveAsPrefabAsset(obj, path, out success);

            if (success)
            {
                Debug.Log($"[Setup] Created prefab: {path}");
            }
            else
            {
                Debug.LogWarning($"[Setup] Failed to create prefab: {path}");
            }

            // Keep in scene or destroy
            Selection.activeObject = obj;
        }

        private static void ValidateScene()
        {
            List<string> issues = new List<string>();
            List<string> warnings = new List<string>();
            List<string> ok = new List<string>();

            // Check essentials
            if (FindObjectOfType<Core.GameManager>() == null)
                issues.Add("Missing GameManager");
            else
                ok.Add("GameManager");

            if (FindObjectOfType<Player.PlayerController>() == null)
                issues.Add("Missing Player");
            else
                ok.Add("Player");

            if (Camera.main == null)
                issues.Add("No Main Camera");
            else
                ok.Add("Main Camera");

            if (FindObjectOfType<UnityEngine.AI.NavMeshSurface>() == null)
                warnings.Add("No NavMesh - AI won't navigate");

            if (FindObjectOfType<Audio.AudioManager>() == null)
                warnings.Add("No AudioManager - no game sounds");

            if (FindObjectOfType<Light>() == null)
                warnings.Add("No lights in scene");

            if (FindObjectOfType<Terrain>() == null)
                warnings.Add("No terrain - consider adding ground");

            // Report
            Debug.Log("=== SCENE VALIDATION ===");

            if (ok.Count > 0)
            {
                Debug.Log($"✓ OK ({ok.Count}):");
                foreach (var item in ok)
                    Debug.Log($"  • {item}");
            }

            if (warnings.Count > 0)
            {
                Debug.LogWarning($"⚠ Warnings ({warnings.Count}):");
                foreach (var item in warnings)
                    Debug.LogWarning($"  • {item}");
            }

            if (issues.Count > 0)
            {
                Debug.LogError($"✗ Issues ({issues.Count}):");
                foreach (var item in issues)
                    Debug.LogError($"  • {item}");
            }
            else
            {
                Debug.Log("✓ Scene is ready to play!");
            }
        }

        #endregion
    }

    /// <summary>
    /// Context menu extensions for quick component adding.
    /// </summary>
    public static class BobsPetroleumContextMenus
    {
        [MenuItem("GameObject/Bob's Petroleum/Create Player", false, 10)]
        public static void CreatePlayerMenu()
        {
            BobsPetroleumSetupWizard.QuickSetupAll();
        }

        [MenuItem("GameObject/Bob's Petroleum/Add Zombie AI", false, 20)]
        public static void AddZombieAI()
        {
            if (Selection.activeGameObject != null)
            {
                Undo.AddComponent<NPC.ZombieAI>(Selection.activeGameObject);
                Debug.Log($"Added ZombieAI to {Selection.activeGameObject.name}");
            }
        }

        [MenuItem("GameObject/Bob's Petroleum/Add Customer AI", false, 21)]
        public static void AddCustomerAI()
        {
            if (Selection.activeGameObject != null)
            {
                Undo.AddComponent<AI.CustomerAI>(Selection.activeGameObject);
                Debug.Log($"Added CustomerAI to {Selection.activeGameObject.name}");
            }
        }

        [MenuItem("GameObject/Bob's Petroleum/Add Simple Animation Player", false, 30)]
        public static void AddSimpleAnimationPlayer()
        {
            if (Selection.activeGameObject != null)
            {
                Undo.AddComponent<Animation.SimpleAnimationPlayer>(Selection.activeGameObject);
                Debug.Log($"Added SimpleAnimationPlayer to {Selection.activeGameObject.name}");
            }
        }

        [MenuItem("GameObject/Bob's Petroleum/Add Gas Pump", false, 40)]
        public static void AddGasPump()
        {
            if (Selection.activeGameObject != null)
            {
                Undo.AddComponent<Economy.GasPump>(Selection.activeGameObject);
                Debug.Log($"Added GasPump to {Selection.activeGameObject.name}");
            }
        }
    }
}
#endif
