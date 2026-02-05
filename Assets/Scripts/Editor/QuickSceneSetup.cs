#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using System.Collections.Generic;

namespace BobsPetroleum.Editor
{
    /// <summary>
    /// QUICK SCENE SETUP - One click and you're playing!
    /// Creates a complete gas station scene with all systems.
    ///
    /// Window > Bob's Petroleum > Quick Scene Setup
    ///
    /// Creates:
    /// - Terrain with gas station layout
    /// - All managers
    /// - Player spawn
    /// - Bob
    /// - Gas pumps
    /// - Cash register
    /// - Shelves
    /// - Customer spawner
    /// - UI canvases
    /// </summary>
    public class QuickSceneSetup : EditorWindow
    {
        private enum SceneTemplate
        {
            BasicGasStation,
            FullGasStation,
            TestScene
        }

        private SceneTemplate selectedTemplate = SceneTemplate.BasicGasStation;
        private bool createTerrain = true;
        private bool createLighting = true;
        private bool createNavMesh = true;
        private bool createUI = true;
        private bool createDebugTools = true;
        private Vector2 scrollPos;

        [MenuItem("Window/Bob's Petroleum/Quick Scene Setup")]
        public static void ShowWindow()
        {
            var window = GetWindow<QuickSceneSetup>("Quick Scene Setup");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("BOB'S PETROLEUM - QUICK SCENE SETUP", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create a complete playable scene with ONE CLICK!\n\n" +
                "This will set up everything you need to start testing.",
                MessageType.Info);

            EditorGUILayout.Space(20);

            // Template selection
            EditorGUILayout.LabelField("Scene Template:", EditorStyles.boldLabel);
            selectedTemplate = (SceneTemplate)EditorGUILayout.EnumPopup(selectedTemplate);

            EditorGUILayout.Space(10);

            // Options
            EditorGUILayout.LabelField("Options:", EditorStyles.boldLabel);
            createTerrain = EditorGUILayout.Toggle("Create Ground Plane", createTerrain);
            createLighting = EditorGUILayout.Toggle("Setup Lighting", createLighting);
            createNavMesh = EditorGUILayout.Toggle("Bake NavMesh", createNavMesh);
            createUI = EditorGUILayout.Toggle("Create UI Canvases", createUI);
            createDebugTools = EditorGUILayout.Toggle("Add Debug Console", createDebugTools);

            EditorGUILayout.Space(20);

            // Template descriptions
            EditorGUILayout.LabelField("Template Info:", EditorStyles.boldLabel);
            switch (selectedTemplate)
            {
                case SceneTemplate.BasicGasStation:
                    EditorGUILayout.HelpBox(
                        "BASIC GAS STATION\n\n" +
                        "- 2 Gas pumps\n" +
                        "- Small store with register\n" +
                        "- Bob\n" +
                        "- Player spawn\n" +
                        "- Basic customer spawner\n\n" +
                        "Perfect for quick testing!",
                        MessageType.None);
                    break;

                case SceneTemplate.FullGasStation:
                    EditorGUILayout.HelpBox(
                        "FULL GAS STATION\n\n" +
                        "- 4 Gas pumps\n" +
                        "- Large store with shelves\n" +
                        "- Multiple spawn tubes\n" +
                        "- Bob's area\n" +
                        "- Horror spawn points\n" +
                        "- Full customer system\n\n" +
                        "Complete game experience!",
                        MessageType.None);
                    break;

                case SceneTemplate.TestScene:
                    EditorGUILayout.HelpBox(
                        "TEST SCENE\n\n" +
                        "- Flat plane\n" +
                        "- All managers\n" +
                        "- Player only\n" +
                        "- Debug tools\n\n" +
                        "For testing specific systems.",
                        MessageType.None);
                    break;
            }

            EditorGUILayout.Space(20);

            // Warning
            EditorGUILayout.HelpBox(
                "WARNING: This will create a new scene. Save your current work first!",
                MessageType.Warning);

            EditorGUILayout.Space(10);

            // Create button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("CREATE SCENE", GUILayout.Height(50)))
            {
                if (EditorUtility.DisplayDialog("Create Scene",
                    $"Create a new {selectedTemplate} scene?\n\nThis will create a new scene. Make sure you've saved your current work.",
                    "Create", "Cancel"))
                {
                    CreateScene();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(20);

            // Quick actions for existing scenes
            EditorGUILayout.LabelField("Quick Actions (Current Scene):", EditorStyles.boldLabel);

            if (GUILayout.Button("Add All Managers"))
            {
                AddAllManagers();
            }

            if (GUILayout.Button("Add Player"))
            {
                AddPlayer();
            }

            if (GUILayout.Button("Add Bob"))
            {
                AddBob();
            }

            if (GUILayout.Button("Add Debug Console"))
            {
                AddDebugConsole();
            }

            EditorGUILayout.EndScrollView();
        }

        #region Scene Creation

        private void CreateScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Create base structure
            CreateFolderObjects();

            // Create based on template
            switch (selectedTemplate)
            {
                case SceneTemplate.BasicGasStation:
                    CreateBasicGasStation();
                    break;
                case SceneTemplate.FullGasStation:
                    CreateFullGasStation();
                    break;
                case SceneTemplate.TestScene:
                    CreateTestScene();
                    break;
            }

            // Common elements
            if (createLighting) SetupLighting();
            if (createUI) CreateUICanvases();
            if (createDebugTools) AddDebugConsole();
            if (createNavMesh) BakeNavMesh();

            // Save scene
            string sceneName = $"BobsPetroleum_{selectedTemplate}";
            string path = EditorUtility.SaveFilePanelInProject("Save Scene", sceneName, "unity", "Save the new scene");

            if (!string.IsNullOrEmpty(path))
            {
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log($"[QuickSetup] Scene created and saved to: {path}");
            }

            EditorUtility.DisplayDialog("Scene Created!",
                "Your new scene has been created!\n\n" +
                "Next steps:\n" +
                "1. Replace placeholder meshes with your models\n" +
                "2. Add your materials\n" +
                "3. Press Play to test!",
                "Got it!");
        }

        private void CreateFolderObjects()
        {
            new GameObject("---MANAGERS---");
            new GameObject("---ENVIRONMENT---");
            new GameObject("---GAMEPLAY---");
            new GameObject("---UI---");
        }

        private void CreateBasicGasStation()
        {
            var envParent = GameObject.Find("---ENVIRONMENT---").transform;
            var gameplayParent = GameObject.Find("---GAMEPLAY---").transform;

            // Ground
            if (createTerrain)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = new Vector3(10, 1, 10);
                ground.transform.SetParent(envParent);
                ground.tag = "Ground";
            }

            // Managers
            AddAllManagers();

            // Player
            AddPlayer();

            // Bob
            var bob = AddBob();
            bob.transform.position = new Vector3(5, 0, 5);

            // Gas Pumps
            CreateGasPump(new Vector3(-5, 0, 0), 1, gameplayParent);
            CreateGasPump(new Vector3(-5, 0, 4), 2, gameplayParent);

            // Store building placeholder
            var store = GameObject.CreatePrimitive(PrimitiveType.Cube);
            store.name = "StoreBuilding";
            store.transform.position = new Vector3(10, 2, 0);
            store.transform.localScale = new Vector3(8, 4, 10);
            store.transform.SetParent(envParent);

            // Cash Register
            var register = CreateCashRegister(new Vector3(8, 0, 2), gameplayParent);

            // Customer Spawner
            CreateCustomerSpawner(gameplayParent);

            // Spawn tube
            CreateSpawnTube(new Vector3(0, 0, -5), gameplayParent);
        }

