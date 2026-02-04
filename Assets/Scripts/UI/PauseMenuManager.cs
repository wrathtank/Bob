using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// PAUSE MENU - All the buttons you need!
    /// Auto-connects to GameStateManager for pausing.
    ///
    /// SETUP:
    /// 1. Create a Canvas with your pause menu panel
    /// 2. Add buttons: Resume, Settings, Restart, Main Menu, Quit
    /// 3. Drag buttons into the slots below
    /// 4. Done! It auto-connects everything.
    ///
    /// The pause menu automatically shows/hides based on game state.
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        public static PauseMenuManager Instance { get; private set; }

        [Header("=== PANELS ===")]
        [Tooltip("Main pause menu panel - gets shown when paused")]
        public GameObject pausePanel;

        [Tooltip("Settings panel - shown when clicking Settings")]
        public GameObject settingsPanel;

        [Tooltip("Confirm quit panel - 'Are you sure?' dialog")]
        public GameObject confirmQuitPanel;

        [Header("=== BUTTONS ===")]
        [Tooltip("Resume button - returns to game")]
        public Button resumeButton;

        [Tooltip("Settings button - opens settings")]
        public Button settingsButton;

        [Tooltip("Back from settings button")]
        public Button settingsBackButton;

        [Tooltip("Restart button - restarts current game")]
        public Button restartButton;

        [Tooltip("Main menu button - returns to main menu")]
        public Button mainMenuButton;

        [Tooltip("Quit button - quits application")]
        public Button quitButton;

        [Tooltip("Confirm quit YES button")]
        public Button confirmQuitYesButton;

        [Tooltip("Confirm quit NO button")]
        public Button confirmQuitNoButton;

        [Header("=== DISPLAY ===")]
        [Tooltip("Title text (shows 'PAUSED')")]
        public TMP_Text titleText;

        [Tooltip("Current night text (for 7 Night Runs)")]
        public TMP_Text nightText;

        [Tooltip("Current money text")]
        public TMP_Text moneyText;

        [Tooltip("Play time text")]
        public TMP_Text playTimeText;

        [Header("=== SETTINGS ===")]
        [Tooltip("Pause key (usually Escape)")]
        public KeyCode pauseKey = KeyCode.Escape;

        [Tooltip("Can pause during intro?")]
        public bool canPauseDuringIntro = false;

        [Header("=== AUDIO ===")]
        [Tooltip("Sound when opening pause menu")]
        public AudioClip pauseSound;

        [Tooltip("Sound when closing pause menu")]
        public AudioClip resumeSound;

        [Tooltip("Button click sound")]
        public AudioClip buttonClickSound;

        [Range(0f, 1f)]
        public float soundVolume = 0.5f;

        [Header("=== EVENTS ===")]
        public UnityEvent onPauseMenuOpened;
        public UnityEvent onPauseMenuClosed;
        public UnityEvent onSettingsOpened;

        // References
        private Core.GameStateManager gameState;
        private AudioSource audioSource;
        private float playTime = 0f;
        private bool isPaused = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Find GameStateManager
            gameState = Core.GameStateManager.Instance ?? FindObjectOfType<Core.GameStateManager>();

            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Wire up buttons
            WireButtons();

            // Hide all panels initially
            HideAllPanels();

            // Subscribe to game state changes
            if (gameState != null)
            {
                gameState.onStateChanged.AddListener(OnGameStateChanged);
            }
        }

        private void Update()
        {
            // Track play time when playing
            if (gameState != null && gameState.IsPlaying)
            {
                playTime += Time.deltaTime;
            }

            // Update display
            UpdateDisplay();
        }

        private void WireButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);

            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(CloseSettings);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);

            if (quitButton != null)
                quitButton.onClick.AddListener(ShowQuitConfirm);

            if (confirmQuitYesButton != null)
                confirmQuitYesButton.onClick.AddListener(QuitGame);

            if (confirmQuitNoButton != null)
                confirmQuitNoButton.onClick.AddListener(HideQuitConfirm);
        }

        private void HideAllPanels()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
        }

        private void OnGameStateChanged(Core.GameStateManager.GameState newState)
        {
            if (newState == Core.GameStateManager.GameState.Paused)
            {
                ShowPauseMenu();
            }
            else
            {
                HidePauseMenu();
            }
        }

        #region Pause Menu

        public void ShowPauseMenu()
        {
            isPaused = true;

            // Show main pause panel
            if (pausePanel != null) pausePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);

            // Play sound
            PlaySound(pauseSound);

            onPauseMenuOpened?.Invoke();

            Debug.Log("[PauseMenu] Opened");
        }

        public void HidePauseMenu()
        {
            isPaused = false;
            HideAllPanels();

            onPauseMenuClosed?.Invoke();

            Debug.Log("[PauseMenu] Closed");
        }

        #endregion

        #region Button Actions

        public void Resume()
        {
            PlaySound(buttonClickSound);
            PlaySound(resumeSound);

            if (gameState != null)
            {
                gameState.ResumeGame();
            }
            else
            {
                // Fallback
                Time.timeScale = 1f;
                HidePauseMenu();
            }
        }

        public void OpenSettings()
        {
            PlaySound(buttonClickSound);

            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);

            onSettingsOpened?.Invoke();
        }

        public void CloseSettings()
        {
            PlaySound(buttonClickSound);

            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void RestartGame()
        {
            PlaySound(buttonClickSound);

            if (gameState != null)
            {
                gameState.RestartGame();
            }
            else
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
                );
            }
        }

        public void ReturnToMainMenu()
        {
            PlaySound(buttonClickSound);

            if (gameState != null)
            {
                gameState.ReturnToMainMenu();
            }
            else
            {
                Time.timeScale = 1f;
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
        }

        public void ShowQuitConfirm()
        {
            PlaySound(buttonClickSound);

            if (pausePanel != null) pausePanel.SetActive(false);
            if (confirmQuitPanel != null) confirmQuitPanel.SetActive(true);
        }

        public void HideQuitConfirm()
        {
            PlaySound(buttonClickSound);

            if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void QuitGame()
        {
            PlaySound(buttonClickSound);

            if (gameState != null)
            {
                gameState.QuitGame();
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        #endregion

        #region Display Update

        private void UpdateDisplay()
        {
            if (!isPaused) return;

            // Update title
            if (titleText != null)
            {
                titleText.text = "PAUSED";
            }

            // Update night (for 7 Night Runs)
            if (nightText != null && gameState != null)
            {
                if (gameState.isSevenNightRun)
                {
                    nightText.text = $"Night {gameState.currentNight} / 7";
                    nightText.gameObject.SetActive(true);
                }
                else
                {
                    nightText.gameObject.SetActive(false);
                }
            }

            // Update money
            if (moneyText != null)
            {
                var inventory = FindObjectOfType<Player.PlayerInventory>();
                if (inventory != null)
                {
                    moneyText.text = $"${inventory.Money:N0}";
                }
            }

            // Update play time
            if (playTimeText != null)
            {
                int hours = Mathf.FloorToInt(playTime / 3600);
                int minutes = Mathf.FloorToInt((playTime % 3600) / 60);
                int seconds = Mathf.FloorToInt(playTime % 60);

                if (hours > 0)
                {
                    playTimeText.text = $"Time: {hours}:{minutes:D2}:{seconds:D2}";
                }
                else
                {
                    playTimeText.text = $"Time: {minutes}:{seconds:D2}";
                }
            }
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, soundVolume);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Toggle pause state (call from input system if needed)
        /// </summary>
        public void TogglePause()
        {
            if (gameState == null) return;

            if (gameState.IsPaused)
            {
                Resume();
            }
            else if (gameState.IsPlaying || (canPauseDuringIntro && gameState.currentState == Core.GameStateManager.GameState.Intro))
            {
                gameState.PauseGame();
            }
        }

        /// <summary>
        /// Get total play time in seconds
        /// </summary>
        public float GetPlayTime() => playTime;

        /// <summary>
        /// Reset play time (call when starting new game)
        /// </summary>
        public void ResetPlayTime()
        {
            playTime = 0f;
        }

        #endregion
    }
}
