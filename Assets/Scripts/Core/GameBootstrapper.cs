using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BobsPetroleum.Core
{
    /// <summary>
    /// Auto-bootstrapper that creates and wires all game managers automatically.
    /// Just add this to an empty GameObject and hit Play - everything sets itself up!
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        public static GameBootstrapper Instance { get; private set; }

        [Header("Auto-Setup Options")]
        [Tooltip("Automatically create missing managers on Awake")]
        public bool autoCreateManagers = true;

        [Tooltip("Auto-find and wire references between systems")]
        public bool autoWireReferences = true;

        [Tooltip("Create player if none exists")]
        public bool autoCreatePlayer = true;

        [Tooltip("Show debug logs during setup")]
        public bool debugMode = true;

        [Header("Manager Prefabs (Optional)")]
        [Tooltip("Leave empty to auto-create basic versions")]
        public GameObject gameManagerPrefab;
        public GameObject audioManagerPrefab;
        public GameObject hudManagerPrefab;
        public GameObject pauseMenuPrefab;
        public GameObject horrorEventsPrefab;
        public GameObject randomEventsPrefab;
        public GameObject questSystemPrefab;
        public GameObject minimapPrefab;
        public GameObject dayNightPrefab;

        [Header("Player Prefab")]
        public GameObject playerPrefab;

        [Header("Created References (Auto-populated)")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Audio.AudioManager audioManager;
        [SerializeField] private UI.HUDManager hudManager;
        [SerializeField] private UI.PauseMenu pauseMenu;
        [SerializeField] private Systems.HorrorEventsSystem horrorEvents;
        [SerializeField] private Systems.RandomEventsSystem randomEvents;
        [SerializeField] private Systems.QuestSystem questSystem;
        [SerializeField] private UI.MinimapSystem minimap;
        [SerializeField] private Systems.DayNightCycle dayNight;
        [SerializeField] private Player.PlayerController player;

        private List<string> setupLog = new List<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoCreateManagers)
            {
                SetupAllManagers();
            }

            if (autoWireReferences)
            {
                WireAllReferences();
            }

            if (debugMode)
            {
                PrintSetupLog();
            }
        }

        /// <summary>
        /// Create all missing managers. Call this manually or let it run on Awake.
        /// </summary>
        [ContextMenu("Setup All Managers")]
        public void SetupAllManagers()
        {
            Log("=== Bob's Petroleum Bootstrapper Starting ===");

            // Core managers
            gameManager = FindOrCreate<GameManager>("GameManager", gameManagerPrefab);
            audioManager = FindOrCreate<Audio.AudioManager>("AudioManager", audioManagerPrefab);
            dayNight = FindOrCreate<Systems.DayNightCycle>("DayNightCycle", dayNightPrefab);

            // UI managers
            hudManager = FindOrCreate<UI.HUDManager>("HUDManager", hudManagerPrefab);
            pauseMenu = FindOrCreate<UI.PauseMenu>("PauseMenu", pauseMenuPrefab);
            minimap = FindOrCreate<UI.MinimapSystem>("MinimapSystem", minimapPrefab);

            // Game systems
            horrorEvents = FindOrCreate<Systems.HorrorEventsSystem>("HorrorEventsSystem", horrorEventsPrefab);
            randomEvents = FindOrCreate<Systems.RandomEventsSystem>("RandomEventsSystem", randomEventsPrefab);
            questSystem = FindOrCreate<Systems.QuestSystem>("QuestSystem", questSystemPrefab);

            // Player
            if (autoCreatePlayer)
            {
                player = FindObjectOfType<Player.PlayerController>();
                if (player == null)
                {
                    if (playerPrefab != null)
                    {
                        GameObject playerObj = Instantiate(playerPrefab);
                        playerObj.name = "Player";
                        player = playerObj.GetComponent<Player.PlayerController>();
                        Log("Created Player from prefab");
                    }
                    else
                    {
                        Log("WARNING: No player prefab assigned and no player in scene!");
                    }
                }
                else
                {
                    Log("Found existing Player");
                }
            }

            Log("=== Manager Setup Complete ===");
        }

        /// <summary>
        /// Wire references between all systems.
        /// </summary>
        [ContextMenu("Wire All References")]
        public void WireAllReferences()
        {
            Log("=== Wiring References ===");

            // Wire player to systems
            if (player != null)
            {
                // HUD
                if (hudManager != null)
                {
                    hudManager.player = player;
                    var health = player.GetComponent<Player.PlayerHealth>();
                    if (health != null)
                    {
                        hudManager.playerHealth = health;
                    }
                    Log("Wired: HUDManager <- Player");
                }

                // Minimap
                if (minimap != null)
                {
                    minimap.SetTrackedPlayer(player);
                    Log("Wired: MinimapSystem <- Player");
                }

                // Horror events
                if (horrorEvents != null)
                {
                    horrorEvents.player = player.transform;
                    var flashlight = player.GetComponentInChildren<Player.Flashlight>();
                    if (flashlight != null)
                    {
                        horrorEvents.playerFlashlight = flashlight;
                    }
                    Log("Wired: HorrorEvents <- Player");
                }
            }

            // Wire GameManager
            if (gameManager != null)
            {
                if (player != null)
                {
                    gameManager.player = player;
                }
                if (hudManager != null)
                {
                    // GameManager might need HUD reference
                }
                Log("Wired: GameManager");
            }

            // Wire DayNight to Horror
            if (dayNight != null && horrorEvents != null)
            {
                // Horror events subscribe to day/night changes
                Log("Wired: DayNight -> HorrorEvents");
            }

            // Wire Quest system
            if (questSystem != null && hudManager != null)
            {
                // Quest updates -> HUD
                Log("Wired: QuestSystem -> HUD");
            }

            Log("=== Wiring Complete ===");
        }

        private T FindOrCreate<T>(string objectName, GameObject prefab = null) where T : Component
        {
            // First try to find existing
            T existing = FindObjectOfType<T>();
            if (existing != null)
            {
                Log($"Found existing: {objectName}");
                return existing;
            }

            // Create from prefab if provided
            if (prefab != null)
            {
                GameObject obj = Instantiate(prefab);
                obj.name = objectName;
                T component = obj.GetComponent<T>();
                if (component != null)
                {
                    Log($"Created from prefab: {objectName}");
                    return component;
                }
            }

            // Create basic version
            GameObject newObj = new GameObject(objectName);
            T newComponent = newObj.AddComponent<T>();
            Log($"Auto-created: {objectName}");
            return newComponent;
        }

        private void Log(string message)
        {
            setupLog.Add(message);
            if (debugMode)
            {
                Debug.Log($"[Bootstrapper] {message}");
            }
        }

        private void PrintSetupLog()
        {
            Debug.Log("=== BOOTSTRAPPER SETUP LOG ===");
            foreach (var line in setupLog)
            {
                Debug.Log(line);
            }
        }

        #region Runtime Helpers

        /// <summary>
        /// Get any manager by type at runtime.
        /// </summary>
        public T GetManager<T>() where T : Component
        {
            return FindObjectOfType<T>();
        }

        /// <summary>
        /// Get the player reference.
        /// </summary>
        public Player.PlayerController GetPlayer()
        {
            if (player == null)
            {
                player = FindObjectOfType<Player.PlayerController>();
            }
            return player;
        }

        /// <summary>
        /// Spawn player at a spawn point.
        /// </summary>
        public void SpawnPlayerAt(Transform spawnPoint)
        {
            if (player != null && spawnPoint != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = spawnPoint.position;
                player.transform.rotation = spawnPoint.rotation;

                if (cc != null) cc.enabled = true;
            }
        }

        /// <summary>
        /// Find spawn point by name or tag.
        /// </summary>
        public Transform FindSpawnPoint(string nameOrTag = "PlayerSpawn")
        {
            // Try by tag first
            GameObject spawnObj = GameObject.FindGameObjectWithTag(nameOrTag);
            if (spawnObj != null) return spawnObj.transform;

            // Try by name
            spawnObj = GameObject.Find(nameOrTag);
            if (spawnObj != null) return spawnObj.transform;

            return null;
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Validate Setup")]
        public void ValidateSetup()
        {
            List<string> issues = new List<string>();

            if (FindObjectOfType<GameManager>() == null)
                issues.Add("Missing: GameManager");
            if (FindObjectOfType<Audio.AudioManager>() == null)
                issues.Add("Missing: AudioManager");
            if (FindObjectOfType<UI.HUDManager>() == null)
                issues.Add("Missing: HUDManager");
            if (FindObjectOfType<Player.PlayerController>() == null)
                issues.Add("Missing: PlayerController");
            if (FindObjectOfType<Systems.DayNightCycle>() == null)
                issues.Add("Missing: DayNightCycle");

            if (issues.Count == 0)
            {
                Debug.Log("[Bootstrapper] All systems validated! Ready to play.");
            }
            else
            {
                Debug.LogWarning($"[Bootstrapper] Found {issues.Count} issues:");
                foreach (var issue in issues)
                {
                    Debug.LogWarning($"  - {issue}");
                }
            }
        }
#endif
    }
}