        private void CreateFullGasStation()
        {
            // Start with basic, then add more
            CreateBasicGasStation();

            var envParent = GameObject.Find("---ENVIRONMENT---").transform;
            var gameplayParent = GameObject.Find("---GAMEPLAY---").transform;

            // More pumps
            CreateGasPump(new Vector3(-5, 0, 8), 3, gameplayParent);
            CreateGasPump(new Vector3(-5, 0, 12), 4, gameplayParent);

            // More spawn tubes
            CreateSpawnTube(new Vector3(2, 0, -5), gameplayParent);
            CreateSpawnTube(new Vector3(4, 0, -5), gameplayParent);
            CreateSpawnTube(new Vector3(6, 0, -5), gameplayParent);

            // Shelves
            CreateShelf(new Vector3(12, 0, -2), gameplayParent);
            CreateShelf(new Vector3(12, 0, 0), gameplayParent);
            CreateShelf(new Vector3(12, 0, 2), gameplayParent);

            // Horror spawn points
            CreateHorrorSpawnPoints(envParent);
        }

        private void CreateTestScene()
        {
            var envParent = GameObject.Find("---ENVIRONMENT---").transform;

            // Simple ground
            if (createTerrain)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = new Vector3(5, 1, 5);
                ground.transform.SetParent(envParent);
                ground.tag = "Ground";
            }

