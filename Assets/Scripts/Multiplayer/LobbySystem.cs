using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace BobsPetroleum.Multiplayer
{
    /// <summary>
    /// Lobby system for multiplayer co-op.
    /// Handles player joining, ready status, and game start.
    /// </summary>
    public class LobbySystem : MonoBehaviour
    {
        public static LobbySystem Instance { get; private set; }

        [Header("Lobby Settings")]
        [Tooltip("Maximum players in lobby")]
        public int maxPlayers = 4;

        [Tooltip("Minimum players to start")]
        public int minPlayersToStart = 1;

        [Tooltip("Auto-start when all ready")]
        public bool autoStartWhenReady = true;

        [Tooltip("Countdown duration before start")]
        public float startCountdown = 5f;

        [Header("Lobby State")]
        [Tooltip("Current lobby code")]
        public string lobbyCode = "";

        [Tooltip("Is this the host?")]
        public bool isHost = false;

        [Header("UI References")]
        [Tooltip("Lobby panel")]
        public GameObject lobbyPanel;

        [Tooltip("Player list container")]
        public Transform playerListContainer;

        [Tooltip("Player slot prefab")]
        public GameObject playerSlotPrefab;

        [Tooltip("Ready button")]
        public UnityEngine.UI.Button readyButton;

        [Tooltip("Start button (host only)")]
        public UnityEngine.UI.Button startButton;

        [Tooltip("Lobby code display")]
        public TMPro.TMP_Text lobbyCodeText;

        [Tooltip("Countdown text")]
        public TMPro.TMP_Text countdownText;

        [Header("Events")]
        public UnityEvent onLobbyCreated;
        public UnityEvent onLobbyJoined;
        public UnityEvent onPlayerJoined;
        public UnityEvent onPlayerLeft;
        public UnityEvent onAllPlayersReady;
        public UnityEvent onGameStarting;

        // Runtime
        private List<LobbyPlayer> players = new List<LobbyPlayer>();
        private bool isReady = false;
        private bool isCountingDown = false;
        private float countdownTimer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Hide lobby by default
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // Countdown logic
            if (isCountingDown)
            {
                countdownTimer -= Time.deltaTime;

                if (countdownText != null)
                {
                    countdownText.text = $"Starting in {Mathf.CeilToInt(countdownTimer)}...";
                }

                if (countdownTimer <= 0f)
                {
                    StartGame();
                }
            }
        }

        #region Lobby Management

        /// <summary>
        /// Create a new lobby as host.
        /// </summary>
        public void CreateLobby()
        {
            isHost = true;
            lobbyCode = GenerateLobbyCode();

            // Add self as first player
            AddLocalPlayer();

            // Show lobby UI
            ShowLobby();

            // Update code display
            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = $"Code: {lobbyCode}";
            }

            // Enable start button for host
            if (startButton != null)
            {
                startButton.gameObject.SetActive(true);
                startButton.interactable = false; // Until min players
            }

            onLobbyCreated?.Invoke();

            Debug.Log($"[Lobby] Created lobby: {lobbyCode}");
        }

        /// <summary>
        /// Join an existing lobby.
        /// </summary>
        public void JoinLobby(string code)
        {
            if (string.IsNullOrEmpty(code)) return;

            isHost = false;
            lobbyCode = code.ToUpper();

            // Add self
            AddLocalPlayer();

            // Show lobby
            ShowLobby();

            // Update code display
            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = $"Code: {lobbyCode}";
            }

            // Hide start button for non-host
            if (startButton != null)
            {
                startButton.gameObject.SetActive(false);
            }

            onLobbyJoined?.Invoke();

            Debug.Log($"[Lobby] Joined lobby: {lobbyCode}");
        }

        /// <summary>
        /// Leave current lobby.
        /// </summary>
        public void LeaveLobby()
        {
            // Remove self from players
            players.RemoveAll(p => p.isLocal);

            // Clear state
            isHost = false;
            isReady = false;
            lobbyCode = "";

            // Hide lobby
            HideLobby();

            Debug.Log("[Lobby] Left lobby");
        }

        #endregion

        #region Player Management

        private void AddLocalPlayer()
        {
            string playerName = PlayerPrefs.GetString("PlayerName", "Player " + Random.Range(1000, 9999));

            LobbyPlayer localPlayer = new LobbyPlayer
            {
                playerId = System.Guid.NewGuid().ToString(),
                playerName = playerName,
                isHost = isHost,
                isReady = false,
                isLocal = true
            };

            players.Add(localPlayer);
            RefreshPlayerList();

            onPlayerJoined?.Invoke();
        }

        /// <summary>
        /// Add a remote player (called from network).
        /// </summary>
        public void AddRemotePlayer(string playerId, string playerName, bool playerIsHost)
        {
            // Check if already exists
            if (players.Exists(p => p.playerId == playerId)) return;

            // Check max players
            if (players.Count >= maxPlayers)
            {
                Debug.LogWarning("[Lobby] Lobby is full!");
                return;
            }

            LobbyPlayer player = new LobbyPlayer
            {
                playerId = playerId,
                playerName = playerName,
                isHost = playerIsHost,
                isReady = false,
                isLocal = false
            };

            players.Add(player);
            RefreshPlayerList();

            onPlayerJoined?.Invoke();
        }

        /// <summary>
        /// Remove a player (called from network).
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            players.RemoveAll(p => p.playerId == playerId);
            RefreshPlayerList();
            onPlayerLeft?.Invoke();
        }

        /// <summary>
        /// Set a player's ready status.
        /// </summary>
        public void SetPlayerReady(string playerId, bool ready)
        {
            var player = players.Find(p => p.playerId == playerId);
            if (player != null)
            {
                player.isReady = ready;
                RefreshPlayerList();
                CheckAllReady();
            }
        }

        /// <summary>
        /// Toggle local player ready status.
        /// </summary>
        public void ToggleReady()
        {
            isReady = !isReady;

            var localPlayer = players.Find(p => p.isLocal);
            if (localPlayer != null)
            {
                localPlayer.isReady = isReady;
            }

            // Update button text
            if (readyButton != null)
            {
                var text = readyButton.GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null)
                {
                    text.text = isReady ? "READY!" : "Ready Up";
                }
            }

            RefreshPlayerList();
            CheckAllReady();

            // Broadcast to network (implement with your networking solution)
            // NetworkManager.Instance?.BroadcastReadyStatus(isReady);
        }

        private void CheckAllReady()
        {
            // Check if minimum players and all ready
            if (players.Count < minPlayersToStart) return;

            bool allReady = true;
            foreach (var player in players)
            {
                if (!player.isReady)
                {
                    allReady = false;
                    break;
                }
            }

            if (allReady)
            {
                onAllPlayersReady?.Invoke();

                if (autoStartWhenReady && isHost)
                {
                    StartCountdown();
                }
            }
            else
            {
                // Cancel countdown if someone unreadied
                if (isCountingDown)
                {
                    CancelCountdown();
                }
            }

            // Update start button for host
            if (isHost && startButton != null)
            {
                startButton.interactable = allReady && players.Count >= minPlayersToStart;
            }
        }

        #endregion

        #region Game Start

        /// <summary>
        /// Start countdown to game (host only).
        /// </summary>
        public void StartCountdown()
        {
            if (!isHost) return;
            if (isCountingDown) return;

            isCountingDown = true;
            countdownTimer = startCountdown;

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
            }

            onGameStarting?.Invoke();

            Debug.Log("[Lobby] Starting countdown...");
        }

        /// <summary>
        /// Cancel countdown.
        /// </summary>
        public void CancelCountdown()
        {
            isCountingDown = false;

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }

            Debug.Log("[Lobby] Countdown cancelled");
        }

        /// <summary>
        /// Force start game (host only).
        /// </summary>
        public void ForceStartGame()
        {
            if (!isHost) return;
            StartGame();
        }

        private void StartGame()
        {
            isCountingDown = false;

            // Hide lobby
            HideLobby();

            // Load game scene or start game
            Debug.Log("[Lobby] Starting game with " + players.Count + " players!");

            // Spawn all players
            SpawnAllPlayers();

            // Start game manager
            Core.GameManager.Instance?.StartGame();
        }

        private void SpawnAllPlayers()
        {
            // Find spawn points
            var spawnPoints = FindObjectsOfType<PlayerSpawnPoint>();

            int spawnIndex = 0;
            foreach (var player in players)
            {
                if (player.isLocal)
                {
                    // Spawn local player
                    Vector3 spawnPos = spawnPoints.Length > spawnIndex ?
                        spawnPoints[spawnIndex].transform.position : Vector3.zero;

                    var playerObj = FindObjectOfType<Player.PlayerController>();
                    if (playerObj != null)
                    {
                        var cc = playerObj.GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        playerObj.transform.position = spawnPos;
                        if (cc != null) cc.enabled = true;
                    }
                }
                // Remote players would be spawned via Netcode

                spawnIndex++;
            }
        }

        #endregion

        #region UI

        private void ShowLobby()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(true);
            }

            // Lock cursor for menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HideLobby()
        {
            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(false);
            }

            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void RefreshPlayerList()
        {
            if (playerListContainer == null || playerSlotPrefab == null) return;

            // Clear existing
            foreach (Transform child in playerListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create slots
            foreach (var player in players)
            {
                GameObject slot = Instantiate(playerSlotPrefab, playerListContainer);

                // Set name
                var nameText = slot.GetComponentInChildren<TMPro.TMP_Text>();
                if (nameText != null)
                {
                    string hostTag = player.isHost ? " [HOST]" : "";
                    string readyTag = player.isReady ? " ✓" : "";
                    nameText.text = player.playerName + hostTag + readyTag;
                }

                // Set color based on ready status
                var image = slot.GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.color = player.isReady ? Color.green : Color.white;
                }
            }
        }

        #endregion

        #region Helpers

        private string GenerateLobbyCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] code = new char[6];

            for (int i = 0; i < 6; i++)
            {
                code[i] = chars[Random.Range(0, chars.Length)];
            }

            return new string(code);
        }

        /// <summary>
        /// Copy lobby code to clipboard.
        /// </summary>
        public void CopyLobbyCode()
        {
            GUIUtility.systemCopyBuffer = lobbyCode;
            UI.HUDManager.Instance?.ShowNotification("Lobby code copied!");
        }

        #endregion

        #region Properties

        public int PlayerCount => players.Count;
        public bool IsInLobby => !string.IsNullOrEmpty(lobbyCode);
        public bool CanStart => players.Count >= minPlayersToStart && players.TrueForAll(p => p.isReady);

        #endregion
    }

    [System.Serializable]
    public class LobbyPlayer
    {
        public string playerId;
        public string playerName;
        public bool isHost;
        public bool isReady;
        public bool isLocal;
    }

    /// <summary>
    /// Player spawn point marker.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
        public int spawnIndex;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }
    }
}
