using UnityEngine;
using UnityEngine.Events;
using BobsPetroleum.Core;
using BobsPetroleum.AI;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Day/night cycle system for the 7-day adventure.
    /// Controls lighting, spawners, and day progression.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Instance { get; private set; }

        [Header("Time Settings")]
        [Tooltip("Duration of a full day in real seconds")]
        public float dayDurationSeconds = 600f; // 10 minutes

        [Tooltip("Percentage of day that is daytime (0-1)")]
        [Range(0f, 1f)]
        public float dayTimeRatio = 0.6f;

        [Tooltip("Current time (0-1, 0=midnight, 0.5=noon)")]
        [Range(0f, 1f)]
        public float currentTime = 0.25f;

        [Header("Lighting")]
        [Tooltip("Directional light (sun)")]
        public Light sunLight;

        [Tooltip("Sun rotation at noon")]
        public float noonRotation = 90f;

        [Tooltip("Sun color during day")]
        public Color dayColor = Color.white;

        [Tooltip("Sun color at sunrise/sunset")]
        public Color sunsetColor = new Color(1f, 0.5f, 0.2f);

        [Tooltip("Sun color at night")]
        public Color nightColor = new Color(0.1f, 0.1f, 0.3f);

        [Tooltip("Sun intensity during day")]
        public float dayIntensity = 1f;

        [Tooltip("Sun intensity at night")]
        public float nightIntensity = 0.1f;

        [Header("Skybox (Optional)")]
        [Tooltip("Day skybox material")]
        public Material daySkybox;

        [Tooltip("Night skybox material")]
        public Material nightSkybox;

        [Tooltip("Skybox blend speed")]
        public float skyboxBlendSpeed = 1f;

        [Header("Day Phases")]
        [Tooltip("Dawn start time (0-1)")]
        public float dawnStart = 0.2f;

        [Tooltip("Day start time (0-1)")]
        public float dayStart = 0.3f;

        [Tooltip("Dusk start time (0-1)")]
        public float duskStart = 0.7f;

        [Tooltip("Night start time (0-1)")]
        public float nightStart = 0.8f;

        [Header("Ambient Audio")]
        [Tooltip("Use AudioManager for ambient sounds")]
        public bool useAudioManager = true;

        [Tooltip("Day ambient sound (if not using AudioManager)")]
        public AudioClip dayAmbientClip;

        [Tooltip("Night ambient sound (if not using AudioManager)")]
        public AudioClip nightAmbientClip;

        [Tooltip("Dawn transition sound")]
        public AudioClip dawnSound;

        [Tooltip("Dusk transition sound")]
        public AudioClip duskSound;

        [Header("Events")]
        public UnityEvent onDawnStart;
        public UnityEvent onDayStart;
        public UnityEvent onDuskStart;
        public UnityEvent onNightStart;
        public UnityEvent onNewDay;

        public enum TimeOfDay { Night, Dawn, Day, Dusk }
        public TimeOfDay CurrentPhase { get; private set; }

        private TimeOfDay previousPhase;
        private bool isPaused = false;
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && !useAudioManager)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = true;
                audioSource.spatialBlend = 0f; // 2D sound
            }
        }

        private void Start()
        {
            previousPhase = GetTimeOfDay();
            CurrentPhase = previousPhase;

            // Initialize ambient audio based on current phase
            UpdateAmbientAudio(CurrentPhase);
        }

        private void Update()
        {
            if (isPaused) return;
            if (GameManager.Instance != null && !GameManager.Instance.gameStarted) return;

            // Advance time
            float timeStep = Time.deltaTime / dayDurationSeconds;
            currentTime += timeStep;

            // Check for new day
            if (currentTime >= 1f)
            {
                currentTime = 0f;
                OnNewDay();
            }

            // Update lighting
            UpdateLighting();

            // Check for phase changes
            CurrentPhase = GetTimeOfDay();
            if (CurrentPhase != previousPhase)
            {
                OnPhaseChange(CurrentPhase);
                previousPhase = CurrentPhase;
            }
        }

        private void UpdateLighting()
        {
            if (sunLight == null) return;

            // Calculate sun rotation
            float sunAngle = (currentTime - 0.25f) * 360f; // 0.25 = 6AM = sunrise
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

            // Calculate color and intensity based on time
            TimeOfDay phase = GetTimeOfDay();

            switch (phase)
            {
                case TimeOfDay.Night:
                    sunLight.color = nightColor;
                    sunLight.intensity = nightIntensity;
                    break;

                case TimeOfDay.Dawn:
                    float dawnProgress = Mathf.InverseLerp(dawnStart, dayStart, currentTime);
                    sunLight.color = Color.Lerp(nightColor, sunsetColor, dawnProgress);
                    sunLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, dawnProgress);
                    break;

                case TimeOfDay.Day:
                    float dayProgress = Mathf.InverseLerp(dayStart, duskStart, currentTime);
                    // Peak at noon
                    float noonFactor = 1f - Mathf.Abs(dayProgress - 0.5f) * 2f;
                    sunLight.color = Color.Lerp(sunsetColor, dayColor, noonFactor);
                    sunLight.intensity = dayIntensity;
                    break;

                case TimeOfDay.Dusk:
                    float duskProgress = Mathf.InverseLerp(duskStart, nightStart, currentTime);
                    sunLight.color = Color.Lerp(sunsetColor, nightColor, duskProgress);
                    sunLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, duskProgress);
                    break;
            }

            // Update skybox
            if (daySkybox != null && nightSkybox != null)
            {
                bool isNightPhase = phase == TimeOfDay.Night;
                RenderSettings.skybox = isNightPhase ? nightSkybox : daySkybox;
            }
        }

        private TimeOfDay GetTimeOfDay()
        {
            if (currentTime < dawnStart || currentTime >= nightStart)
                return TimeOfDay.Night;
            if (currentTime < dayStart)
                return TimeOfDay.Dawn;
            if (currentTime < duskStart)
                return TimeOfDay.Day;
            return TimeOfDay.Dusk;
        }

        private void OnPhaseChange(TimeOfDay newPhase)
        {
            // Update ambient audio
            UpdateAmbientAudio(newPhase);

            switch (newPhase)
            {
                case TimeOfDay.Dawn:
                    PlayTransitionSound(dawnSound);
                    onDawnStart?.Invoke();
                    break;
                case TimeOfDay.Day:
                    onDayStart?.Invoke();
                    NotifySpawners(true);
                    break;
                case TimeOfDay.Dusk:
                    PlayTransitionSound(duskSound);
                    onDuskStart?.Invoke();
                    break;
                case TimeOfDay.Night:
                    onNightStart?.Invoke();
                    NotifySpawners(false);
                    break;
            }
        }

        private void UpdateAmbientAudio(TimeOfDay phase)
        {
            bool isDay = phase == TimeOfDay.Day || phase == TimeOfDay.Dawn;

            if (useAudioManager)
            {
                if (isDay)
                {
                    AudioManager.Instance?.SetDayAmbient();
                }
                else
                {
                    AudioManager.Instance?.SetNightAmbient();
                }
            }
            else if (audioSource != null)
            {
                // Use local audio source
                AudioClip targetClip = isDay ? dayAmbientClip : nightAmbientClip;
                if (targetClip != null && audioSource.clip != targetClip)
                {
                    audioSource.clip = targetClip;
                    audioSource.Play();
                }
            }
        }

        private void PlayTransitionSound(AudioClip clip)
        {
            if (clip == null) return;

            if (useAudioManager)
            {
                AudioManager.Instance?.PlaySFX2D(clip);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void NotifySpawners(bool isDay)
        {
            ZombieSpawner.Instance?.SetNightMode(!isDay);
            CustomerSpawner.Instance?.SetDayTime(isDay);
        }

        private void OnNewDay()
        {
            onNewDay?.Invoke();
            GameManager.Instance?.AdvanceDay();
        }

        /// <summary>
        /// Set time to a specific value (0-1).
        /// </summary>
        public void SetTime(float time)
        {
            currentTime = Mathf.Clamp01(time);
            UpdateLighting();
        }

        /// <summary>
        /// Skip to dawn.
        /// </summary>
        public void SkipToDawn()
        {
            currentTime = dawnStart;
        }

        /// <summary>
        /// Skip to night.
        /// </summary>
        public void SkipToNight()
        {
            currentTime = nightStart;
        }

        /// <summary>
        /// Pause/resume time progression.
        /// </summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        /// <summary>
        /// Get formatted time string.
        /// </summary>
        public string GetTimeString()
        {
            float hours = currentTime * 24f;
            int hour = Mathf.FloorToInt(hours);
            int minute = Mathf.FloorToInt((hours - hour) * 60f);
            return $"{hour:00}:{minute:00}";
        }

        /// <summary>
        /// Check if it's currently day.
        /// </summary>
        public bool IsDay()
        {
            return CurrentPhase == TimeOfDay.Day || CurrentPhase == TimeOfDay.Dawn;
        }

        /// <summary>
        /// Check if it's currently night.
        /// </summary>
        public bool IsNight()
        {
            return CurrentPhase == TimeOfDay.Night;
        }
    }
}
