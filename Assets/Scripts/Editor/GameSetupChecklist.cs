#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BobsPetroleum.Editor
{
    /// <summary>
    /// Master checklist showing EVERYTHING that needs to be set up.
    /// One window to see it all - no more guessing!
    /// </summary>
    public class GameSetupChecklist : EditorWindow
    {
        private Vector2 scrollPos;
        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

        [MenuItem("Window/Bob's Petroleum/Setup Checklist")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSetupChecklist>("Game Setup Checklist");
            window.minSize = new Vector2(450, 600);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            EditorGUILayout.Space(10);
            GUILayout.Label("BOB'S PETROLEUM - SETUP CHECKLIST", EditorStyles.boldLabel);
            GUILayout.Label("Check each section. Green = Ready, Red = Missing", EditorStyles.miniLabel);
            EditorGUILayout.Space(10);

            // Quick status
            DrawQuickStatus();

            EditorGUILayout.Space(10);

            // Sections
            DrawSection("MANAGERS", DrawManagersChecklist);
            DrawSection("PLAYER", DrawPlayerChecklist);
            DrawSection("BOB (The Dying Owner)", DrawBobChecklist);
            DrawSection("SHOP & ECONOMY", DrawEconomyChecklist);
            DrawSection("COMBAT & PETS", DrawCombatChecklist);
            DrawSection("UI", DrawUIChecklist);
            DrawSection("WORLD", DrawWorldChecklist);
            DrawSection("AUDIO", DrawAudioChecklist);
            DrawSection("MULTIPLAYER", DrawMultiplayerChecklist);

            EditorGUILayout.Space(20);

            // Action buttons
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickStatus()
        {
            int total = 0;
            int ready = 0;

            // Count all checks
            ready += Check(FindObjectOfType<Core.GameManager>() != null, ref total);
            ready += Check(FindObjectOfType<Core.BobCharacter>() != null, ref total);
            ready += Check(FindObjectOfType<Player.PlayerController>() != null, ref total);
            ready += Check(FindObjectOfType<Economy.CashRegister>() != null, ref total);
            ready += Check(FindObjectOfType<Economy.ShopManager>() != null, ref total);
            ready += Check(FindObjectOfType<UI.HUDManager>() != null, ref total);
            ready += Check(Camera.main != null, ref total);
            ready += Check(FindObjectOfType<UnityEngine.AI.NavMeshSurface>() != null ||
                          UnityEngine.AI.NavMesh.CalculateTriangulation().vertices.Length > 0, ref total);

            float percent = total > 0 ? (float)ready / total * 100f : 0f;

            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label($"Overall Progress: {ready}/{total} ({percent:F0}%)", EditorStyles.boldLabel);

            if (percent >= 100)
            {
                GUI.color = Color.green;
                GUILayout.Label("READY TO PLAY!", EditorStyles.boldLabel);
            }
            else if (percent >= 50)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("Getting There...", EditorStyles.boldLabel);
            }
            else
            {
                GUI.color = Color.red;
                GUILayout.Label("Needs Setup", EditorStyles.boldLabel);
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private int Check(bool condition, ref int total)
        {
            total++;
            return condition ? 1 : 0;
        }

        private void DrawSection(string title, System.Action drawContent)
        {
            if (!foldouts.ContainsKey(title))
                foldouts[title] = true;

            EditorGUILayout.BeginVertical("box");
            foldouts[title] = EditorGUILayout.Foldout(foldouts[title], title, true, EditorStyles.foldoutHeader);

            if (foldouts[title])
            {
                EditorGUI.indentLevel++;
                drawContent();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        #region Checklists

        private void DrawManagersChecklist()
        {
            DrawCheckItem("GameManager", FindObjectOfType<Core.GameManager>() != null,
                "Controls game state, days, win/lose", () => CreateManager<Core.GameManager>("GameManager"));

            DrawCheckItem("GameBootstrapper", FindObjectOfType<Core.GameBootstrapper>() != null,
                "Auto-creates managers on Play", () => CreateManager<Core.GameBootstrapper>("GameBootstrapper"));

            DrawCheckItem("AudioManager", FindObjectOfType<Audio.AudioManager>() != null,
                "Handles all game sounds", () => CreateManager<Audio.AudioManager>("AudioManager"));

            DrawCheckItem("DayNightCycle", FindObjectOfType<Systems.DayNightCycle>() != null,
                "Day/night transitions", () => CreateManager<Systems.DayNightCycle>("DayNightCycle"));

            DrawCheckItem("HorrorEventsSystem", FindObjectOfType<Systems.HorrorEventsSystem>() != null,
                "Scary events at night", () => CreateManager<Systems.HorrorEventsSystem>("HorrorEventsSystem"));

            DrawCheckItem("QuestSystem", FindObjectOfType<Systems.QuestSystem>() != null,
                "Daily tasks and objectives", () => CreateManager<Systems.QuestSystem>("QuestSystem"));

            DrawCheckItem("DialogueSystem", FindObjectOfType<Systems.DialogueSystem>() != null,
                "NPC conversations", () => CreateManager<Systems.DialogueSystem>("DialogueSystem"));
        }

        private void DrawPlayerChecklist()
        {
            var player = FindObjectOfType<Player.PlayerController>();

            DrawCheckItem("Player Object", player != null,
                "First-person player controller", () => CreatePlayer());

            if (player != null)
            {
                DrawCheckItem("  CharacterController", player.GetComponent<CharacterController>() != null,
                    "Required for movement");

                DrawCheckItem("  PlayerHealth", player.GetComponent<Player.PlayerHealth>() != null,
                    "Health and damage", () => player.gameObject.AddComponent<Player.PlayerHealth>());

                DrawCheckItem("  PlayerInventory", player.GetComponent<Player.PlayerInventory>() != null,
                    "Money and items", () => player.gameObject.AddComponent<Player.PlayerInventory>());

                DrawCheckItem("  Camera", player.GetComponentInChildren<Camera>() != null,
                    "Main camera on player");

                DrawCheckItem("  Flashlight", player.GetComponentInChildren<Player.Flashlight>() != null,
                    "Flashlight for night");

                DrawCheckItem("  SimpleGunSystem", FindObjectOfType<Combat.SimpleGunSystem>() != null,
                    "Weapon handling", () => player.gameObject.AddComponent<Combat.SimpleGunSystem>());

                DrawCheckItem("  PetCaptureSystem", FindObjectOfType<Battle.PetCaptureSystem>() != null,
                    "Net throwing for pets", () => player.gameObject.AddComponent<Battle.PetCaptureSystem>());
            }
        }

        private void DrawBobChecklist()
        {
            var bob = FindObjectOfType<Core.BobCharacter>();

            DrawCheckItem("Bob Character", bob != null,
                "The dying owner you save!", () => CreateBob());

            if (bob != null)
            {
                DrawCheckItem("  Health Set", bob.currentHealth > 0 && bob.maxHealth > 0,
                    "Health values configured");

                DrawCheckItem("  HamburgerDropSpot", bob.hamburgerDropSpot != null,
                    "Where hamburgers appear", () => {
                        var spot = new GameObject("HamburgerDropSpot");
                        spot.transform.SetParent(bob.transform);
                        spot.transform.localPosition = new Vector3(0, 1, 0.5f);
                        bob.hamburgerDropSpot = spot.transform;
                    });

                DrawCheckItem("  MoneyDispenseSpot", bob.moneyDispenseSpot != null,
                    "Where rewards spawn", () => {
                        var spot = new GameObject("MoneyDispenseSpot");
                        spot.transform.SetParent(bob.transform);
                        spot.transform.localPosition = new Vector3(0.5f, 1, 0);
                        bob.moneyDispenseSpot = spot.transform;
                    });

                DrawCheckItem("  Hamburger Prefab", bob.hamburgerPrefab != null,
                    "Hamburger model to spawn");

                DrawCheckItem("  Animator/SimpleAnimator", bob.animator != null || bob.simpleAnimator != null,
                    "For eating animations");

                DrawCheckItem("  Audio Clips", bob.eatingSound != null,
                    "Sounds for feeding");
            }
        }

        private void DrawEconomyChecklist()
        {
            DrawCheckItem("CashRegister", FindObjectOfType<Economy.CashRegister>() != null,
                "Customer checkout system", () => CreateManager<Economy.CashRegister>("CashRegister"));

            var register = FindObjectOfType<Economy.CashRegister>();
            if (register != null)
            {
                DrawCheckItem("  Player Position", register.playerPosition != null,
                    "Where player stands at register");
                DrawCheckItem("  Register UI", register.registerUI != null,
                    "Transaction interface");
            }

            DrawCheckItem("ShopManager", FindObjectOfType<Economy.ShopManager>() != null,
                "Open/close shop control", () => CreateManager<Economy.ShopManager>("ShopManager"));

            DrawCheckItem("ShopSystem", FindObjectOfType<Economy.ShopSystem>() != null,
                "Item purchasing", () => CreateManager<Economy.ShopSystem>("ShopSystem"));

            DrawCheckItem("GasPump", FindObjectOfType<Economy.GasPump>() != null,
                "Gas station pump", null);
        }

        private void DrawCombatChecklist()
        {
            DrawCheckItem("SimpleGunSystem", FindObjectOfType<Combat.SimpleGunSystem>() != null,
                "Weapon system");

            DrawCheckItem("PetCaptureSystem", FindObjectOfType<Battle.PetCaptureSystem>() != null,
                "Net capture system");

            var capture = FindObjectOfType<Battle.PetCaptureSystem>();
            if (capture != null)
            {
                DrawCheckItem("  Net Prefab", capture.netPrefab != null,
                    "Throwable net object");
            }

            DrawCheckItem("BattleCameraSystem", FindObjectOfType<Battle.BattleCameraSystem>() != null,
                "Pet battle camera", () => CreateManager<Battle.BattleCameraSystem>("BattleCameraSystem"));

            DrawCheckItem("BattleSystem", FindObjectOfType<Battle.BattleSystem>() != null,
                "Pet battle logic");
        }

        private void DrawUIChecklist()
        {
            DrawCheckItem("Canvas", FindObjectOfType<Canvas>() != null,
                "UI container", () => CreateCanvas());

            DrawCheckItem("EventSystem", FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null,
                "UI input handling");

            DrawCheckItem("HUDManager", FindObjectOfType<UI.HUDManager>() != null,
                "Health, stamina, money display");

            DrawCheckItem("PauseMenu", FindObjectOfType<UI.PauseMenu>() != null,
                "Escape menu");

            DrawCheckItem("MinimapSystem", FindObjectOfType<UI.MinimapSystem>() != null,
                "Minimap display");
        }

        private void DrawWorldChecklist()
        {
            DrawCheckItem("Main Camera", Camera.main != null,
                "Tagged MainCamera");

            DrawCheckItem("Directional Light", FindObjectOfType<Light>()?.type == LightType.Directional,
                "Sun light");

            var navMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();
            DrawCheckItem("NavMesh Baked", navMesh.vertices.Length > 0,
                "AI navigation mesh (Window > AI > Navigation > Bake)");

            DrawCheckItem("Terrain/Ground", FindObjectOfType<Terrain>() != null ||
                GameObject.FindGameObjectWithTag("Ground") != null,
                "Walkable surface");

            DrawCheckItem("PlayerSpawn Point", GameObject.FindGameObjectWithTag("PlayerSpawn") != null ||
                GameObject.Find("PlayerSpawn") != null,
                "Player start location");
        }

        private void DrawAudioChecklist()
        {
            var audioMgr = FindObjectOfType<Audio.AudioManager>();

            DrawCheckItem("AudioManager", audioMgr != null, "Sound management");

            DrawCheckItem("AudioListener", FindObjectOfType<AudioListener>() != null,
                "Usually on camera");

            // Can't check actual clips easily, just note it
            EditorGUILayout.HelpBox(
                "Audio clips are assigned in inspector on each system:\n" +
                "- AudioManager: Music, ambient\n" +
                "- BobCharacter: Eating, happy, death\n" +
                "- CashRegister: Scan, coins, drawer\n" +
                "- Player: Footsteps, damage",
                MessageType.Info);
        }

        private void DrawMultiplayerChecklist()
        {
            DrawCheckItem("LobbySystem", FindObjectOfType<Multiplayer.LobbySystem>() != null,
                "Multiplayer lobby", () => CreateManager<Multiplayer.LobbySystem>("LobbySystem"));

            DrawCheckItem("PlayerSpawnPoints", FindObjectsOfType<Multiplayer.PlayerSpawnPoint>().Length >= 2,
                "At least 2 spawn points");

            EditorGUILayout.HelpBox(
                "For full multiplayer, you'll need:\n" +
                "- Unity Netcode for GameObjects package\n" +
                "- NetworkManager component\n" +
                "- Player prefab with NetworkObject",
                MessageType.Info);
        }

        #endregion

        #region Drawing Helpers

        private void DrawCheckItem(string name, bool isComplete, string tooltip, System.Action fixAction = null)
        {
            EditorGUILayout.BeginHorizontal();

            // Status icon
            GUI.color = isComplete ? Color.green : Color.red;
            GUILayout.Label(isComplete ? "✓" : "✗", GUILayout.Width(20));
            GUI.color = Color.white;

            // Name
            GUILayout.Label(new GUIContent(name, tooltip), GUILayout.Width(200));

            // Fix button
            if (!isComplete && fixAction != null)
            {
                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    fixAction();
                }
            }
            else if (isComplete)
            {
                GUILayout.Label("Ready", GUILayout.Width(60));
            }
            else
            {
                GUILayout.Label("Manual", GUILayout.Width(60));
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Creation Helpers

        private void CreateManager<T>(string name) where T : Component
        {
            if (FindObjectOfType<T>() != null) return;

            GameObject obj = new GameObject(name);
            obj.AddComponent<T>();
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            Selection.activeObject = obj;
        }

        private void CreatePlayer()
        {
            if (FindObjectOfType<Player.PlayerController>() != null) return;

            GameObject player = new GameObject("Player");
            player.tag = "Player";

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            player.AddComponent<Player.PlayerController>();
            player.AddComponent<Player.PlayerHealth>();
            player.AddComponent<Player.PlayerInventory>();

            GameObject camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            Camera cam = camHolder.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camHolder.AddComponent<AudioListener>();

            GameObject flashlight = new GameObject("Flashlight");
            flashlight.transform.SetParent(camHolder.transform);
            flashlight.AddComponent<Player.Flashlight>();

            player.transform.position = new Vector3(0, 1, 0);

            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            Selection.activeObject = player;
        }

        private void CreateBob()
        {
            if (FindObjectOfType<Core.BobCharacter>() != null) return;

            GameObject bob = new GameObject("Bob");
            bob.AddComponent<Core.BobCharacter>();

            // Add basic collider
            var col = bob.AddComponent<CapsuleCollider>();
            col.height = 1.8f;
            col.radius = 0.4f;
            col.center = new Vector3(0, 0.9f, 0);

            bob.transform.position = new Vector3(5, 0, 5);

            Undo.RegisterCreatedObjectUndo(bob, "Create Bob");
            Selection.activeObject = bob;
        }

        private void CreateCanvas()
        {
            if (FindObjectOfType<Canvas>() != null) return;

            GameObject canvasObj = new GameObject("GameCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE ALL MISSING", GUILayout.Height(30)))
            {
                CreateAllMissing();
            }

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("VALIDATE SCENE", GUILayout.Height(30)))
            {
                Utilities.SceneValidator.RunValidation(true);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Open Full Setup Wizard"))
            {
                BobsPetroleumSetupWizard.ShowWindow();
            }
        }

        private void CreateAllMissing()
        {
            // Managers
            if (FindObjectOfType<Core.GameBootstrapper>() == null)
                CreateManager<Core.GameBootstrapper>("GameBootstrapper");
            if (FindObjectOfType<Core.GameManager>() == null)
                CreateManager<Core.GameManager>("GameManager");
            if (FindObjectOfType<Audio.AudioManager>() == null)
                CreateManager<Audio.AudioManager>("AudioManager");
            if (FindObjectOfType<Systems.DayNightCycle>() == null)
                CreateManager<Systems.DayNightCycle>("DayNightCycle");

            // Player
            if (FindObjectOfType<Player.PlayerController>() == null)
                CreatePlayer();

            // Bob
            if (FindObjectOfType<Core.BobCharacter>() == null)
                CreateBob();

            // Economy
            if (FindObjectOfType<Economy.CashRegister>() == null)
                CreateManager<Economy.CashRegister>("CashRegister");
            if (FindObjectOfType<Economy.ShopManager>() == null)
                CreateManager<Economy.ShopManager>("ShopManager");

            // Canvas
            if (FindObjectOfType<Canvas>() == null)
                CreateCanvas();

            Debug.Log("[Setup] Created all missing components!");
        }

        #endregion
    }
}
#endif
