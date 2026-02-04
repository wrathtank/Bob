using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_NETCODE
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif

namespace BobsPetroleum.Networking
{
    /// <summary>
    /// Easy-to-use network manager for client-hosted (listen server) multiplayer.
    /// Just drop on a GameObject and configure in inspector!
    ///
    /// SETUP:
    /// 1. Install "Netcode for GameObjects" from Package Manager
    /// 2. Add this to a GameObject in your scene
    /// 3. Assign player prefab (must have NetworkObject component)
    /// 4. Click "Host Game" or "Join Game" at runtime
    /// </summary>
    public class NetworkGameManager : MonoBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        [Header("=== EASY SETUP ===")]
        [Tooltip("Your player prefab - MUST have NetworkObject component!")]
        public GameObject playerPrefab;

        [Tooltip("Where players spawn")]
        public Transform[] spawnPoints;

        [Tooltip("Default port for hosting")]
        public ushort defaultPort = 7777;

        [Tooltip("Max players allowed")]
        public int maxPlayers = 4;

        [Header("=== CONNECTION SETTINGS ===")]
        [Tooltip("IP to join (for clients)")]
        public string joinIP = "127.0.0.1";

        [Tooltip("Port to join (for clients)")]
        public ushort joinPort = 7777;

        [Tooltip("Connection timeout in seconds")]
        public float connectionTimeout = 10f;

        [Header("=== AUTO SETUP ===")]
        [Tooltip("Auto-create NetworkManager if missing")]
        public bool autoCreateNetworkManager = true;

        [Tooltip("Auto-register player prefab")]
        public bool autoRegisterPrefabs = true;

        [Header("=== UI REFERENCES (Optional) ===")]
        [Tooltip("Main menu panel to hide when connected")]
        public GameObject mainMenuPanel;

        [Tooltip("Loading panel to show during connection")]
        public GameObject loadingPanel;

        [Tooltip("Status text for connection info")]
        public TMPro.TMP_Text statusText;

        [Header("=== EVENTS ===")]
        public UnityEvent onHostStarted;
        public UnityEvent onClientConnected;
        public UnityEvent onClientDisconnected;
        public UnityEvent onConnectionFailed;
        public UnityEvent<ulong> onPlayerJoined;
        public UnityEvent<ulong> onPlayerLeft;

        // Runtime state
        public bool IsHost { get; private set; }
        public bool IsClient { get; private set; }
        public bool IsConnected { get; private set; }
        public int PlayerCount { get; private set; }
        public string CurrentIP { get; private set; }
        public ushort CurrentPort { get; private set; }

        private Dictionary<ulong, GameObject> connectedPlayers = new Dictionary<ulong, GameObject>();
        private Coroutine connectionTimeoutCoroutine;

#if UNITY_NETCODE
        private NetworkManager networkManager;
        private UnityTransport transport;
#endif

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

            SetupNetworking();
        }

        private void SetupNetworking()
        {
#if UNITY_NETCODE
            // Find or create NetworkManager
            networkManager = FindObjectOfType<NetworkManager>();

            if (networkManager == null && autoCreateNetworkManager)
            {
                GameObject nmObj = new GameObject("NetworkManager");
                DontDestroyOnLoad(nmObj);
                networkManager = nmObj.AddComponent<NetworkManager>();
                transport = nmObj.AddComponent<UnityTransport>();
                networkManager.NetworkConfig = new NetworkConfig();

                Debug.Log("[Network] Auto-created NetworkManager");
            }

            if (networkManager != null)
            {
                // Get or add transport
                transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    transport = networkManager.gameObject.AddComponent<UnityTransport>();
                }

                // Set transport
                networkManager.NetworkConfig.NetworkTransport = transport;

                // Register player prefab
                if (autoRegisterPrefabs && playerPrefab != null)
                {
                    if (playerPrefab.GetComponent<NetworkObject>() == null)
                    {
                        Debug.LogError("[Network] Player prefab needs NetworkObject component!");
                    }
                    else
                    {
                        networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
                    }
                }

                // Subscribe to events
                networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
                networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            }
#else
            Debug.LogWarning("[Network] Netcode for GameObjects not installed! Install from Package Manager.");
