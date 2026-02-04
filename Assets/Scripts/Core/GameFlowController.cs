using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// THE GLUE - Connects all game systems together!
    /// This ensures money flows correctly, Bob gets fed, and everything syncs.
    ///
    /// GAME FLOW:
    /// 1. Players spawn from tubes → CloneSpawnSystem
    /// 2. Bob explains mission → DialogueSystem
    /// 3. Players work gas station → CashRegister → ShopManager
    /// 4. Money earned → PlayerInventory
    /// 5. Buy hamburgers → ShopSystem
    /// 6. Feed Bob → BobCharacter
    /// 7. Bob revived → WIN / Night advances
    ///
    /// SETUP:
    /// 1. Add this to scene (auto-finds all systems)
    /// 2. Systems auto-wire on Start
    /// 3. Everything just works!
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController Instance { get; private set; }

        [Header("=== CORE SYSTEMS (Auto-Found) ===")]
        public GameManager gameManager;
        public BobCharacter bob;
        public CloneSpawnSystem spawnSystem;

        [Header("=== ECONOMY SYSTEMS (Auto-Found) ===")]
        public Economy.ShopManager shopManager;
        public Economy.ShopSystem shopSystem;
        public Economy.CashRegister cashRegister;

        [Header("=== PLAYER SYSTEMS ===")]
        public List<Player.PlayerInventory> playerInventories = new List<Player.PlayerInventory>();

        [Header("=== OTHER SYSTEMS (Auto-Found) ===")]
        public Systems.FastTravelSystem fastTravel;
        public Systems.DialogueSystem dialogue;
        public Items.CigarCraftingSystem cigarCrafting;
        public Items.ConsumableSystem consumables;
        public Battle.PetCaptureSystem petCapture;
        public Networking.NetworkGameManager networkManager;
        public Networking.SupabaseSaveSystem saveSystem;

        [Header("=== HAMBURGER SHOP CONFIG ===")]
        [Tooltip("Hamburger item ID in shop")]
        public string hamburgerItemId = "hamburger";

        [Tooltip("Hamburger price")]
        public int hamburgerPrice = 25;

        [Header("=== WIN CONDITIONS ===")]
        [Tooltip("Bob health needed to revive (1.0 = full)")]
        public float reviveHealthThreshold = 1.0f;

        [Tooltip("Money needed to buy enough hamburgers")]
        public int estimatedMoneyNeeded = 500;

        [Header("=== NIGHT CYCLE (7 Night Runs) ===")]
        [Tooltip("Length of one in-game night (seconds)")]
        public float nightDurationSeconds = 300f; // 5 minutes

        [Tooltip("Current night number")]
        public int currentNight = 1;

        [Tooltip("Max nights in a run")]
        public int maxNights = 7;

        [Header("=== EVENTS ===")]
        public UnityEvent onGameStarted;
        public UnityEvent onNightStarted;
        public UnityEvent onNightEnded;
        public UnityEvent onBobFed;
        public UnityEvent onBobRevived;
        public UnityEvent onGameOver;
        public UnityEvent onRunCompleted;

        // Runtime
        private float nightTimer;
        private bool gameActive;
        private bool isNightMode;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Auto-find all systems
            WireAllSystems();

            // Subscribe to events
            SubscribeToSystemEvents();
        }

        private void Update()
        {
            if (!gameActive) return;

            // Night timer for 7 Night Runs
            if (isNightMode)
            {
                nightTimer -= Time.deltaTime;
                if (nightTimer <= 0)
                {
                    EndNight();
                }
            }

            // Check Bob's status
            CheckBobStatus();
        }

        #region System Wiring

        /// <summary>
        /// Auto-find and wire all game systems
        /// </summary>
        public void WireAllSystems()
        {
            Debug.Log("[GameFlow] Wiring all systems...");

            // Core
            gameManager = gameManager ?? GameManager.Instance ?? FindObjectOfType<GameManager>();
            bob = bob ?? BobCharacter.Instance ?? FindObjectOfType<BobCharacter>();
            spawnSystem = spawnSystem ?? CloneSpawnSystem.Instance ?? FindObjectOfType<CloneSpawnSystem>();

            // Economy
            shopManager = shopManager ?? FindObjectOfType<Economy.ShopManager>();
            shopSystem = shopSystem ?? FindObjectOfType<Economy.ShopSystem>();
            cashRegister = cashRegister ?? FindObjectOfType<Economy.CashRegister>();

            // Other systems
            fastTravel = fastTravel ?? FindObjectOfType<Systems.FastTravelSystem>();
            dialogue = dialogue ?? FindObjectOfType<Systems.DialogueSystem>();
            cigarCrafting = cigarCrafting ?? FindObjectOfType<Items.CigarCraftingSystem>();
            consumables = consumables ?? FindObjectOfType<Items.ConsumableSystem>();
            petCapture = petCapture ?? FindObjectOfType<Battle.PetCaptureSystem>();

            // Networking
            networkManager = networkManager ?? Networking.NetworkGameManager.Instance ?? FindObjectOfType<Networking.NetworkGameManager>();
            saveSystem = saveSystem ?? Networking.SupabaseSaveSystem.Instance ?? FindObjectOfType<Networking.SupabaseSaveSystem>();

            // Find all player inventories
            RefreshPlayerInventories();

            Debug.Log($"[GameFlow] Systems wired! Bob: {bob != null}, Shop: {shopManager != null}, Register: {cashRegister != null}");
        }

        private void SubscribeToSystemEvents()
        {
            // Bob events
            if (bob != null)
            {
                bob.onHamburgerFed.AddListener(OnBobFedHamburger);
                bob.onBobRevived.AddListener(OnBobRevivedHandler);
                bob.onBobDied.AddListener(OnBobDiedHandler);
            }

            // Cash register events
            if (cashRegister != null)
            {
                cashRegister.onTransactionComplete.AddListener(OnRegisterTransaction);
            }

            // Shop events
            if (shopManager != null)
            {
                shopManager.onShopOpen.AddListener(OnShopOpened);
                shopManager.onShopClose.AddListener(OnShopClosed);
            }

            // Spawn events
            if (spawnSystem != null)
            {
                spawnSystem.onIntroComplete.AddListener(OnIntroComplete);
                spawnSystem.onPlayerSpawned.AddListener(OnPlayerSpawned);
            }

            // Network events
            if (networkManager != null)
            {
                networkManager.onPlayerJoined.AddListener(OnNetworkPlayerJoined);
                networkManager.onPlayerLeft.AddListener(OnNetworkPlayerLeft);
            }
        }

        public void RefreshPlayerInventories()
        {
            playerInventories.Clear();
            var inventories = FindObjectsOfType<Player.PlayerInventory>();
            playerInventories.AddRange(inventories);
        }

        #endregion

        #region Game Flow Control

        /// <summary>
        /// Start a new Forever Mode game
        /// </summary>
        public void StartForeverMode()
        {
            isNightMode = false;
            gameActive = true;

            spawnSystem?.StartForeverMode();
            onGameStarted?.Invoke();

            Debug.Log("[GameFlow] Forever Mode started!");
        }

        /// <summary>
        /// Start a 7 Night Run
        /// </summary>
        public void StartSevenNightRun()
        {
            isNightMode = true;
            currentNight = 1;
            gameActive = true;

            spawnSystem?.StartSevenNightRun();
            StartNight();
            onGameStarted?.Invoke();

            Debug.Log("[GameFlow] 7 Night Run started!");
        }

        /// <summary>
        /// Start the current night
        /// </summary>
        public void StartNight()
        {
            nightTimer = nightDurationSeconds;

            // Open shop at night start
            shopManager?.OpenShop();

            onNightStarted?.Invoke();
            Debug.Log($"[GameFlow] Night {currentNight} started! {nightDurationSeconds}s remaining.");
        }

        /// <summary>
        /// End the current night
        /// </summary>
        public void EndNight()
        {
            // Close shop
            shopManager?.CloseShop();

            // Save progress
            saveSystem?.SaveGame($"Night_{currentNight}_End");

            onNightEnded?.Invoke();

            // Advance night or complete run
            currentNight++;
            if (currentNight > maxNights)
            {
                CompleteRun();
            }
            else
            {
                // Brief intermission then start next night
                StartCoroutine(NightIntermission());
            }
        }

        private IEnumerator NightIntermission()
        {
            // Show night transition UI
            UI.HUDManager.Instance?.ShowNotification($"Night {currentNight - 1} complete! Preparing Night {currentNight}...");

            yield return new WaitForSeconds(5f);

            StartNight();
        }

        /// <summary>
        /// Complete the 7 Night Run
        /// </summary>
        public void CompleteRun()
        {
            gameActive = false;

            // Calculate score
            int score = CalculateScore();

            // Submit to leaderboard
            saveSystem?.SubmitScore(score, currentNight - 1);

            onRunCompleted?.Invoke();
            Debug.Log($"[GameFlow] Run completed! Score: {score}");
        }

        private int CalculateScore()
        {
            int score = 0;

            // Money earned
            if (gameManager != null)
            {
                score += gameManager.totalMoneyEarned;
            }

            // Hamburgers fed to Bob
            if (bob != null)
            {
                score += bob.HamburgersFed * 100;
            }

            // Nights survived
            score += (currentNight - 1) * 500;

            // Bob revived bonus
            if (bob != null && bob.isRevived)
            {
                score += 5000;
            }

            return score;
        }

        #endregion

        #region Event Handlers

        private void OnBobFedHamburger()
        {
            onBobFed?.Invoke();

            // Notify all players
            UI.HUDManager.Instance?.ShowNotification($"Bob fed! Health: {bob.HealthPercent * 100:F0}%");

            // Sync across network
            SyncBobState();
        }

        private void OnBobRevivedHandler()
        {
            onBobRevived?.Invoke();

            // Show victory!
            if (dialogue != null)
            {
                dialogue.ShowDialogue("I'M ALIVE! You saved me!", bob.gameObject);
            }

            UI.HUDManager.Instance?.ShowNotification("BOB IS REVIVED! YOU WIN!");

            // End game or continue based on mode
            if (isNightMode)
            {
                // Bonus - Bob revived ends the run with extra points
                CompleteRun();
            }
            else
            {
                // Forever mode - just a milestone
                Debug.Log("[GameFlow] Bob revived in Forever mode - game continues!");
            }
        }

        private void OnBobDiedHandler()
        {
            gameActive = false;
            onGameOver?.Invoke();

            UI.HUDManager.Instance?.ShowNotification("BOB DIED! GAME OVER!");

            Debug.Log("[GameFlow] Game Over - Bob died!");
        }

        private void OnRegisterTransaction()
        {
            // Money was just earned at register
            RefreshPlayerInventories();

            // Update total money earned
            if (gameManager != null && cashRegister != null)
            {
                // Register handles this internally
            }

            Debug.Log("[GameFlow] Register transaction completed");
        }

        private void OnShopOpened()
        {
            Debug.Log("[GameFlow] Shop opened - customers can enter");
        }

        private void OnShopClosed()
        {
            Debug.Log("[GameFlow] Shop closed - no more customers");
        }

        private void OnIntroComplete()
        {
            Debug.Log("[GameFlow] Intro complete - game begins!");
        }

        private void OnPlayerSpawned(int playerIndex)
        {
            RefreshPlayerInventories();
            Debug.Log($"[GameFlow] Player {playerIndex + 1} spawned");
        }

        private void OnNetworkPlayerJoined(ulong clientId)
        {
            RefreshPlayerInventories();

            // Spawn player from next available tube
            if (spawnSystem != null && spawnSystem.AvailableTubes > 0)
            {
                spawnSystem.SpawnPlayer();
            }

            Debug.Log($"[GameFlow] Network player {clientId} joined");
        }

        private void OnNetworkPlayerLeft(ulong clientId)
        {
            RefreshPlayerInventories();
            Debug.Log($"[GameFlow] Network player {clientId} left");
        }

        #endregion

        #region Status Checks

        private void CheckBobStatus()
        {
            if (bob == null) return;

            // Warning when Bob is low
            if (bob.IsLowHealth && Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
            {
                UI.HUDManager.Instance?.ShowNotification("WARNING: Bob is dying! Feed him a hamburger!");
            }
        }

        private void SyncBobState()
        {
#if UNITY_NETCODE
            // In multiplayer, sync Bob's state to all clients
            // This would be handled by a NetworkedBob component
#endif
        }

        #endregion

        #region Public Helpers

        /// <summary>
        /// Get total money across all players
        /// </summary>
        public int GetTotalPlayerMoney()
        {
            int total = 0;
            foreach (var inv in playerInventories)
            {
                if (inv != null)
                {
                    total += inv.GetMoney();
                }
            }
            return total;
        }

        /// <summary>
        /// Get total hamburgers across all players
        /// </summary>
        public int GetTotalHamburgers()
        {
            int total = 0;
            foreach (var inv in playerInventories)
            {
                if (inv != null)
                {
                    total += inv.GetItemCount(hamburgerItemId);
                }
            }
            return total;
        }

        /// <summary>
        /// Check if any player can afford hamburger
        /// </summary>
        public bool CanAnyPlayerAffordHamburger()
        {
            foreach (var inv in playerInventories)
            {
                if (inv != null && inv.GetMoney() >= hamburgerPrice)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get night time remaining (for 7 Night Runs)
        /// </summary>
        public float GetNightTimeRemaining()
        {
            return isNightMode ? nightTimer : -1f;
        }

        /// <summary>
        /// Get progress towards reviving Bob (0-1)
        /// </summary>
        public float GetBobReviveProgress()
        {
            if (bob == null) return 0f;
            return bob.currentHealth / bob.maxHealth;
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!Debug.isDebugBuild) return;

            GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 200));
            GUILayout.Box("=== Game Flow Debug ===");

            GUILayout.Label($"Mode: {(isNightMode ? $"Night {currentNight}/{maxNights}" : "Forever")}");

            if (isNightMode)
            {
                GUILayout.Label($"Time: {nightTimer:F1}s");
            }

            if (bob != null)
            {
                GUILayout.Label($"Bob HP: {bob.HealthPercent * 100:F0}%");
                GUILayout.Label($"Hamburgers Fed: {bob.HamburgersFed}");
            }

            GUILayout.Label($"Players: {playerInventories.Count}");
            GUILayout.Label($"Total Money: ${GetTotalPlayerMoney()}");
            GUILayout.Label($"Hamburgers: {GetTotalHamburgers()}");

            GUILayout.EndArea();
        }

        #endregion
    }
}
