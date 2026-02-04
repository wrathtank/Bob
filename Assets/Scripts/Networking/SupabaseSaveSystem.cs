using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BobsPetroleum.Networking
{
    /// <summary>
    /// Cloud save system using Supabase for persistent multiplayer saves.
    /// Supports both "Forever Mode" (persistent) and "7 Night Runs" (timed).
    ///
    /// SETUP:
    /// 1. Create free Supabase project at supabase.com
    /// 2. Create tables (SQL provided below)
    /// 3. Copy your project URL and anon key
    /// 4. Paste into inspector
    /// 5. Done!
    ///
    /// SUPABASE TABLE SQL:
    /// ```sql
    /// -- Players table
    /// CREATE TABLE players (
    ///     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ///     username TEXT UNIQUE NOT NULL,
    ///     created_at TIMESTAMP DEFAULT NOW(),
    ///     last_login TIMESTAMP DEFAULT NOW()
    /// );
    ///
    /// -- Game saves table (for Forever mode)
    /// CREATE TABLE game_saves (
    ///     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ///     player_id UUID REFERENCES players(id),
    ///     save_name TEXT NOT NULL,
    ///     game_mode TEXT DEFAULT 'forever',
    ///     save_data JSONB NOT NULL,
    ///     created_at TIMESTAMP DEFAULT NOW(),
    ///     updated_at TIMESTAMP DEFAULT NOW()
    /// );
    ///
    /// -- 7 Night Run saves (temporary)
    /// CREATE TABLE night_runs (
    ///     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ///     player_id UUID REFERENCES players(id),
    ///     current_night INT DEFAULT 1,
    ///     run_data JSONB NOT NULL,
    ///     started_at TIMESTAMP DEFAULT NOW(),
    ///     completed BOOLEAN DEFAULT FALSE
    /// );
    ///
    /// -- Leaderboard for runs
    /// CREATE TABLE leaderboard (
    ///     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ///     player_id UUID REFERENCES players(id),
    ///     username TEXT,
    ///     score INT NOT NULL,
    ///     nights_survived INT DEFAULT 0,
    ///     game_mode TEXT,
    ///     created_at TIMESTAMP DEFAULT NOW()
    /// );
    /// ```
    /// </summary>
    public class SupabaseSaveSystem : MonoBehaviour
    {
        public static SupabaseSaveSystem Instance { get; private set; }

        [Header("=== SUPABASE CONFIG ===")]
        [Tooltip("Your Supabase project URL (e.g., https://xyz.supabase.co)")]
        public string supabaseUrl = "";

        [Tooltip("Your Supabase anon/public key")]
        public string supabaseKey = "";

        [Header("=== PLAYER INFO ===")]
        [Tooltip("Current player username")]
        public string playerUsername = "";

        [Tooltip("Current player ID (from Supabase)")]
        public string playerId = "";

        [Header("=== GAME MODE ===")]
        [Tooltip("Current game mode")]
        public GameMode currentMode = GameMode.Forever;

        [Tooltip("Current night (for 7 Night Runs)")]
        public int currentNight = 1;

        [Tooltip("Max nights for runs")]
        public int maxNights = 7;

        [Header("=== AUTO SAVE ===")]
        [Tooltip("Auto-save interval in seconds (0 = disabled)")]
        public float autoSaveInterval = 60f;

        [Tooltip("Save on scene change")]
        public bool saveOnSceneChange = true;

        [Header("=== OFFLINE SUPPORT ===")]
        [Tooltip("Enable offline mode when no connection")]
        public bool offlineFallback = true;

        [Tooltip("Local save key for offline")]
        public string localSaveKey = "BobsPetroleum_LocalSave";

        [Header("=== EVENTS ===")]
        public UnityEvent<bool> onSaveComplete;
        public UnityEvent<bool> onLoadComplete;
        public UnityEvent<string> onError;
        public UnityEvent onPlayerLoggedIn;
        public UnityEvent<List<LeaderboardEntry>> onLeaderboardLoaded;

        // Runtime state
        public bool IsLoggedIn { get; private set; }
        public bool IsSaving { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsOnline { get; private set; }

        private float autoSaveTimer;
        private GameSaveData currentSaveData;
        private string currentSaveId;

        public enum GameMode
        {
            Forever,        // Persistent world
            SevenNightRun   // 7 night survival
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Check if configured
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                Debug.LogWarning("[Supabase] Not configured! Set URL and Key in inspector.");
                Debug.LogWarning("[Supabase] Get free project at: https://supabase.com");
            }

            currentSaveData = new GameSaveData();
        }

        private void Start()
        {
            // Check connection
            StartCoroutine(CheckConnection());

            // Load username from PlayerPrefs
            playerUsername = PlayerPrefs.GetString("BobsPetroleum_Username", "");
            playerId = PlayerPrefs.GetString("BobsPetroleum_PlayerId", "");

            if (!string.IsNullOrEmpty(playerId))
            {
                IsLoggedIn = true;
            }
        }

        private void Update()
        {
            // Auto-save
            if (autoSaveInterval > 0 && IsLoggedIn && !IsSaving)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= autoSaveInterval)
                {
                    autoSaveTimer = 0f;
                    SaveGame();
                }
            }
        }

        #region Public API

        /// <summary>
        /// Login or create player
        /// </summary>
        public void Login(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                onError?.Invoke("Username cannot be empty!");
                return;
            }

            playerUsername = username;
            StartCoroutine(LoginCoroutine(username));
        }

        /// <summary>
        /// Save current game state
        /// </summary>
        public void SaveGame(string saveName = "AutoSave")
        {
            if (!IsLoggedIn)
            {
                Debug.LogWarning("[Supabase] Not logged in! Using local save.");
                SaveLocally();
                return;
            }

            CollectSaveData();
            StartCoroutine(SaveGameCoroutine(saveName));
        }

        /// <summary>
        /// Load game from cloud
        /// </summary>
        public void LoadGame(string saveId = null)
        {
            if (!IsLoggedIn)
            {
                Debug.LogWarning("[Supabase] Not logged in! Loading local save.");
                LoadLocally();
                return;
            }

            StartCoroutine(LoadGameCoroutine(saveId));
        }

        /// <summary>
        /// Get list of saves for current player
        /// </summary>
        public void GetSavesList(Action<List<SaveInfo>> callback)
        {
            StartCoroutine(GetSavesCoroutine(callback));
        }

        /// <summary>
        /// Delete a save
        /// </summary>
        public void DeleteSave(string saveId)
        {
            StartCoroutine(DeleteSaveCoroutine(saveId));
        }

        /// <summary>
        /// Start a new 7 Night Run
        /// </summary>
        public void StartNightRun()
        {
            currentMode = GameMode.SevenNightRun;
            currentNight = 1;
            currentSaveData = new GameSaveData();
            currentSaveData.gameMode = "7night";
            currentSaveData.currentNight = 1;

            StartCoroutine(CreateNightRunCoroutine());
        }

        /// <summary>
        /// Advance to next night
        /// </summary>
        public void AdvanceNight()
        {
            currentNight++;
            currentSaveData.currentNight = currentNight;

            if (currentNight > maxNights)
            {
                CompleteNightRun();
            }
            else
            {
                SaveGame($"Night_{currentNight}");
            }
        }

        /// <summary>
        /// Complete the 7 night run
        /// </summary>
        public void CompleteNightRun()
        {
            StartCoroutine(CompleteNightRunCoroutine());
        }

        /// <summary>
        /// Submit score to leaderboard
        /// </summary>
        public void SubmitScore(int score, int nightsSurvived)
        {
            StartCoroutine(SubmitScoreCoroutine(score, nightsSurvived));
        }

        /// <summary>
        /// Get leaderboard
        /// </summary>
        public void GetLeaderboard(int limit = 100)
        {
            StartCoroutine(GetLeaderboardCoroutine(limit));
        }

        #endregion

        #region Save Data Collection

        private void CollectSaveData()
        {
            // Collect all game state into save data
            currentSaveData.timestamp = DateTime.UtcNow.ToString("o");
            currentSaveData.gameMode = currentMode == GameMode.Forever ? "forever" : "7night";
            currentSaveData.currentNight = currentNight;

            // Player data
            var playerInventory = FindObjectOfType<Player.PlayerInventory>();
            if (playerInventory != null)
            {
                currentSaveData.money = playerInventory.GetMoney();
                currentSaveData.inventoryItems = playerInventory.GetAllItemIds();
            }

            // Bob's health
            var bob = Core.BobCharacter.Instance;
            if (bob != null)
            {
                currentSaveData.bobHealth = bob.currentHealth;
                currentSaveData.hamburgersFed = bob.HamburgersFed;
                currentSaveData.isBobRevived = bob.isRevived;
            }

            // Game manager stats
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                currentSaveData.totalMoneyEarned = gm.totalMoneyEarned;
                currentSaveData.currentDay = gm.currentDay;
            }

            // Shop state
            var shop = FindObjectOfType<Economy.ShopManager>();
            if (shop != null)
            {
                currentSaveData.isShopOpen = shop.isOpen;
            }

            // Fast travel unlocks
            var fastTravel = FindObjectOfType<Systems.FastTravelSystem>();
            if (fastTravel != null)
            {
                currentSaveData.fastTravelUnlocked = fastTravel.isUnlocked;
                currentSaveData.discoveredStations = fastTravel.GetDiscoveredStationIds();
            }

            // Cigar lab
            var cigarSystem = FindObjectOfType<Items.CigarCraftingSystem>();
            if (cigarSystem != null)
            {
                currentSaveData.ownsLabTable = cigarSystem.ownsLabTable;
            }

            Debug.Log($"[Supabase] Collected save data: ${currentSaveData.money}, Bob HP: {currentSaveData.bobHealth}");
        }

        private void ApplySaveData()
        {
            if (currentSaveData == null) return;

            // Apply to player
            var playerInventory = FindObjectOfType<Player.PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.SetMoney(currentSaveData.money);
                // Restore inventory items...
            }

            // Apply to Bob
            var bob = Core.BobCharacter.Instance;
            if (bob != null)
            {
                bob.currentHealth = currentSaveData.bobHealth;
                bob.isRevived = currentSaveData.isBobRevived;
            }

            // Apply to game manager
            var gm = Core.GameManager.Instance;
            if (gm != null)
            {
                gm.totalMoneyEarned = currentSaveData.totalMoneyEarned;
                gm.currentDay = currentSaveData.currentDay;
            }

            // Apply fast travel
            var fastTravel = FindObjectOfType<Systems.FastTravelSystem>();
            if (fastTravel != null)
            {
                fastTravel.isUnlocked = currentSaveData.fastTravelUnlocked;
                fastTravel.RestoreDiscoveredStations(currentSaveData.discoveredStations);
            }

            // Apply cigar lab
            var cigarSystem = FindObjectOfType<Items.CigarCraftingSystem>();
            if (cigarSystem != null)
            {
                cigarSystem.ownsLabTable = currentSaveData.ownsLabTable;
            }

            currentNight = currentSaveData.currentNight;
            currentMode = currentSaveData.gameMode == "forever" ? GameMode.Forever : GameMode.SevenNightRun;

            Debug.Log("[Supabase] Save data applied!");
        }

        #endregion

        #region Local Save Fallback

        private void SaveLocally()
        {
            CollectSaveData();
            string json = JsonUtility.ToJson(currentSaveData);
            PlayerPrefs.SetString(localSaveKey, json);
            PlayerPrefs.Save();
            Debug.Log("[Supabase] Saved locally (offline mode)");
            onSaveComplete?.Invoke(true);
        }

        private void LoadLocally()
        {
            string json = PlayerPrefs.GetString(localSaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
                ApplySaveData();
                Debug.Log("[Supabase] Loaded from local save");
                onLoadComplete?.Invoke(true);
            }
            else
            {
                Debug.Log("[Supabase] No local save found");
                onLoadComplete?.Invoke(false);
            }
        }

        #endregion

        #region Supabase API Coroutines

        private IEnumerator CheckConnection()
        {
            if (string.IsNullOrEmpty(supabaseUrl))
            {
                IsOnline = false;
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(supabaseUrl))
            {
                request.timeout = 5;
                yield return request.SendWebRequest();
                IsOnline = request.result == UnityWebRequest.Result.Success;
            }

            Debug.Log($"[Supabase] Online: {IsOnline}");
        }

        private IEnumerator LoginCoroutine(string username)
        {
            if (!IsConfigured())
            {
                // Offline login
                playerId = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("BobsPetroleum_Username", username);
                PlayerPrefs.SetString("BobsPetroleum_PlayerId", playerId);
                IsLoggedIn = true;
                onPlayerLoggedIn?.Invoke();
                yield break;
            }

            // Check if player exists
            string selectUrl = $"{supabaseUrl}/rest/v1/players?username=eq.{UnityWebRequest.EscapeURL(username)}&select=id";

            using (UnityWebRequest request = UnityWebRequest.Get(selectUrl))
            {
                AddSupabaseHeaders(request);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;

                    if (response == "[]")
                    {
                        // Create new player
                        yield return CreatePlayerCoroutine(username);
                    }
                    else
                    {
                        // Parse existing player
                        var players = JsonHelper.FromJson<PlayerRecord>(response);
                        if (players.Length > 0)
                        {
                            playerId = players[0].id;
                            PlayerPrefs.SetString("BobsPetroleum_Username", username);
                            PlayerPrefs.SetString("BobsPetroleum_PlayerId", playerId);
                            IsLoggedIn = true;
                            onPlayerLoggedIn?.Invoke();
                            Debug.Log($"[Supabase] Logged in as {username}");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[Supabase] Login failed: {request.error}");
                    onError?.Invoke($"Login failed: {request.error}");
                }
            }
        }

        private IEnumerator CreatePlayerCoroutine(string username)
        {
            string url = $"{supabaseUrl}/rest/v1/players";
            string json = $"{{\"username\":\"{username}\"}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                AddSupabaseHeaders(request);
                request.SetRequestHeader("Prefer", "return=representation");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var players = JsonHelper.FromJson<PlayerRecord>(request.downloadHandler.text);
                    if (players.Length > 0)
                    {
                        playerId = players[0].id;
                        PlayerPrefs.SetString("BobsPetroleum_Username", username);
                        PlayerPrefs.SetString("BobsPetroleum_PlayerId", playerId);
                        IsLoggedIn = true;
                        onPlayerLoggedIn?.Invoke();
                        Debug.Log($"[Supabase] Created new player: {username}");
                    }
                }
                else
                {
                    Debug.LogError($"[Supabase] Create player failed: {request.error}");
                    onError?.Invoke($"Create player failed: {request.error}");
                }
            }
        }

        private IEnumerator SaveGameCoroutine(string saveName)
        {
            if (!IsConfigured())
            {
                SaveLocally();
                yield break;
            }

            IsSaving = true;
            string url = $"{supabaseUrl}/rest/v1/game_saves";

            string saveDataJson = JsonUtility.ToJson(currentSaveData);
            string json = $"{{\"player_id\":\"{playerId}\",\"save_name\":\"{saveName}\",\"game_mode\":\"{currentSaveData.gameMode}\",\"save_data\":{saveDataJson}}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                AddSupabaseHeaders(request);
                request.SetRequestHeader("Prefer", "return=representation");

                yield return request.SendWebRequest();

                IsSaving = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Supabase] Game saved: {saveName}");
                    onSaveComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[Supabase] Save failed: {request.error}");
                    onError?.Invoke($"Save failed: {request.error}");
                    onSaveComplete?.Invoke(false);

                    // Fallback to local
                    if (offlineFallback)
                    {
                        SaveLocally();
                    }
                }
            }
        }

        private IEnumerator LoadGameCoroutine(string saveId)
        {
            if (!IsConfigured())
            {
                LoadLocally();
                yield break;
            }

            IsLoading = true;
            string url;

            if (!string.IsNullOrEmpty(saveId))
            {
                url = $"{supabaseUrl}/rest/v1/game_saves?id=eq.{saveId}&select=*";
            }
            else
            {
                // Get most recent save for player
                url = $"{supabaseUrl}/rest/v1/game_saves?player_id=eq.{playerId}&order=updated_at.desc&limit=1&select=*";
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                AddSupabaseHeaders(request);
                yield return request.SendWebRequest();

                IsLoading = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    var saves = JsonHelper.FromJson<SaveRecord>(response);

                    if (saves.Length > 0)
                    {
                        currentSaveId = saves[0].id;
                        currentSaveData = JsonUtility.FromJson<GameSaveData>(saves[0].save_data);
                        ApplySaveData();
                        Debug.Log($"[Supabase] Game loaded: {saves[0].save_name}");
                        onLoadComplete?.Invoke(true);
                    }
                    else
                    {
                        Debug.Log("[Supabase] No saves found");
                        onLoadComplete?.Invoke(false);
                    }
                }
                else
                {
                    Debug.LogError($"[Supabase] Load failed: {request.error}");
                    onError?.Invoke($"Load failed: {request.error}");
                    onLoadComplete?.Invoke(false);

                    // Fallback to local
                    if (offlineFallback)
                    {
                        LoadLocally();
                    }
                }
            }
        }

        private IEnumerator GetSavesCoroutine(Action<List<SaveInfo>> callback)
        {
            if (!IsConfigured())
            {
                callback?.Invoke(new List<SaveInfo>());
                yield break;
            }

            string url = $"{supabaseUrl}/rest/v1/game_saves?player_id=eq.{playerId}&order=updated_at.desc&select=id,save_name,game_mode,updated_at";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                AddSupabaseHeaders(request);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var records = JsonHelper.FromJson<SaveRecord>(request.downloadHandler.text);
                    var saves = new List<SaveInfo>();

                    foreach (var record in records)
                    {
                        saves.Add(new SaveInfo
                        {
                            id = record.id,
                            saveName = record.save_name,
                            gameMode = record.game_mode,
                            updatedAt = record.updated_at
                        });
                    }

                    callback?.Invoke(saves);
                }
                else
                {
                    callback?.Invoke(new List<SaveInfo>());
                }
            }
        }

        private IEnumerator DeleteSaveCoroutine(string saveId)
        {
            if (!IsConfigured()) yield break;

            string url = $"{supabaseUrl}/rest/v1/game_saves?id=eq.{saveId}";

            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                AddSupabaseHeaders(request);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Supabase] Save deleted: {saveId}");
                }
            }
        }

        private IEnumerator CreateNightRunCoroutine()
        {
            if (!IsConfigured()) yield break;

            string url = $"{supabaseUrl}/rest/v1/night_runs";
            string json = $"{{\"player_id\":\"{playerId}\",\"current_night\":1,\"run_data\":{{}}}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                AddSupabaseHeaders(request);
                request.SetRequestHeader("Prefer", "return=representation");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[Supabase] Night run started!");
                }
            }
        }

        private IEnumerator CompleteNightRunCoroutine()
        {
            // Calculate final score
            int score = CalculateRunScore();

            // Submit to leaderboard
            yield return SubmitScoreCoroutine(score, currentNight - 1);

            Debug.Log($"[Supabase] Night run completed! Score: {score}");
        }

        private IEnumerator SubmitScoreCoroutine(int score, int nightsSurvived)
        {
            if (!IsConfigured()) yield break;

            string url = $"{supabaseUrl}/rest/v1/leaderboard";
            string json = $"{{\"player_id\":\"{playerId}\",\"username\":\"{playerUsername}\",\"score\":{score},\"nights_survived\":{nightsSurvived},\"game_mode\":\"{currentSaveData.gameMode}\"}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                AddSupabaseHeaders(request);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Supabase] Score submitted: {score}");
                }
            }
        }

        private IEnumerator GetLeaderboardCoroutine(int limit)
        {
            if (!IsConfigured())
            {
                onLeaderboardLoaded?.Invoke(new List<LeaderboardEntry>());
                yield break;
            }

            string url = $"{supabaseUrl}/rest/v1/leaderboard?order=score.desc&limit={limit}&select=*";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                AddSupabaseHeaders(request);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var records = JsonHelper.FromJson<LeaderboardRecord>(request.downloadHandler.text);
                    var entries = new List<LeaderboardEntry>();

                    int rank = 1;
                    foreach (var record in records)
                    {
                        entries.Add(new LeaderboardEntry
                        {
                            rank = rank++,
                            username = record.username,
                            score = record.score,
                            nightsSurvived = record.nights_survived
                        });
                    }

                    onLeaderboardLoaded?.Invoke(entries);
                }
                else
                {
                    onLeaderboardLoaded?.Invoke(new List<LeaderboardEntry>());
                }
            }
        }

        #endregion

        #region Helpers

        private bool IsConfigured()
        {
            return !string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey);
        }

        private void AddSupabaseHeaders(UnityWebRequest request)
        {
            request.SetRequestHeader("apikey", supabaseKey);
            request.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
            request.SetRequestHeader("Content-Type", "application/json");
        }

        private int CalculateRunScore()
        {
            int score = 0;
            score += currentSaveData.money;
            score += currentSaveData.hamburgersFed * 100;
            score += (currentNight - 1) * 500;
            if (currentSaveData.isBobRevived) score += 5000;
            return score;
        }

        #endregion

        private void OnApplicationPause(bool pause)
        {
            if (pause && IsLoggedIn)
            {
                SaveGame("AutoSave_Pause");
            }
        }

        private void OnApplicationQuit()
        {
            if (IsLoggedIn)
            {
                // Synchronous save on quit
                SaveLocally();
            }
        }
    }

    #region Data Classes

    [Serializable]
    public class GameSaveData
    {
        public string timestamp;
        public string gameMode = "forever";
        public int currentNight = 1;

        // Player state
        public int money;
        public List<string> inventoryItems = new List<string>();

        // Bob state
        public float bobHealth = 50f;
        public int hamburgersFed;
        public bool isBobRevived;

        // Game state
        public int totalMoneyEarned;
        public int currentDay = 1;

        // Shop state
        public bool isShopOpen = true;

        // Unlocks
        public bool fastTravelUnlocked;
        public List<string> discoveredStations = new List<string>();
        public bool ownsLabTable;

        // Add more save fields as needed...
    }

    [Serializable]
    public class SaveInfo
    {
        public string id;
        public string saveName;
        public string gameMode;
        public string updatedAt;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string username;
        public int score;
        public int nightsSurvived;
    }

    // JSON parsing helpers
    [Serializable]
    public class PlayerRecord
    {
        public string id;
        public string username;
    }

    [Serializable]
    public class SaveRecord
    {
        public string id;
        public string player_id;
        public string save_name;
        public string game_mode;
        public string save_data;
        public string updated_at;
    }

    [Serializable]
    public class LeaderboardRecord
    {
        public string id;
        public string username;
        public int score;
        public int nights_survived;
    }

    // Helper for parsing JSON arrays
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string wrappedJson = "{\"items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
            return wrapper.items;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] items;
        }
    }

    #endregion
}