#endif
        }

        #region Public API - CALL THESE FROM UI BUTTONS!

        /// <summary>
        /// Start hosting a game (you are both server and client)
        /// </summary>
        public void HostGame()
        {
            HostGame(defaultPort);
        }

        /// <summary>
        /// Start hosting on specific port
        /// </summary>
        public void HostGame(ushort port)
        {
#if UNITY_NETCODE
            if (networkManager == null)
            {
                Debug.LogError("[Network] NetworkManager not found!");
                return;
            }

            // Configure transport
            transport.SetConnectionData("0.0.0.0", port);
            CurrentPort = port;
            CurrentIP = GetLocalIPAddress();

            // Start host
            if (networkManager.StartHost())
            {
                IsHost = true;
                IsClient = true;
                IsConnected = true;
                PlayerCount = 1;

                UpdateStatus($"Hosting on {CurrentIP}:{CurrentPort}");
                ShowGameUI();

                onHostStarted?.Invoke();
                Debug.Log($"[Network] Now hosting on port {port}. Your IP: {CurrentIP}");
            }
            else
            {
                UpdateStatus("Failed to start host!");
                onConnectionFailed?.Invoke();
            }
#else
            Debug.LogWarning("[Network] Netcode not installed!");
#endif
        }

        /// <summary>
        /// Join a game as client
        /// </summary>
        public void JoinGame()
        {
            JoinGame(joinIP, joinPort);
        }

        /// <summary>
        /// Join a specific IP and port
        /// </summary>
        public void JoinGame(string ip, ushort port)
        {
#if UNITY_NETCODE
            if (networkManager == null)
            {
                Debug.LogError("[Network] NetworkManager not found!");
                return;
            }

            // Configure transport
            transport.SetConnectionData(ip, port);
            CurrentIP = ip;
            CurrentPort = port;

            UpdateStatus($"Connecting to {ip}:{port}...");
            ShowLoading(true);

            // Start timeout
            if (connectionTimeoutCoroutine != null)
            {
                StopCoroutine(connectionTimeoutCoroutine);
            }
            connectionTimeoutCoroutine = StartCoroutine(ConnectionTimeoutRoutine());

            // Start client
            if (networkManager.StartClient())
            {
                IsClient = true;
                Debug.Log($"[Network] Connecting to {ip}:{port}...");
            }
            else
            {
                UpdateStatus("Failed to start client!");
                ShowLoading(false);
                onConnectionFailed?.Invoke();
            }
#else
            Debug.LogWarning("[Network] Netcode not installed!");
#endif
        }

        /// <summary>
        /// Disconnect from current game
        /// </summary>
        public void Disconnect()
        {
#if UNITY_NETCODE
            if (networkManager == null) return;

            if (networkManager.IsHost)
            {
                networkManager.Shutdown();
            }
            else if (networkManager.IsClient)
            {
                networkManager.Shutdown();
            }

            ResetState();
            UpdateStatus("Disconnected");
            ShowMainMenu();

            Debug.Log("[Network] Disconnected");
#endif
        }

        /// <summary>
        /// Kick a player (host only)
        /// </summary>
        public void KickPlayer(ulong clientId)
        {
#if UNITY_NETCODE
            if (!IsHost)
            {
                Debug.LogWarning("[Network] Only host can kick players!");
                return;
            }

            networkManager.DisconnectClient(clientId);
            Debug.Log($"[Network] Kicked player {clientId}");
#endif
        }

        #endregion

        #region Callbacks

