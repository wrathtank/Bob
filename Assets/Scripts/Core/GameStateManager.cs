using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// CONTROLS THE ENTIRE GAME STATE - Simple and clear!
    ///
    /// States:
    /// - MainMenu: At the main menu
    /// - Loading: Loading the game
    /// - Intro: Clone spawn intro sequence
    /// - Playing: Actually playing
    /// - Paused: Game paused
    /// - GameOver: Bob died or player failed
    /// - Victory: Bob was revived!
    ///
    /// SETUP:
    /// 1. Add this to a "GameManager" object in your game scene
    /// 2. It auto-connects to other systems
    /// 3. Done!
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("=== CURRENT STATE ===")]
        [Tooltip("Current game state (read-only in inspector)")]
        public GameState currentState = GameState.Loading;

        [Tooltip("Previous state (for unpausing)")]
        public GameState previousState = GameState.Loading;

        [Header("=== GAME MODE ===")]
        [Tooltip("Is this a 7 Night Run?")]
        public bool isSevenNightRun = false;

        [Tooltip("Current night (for 7 Night Run)")]
        public int currentNight = 1;

        [Header("=== UI REFERENCES ===")]
        [Tooltip("Pause menu panel")]
        public GameObject pauseMenuPanel;

        [Tooltip("Game over panel")]
        public GameObject gameOverPanel;

        [Tooltip("Victory panel")]
        public GameObject victoryPanel;

        [Tooltip("HUD panel (health, money, etc)")]
        public GameObject hudPanel;

        [Header("=== INPUT ===")]
        [Tooltip("Key to pause game")]
        public KeyCode pauseKey = KeyCode.Escape;

        [Header("=== EVENTS ===")]
        public UnityEvent onGameStarted;
        public UnityEvent onGamePaused;
        public UnityEvent onGameResumed;
        public UnityEvent onGameOver;
        public UnityEvent onVictory;
        public UnityEvent<GameState> onStateChanged;

        public enum GameState
        {
            MainMenu,
            Loading,
            Intro,
            Playing,
            Paused,
            GameOver,
            Victory
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Check game mode from PlayerPrefs (set by main menu)
            isSevenNightRun = PlayerPrefs.GetInt("GameMode", 0) == 1;

            // Start in loading/intro state
            ChangeState(GameState.Intro);
        }

        private void Update()
        {
            // Pause input
            if (Input.GetKeyDown(pauseKey))
            {
                if (currentState == GameState.Playing)
                {
                    PauseGame();
                }
                else if (currentState == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }

        #region State Management

        /// <summary>
        /// Change to a new game state
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (newState == currentState) return;

            previousState = currentState;
            currentState = newState;

            // Handle state-specific logic
            switch (newState)
            {
                case GameState.MainMenu:
                    OnEnterMainMenu();
                    break;
                case GameState.Loading:
                    OnEnterLoading();
                    break;
                case GameState.Intro:
                    OnEnterIntro();
                    break;
                case GameState.Playing:
                    OnEnterPlaying();
                    break;
                case GameState.Paused:
                    OnEnterPaused();
                    break;
                case GameState.GameOver:
                    OnEnterGameOver();
                    break;
                case GameState.Victory:
                    OnEnterVictory();
                    break;
            }

            onStateChanged?.Invoke(newState);
            Debug.Log($"[GameState] Changed to: {newState}");
        }

        private void OnEnterMainMenu()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnEnterLoading()
        {
            // Show loading UI, hide others
            HideAllUI();
        }

        private void OnEnterIntro()
        {
            // Intro sequence plays
            Time.timeScale = 1f;
            HideAllUI();

            // When intro is done, CloneSpawnSystem calls StartPlaying()
        }

        private void OnEnterPlaying()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Show HUD
            HideAllUI();
            if (hudPanel != null) hudPanel.SetActive(true);

            onGameStarted?.Invoke();
        }

        private void OnEnterPaused()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Show pause menu
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

            onGamePaused?.Invoke();
        }

        private void OnEnterGameOver()
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Show game over
            HideAllUI();
            if (gameOverPanel != null) gameOverPanel.SetActive(true);

            onGameOver?.Invoke();
        }

        private void OnEnterVictory()
        {
            Time.timeScale = 1f; // Keep time running for celebration
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Show victory
            HideAllUI();
            if (victoryPanel != null) victoryPanel.SetActive(true);

            onVictory?.Invoke();
        }

        private void HideAllUI()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
        }

        #endregion

        #region Public API - Call these from buttons/events!

        /// <summary>
        /// Call when intro is done to start playing
        /// </summary>
        public void StartPlaying()
        {
            ChangeState(GameState.Playing);
        }

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
            }
        }

        /// <summary>
        /// Resume from pause
        /// </summary>
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

                ChangeState(GameState.Playing);
                onGameResumed?.Invoke();
            }
        }

        /// <summary>
        /// Trigger game over (Bob died)
        /// </summary>
        public void TriggerGameOver()
        {
            ChangeState(GameState.GameOver);
        }

        /// <summary>
        /// Trigger victory (Bob revived)
        /// </summary>
        public void TriggerVictory()
        {
            ChangeState(GameState.Victory);
        }

        /// <summary>
        /// Restart the current game
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Quit the application
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Properties

        public bool IsPlaying => currentState == GameState.Playing;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsGameOver => currentState == GameState.GameOver;
        public bool IsVictory => currentState == GameState.Victory;

        #endregion
    }
}
