using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// COMPLETE SETTINGS MENU - Audio, Graphics, Controls!
    /// All settings auto-save and auto-load.
    ///
    /// SETUP:
    /// 1. Create sliders for volume
    /// 2. Create dropdowns for quality/resolution
    /// 3. Drag them into the slots
    /// 4. Done! Settings persist automatically.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("=== AUDIO SETTINGS ===")]
        [Tooltip("Master volume slider (0-1)")]
        public Slider masterVolumeSlider;

        [Tooltip("Music volume slider (0-1)")]
        public Slider musicVolumeSlider;

        [Tooltip("SFX volume slider (0-1)")]
        public Slider sfxVolumeSlider;

        [Tooltip("Audio mixer (optional - for advanced control)")]
        public AudioMixer audioMixer;

        [Header("=== GRAPHICS SETTINGS ===")]
        [Tooltip("Quality dropdown")]
        public TMP_Dropdown qualityDropdown;

        [Tooltip("Resolution dropdown")]
        public TMP_Dropdown resolutionDropdown;

        [Tooltip("Fullscreen toggle")]
        public Toggle fullscreenToggle;

        [Tooltip("VSync toggle")]
        public Toggle vsyncToggle;

        [Header("=== GAMEPLAY SETTINGS ===")]
        [Tooltip("Mouse sensitivity slider")]
        public Slider sensitivitySlider;

        [Tooltip("Invert Y axis toggle")]
        public Toggle invertYToggle;

        [Tooltip("Show FPS toggle")]
        public Toggle showFPSToggle;

        [Header("=== DISPLAY ===")]
        [Tooltip("Text showing current master volume")]
        public TMP_Text masterVolumeText;

        [Tooltip("Text showing current music volume")]
        public TMP_Text musicVolumeText;

        [Tooltip("Text showing current SFX volume")]
        public TMP_Text sfxVolumeText;

        [Tooltip("Text showing current sensitivity")]
        public TMP_Text sensitivityText;

        [Header("=== BUTTONS ===")]
        [Tooltip("Apply button")]
        public Button applyButton;

        [Tooltip("Reset to defaults button")]
        public Button resetButton;

        // Current settings
        private float masterVolume = 1f;
        private float musicVolume = 0.7f;
        private float sfxVolume = 1f;
        private int qualityLevel = 2;
        private int resolutionIndex = 0;
        private bool isFullscreen = true;
        private bool vsyncEnabled = true;
        private float mouseSensitivity = 2f;
        private bool invertY = false;
        private bool showFPS = false;

        private Resolution[] resolutions;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Get available resolutions
            SetupResolutions();

            // Setup quality dropdown
            SetupQualityDropdown();

            // Load saved settings
            LoadSettings();

            // Wire up UI
            WireUI();

            // Apply loaded settings to UI
            ApplySettingsToUI();
        }

        #region Setup

        private void SetupResolutions()
        {
            resolutions = Screen.resolutions;

            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                List<string> options = new List<string>();

                int currentResIndex = 0;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    string option = $"{resolutions[i].width} x {resolutions[i].height} @ {resolutions[i].refreshRate}Hz";
                    options.Add(option);

                    if (resolutions[i].width == Screen.currentResolution.width &&
                        resolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResIndex = i;
                    }
                }

                resolutionDropdown.AddOptions(options);
                resolutionIndex = currentResIndex;
            }
        }

        private void SetupQualityDropdown()
        {
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
                qualityLevel = QualitySettings.GetQualityLevel();
            }
        }

        private void WireUI()
        {
            // Volume sliders
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            // Graphics
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);

            // Gameplay
            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

            if (invertYToggle != null)
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);

            if (showFPSToggle != null)
                showFPSToggle.onValueChanged.AddListener(OnShowFPSChanged);

            // Buttons
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);

            if (resetButton != null)
                resetButton.onClick.AddListener(ResetToDefaults);
        }

        private void ApplySettingsToUI()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.value = masterVolume;
            if (musicVolumeSlider != null) musicVolumeSlider.value = musicVolume;
            if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVolume;
            if (qualityDropdown != null) qualityDropdown.value = qualityLevel;
            if (resolutionDropdown != null) resolutionDropdown.value = resolutionIndex;
            if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;
            if (vsyncToggle != null) vsyncToggle.isOn = vsyncEnabled;
            if (sensitivitySlider != null) sensitivitySlider.value = mouseSensitivity;
            if (invertYToggle != null) invertYToggle.isOn = invertY;
            if (showFPSToggle != null) showFPSToggle.isOn = showFPS;

            UpdateVolumeTexts();
        }

        #endregion

        #region UI Callbacks

        private void OnMasterVolumeChanged(float value)
        {
            masterVolume = value;
            ApplyMasterVolume();
            UpdateVolumeTexts();
        }

        private void OnMusicVolumeChanged(float value)
        {
            musicVolume = value;
            ApplyMusicVolume();
            UpdateVolumeTexts();
        }

        private void OnSFXVolumeChanged(float value)
        {
            sfxVolume = value;
            ApplySFXVolume();
            UpdateVolumeTexts();
        }

        private void OnQualityChanged(int index)
        {
            qualityLevel = index;
        }

        private void OnResolutionChanged(int index)
        {
            resolutionIndex = index;
        }

        private void OnFullscreenChanged(bool value)
        {
            isFullscreen = value;
        }

        private void OnVSyncChanged(bool value)
        {
            vsyncEnabled = value;
        }

        private void OnSensitivityChanged(float value)
        {
            mouseSensitivity = value;
            if (sensitivityText != null)
                sensitivityText.text = $"{value:F1}";
        }

        private void OnInvertYChanged(bool value)
        {
            invertY = value;
        }

        private void OnShowFPSChanged(bool value)
        {
            showFPS = value;
        }

        private void UpdateVolumeTexts()
        {
            if (masterVolumeText != null)
                masterVolumeText.text = $"{Mathf.RoundToInt(masterVolume * 100)}%";
            if (musicVolumeText != null)
                musicVolumeText.text = $"{Mathf.RoundToInt(musicVolume * 100)}%";
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{Mathf.RoundToInt(sfxVolume * 100)}%";
        }

        #endregion

        #region Apply Settings

        public void ApplySettings()
        {
            // Apply graphics
            QualitySettings.SetQualityLevel(qualityLevel);

            if (resolutions != null && resolutionIndex < resolutions.Length)
            {
                Resolution res = resolutions[resolutionIndex];
                Screen.SetResolution(res.width, res.height, isFullscreen);
            }

            QualitySettings.vSyncCount = vsyncEnabled ? 1 : 0;

            // Apply audio
            ApplyMasterVolume();
            ApplyMusicVolume();
            ApplySFXVolume();

            // Save settings
            SaveSettings();

            Debug.Log("[Settings] Settings applied and saved!");
        }

        private void ApplyMasterVolume()
        {
            AudioListener.volume = masterVolume;

            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(0.0001f, masterVolume)) * 20);
            }
        }

        private void ApplyMusicVolume()
        {
            if (audioMixer != null)
            {
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, musicVolume)) * 20);
            }

            // Also notify AudioManager if it exists
            var audioManager = FindObjectOfType<Audio.AudioManager>();
            if (audioManager != null)
            {
                audioManager.SetMusicVolume(musicVolume);
            }
        }

        private void ApplySFXVolume()
        {
            if (audioMixer != null)
            {
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfxVolume)) * 20);
            }

            var audioManager = FindObjectOfType<Audio.AudioManager>();
            if (audioManager != null)
            {
                audioManager.SetSFXVolume(sfxVolume);
            }
        }

        public void ResetToDefaults()
        {
            masterVolume = 1f;
            musicVolume = 0.7f;
            sfxVolume = 1f;
            qualityLevel = 2;
            isFullscreen = true;
            vsyncEnabled = true;
            mouseSensitivity = 2f;
            invertY = false;
            showFPS = false;

            ApplySettingsToUI();
            ApplySettings();

            Debug.Log("[Settings] Reset to defaults!");
        }

        #endregion

        #region Save/Load

        public void SaveSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("QualityLevel", qualityLevel);
            PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.SetInt("VSync", vsyncEnabled ? 1 : 0);
            PlayerPrefs.SetFloat("Sensitivity", mouseSensitivity);
            PlayerPrefs.SetInt("InvertY", invertY ? 1 : 0);
            PlayerPrefs.SetInt("ShowFPS", showFPS ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            qualityLevel = PlayerPrefs.GetInt("QualityLevel", 2);
            resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
            isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
            vsyncEnabled = PlayerPrefs.GetInt("VSync", 1) == 1;
            mouseSensitivity = PlayerPrefs.GetFloat("Sensitivity", 2f);
            invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
            showFPS = PlayerPrefs.GetInt("ShowFPS", 0) == 1;

            // Apply loaded settings
            ApplyMasterVolume();
            ApplyMusicVolume();
            ApplySFXVolume();
        }

        #endregion

        #region Public Getters (for other scripts)

        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        public float GetSensitivity() => mouseSensitivity;
        public bool GetInvertY() => invertY;
        public bool GetShowFPS() => showFPS;

        #endregion
    }
}
