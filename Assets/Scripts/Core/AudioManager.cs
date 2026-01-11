using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// Central audio manager for music, ambient sounds, and spatial audio.
    /// Handles background music, ambient loops, and provides audio utilities.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [Tooltip("Main audio mixer (optional)")]
        public AudioMixer audioMixer;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;
        [Range(0f, 1f)]
        public float sfxVolume = 1f;
        [Range(0f, 1f)]
        public float ambientVolume = 0.5f;

        [Header("Music")]
        [Tooltip("Background music tracks")]
        public List<MusicTrack> musicTracks = new List<MusicTrack>();

        [Tooltip("Current music audio source")]
        public AudioSource musicSource;

        [Tooltip("Secondary music source for crossfading")]
        public AudioSource musicSourceB;

        [Tooltip("Music crossfade duration")]
        public float musicCrossfadeDuration = 2f;

        [Header("Ambient Audio")]
        [Tooltip("Ambient sound loops")]
        public List<AmbientLoop> ambientLoops = new List<AmbientLoop>();

        [Tooltip("Day ambient sounds")]
        public AudioClip dayAmbient;

        [Tooltip("Night ambient sounds")]
        public AudioClip nightAmbient;

        [Tooltip("Ambient audio source")]
        public AudioSource ambientSource;

        [Header("UI Sounds")]
        public AudioClip buttonClickSound;
        public AudioClip buttonHoverSound;
        public AudioClip purchaseSound;
        public AudioClip errorSound;
        public AudioClip successSound;
        public AudioClip notificationSound;

        [Header("Game Event Sounds")]
        public AudioClip dayChangeSound;
        public AudioClip gameStartSound;
        public AudioClip victorySound;
        public AudioClip gameOverSound;
        public AudioClip hamburgerFedSound;

        [Header("Settings")]
        [Tooltip("Persist across scenes")]
        public bool dontDestroyOnLoad = true;

        // Internal state
        private AudioSource currentMusicSource;
        private AudioSource nextMusicSource;
        private bool isCrossfading = false;
        private float crossfadeTimer = 0f;
        private Dictionary<string, AudioSource> activeSounds = new Dictionary<string, AudioSource>();
        private AudioSource uiAudioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                }
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // Create music sources if not assigned
            if (musicSource == null)
            {
                musicSource = CreateAudioSource("MusicSource");
                musicSource.loop = true;
            }

            if (musicSourceB == null)
            {
                musicSourceB = CreateAudioSource("MusicSourceB");
                musicSourceB.loop = true;
            }

            // Create ambient source if not assigned
            if (ambientSource == null)
            {
                ambientSource = CreateAudioSource("AmbientSource");
                ambientSource.loop = true;
            }

            // Create UI audio source
            uiAudioSource = CreateAudioSource("UIAudioSource");
            uiAudioSource.spatialBlend = 0f; // 2D sound

            currentMusicSource = musicSource;
            nextMusicSource = musicSourceB;

            ApplyVolumeSettings();
        }

        private AudioSource CreateAudioSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private void Update()
        {
            // Handle crossfading
            if (isCrossfading)
            {
                crossfadeTimer += Time.deltaTime;
                float t = crossfadeTimer / musicCrossfadeDuration;

                currentMusicSource.volume = Mathf.Lerp(musicVolume * masterVolume, 0f, t);
                nextMusicSource.volume = Mathf.Lerp(0f, musicVolume * masterVolume, t);

                if (t >= 1f)
                {
                    currentMusicSource.Stop();
                    var temp = currentMusicSource;
                    currentMusicSource = nextMusicSource;
                    nextMusicSource = temp;
                    isCrossfading = false;
                }
            }
        }

        #region Volume Control

        /// <summary>
        /// Set master volume (0-1).
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        /// <summary>
        /// Set music volume (0-1).
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        /// <summary>
        /// Set SFX volume (0-1).
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        /// <summary>
        /// Set ambient volume (0-1).
        /// </summary>
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            SaveVolumeSettings();
        }

        private void ApplyVolumeSettings()
        {
            if (currentMusicSource != null && !isCrossfading)
            {
                currentMusicSource.volume = musicVolume * masterVolume;
            }

            if (ambientSource != null)
            {
                ambientSource.volume = ambientVolume * masterVolume;
            }

            // Apply to mixer if available
            if (audioMixer != null)
            {
                audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(masterVolume, 0.0001f)) * 20f);
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(musicVolume, 0.0001f)) * 20f);
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(sfxVolume, 0.0001f)) * 20f);
                audioMixer.SetFloat("AmbientVolume", Mathf.Log10(Mathf.Max(ambientVolume, 0.0001f)) * 20f);
            }
        }

        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
        }

        /// <summary>
        /// Load saved volume settings.
        /// </summary>
        public void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.5f);
            ApplyVolumeSettings();
        }

        #endregion

        #region Music

        /// <summary>
        /// Play music track by name.
        /// </summary>
        public void PlayMusic(string trackName, bool crossfade = true)
        {
            var track = musicTracks.Find(t => t.trackName == trackName);
            if (track != null && track.clip != null)
            {
                PlayMusic(track.clip, crossfade);
            }
        }

        /// <summary>
        /// Play music clip directly.
        /// </summary>
        public void PlayMusic(AudioClip clip, bool crossfade = true)
        {
            if (clip == null) return;

            if (crossfade && currentMusicSource.isPlaying)
            {
                nextMusicSource.clip = clip;
                nextMusicSource.volume = 0f;
                nextMusicSource.Play();
                isCrossfading = true;
                crossfadeTimer = 0f;
            }
            else
            {
                currentMusicSource.clip = clip;
                currentMusicSource.volume = musicVolume * masterVolume;
                currentMusicSource.Play();
            }
        }

        /// <summary>
        /// Stop music.
        /// </summary>
        public void StopMusic(bool fadeOut = true)
        {
            if (fadeOut)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                currentMusicSource.Stop();
            }
        }

        private System.Collections.IEnumerator FadeOutMusic()
        {
            float startVolume = currentMusicSource.volume;
            float timer = 0f;

            while (timer < musicCrossfadeDuration)
            {
                timer += Time.deltaTime;
                currentMusicSource.volume = Mathf.Lerp(startVolume, 0f, timer / musicCrossfadeDuration);
                yield return null;
            }

            currentMusicSource.Stop();
            currentMusicSource.volume = startVolume;
        }

        /// <summary>
        /// Pause/unpause music.
        /// </summary>
        public void PauseMusic(bool pause)
        {
            if (pause)
            {
                currentMusicSource.Pause();
            }
            else
            {
                currentMusicSource.UnPause();
            }
        }

        #endregion

        #region Ambient

        /// <summary>
        /// Play ambient loop by name.
        /// </summary>
        public void PlayAmbient(string ambientName)
        {
            var ambient = ambientLoops.Find(a => a.name == ambientName);
            if (ambient != null && ambient.clip != null)
            {
                PlayAmbient(ambient.clip);
            }
        }

        /// <summary>
        /// Play ambient clip.
        /// </summary>
        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null || ambientSource == null) return;

            ambientSource.clip = clip;
            ambientSource.volume = ambientVolume * masterVolume;
            ambientSource.Play();
        }

        /// <summary>
        /// Switch to day ambient sounds.
        /// </summary>
        public void SetDayAmbient()
        {
            if (dayAmbient != null)
            {
                PlayAmbient(dayAmbient);
            }
        }

        /// <summary>
        /// Switch to night ambient sounds.
        /// </summary>
        public void SetNightAmbient()
        {
            if (nightAmbient != null)
            {
                PlayAmbient(nightAmbient);
            }
        }

        /// <summary>
        /// Stop ambient audio.
        /// </summary>
        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }

        #endregion

        #region SFX

        /// <summary>
        /// Play a one-shot sound effect at a position.
        /// </summary>
        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume * masterVolume);
        }

        /// <summary>
        /// Play a 2D sound effect (UI, etc).
        /// </summary>
        public void PlaySFX2D(AudioClip clip, float volume = 1f)
        {
            if (clip == null || uiAudioSource == null) return;
            uiAudioSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
        }

        /// <summary>
        /// Play a looping sound and return an ID to stop it later.
        /// </summary>
        public string PlayLoopingSFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return null;

            string id = System.Guid.NewGuid().ToString();
            var source = CreateAudioSource($"Loop_{id}");
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.loop = true;
            source.spatialBlend = 1f;
            source.Play();

            activeSounds[id] = source;
            return id;
        }

        /// <summary>
        /// Stop a looping sound by ID.
        /// </summary>
        public void StopLoopingSFX(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (activeSounds.TryGetValue(id, out AudioSource source))
            {
                source.Stop();
                Destroy(source.gameObject);
                activeSounds.Remove(id);
            }
        }

        #endregion

        #region UI Sounds

        public void PlayButtonClick()
        {
            PlaySFX2D(buttonClickSound);
        }

        public void PlayButtonHover()
        {
            PlaySFX2D(buttonHoverSound, 0.5f);
        }

        public void PlayPurchase()
        {
            PlaySFX2D(purchaseSound);
        }

        public void PlayError()
        {
            PlaySFX2D(errorSound);
        }

        public void PlaySuccess()
        {
            PlaySFX2D(successSound);
        }

        public void PlayNotification()
        {
            PlaySFX2D(notificationSound);
        }

        #endregion

        #region Game Event Sounds

        public void PlayDayChange()
        {
            PlaySFX2D(dayChangeSound);
        }

        public void PlayGameStart()
        {
            PlaySFX2D(gameStartSound);
        }

        public void PlayVictory()
        {
            PlaySFX2D(victorySound);
        }

        public void PlayGameOver()
        {
            PlaySFX2D(gameOverSound);
        }

        public void PlayHamburgerFed()
        {
            PlaySFX2D(hamburgerFedSound);
        }

        #endregion
    }

    [System.Serializable]
    public class MusicTrack
    {
        public string trackName;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volumeMultiplier = 1f;
    }

    [System.Serializable]
    public class AmbientLoop
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volumeMultiplier = 1f;
    }
}
