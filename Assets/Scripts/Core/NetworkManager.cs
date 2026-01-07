using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.Netcode;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// Network manager for lobby creation, joining, and player management.
    /// Requires Unity Netcode for GameObjects package.
    /// </summary>
    public class BobsNetworkManager : MonoBehaviour
    {
        public static BobsNetworkManager Instance { get; private set; }

        [Header("Lobby Settings")]
        [Tooltip("Maximum players allowed in a lobby")]
        public int maxPlayers = 4;

        [Tooltip("Is the lobby public or private")]
        public bool isPublicLobby = false;

        [Tooltip("Current lobby code for private lobbies")]
        public string lobbyCode = "";

        [Header("Player Tracking")]
        public List<PlayerInfo> connectedPlayers = new List<PlayerInfo>();

        [Header("Events")]
        public UnityEvent onHostStarted;
        public UnityEvent onClientConnected;
        public UnityEvent onClientDisconnected;
        public UnityEvent<string> onLobbyCodeGenerated;
        public UnityEvent<PlayerInfo> onPlayerJoined;
        public UnityEvent<PlayerInfo> onPlayerLeft;

        private NetworkManager networkManager;

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
            }
        }

        private void Start()
        {
            networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback += OnClientConnected;
                networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }
        }

        /// <summary>
        /// Start hosting a game. Generates a lobby code if private.
        /// </summary>
        public void HostGame(bool publicLobby = false)
        {
            isPublicLobby = publicLobby;

            if (!publicLobby)
            {
                lobbyCode = GenerateLobbyCode();
                onLobbyCodeGenerated?.Invoke(lobbyCode);
            }

            if (networkManager != null)
            {
                networkManager.StartHost();
                onHostStarted?.Invoke();

                // Add host as first player
                AddPlayer(networkManager.LocalClientId, SaveSystem.Instance?.GetPlayerName() ?? "Host");
            }
        }

        /// <summary>
        /// Join a game using a lobby code.
        /// </summary>
        public void JoinGame(string code)
        {
            lobbyCode = code;

            if (networkManager != null)
            {
                // In a real implementation, you'd validate the code with a relay/lobby service
                networkManager.StartClient();
            }
        }

        /// <summary>
        /// Join a public lobby.
        /// </summary>
        public void JoinPublicLobby()
        {
            // In a real implementation, this would query available public lobbies
            if (networkManager != null)
            {
                networkManager.StartClient();
            }
        }

        /// <summary>
        /// Leave the current game.
        /// </summary>
        public void LeaveGame()
        {
            if (networkManager != null)
            {
                if (networkManager.IsHost)
                {
                    networkManager.Shutdown();
                }
                else if (networkManager.IsClient)
                {
                    networkManager.Shutdown();
                }
            }

            connectedPlayers.Clear();
            lobbyCode = "";
        }

        private void OnClientConnected(ulong clientId)
        {
            onClientConnected?.Invoke();

            if (networkManager.IsHost)
            {
                // Request player name from client
                // For now, add with default name
                AddPlayer(clientId, $"Player_{clientId}");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            onClientDisconnected?.Invoke();
            RemovePlayer(clientId);
        }

        private void AddPlayer(ulong clientId, string playerName)
        {
            var playerInfo = new PlayerInfo
            {
                clientId = clientId,
                playerName = playerName,
                isHost = clientId == networkManager.LocalClientId && networkManager.IsHost
            };

            connectedPlayers.Add(playerInfo);
            onPlayerJoined?.Invoke(playerInfo);
        }

        private void RemovePlayer(ulong clientId)
        {
            var player = connectedPlayers.Find(p => p.clientId == clientId);
            if (player != null)
            {
                connectedPlayers.Remove(player);
                onPlayerLeft?.Invoke(player);
            }
        }

        private string GenerateLobbyCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            char[] code = new char[6];

            for (int i = 0; i < code.Length; i++)
            {
                code[i] = chars[Random.Range(0, chars.Length)];
            }

            return new string(code);
        }

        public bool IsHost()
        {
            return networkManager != null && networkManager.IsHost;
        }

        public bool IsConnected()
        {
            return networkManager != null && (networkManager.IsHost || networkManager.IsClient);
        }

        public int GetPlayerCount()
        {
            return connectedPlayers.Count;
        }

        public List<PlayerInfo> GetConnectedPlayers()
        {
            return new List<PlayerInfo>(connectedPlayers);
        }

        /// <summary>
        /// Update a player's name (called when client sends their name).
        /// </summary>
        public void UpdatePlayerName(ulong clientId, string newName)
        {
            var player = connectedPlayers.Find(p => p.clientId == clientId);
            if (player != null)
            {
                player.playerName = newName;
            }
        }
    }

    [System.Serializable]
    public class PlayerInfo
    {
        public ulong clientId;
        public string playerName;
        public bool isHost;
        public bool isReady;
    }
}
