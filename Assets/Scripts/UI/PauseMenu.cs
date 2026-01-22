using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using BobsPetroleum.Core;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Pause menu with settings, controls display, and game options.
    /// Essential for any S-tier game.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        [Header("Panels")]
        [Tooltip("Main pause menu panel")]
        public GameObject pausePanel;

        [Tooltip("Settings panel")]
        public GameObject settingsPanel;

        [Tooltip("Controls panel")]
        public GameObject controlsPanel;

        [Tooltip("Confirm quit panel")]
        public GameObject confirmQuitPanel;

        [Header("Input")]
        [Tooltip("Key to toggle pause")]
        public KeyCode pauseKey = KeyCode.Escape;

        [Tooltip("Alternative pause key")]
        public KeyCode altPauseKey = KeyCode.P;

        [Header("Audio Sliders")]
        public Slider masterVolumeSlider;
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public Slider ambientVolumeSlider;

        [Header("Volume Labels")]
        public TMP_Text masterVolumeLabel;
        public TMP_Text musicVolumeLabel;
        public TMP_Text sfxVolumeLabel;
        public TMP_Text ambientVolumeLabel;

        [Header("Graphics Settings")]
        public TMP_Dropdown qualityDropdown;
        public TMP_Dropdown resolutionDropdown;
        public Toggle fullscreenToggle;
        public Toggle vsyncToggle;

        [Header("Gameplay Settings")]
        public Slider mouseSensitivitySlider;
        public TMP_Text mouseSensitivityLabel;
        public Toggle invertYToggle;
        public Toggle headBobToggle;
        public Toggle screenShakeToggle;

        [Header("Audio")]
        public AudioClip pauseSound;
        public AudioClip unpauseSound;
        public AudioClip buttonClickSound;

        [Header("Events")]
        public UnityEvent onPause;
        public UnityEvent onResume;
        public UnityEvent onQuitToMenu;

        private bool isPaused = false;
        private AudioSource audioSource;
        private Resolution[] availableResolutions;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            // Hide all panels initially
            HideAllPanels();

            // Initialize settings UI
            InitializeAudioSettings();
            InitializeGraphicsSettings();
            InitializeGameplaySettings();

            // Load saved settings
            LoadSettings();
        }

        private void Update()
        {
            if (Input.GetKeyDown(pauseKey) || Input.GetKeyDown(altPauseKey))
            {
                TogglePause();
            }
        }

        #region Pause Control

        public void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (isPaused) return;

            isPaused = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // Show pause panel
            HideAllPanels();
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Play sound
            PlaySound(pauseSound);

            onPause?.Invoke();
        }

        public void Resume()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = previousTimeScale;

            // Hide all panels
            HideAllPanels();

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Play sound
            PlaySound(unpauseSound);

            // Save settings
            SaveSettings();

            onResume?.Invoke();
        }

        #endregion

        #region Panel Navigation

        public void ShowSettings()
        {
            HideAllPanels();
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
            PlayButtonClick();
        }

        public void ShowControls()
        {
            HideAllPanels();
            if (controlsPanel != null)
            {
                controlsPanel.SetActive(true);
            }
            PlayButtonClick();
        }

        public void ShowConfirmQuit()
        {
            HideAllPanels();
            if (confirmQuitPanel != null)
            {
                confirmQuitPanel.SetActive(true);
            }
            PlayButtonClick();
        }

        public void BackToPauseMenu()
        {
            HideAllPanels();
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }
            PlayButtonClick();
        }

        private void HideAllPanels()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            if (confirmQuitPanel != null) confirmQuitPanel.SetActive(false);
        }

        #endregion

        #region Audio Settings

        private void InitializeAudioSettings()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            if (ambientVolumeSlider != null)
            {
                ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMasterVolume(value);
            UpdateVolumeLabel(masterVolumeLabel, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
            UpdateVolumeLabel(musicVolumeLabel, value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
            UpdateVolumeLabel(sfxVolumeLabel, value);
        }

        private void OnAmbientVolumeChanged(float value)
        {
            AudioManager.Instance?.SetAmbientVolume(value);
            UpdateVolumeLabel(ambientVolumeLabel, value);
        }

        private void UpdateVolumeLabel(TMP_Text label, float value)
        {
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value * 100) + "%";
            }
        }

        #endregion

        #region Graphics Settings

        private void InitializeGraphicsSettings()
        {
            // Quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                qualityDropdown.value = QualitySettings.GetQualityLevel();
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            // Resolution dropdown
            if (resolutionDropdown != null)
            {
                availableResolutions = Screen.resolutions;
                resolutionDropdown.ClearOptions();

                var options = new System.Collections.Generic.List<string>();
                int currentResIndex = 0;

                for (int i = 0; i < availableResolutions.Length; i++)
                {
                    string option = $"{availableResolutions[i].width} x {availableResolutions[i].height}";
                    options.Add(option);

                    if (availableResolutions[i].width == Screen.currentResolution.width &&
                        availableResolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResIndex = i;
                    }
                }

                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = currentResIndex;
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // Fullscreen toggle
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            // VSync toggle
            if (vsyncToggle != null)
            {
                vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            }
        }

        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index);
            PlayButtonClick();
        }

        private void OnResolutionChanged(int index)
        {
            if (availableResolutions != null && index < availableResolutions.Length)
            {
                Resolution res = availableResolutions[index];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            }
            PlayButtonClick();
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayButtonClick();
        }

        private void OnVSyncChanged(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            PlayButtonClick();
        }

        #endregion

        #region Gameplay Settings

        private void InitializeGameplaySettings()
        {
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            }

            if (invertYToggle != null)
            {
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            }

            if (headBobToggle != null)
            {
                headBobToggle.onValueChanged.AddListener(OnHeadBobChanged);
            }

            if (screenShakeToggle != null)
            {
                screenShakeToggle.onValueChanged.AddListener(OnScreenShakeChanged);
            }
        }

        private void OnMouseSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat("MouseSensitivity", value);
            if (mouseSensitivityLabel != null)
            {
                mouseSensitivityLabel.text = value.ToString("F1");
            }

            // Apply to active player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.mouseSensitivity = value;
            }
        }

        private void OnInvertYChanged(bool inverted)
        {
            PlayerPrefs.SetInt("InvertY", inverted ? 1 : 0);
            PlayButtonClick();
        }

        private void OnHeadBobChanged(bool enabled)
        {
            PlayerPrefs.SetInt("HeadBob", enabled ? 1 : 0);

            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.headBobEnabled = enabled;
            }
            PlayButtonClick();
        }

        private void OnScreenShakeChanged(bool enabled)
        {
            PlayerPrefs.SetInt("ScreenShake", enabled ? 1 : 0);
            PlayButtonClick();
        }

        #endregion

        #region Save/Load Settings

        private void SaveSettings()
        {
            // Audio
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider?.value ?? 1f);
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider?.value ?? 0.7f);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider?.value ?? 1f);
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolumeSlider?.value ?? 0.5f);

            // Graphics
            PlayerPrefs.SetInt("QualityLevel", qualityDropdown?.value ?? QualitySettings.GetQualityLevel());
            PlayerPrefs.SetInt("Fullscreen", Screen.fullScreen ? 1 : 0);
            PlayerPrefs.SetInt("VSync", QualitySettings.vSyncCount);

            // Gameplay
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivitySlider?.value ?? 2f);

            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            // Audio
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            }
            if (ambientVolumeSlider != null)
            {
                ambientVolumeSlider.value = PlayerPrefs.GetFloat("AmbientVolume", 0.5f);
            }

            // Graphics
            if (qualityDropdown != null)
            {
                qualityDropdown.value = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
            }
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            }
            if (vsyncToggle != null)
            {
                vsyncToggle.isOn = PlayerPrefs.GetInt("VSync", 1) > 0;
            }

            // Gameplay
            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
            }
            if (invertYToggle != null)
            {
                invertYToggle.isOn = PlayerPrefs.GetInt("InvertY", 0) == 1;
            }
            if (headBobToggle != null)
            {
                headBobToggle.isOn = PlayerPrefs.GetInt("HeadBob", 1) == 1;
            }
            if (screenShakeToggle != null)
            {
                screenShakeToggle.isOn = PlayerPrefs.GetInt("ScreenShake", 1) == 1;
            }
        }

        #endregion

        #region Quit Functions

        public void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            isPaused = false;

            // Leave network game if connected
            BobsNetworkManager.Instance?.LeaveGame();

            // Save settings
            SaveSettings();
            SaveSystem.Instance?.SaveAllData();

            onQuitToMenu?.Invoke();

            // Load main menu scene (scene 0)
            SceneManager.LoadScene(0);
        }

        public void QuitGame()
        {
            SaveSettings();
            SaveSystem.Instance?.SaveAllData();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void CancelQuit()
        {
            BackToPauseMenu();
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void PlayButtonClick()
        {
            if (buttonClickSound != null)
            {
                AudioManager.Instance?.PlaySFX2D(buttonClickSound, 0.5f);
            }
        }

        #endregion

        public bool IsPaused => isPaused;
    }
}