            // Managers
            AddAllManagers();

            // Player only
            AddPlayer();
        }

        #endregion

        #region Object Creation

        private void AddAllManagers()
        {
            var parent = GameObject.Find("---MANAGERS---")?.transform;
            if (parent == null)
            {
                parent = new GameObject("---MANAGERS---").transform;
            }

            CreateManager<Core.GameManager>("GameManager", parent);
            CreateManager<Core.GameStateManager>("GameStateManager", parent);
            CreateManager<Core.GameFlowController>("GameFlowController", parent);
            CreateManager<Core.SceneLoader>("SceneLoader", parent);
            CreateManager<Audio.AudioManager>("AudioManager", parent);
            CreateManager<Systems.DayNightCycle>("DayNightCycle", parent);
            CreateManager<Systems.DialogueSystem>("DialogueSystem", parent);
            CreateManager<Systems.HorrorEventsSystem>("HorrorEventsSystem", parent);

            Debug.Log("[QuickSetup] All managers created!");
        }

        private void CreateManager<T>(string name, Transform parent) where T : Component
        {
            if (FindObjectOfType<T>() != null) return;

            var obj = new GameObject(name);
            obj.AddComponent<T>();
            obj.transform.SetParent(parent);
        }

        private GameObject AddPlayer()
        {
            if (FindObjectOfType<Player.PlayerController>() != null)
            {
                return FindObjectOfType<Player.PlayerController>().gameObject;
            }

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

            // Camera
            var camHolder = new GameObject("CameraHolder");
            camHolder.transform.SetParent(player.transform);
            camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            var cam = camHolder.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camHolder.AddComponent<AudioListener>();

            Debug.Log("[QuickSetup] Player created!");
            return player;
        }

        private GameObject AddBob()
        {
            if (FindObjectOfType<Core.BobCharacter>() != null)
            {
                return FindObjectOfType<Core.BobCharacter>().gameObject;
            }

            var parent = GameObject.Find("---GAMEPLAY---")?.transform;

            var bob = new GameObject("Bob");
            if (parent != null) bob.transform.SetParent(parent);

            // Placeholder mesh
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh.name = "BobMesh";
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

            Debug.Log("[QuickSetup] Bob created!");
            return bob;
        }

        private void AddDebugConsole()
        {
            if (FindObjectOfType<Utilities.DebugConsole>() != null) return;

            var console = new GameObject("DebugConsole");
            console.AddComponent<Utilities.DebugConsole>();

            Debug.Log("[QuickSetup] Debug Console added!");
        }

        private GameObject CreateGasPump(Vector3 position, int number, Transform parent)
        {
            var pump = new GameObject($"GasPump_{number}");
            pump.transform.position = position;
            if (parent != null) pump.transform.SetParent(parent);

            // Placeholder mesh
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "PumpMesh";
            mesh.transform.SetParent(pump.transform);
            mesh.transform.localScale = new Vector3(0.8f, 1.5f, 0.5f);
            mesh.transform.localPosition = new Vector3(0, 0.75f, 0);

            var pumpComp = pump.AddComponent<Economy.GasPump>();
            pumpComp.pumpNumber = number;

            // Nozzle positions
            var nozzleRest = new GameObject("NozzleRestPosition");
            nozzleRest.transform.SetParent(pump.transform);
            nozzleRest.transform.localPosition = new Vector3(0.5f, 1, 0);
            pumpComp.nozzleRestPosition = nozzleRest.transform;

            var nozzlePump = new GameObject("NozzlePumpPosition");
            nozzlePump.transform.SetParent(pump.transform);
            nozzlePump.transform.localPosition = new Vector3(1.5f, 0.5f, 0);
            pumpComp.nozzlePumpPosition = nozzlePump.transform;

            return pump;
        }

        private GameObject CreateCashRegister(Vector3 position, Transform parent)
        {
            var register = new GameObject("CashRegister");
            register.transform.position = position;
            if (parent != null) register.transform.SetParent(parent);

            // Placeholder mesh
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "RegisterMesh";
            mesh.transform.SetParent(register.transform);
            mesh.transform.localScale = new Vector3(0.6f, 0.4f, 0.4f);
            mesh.transform.localPosition = new Vector3(0, 0.2f, 0);
            DestroyImmediate(mesh.GetComponent<Collider>());

            register.AddComponent<Economy.CashRegister>();
            var col = register.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2, 2, 2);

