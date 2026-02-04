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
            DrawSection("CORE SYSTEMS", DrawCoreSystemsChecklist);
            DrawSection("PLAYER", DrawPlayerChecklist);
            DrawSection("BOB (The Dying Owner)", DrawBobChecklist);
            DrawSection("CLONE SPAWN (Tubes)", DrawCloneSpawnChecklist);
            DrawSection("SHOP & ECONOMY", DrawEconomyChecklist);
            DrawSection("COMBAT & WEAPONS", DrawCombatChecklist);
            DrawSection("PETS & BATTLES", DrawPetsChecklist);
            DrawSection("ITEMS & CRAFTING", DrawItemsChecklist);
            DrawSection("WORLD SYSTEMS", DrawWorldChecklist);
            DrawSection("UI", DrawUIChecklist);
            DrawSection("AUDIO", DrawAudioChecklist);
            DrawSection("NETWORKING & SAVES", DrawNetworkingChecklist);

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

        private void DrawCoreSystemsChecklist()
        {
            DrawCheckItem("GameManager", FindObjectOfType<Core.GameManager>() != null,
                "Controls game state, days, win/lose", () => CreateManager<Core.GameManager>("GameManager"));

            DrawCheckItem("GameStateManager", FindObjectOfType<Core.GameStateManager>() != null,
                "Clear game states (Playing, Paused, GameOver)", () => CreateManager<Core.GameStateManager>("GameStateManager"));

            DrawCheckItem("GameFlowController", FindObjectOfType<Core.GameFlowController>() != null,
                "THE GLUE - connects all systems", () => CreateManager<Core.GameFlowController>("GameFlowController"));

            DrawCheckItem("GameBootstrapper", FindObjectOfType<Core.GameBootstrapper>() != null,
                "Auto-creates managers on Play", () => CreateManager<Core.GameBootstrapper>("GameBootstrapper"));

            DrawCheckItem("SceneLoader", FindObjectOfType<Core.SceneLoader>() != null,
                "Loading screens between scenes", () => CreateManager<Core.SceneLoader>("SceneLoader"));

            DrawCheckItem("AudioManager", FindObjectOfType<Audio.AudioManager>() != null,
                "Handles all game sounds", () => CreateManager<Audio.AudioManager>("AudioManager"));

            DrawCheckItem("DayNightCycle", FindObjectOfType<Systems.DayNightCycle>() != null,
                "Day/night transitions", () => CreateManager<Systems.DayNightCycle>("DayNightCycle"));

            DrawCheckItem("HorrorEventsSystem", FindObjectOfType<Systems.HorrorEventsSystem>() != null,
                "Scary events at night", () => CreateManager<Systems.HorrorEventsSystem>("HorrorEventsSystem"));

            DrawCheckItem("DialogueSystem", FindObjectOfType<Systems.DialogueSystem>() != null,
                "NPC conversations with auto camera", () => CreateManager<Systems.DialogueSystem>("DialogueSystem"));

            DrawCheckItem("FastTravelSystem", FindObjectOfType<Systems.FastTravelSystem>() != null,
                "Subway stations", () => CreateManager<Systems.FastTravelSystem>("FastTravelSystem"));
        }

        private void DrawCloneSpawnChecklist()
        {
            var spawnSystem = FindObjectOfType<Core.CloneSpawnSystem>();

            DrawCheckItem("CloneSpawnSystem", spawnSystem != null,
                "Spawns players from tubes", () => CreateManager<Core.CloneSpawnSystem>("CloneSpawnSystem"));

            if (spawnSystem != null)
            {
                DrawCheckItem("  Spawn Tubes", spawnSystem.spawnTubes != null && spawnSystem.spawnTubes.Length >= 4,
                    "4 tubes for 4 players");

                DrawCheckItem("  Player Prefab", spawnSystem.playerPrefab != null,
                    "Prefab to spawn for players");

                DrawCheckItem("  Bob Reference", spawnSystem.injuredBob != null,
                    "Reference to injured Bob");

                DrawCheckItem("  Intro Dialogue", spawnSystem.introDialogue != null && spawnSystem.introDialogue.Length > 0,
                    "Bob's intro speech");
            }
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

                DrawCheckItem("  DeathRespawnSystem", player.GetComponent<Player.DeathRespawnSystem>() != null,
                    "Death and respawn with home spots", () => player.gameObject.AddComponent<Player.DeathRespawnSystem>());

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

                // Multiplayer components
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Multiplayer (optional):", EditorStyles.miniLabel);

#if UNITY_NETCODE
                DrawCheckItem("  NetworkObject", player.GetComponent<Unity.Netcode.NetworkObject>() != null,
                    "Required for multiplayer");
                DrawCheckItem("  NetworkedPlayer", player.GetComponent<Networking.NetworkedPlayer>() != null,
                    "Syncs player across network");
#else
                EditorGUILayout.HelpBox("Install 'Netcode for GameObjects' for multiplayer components", MessageType.Info);
#endif
            }

            // Home spots
            var homeSpots = FindObjectsOfType<Player.HomeSpot>();
            DrawCheckItem("Home Spots", homeSpots.Length > 0,
                $"Places to set respawn ({homeSpots.Length} found)");
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
                "Weapon system (pistol, shotgun, flamethrower)");

            var gunSystem = FindObjectOfType<Combat.SimpleGunSystem>();
            if (gunSystem != null)
            {
                DrawCheckItem("  WeaponVisuals", FindObjectOfType<Combat.WeaponVisuals>() != null,
                    "FPS reload animations, flamethrower particles");
            }

            EditorGUILayout.HelpBox(
                "Weapons supported:\n" +
                "- Pistol: Simple reload (move off screen)\n" +
                "- Shotgun: Simple reload\n" +
                "- Flamethrower: Particle effects, no reload",
                MessageType.Info);
        }

        private void DrawPetsChecklist()
        {
            DrawCheckItem("PetCaptureSystem", FindObjectOfType<Battle.PetCaptureSystem>() != null,
                "Net capture system");

            var capture = FindObjectOfType<Battle.PetCaptureSystem>();
            if (capture != null)
            {
                DrawCheckItem("  Net Prefab", capture.netPrefab != null,
                    "Throwable net object");
            }

            DrawCheckItem("BattleCameraSystem", FindObjectOfType<Battle.BattleCameraSystem>() != null,
                "Pokemon-style battle camera", () => CreateManager<Battle.BattleCameraSystem>("BattleCameraSystem"));

            DrawCheckItem("BattleSystem", FindObjectOfType<Battle.BattleSystem>() != null,
                "Pet battle logic");

            // Check for pet animation controllers
            var petAnimControllers = FindObjectsOfType<Battle.PetAnimationController>();
            DrawCheckItem("Pet Prefabs", petAnimControllers.Length > 0,
                $"Pets with PetAnimationController ({petAnimControllers.Length} found)");

            EditorGUILayout.HelpBox(
                "Pet types: Rats, Dogs, Cats + Demonic versions\n" +
                "Each needs PetAnimationController for animations:\n" +
                "- Movement: Idle, Walk, Run\n" +
                "- Combat: Attack, Bite, Claw, Pounce\n" +
                "- Demonic: Fire Breath, Dark Pulse",
                MessageType.Info);
        }

        private void DrawItemsChecklist()
        {
            DrawCheckItem("ConsumableSystem", FindObjectOfType<Items.ConsumableSystem>() != null,
                "Inventory with thumbnails", () => CreateManager<Items.ConsumableSystem>("ConsumableSystem"));

            DrawCheckItem("CigarCraftingSystem", FindObjectOfType<Items.CigarCraftingSystem>() != null,
                "Lab table crafting", () => CreateManager<Items.CigarCraftingSystem>("CigarCraftingSystem"));

            var cigar = FindObjectOfType<Items.CigarCraftingSystem>();
            if (cigar != null)
            {
                DrawCheckItem("  Lab Table Price", cigar.labTablePrice > 0,
                    "Cost to buy lab table");
                DrawCheckItem("  Recipes", cigar.recipes != null && cigar.recipes.Count > 0,
                    "Cigar recipes with effects");
            }

            EditorGUILayout.HelpBox(
                "Consumables need:\n" +
                "- ConsumableData ScriptableObject\n" +
                "- Icon (thumbnail for inventory)\n" +
                "- Effects (heal, speed, etc.)\n\n" +
                "Cigars give temporary powers!",
                MessageType.Info);
        }

        private void DrawUIChecklist()
        {
            DrawCheckItem("Canvas", FindObjectOfType<Canvas>() != null,
                "UI container", () => CreateCanvas());

            DrawCheckItem("EventSystem", FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null,
                "UI input handling");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Main UI Systems:", EditorStyles.miniLabel);

            DrawCheckItem("MainMenuManager", FindObjectOfType<UI.MainMenuManager>() != null,
                "Main menu with Play, Settings, Quit", () => CreateManager<UI.MainMenuManager>("MainMenuManager"));

            DrawCheckItem("HUDManager", FindObjectOfType<UI.HUDManager>() != null,
                "Health, money, Bob status, hamburgers", () => CreateManager<UI.HUDManager>("HUDManager"));

            DrawCheckItem("PauseMenuManager", FindObjectOfType<UI.PauseMenuManager>() != null,
                "Pause menu with Resume, Settings, Quit", () => CreateManager<UI.PauseMenuManager>("PauseMenuManager"));

            DrawCheckItem("SettingsManager", FindObjectOfType<UI.SettingsManager>() != null,
                "Audio, graphics, controls settings", () => CreateManager<UI.SettingsManager>("SettingsManager"));

            DrawCheckItem("TutorialManager", FindObjectOfType<UI.TutorialManager>() != null,
                "Tutorial for new players", () => CreateManager<UI.TutorialManager>("TutorialManager"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Optional UI:", EditorStyles.miniLabel);

            DrawCheckItem("MinimapSystem", FindObjectOfType<UI.MinimapSystem>() != null,
                "Minimap display");

            EditorGUILayout.HelpBox(
                "UI Setup Tips:\n" +
                "- MainMenuManager: Create Canvas > Add panels and buttons > Wire to slots\n" +
                "- HUDManager: Health bars, money text, crosshair\n" +
                "- Use Window > Bob's Petroleum > Prefab Creator for pre-made UI",
                MessageType.Info);
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

            // Water system
            DrawCheckItem("WaterSystem", FindObjectOfType<Environment.WaterSystem>() != null,
                "Wavy water planes");

            var water = FindObjectOfType<Environment.WaterSystem>();
            if (water != null)
            {
                DrawCheckItem("  Water Shader", Shader.Find("BobsPetroleum/Water") != null,
                    "Custom water shader");
            }

            // Fast travel
            DrawCheckItem("FastTravelSystem", FindObjectOfType<Systems.FastTravelSystem>() != null,
                "Subway station travel");

            var fastTravel = FindObjectOfType<Systems.FastTravelSystem>();
            if (fastTravel != null)
            {
                DrawCheckItem("  Subway Stations", fastTravel.stations != null && fastTravel.stations.Count > 0,
                    $"Travel destinations ({fastTravel.stations?.Count ?? 0})");
                DrawCheckItem("  Unlock Item", !string.IsNullOrEmpty(fastTravel.unlockItemId),
                    "Pipe item to unlock travel");
            }

            EditorGUILayout.HelpBox(
                "World is a single open scene.\n" +
                "Add WaterSystem to any plane for wavy water.\n" +
                "Place SubwayStation prefabs for fast travel.",
                MessageType.Info);
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

        private void DrawNetworkingChecklist()
        {
            EditorGUILayout.LabelField("Client-Hosted Multiplayer (1-4 players):", EditorStyles.boldLabel);

            DrawCheckItem("NetworkGameManager", FindObjectOfType<Networking.NetworkGameManager>() != null,
                "Easy host/join system", () => CreateManager<Networking.NetworkGameManager>("NetworkGameManager"));

            var netMgr = FindObjectOfType<Networking.NetworkGameManager>();
            if (netMgr != null)
            {
                DrawCheckItem("  Player Prefab", netMgr.playerPrefab != null,
                    "Player prefab with NetworkObject");
                DrawCheckItem("  Spawn Points", netMgr.spawnPoints != null && netMgr.spawnPoints.Length >= 4,
                    "4 spawn points for 4 players");
            }

#if UNITY_NETCODE
            DrawCheckItem("NetworkManager (Unity)", FindObjectOfType<Unity.Netcode.NetworkManager>() != null,
                "Unity Netcode NetworkManager");
#else
            EditorGUILayout.HelpBox(
                "Netcode for GameObjects NOT INSTALLED!\n\n" +
                "To enable multiplayer:\n" +
                "1. Window > Package Manager\n" +
                "2. Search 'Netcode for GameObjects'\n" +
                "3. Click Install",
                MessageType.Warning);
#endif

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Cloud Saves (Supabase):", EditorStyles.boldLabel);

            DrawCheckItem("SupabaseSaveSystem", FindObjectOfType<Networking.SupabaseSaveSystem>() != null,
                "Cloud database saves", () => CreateManager<Networking.SupabaseSaveSystem>("SupabaseSaveSystem"));

            var saveSystem = FindObjectOfType<Networking.SupabaseSaveSystem>();
            if (saveSystem != null)
            {
                DrawCheckItem("  Supabase URL", !string.IsNullOrEmpty(saveSystem.supabaseUrl),
                    "Project URL from supabase.com");
                DrawCheckItem("  Supabase Key", !string.IsNullOrEmpty(saveSystem.supabaseKey),
                    "Anon/public API key");
            }

            EditorGUILayout.HelpBox(
                "Game Modes:\n" +
                "- Forever Mode: Persistent cloud saves\n" +
                "- 7 Night Runs: Leaderboard scoring\n\n" +
                "Get free Supabase project at: supabase.com\n" +
                "See SupabaseSaveSystem.cs for table SQL",
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
