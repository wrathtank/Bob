using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// COMPLETE MAIN MENU - Just drag UI elements into the slots!
    ///
    /// SETUP (5 minutes):
    /// 1. Create Canvas (GameObject > UI > Canvas)
    /// 2. Add this component to the Canvas
    /// 3. Create buttons and panels as children
    /// 4. Drag them into the slots below
    /// 5. Done!
    ///
    /// BUTTON SETUP:
    /// - Each button just needs to exist - this script wires them automatically!
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager Instance { get; private set; }

        [Header("=== DRAG YOUR PANELS HERE ===")]
        [Tooltip("The main menu panel with Play, Settings, Quit buttons")]
        public GameObject mainMenuPanel;

        [Tooltip("Settings panel")]
        public GameObject settingsPanel;

        [Tooltip("Credits panel")]
        public GameObject creditsPanel;

        [Tooltip("How to Play panel")]
        public GameObject howToPlayPanel;

        [Tooltip("Mode selection panel (Forever vs 7 Night)")]
        public GameObject modeSelectPanel;

        [Tooltip("Multiplayer panel (Host/Join)")]
        public GameObject multiplayerPanel;

        [Tooltip("Loading screen panel")]
        public GameObject loadingPanel;

        [Header("=== DRAG YOUR BUTTONS HERE ===")]
        [Tooltip("Play/Start Game button")]
        public Button playButton;

        [Tooltip("Forever Mode button")]
        public Button foreverModeButton;

        [Tooltip("7 Night Run button")]
        public Button sevenNightButton;

        [Tooltip("Multiplayer button")]
        public Button multiplayerButton;

        [Tooltip("Host Game button")]
        public Button hostButton;

        [Tooltip("Join Game button")]
        public Button joinButton;

        [Tooltip("Settings button")]
        public Button settingsButton;

        [Tooltip("How to Play button")]
        public Button howToPlayButton;

        [Tooltip("Credits button")]
        public Button creditsButton;

        [Tooltip("Quit button")]
        public Button quitButton;

        [Tooltip("Back buttons (add all of them)")]
        public Button[] backButtons;

        [Header("=== MULTIPLAYER UI ===")]
        [Tooltip("Input field for join IP")]
        public TMP_InputField joinIPInput;

        [Tooltip("Text showing your IP when hosting")]
        public TMP_Text hostIPText;

        [Tooltip("Status text for connection")]
        public TMP_Text connectionStatusText;

        [Header("=== LOADING SCREEN ===")]
        [Tooltip("Loading progress bar")]
        public Slider loadingBar;

        [Tooltip("Loading text")]
        public TMP_Text loadingText;

        [Tooltip("Loading tips text")]
        public TMP_Text loadingTipsText;

        [Header("=== SCENE SETTINGS ===")]
        [Tooltip("Name of your game scene")]
        public string gameSceneName = "GameScene";

        [Tooltip("Loading tips to show")]
        public string[] loadingTips = new string[]
        {
            "Feed Bob hamburgers to keep him alive!",
            "Set a home spot to respawn there when you die.",
            "Find the pipe to unlock fast travel!",
            "Catch pets with the net and battle with them!",
            "Buy a lab table to craft powerful cigars!",
            "Work the cash register to earn money fast!",
            "Explore the world for hidden secrets!"
        };

        [Header("=== AUDIO ===")]
        [Tooltip("Button click sound")]
        public AudioClip clickSound;

        [Tooltip("Menu music")]
        public AudioClip menuMusic;

        [Tooltip("Music volume")]
        [Range(0f, 1f)]
        public float musicVolume = 0.5f;

        // Runtime
        private AudioSource audioSource;
        private AudioSource musicSource;
        private GameObject currentPanel;

        private void Awake()
        {
            Instance = this;

            // Setup audio
            audioSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }

        private void Start()
        {
            // Wire up all buttons automatically
            WireButtons();

            // Show main menu
            ShowPanel(mainMenuPanel);

            // Play menu music
            if (menuMusic != null)
            {
                musicSource.clip = menuMusic;
                musicSource.Play();
            }

            // Unlock cursor for menu
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        #region Button Wiring

        private void WireButtons()
        {
            // Main buttons
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (foreverModeButton != null)
                foreverModeButton.onClick.AddListener(OnForeverModeClicked);

            if (sevenNightButton != null)
                sevenNightButton.onClick.AddListener(OnSevenNightClicked);

            if (multiplayerButton != null)
                multiplayerButton.onClick.AddListener(OnMultiplayerClicked);

            if (hostButton != null)
                hostButton.onClick.AddListener(OnHostClicked);

            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (howToPlayButton != null)
                howToPlayButton.onClick.AddListener(OnHowToPlayClicked);

            if (creditsButton != null)
                creditsButton.onClick.AddListener(OnCreditsClicked);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);

            // Back buttons
            if (backButtons != null)
            {
                foreach (var btn in backButtons)
                {
                    if (btn != null)
                        btn.onClick.AddListener(OnBackClicked);
                }
            }
        }

        #endregion

        #region Button Handlers

        public void OnPlayClicked()
        {
            PlayClickSound();

            // If we have mode select, show it
            if (modeSelectPanel != null)
            {
                ShowPanel(modeSelectPanel);
            }
            else
            {
                // Just start the game
                StartGame(false);
            }
        }

        public void OnForeverModeClicked()
        {
            PlayClickSound();
            StartGame(false); // Forever mode
        }

        public void OnSevenNightClicked()
        {
            PlayClickSound();
            StartGame(true); // 7 Night Run
        }

        public void OnMultiplayerClicked()
        {
            PlayClickSound();
            ShowPanel(multiplayerPanel);
        }

        public void OnHostClicked()
        {
            PlayClickSound();

            // Start hosting
            var netManager = Networking.NetworkGameManager.Instance;
            if (netManager != null)
            {
                netManager.HostGame();

                // Show IP
                if (hostIPText != null)
                {
                    hostIPText.text = $"Your IP: {netManager.CurrentIP}:{netManager.CurrentPort}";
                }

                UpdateConnectionStatus("Hosting... Waiting for players");
            }
            else
            {
                UpdateConnectionStatus("Network Manager not found!");
            }
        }

        public void OnJoinClicked()
        {
            PlayClickSound();

            string ip = joinIPInput != null ? joinIPInput.text : "127.0.0.1";

            var netManager = Networking.NetworkGameManager.Instance;
            if (netManager != null)
            {
                // Parse IP:Port if provided
                if (ip.Contains(":"))
                {
                    netManager.ParseAndJoin(ip);
                }
                else
                {
                    netManager.JoinGame(ip, 7777);
                }

                UpdateConnectionStatus($"Connecting to {ip}...");
            }
            else
            {
                UpdateConnectionStatus("Network Manager not found!");
            }
        }

        public void OnSettingsClicked()
        {
            PlayClickSound();
            ShowPanel(settingsPanel);
        }

        public void OnHowToPlayClicked()
        {
            PlayClickSound();
            ShowPanel(howToPlayPanel);
        }

        public void OnCreditsClicked()
        {
            PlayClickSound();
            ShowPanel(creditsPanel);
        }

        public void OnQuitClicked()
        {
            PlayClickSound();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnBackClicked()
        {
            PlayClickSound();
            ShowPanel(mainMenuPanel);
        }

        #endregion

        #region Panel Management

        private void ShowPanel(GameObject panel)
        {
            // Hide all panels
            HideAllPanels();

            // Show requested panel
            if (panel != null)
            {
                panel.SetActive(true);
                currentPanel = panel;
            }
        }

        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
            if (modeSelectPanel != null) modeSelectPanel.SetActive(false);
            if (multiplayerPanel != null) multiplayerPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        #endregion

        #region Game Start

        public void StartGame(bool isSevenNightRun)
        {
            // Store game mode
            PlayerPrefs.SetInt("GameMode", isSevenNightRun ? 1 : 0);
            PlayerPrefs.Save();

            // Stop menu music
            if (musicSource != null)
            {
                musicSource.Stop();
            }

            // Load game scene
            StartCoroutine(LoadGameScene());
        }

        private IEnumerator LoadGameScene()
        {
            // Show loading screen
            ShowPanel(loadingPanel);

            // Show random tip
            if (loadingTipsText != null && loadingTips.Length > 0)
            {
                loadingTipsText.text = loadingTips[Random.Range(0, loadingTips.Length)];
            }

            // Start async load
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(gameSceneName);
            asyncLoad.allowSceneActivation = false;

            // Update progress bar
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

                if (loadingBar != null)
                {
                    loadingBar.value = progress;
                }

                if (loadingText != null)
                {
                    loadingText.text = $"Loading... {(progress * 100):F0}%";
                }

                // When ready, activate scene
                if (asyncLoad.progress >= 0.9f)
                {
                    if (loadingText != null)
                    {
                        loadingText.text = "Press any key to continue...";
                    }

                    if (Input.anyKeyDown)
                    {
                        asyncLoad.allowSceneActivation = true;
                    }
                }

                yield return null;
            }
        }

        #endregion

        #region Helpers

        private void PlayClickSound()
        {
            if (clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        private void UpdateConnectionStatus(string status)
        {
            if (connectionStatusText != null)
            {
                connectionStatusText.text = status;
            }
            Debug.Log($"[MainMenu] {status}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Call this to return to main menu from game
        /// </summary>
        public static void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0); // Assumes main menu is scene 0
        }

        #endregion
    }
}