#if UNITY_NETCODE
        private void OnClientConnectedCallback(ulong clientId)
        {
            if (connectionTimeoutCoroutine != null)
            {
                StopCoroutine(connectionTimeoutCoroutine);
                connectionTimeoutCoroutine = null;
            }

            PlayerCount++;
            IsConnected = true;

            ShowLoading(false);
            ShowGameUI();

            if (networkManager.IsHost)
            {
                // Host: spawn player at spawn point
                SpawnPlayerForClient(clientId);
                UpdateStatus($"Players: {PlayerCount}/{maxPlayers}");
            }
            else if (clientId == networkManager.LocalClientId)
            {
                // We just connected
                UpdateStatus("Connected!");
                onClientConnected?.Invoke();
            }

            onPlayerJoined?.Invoke(clientId);
            Debug.Log($"[Network] Player {clientId} connected. Total: {PlayerCount}");
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            PlayerCount = Mathf.Max(0, PlayerCount - 1);

            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
            }

            if (clientId == networkManager.LocalClientId)
            {
                // We disconnected
                ResetState();
                ShowMainMenu();
                onClientDisconnected?.Invoke();
            }

            onPlayerLeft?.Invoke(clientId);
            UpdateStatus($"Players: {PlayerCount}/{maxPlayers}");
            Debug.Log($"[Network] Player {clientId} disconnected. Total: {PlayerCount}");
        }

        private void SpawnPlayerForClient(ulong clientId)
        {
            if (playerPrefab == null) return;

            // Get spawn point
            Vector3 spawnPos = GetSpawnPoint();

            // The NetworkManager handles spawning the player prefab automatically
            // This is just for tracking
            Debug.Log($"[Network] Player {clientId} spawning at {spawnPos}");
        }
#endif

        private Vector3 GetSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int index = PlayerCount % spawnPoints.Length;
                if (spawnPoints[index] != null)
                {
                    return spawnPoints[index].position;
                }
            }
            return Vector3.zero;
        }

        private IEnumerator ConnectionTimeoutRoutine()
        {
            yield return new WaitForSeconds(connectionTimeout);

#if UNITY_NETCODE
            if (!IsConnected)
            {
                networkManager.Shutdown();
                ResetState();
                UpdateStatus("Connection timed out!");
                ShowLoading(false);
                ShowMainMenu();
                onConnectionFailed?.Invoke();
                Debug.Log("[Network] Connection timed out!");
            }
#endif
        }

        #endregion

        #region UI Helpers

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[Network] {message}");
        }

        private void ShowLoading(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }
        }

        private void ShowMainMenu()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        private void ShowGameUI()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(false);
            }
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        private void ResetState()
        {
            IsHost = false;
            IsClient = false;
            IsConnected = false;
            PlayerCount = 0;
            connectedPlayers.Clear();
        }

        #endregion

        #region Utility

        private string GetLocalIPAddress()
        {
            string localIP = "127.0.0.1";
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Network] Could not get local IP: {e.Message}");
            }
            return localIP;
        }

        /// <summary>
        /// Copy your IP to clipboard for easy sharing
        /// </summary>
        public void CopyIPToClipboard()
        {
            string connectionInfo = $"{CurrentIP}:{CurrentPort}";
            GUIUtility.systemCopyBuffer = connectionInfo;
            UpdateStatus($"Copied: {connectionInfo}");
        }

        /// <summary>
        /// Parse IP:Port string (e.g., from clipboard)
        /// </summary>
        public void ParseAndJoin(string ipPort)
        {
            string[] parts = ipPort.Split(':');
            if (parts.Length == 2 && ushort.TryParse(parts[1], out ushort port))
            {
                JoinGame(parts[0], port);
            }
            else
            {
                UpdateStatus("Invalid format! Use IP:PORT");
            }
        }

        #endregion

        private void OnDestroy()
        {
#if UNITY_NETCODE
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
#endif
        }

        private void OnGUI()
        {
            // Debug UI when no UI assigned
            if (mainMenuPanel == null && !IsConnected)
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 300));
                GUILayout.Label("=== Bob's Petroleum Network ===");

                if (GUILayout.Button("Host Game"))
                {
                    HostGame();
                }

                GUILayout.Label("Join IP:");
                joinIP = GUILayout.TextField(joinIP);

                if (GUILayout.Button("Join Game"))
                {
                    JoinGame();
                }

                GUILayout.EndArea();
            }
            else if (IsConnected)
            {
                GUILayout.BeginArea(new Rect(10, 10, 200, 100));
                GUILayout.Label($"Connected: {(IsHost ? "Host" : "Client")}");
                GUILayout.Label($"Players: {PlayerCount}");
                if (GUILayout.Button("Disconnect"))
                {
                    Disconnect();
                }
                GUILayout.EndArea();
            }
        }
    }
}