            // Player position
            var playerPos = new GameObject("PlayerPosition");
            playerPos.transform.SetParent(register.transform);
            playerPos.transform.localPosition = new Vector3(0, 0, -1.5f);

            return register;
        }

        private void CreateCustomerSpawner(Transform parent)
        {
            var spawner = new GameObject("CustomerSpawner");
            if (parent != null) spawner.transform.SetParent(parent);

            var comp = spawner.AddComponent<AI.CustomerSpawner>();

            // Create waypoints
            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = new Vector3(-20, 0, 0);
            spawnPoint.transform.SetParent(spawner.transform);
            comp.spawnPoint = spawnPoint.transform;

            var exitPoint = new GameObject("ExitPoint");
            exitPoint.transform.position = new Vector3(-20, 0, 20);
            exitPoint.transform.SetParent(spawner.transform);
            comp.exitPoint = exitPoint.transform;

            var storeEntrance = new GameObject("StoreEntrance");
            storeEntrance.transform.position = new Vector3(6, 0, 0);
            storeEntrance.transform.SetParent(spawner.transform);
            comp.storeEntrance = storeEntrance.transform;

            var registerWaypoint = new GameObject("RegisterWaypoint");
            registerWaypoint.transform.position = new Vector3(8, 0, 1);
            registerWaypoint.transform.SetParent(spawner.transform);
            comp.registerWaypoint = registerWaypoint.transform;
        }

        private void CreateSpawnTube(Vector3 position, Transform parent)
        {
            var tube = new GameObject("SpawnTube");
            tube.transform.position = position;
            if (parent != null) tube.transform.SetParent(parent);

            // Placeholder mesh
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mesh.name = "TubeMesh";
            mesh.transform.SetParent(tube.transform);
            mesh.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            mesh.transform.localPosition = new Vector3(0, 2f, 0);

            // Spawn point
            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(tube.transform);
            spawnPoint.transform.localPosition = new Vector3(0, 0, 1.5f);
        }

        private void CreateShelf(Vector3 position, Transform parent)
        {
            var shelf = new GameObject("ItemShelf");
            shelf.transform.position = position;
            if (parent != null) shelf.transform.SetParent(parent);

            // Placeholder mesh
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "ShelfMesh";
            mesh.transform.SetParent(shelf.transform);
            mesh.transform.localScale = new Vector3(2f, 1.5f, 0.5f);
            mesh.transform.localPosition = new Vector3(0, 0.75f, 0);
        }

        private void CreateHorrorSpawnPoints(Transform parent)
        {
            string[] names = { "Horror_Behind_Store", "Horror_Pump_Area", "Horror_Road", "Horror_Dark_Corner" };
            Vector3[] positions = {
                new Vector3(15, 0, 0),
                new Vector3(-10, 0, 5),
                new Vector3(-15, 0, -10),
                new Vector3(20, 0, -5)
            };

            for (int i = 0; i < names.Length; i++)
            {
                var point = new GameObject(names[i]);
                point.transform.position = positions[i];
                point.transform.SetParent(parent);
            }
        }

        #endregion

        #region Setup Helpers

        private void SetupLighting()
        {
            // Directional light
            var light = new GameObject("Directional Light");
            var lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.color = new Color(1f, 0.95f, 0.85f);
            lightComp.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Ambient
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.6f, 0.7f);
            RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        }

        private void CreateUICanvases()
        {
            var parent = GameObject.Find("---UI---")?.transform;
            if (parent == null) parent = new GameObject("---UI---").transform;

            // HUD Canvas
            CreateCanvas("HUD_Canvas", parent).AddComponent<UI.HUDManager>();

            // Create EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                eventSystem.transform.SetParent(parent);
            }
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

        private void BakeNavMesh()
        {
            // Add NavMeshSurface if available
            var ground = GameObject.FindGameObjectWithTag("Ground");
            if (ground != null)
            {
                // Note: NavMeshSurface is from AI Navigation package
                // If not available, user needs to bake manually
                Debug.Log("[QuickSetup] NavMesh: Please bake manually via Window > AI > Navigation");
            }
        }

        #endregion
    }
}
#endif
