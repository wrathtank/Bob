using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using BobsPetroleum.Core;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Main menu controller for lobby creation, joining, and game start.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("Main menu panel with Play button")]
        public GameObject mainPanel;

        [Tooltip("Lobby options panel (host/join selection)")]
        public GameObject lobbyOptionsPanel;

        [Tooltip("Host game panel")]
        public GameObject hostPanel;

        [Tooltip("Join game panel")]
        public GameObject joinPanel;

        [Tooltip("Lobby waiting room panel")]
        public GameObject lobbyPanel;

        [Tooltip("Pre-run inventory panel")]
        public GameObject inventoryPanel;

        [Header("Player Name Input")]
        [Tooltip("Input field for player name")]
        public TMP_InputField playerNameInput;

        [Header("Host Options")]
        [Tooltip("Toggle for public/private lobby")]
        public Toggle publicLobbyToggle;

        [Tooltip("Text displaying the lobby code")]
        public TMP_Text lobbyCodeText;

        [Tooltip("Button to copy lobby code")]
        public Button copyCodeButton;

        [Header("Join Options")]
        [Tooltip("Input field for lobby code")]
        public TMP_InputField lobbyCodeInput;

        [Tooltip("Button to join public lobbies")]
        public Button joinPublicButton;

        [Header("Lobby Display")]
        [Tooltip("Container for player list items")]
        public Transform playerListContainer;

        [Tooltip("Prefab for player list item")]
        public GameObject playerListItemPrefab;

        [Tooltip("Text showing player count")]
        public TMP_Text playerCountText;

        [Tooltip("Start game button (host only)")]
        public Button startGameButton;

        [Header("Pre-Run Inventory")]
        [Tooltip("Container for consumable items")]
        public Transform consumableContainer;

        [Tooltip("Prefab for consumable item display")]
        public GameObject consumableItemPrefab;

        [Header("Events")]
        public UnityEvent onGameStart;
        public UnityEvent onReturnToMenu;

        private void Start()
        {
            // Load saved player name
            if (SaveSystem.Instance != null && playerNameInput != null)
            {
                playerNameInput.text = SaveSystem.Instance.GetPlayerName();
                playerNameInput.onEndEdit.AddListener(OnPlayerNameChanged);
            }

            // Setup network events
            if (BobsNetworkManager.Instance != null)
            {
                BobsNetworkManager.Instance.onLobbyCodeGenerated.AddListener(OnLobbyCodeGenerated);
                BobsNetworkManager.Instance.onPlayerJoined.AddListener(OnPlayerJoined);
                BobsNetworkManager.Instance.onPlayerLeft.AddListener(OnPlayerLeft);
            }

            ShowMainPanel();
        }

        private void OnDestroy()
        {
            if (playerNameInput != null)
                playerNameInput.onEndEdit.RemoveListener(OnPlayerNameChanged);

            if (BobsNetworkManager.Instance != null)
            {
                BobsNetworkManager.Instance.onLobbyCodeGenerated.RemoveListener(OnLobbyCodeGenerated);
                BobsNetworkManager.Instance.onPlayerJoined.RemoveListener(OnPlayerJoined);
                BobsNetworkManager.Instance.onPlayerLeft.RemoveListener(OnPlayerLeft);
            }
        }

        #region Panel Navigation

        public void ShowMainPanel()
        {
            HideAllPanels();
            if (mainPanel != null) mainPanel.SetActive(true);
        }

        public void ShowLobbyOptions()
        {
            HideAllPanels();
            if (lobbyOptionsPanel != null) lobbyOptionsPanel.SetActive(true);
        }

        public void ShowHostPanel()
        {
            HideAllPanels();
            if (hostPanel != null) hostPanel.SetActive(true);
        }

        public void ShowJoinPanel()
        {
            HideAllPanels();
            if (joinPanel != null) joinPanel.SetActive(true);
        }

        public void ShowLobbyPanel()
        {
            HideAllPanels();
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            UpdatePlayerList();
        }

        public void ShowInventoryPanel()
        {
            HideAllPanels();
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            RefreshInventoryDisplay();
        }

        private void HideAllPanels()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (lobbyOptionsPanel != null) lobbyOptionsPanel.SetActive(false);
            if (hostPanel != null) hostPanel.SetActive(false);
            if (joinPanel != null) joinPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
        }

        #endregion

        #region Player Name

        private void OnPlayerNameChanged(string newName)
        {
            if (!string.IsNullOrEmpty(newName))
            {
                SaveSystem.Instance?.SetPlayerName(newName);
            }
        }

        #endregion

        #region Hosting

        public void OnHostButtonClicked()
        {
            bool isPublic = publicLobbyToggle != null && publicLobbyToggle.isOn;
            BobsNetworkManager.Instance?.HostGame(isPublic);
            ShowLobbyPanel();

            // Host can always start
            if (startGameButton != null)
                startGameButton.interactable = true;
        }

        private void OnLobbyCodeGenerated(string code)
        {
            if (lobbyCodeText != null)
            {
                lobbyCodeText.text = code;
            }
        }

        public void OnCopyCodeClicked()
        {
            if (BobsNetworkManager.Instance != null)
            {
                GUIUtility.systemCopyBuffer = BobsNetworkManager.Instance.lobbyCode;
            }
        }

        #endregion

        #region Joining

        public void OnJoinWithCodeClicked()
        {
            if (lobbyCodeInput != null && !string.IsNullOrEmpty(lobbyCodeInput.text))
            {
                BobsNetworkManager.Instance?.JoinGame(lobbyCodeInput.text.ToUpper());
                ShowLobbyPanel();
            }
        }

        public void OnJoinPublicClicked()
        {
            BobsNetworkManager.Instance?.JoinPublicLobby();
            ShowLobbyPanel();
        }

        #endregion

        #region Lobby Management

        private void OnPlayerJoined(PlayerInfo player)
        {
            UpdatePlayerList();
        }

        private void OnPlayerLeft(PlayerInfo player)
        {
            UpdatePlayerList();
        }

        private void UpdatePlayerList()
        {
            if (playerListContainer == null || playerListItemPrefab == null)
                return;

            // Clear existing items
            foreach (Transform child in playerListContainer)
            {
                Destroy(child.gameObject);
            }

            // Add player items
            if (BobsNetworkManager.Instance != null)
            {
                var players = BobsNetworkManager.Instance.GetConnectedPlayers();

                foreach (var player in players)
                {
                    var item = Instantiate(playerListItemPrefab, playerListContainer);
                    var text = item.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        string hostTag = player.isHost ? " (Host)" : "";
                        text.text = player.playerName + hostTag;
                    }
                }

                // Update player count
                if (playerCountText != null)
                {
                    playerCountText.text = $"Players: {players.Count}/{BobsNetworkManager.Instance.maxPlayers}";
                }
            }
        }

        public void OnStartGameClicked()
        {
            if (BobsNetworkManager.Instance != null && BobsNetworkManager.Instance.IsHost())
            {
                GameManager.Instance?.StartGame();
                onGameStart?.Invoke();
            }
        }

        public void OnLeaveLobbyCLicked()
        {
            BobsNetworkManager.Instance?.LeaveGame();
            ShowMainPanel();
            onReturnToMenu?.Invoke();
        }

        #endregion

        #region Pre-Run Inventory

        private void RefreshInventoryDisplay()
        {
            if (consumableContainer == null || consumableItemPrefab == null)
                return;

            // Clear existing items
            foreach (Transform child in consumableContainer)
            {
                Destroy(child.gameObject);
            }

            // Add consumable items
            if (SaveSystem.Instance != null)
            {
                foreach (var consumable in SaveSystem.Instance.preRunConsumables)
                {
                    var item = Instantiate(consumableItemPrefab, consumableContainer);
                    var text = item.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        text.text = $"{consumable.consumableId} x{consumable.quantity}";
                    }
                }
            }
        }

        #endregion
    }
}
