using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// POWER SYSTEM - Control all lights for horror!
    /// Flicker lights, blackouts, and spooky effects.
    ///
    /// SETUP:
    /// 1. Add PowerSystem to scene
    /// 2. Assign light groups
    /// 3. Use for horror events!
    ///
    /// FEATURES:
    /// - Power outages
    /// - Light flickering
    /// - Individual zone control
    /// - Generator mechanic
    /// </summary>
    public class PowerSystem : MonoBehaviour
    {
        public static PowerSystem Instance { get; private set; }

        [System.Serializable]
        public class LightZone
        {
            public string zoneName = "Zone";
            public List<Light> lights = new List<Light>();
            public List<GameObject> emissiveObjects = new List<GameObject>();
            public bool isPowered = true;
            [HideInInspector] public Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();
        }

        [Header("=== POWER STATE ===")]
        [Tooltip("Is main power on?")]
        public bool mainPowerOn = true;

        [Tooltip("Generator fuel (0-100)")]
        [Range(0, 100)]
        public float generatorFuel = 100f;

        [Tooltip("Fuel consumption per second when on backup")]
        public float fuelConsumption = 1f;

        [Header("=== LIGHT ZONES ===")]
        [Tooltip("All light zones in the building")]
        public List<LightZone> lightZones = new List<LightZone>();

        [Header("=== AUTO-FIND ===")]
        [Tooltip("Auto-find all lights in scene")]
        public bool autoFindLights = true;

        [Tooltip("Tag for lights to include")]
        public string lightTag = "PoweredLight";

        [Header("=== FLICKER SETTINGS ===")]
        [Tooltip("Flicker intensity variation")]
        [Range(0f, 1f)]
        public float flickerIntensity = 0.5f;

        [Tooltip("Flicker speed")]
        public float flickerSpeed = 10f;

        [Tooltip("Chance of flicker per second")]
        [Range(0f, 1f)]
        public float flickerChance = 0.1f;

        [Header("=== BLACKOUT SETTINGS ===")]
        [Tooltip("Blackout duration range")]
        public Vector2 blackoutDuration = new Vector2(3f, 10f);

        [Tooltip("Warning flickers before blackout")]
        public int warningFlickers = 3;

        [Header("=== AUDIO ===")]
        public AudioClip powerDownSound;
        public AudioClip powerUpSound;
        public AudioClip flickerSound;
        public AudioClip generatorSound;
        public AudioClip generatorStartSound;

        [Header("=== EVENTS ===")]
        public UnityEvent onPowerLost;
        public UnityEvent onPowerRestored;
        public UnityEvent onBlackoutStart;
        public UnityEvent onBlackoutEnd;
        public UnityEvent onGeneratorEmpty;
        public UnityEvent<LightZone> onZonePowerChanged;

        // Internal
        private AudioSource audioSource;
        private bool isOnBackupPower = false;
        private Coroutine flickerCoroutine;
        private Coroutine blackoutCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (autoFindLights)
            {
                AutoFindLights();
            }

            // Store original intensities
            foreach (var zone in lightZones)
            {
                foreach (var light in zone.lights)
                {
                    if (light != null)
                    {
                        zone.originalIntensities[light] = light.intensity;
                    }
                }
            }
        }

        private void Update()
        {
            // Consume generator fuel when on backup
            if (isOnBackupPower && generatorFuel > 0)
            {
                generatorFuel -= fuelConsumption * Time.deltaTime;

                if (generatorFuel <= 0)
                {
                    generatorFuel = 0;
                    OnGeneratorEmpty();
                }
            }

            // Random flicker chance
            if (mainPowerOn && Random.value < flickerChance * Time.deltaTime)
            {
                RandomFlicker();
            }
        }

        #region Power Control

        /// <summary>
        /// Turn main power on/off.
        /// </summary>
        public void SetMainPower(bool on)
        {
            if (mainPowerOn == on) return;

            mainPowerOn = on;

            if (on)
            {
                RestoreAllPower();
                PlaySound(powerUpSound);
                onPowerRestored?.Invoke();
                isOnBackupPower = false;
            }
            else
            {
                CutAllPower();
                PlaySound(powerDownSound);
                onPowerLost?.Invoke();
            }

            Debug.Log($"[PowerSystem] Main power: {(on ? "ON" : "OFF")}");
        }

        /// <summary>
        /// Toggle main power.
        /// </summary>
        public void ToggleMainPower()
        {
            SetMainPower(!mainPowerOn);
        }

        /// <summary>
        /// Start backup generator.
        /// </summary>
        public void StartGenerator()
        {
            if (generatorFuel <= 0)
            {
                Debug.Log("[PowerSystem] Generator out of fuel!");
                return;
            }

            if (!mainPowerOn)
            {
                isOnBackupPower = true;
                RestoreAllPower();
                PlaySound(generatorStartSound);

                // Loop generator sound
                if (generatorSound != null)
                {
                    audioSource.clip = generatorSound;
                    audioSource.loop = true;
                    audioSource.Play();
                }

                Debug.Log("[PowerSystem] Generator started");
            }
        }

        /// <summary>
        /// Stop backup generator.
        /// </summary>
        public void StopGenerator()
        {
            if (isOnBackupPower)
            {
                isOnBackupPower = false;
                audioSource.Stop();

                if (!mainPowerOn)
                {
                    CutAllPower();
                }

                Debug.Log("[PowerSystem] Generator stopped");
            }
        }

        /// <summary>
        /// Refuel generator.
        /// </summary>
        public void RefuelGenerator(float amount)
        {
            generatorFuel = Mathf.Min(generatorFuel + amount, 100f);
            Debug.Log($"[PowerSystem] Generator fuel: {generatorFuel:F0}%");
        }

        private void OnGeneratorEmpty()
        {
            isOnBackupPower = false;
            audioSource.Stop();

            if (!mainPowerOn)
            {
                CutAllPower();
            }

            onGeneratorEmpty?.Invoke();
            Debug.Log("[PowerSystem] Generator ran out of fuel!");
        }

        #endregion

        #region Zone Control

        /// <summary>
        /// Set power for a specific zone.
        /// </summary>
        public void SetZonePower(string zoneName, bool on)
        {
            var zone = lightZones.Find(z => z.zoneName == zoneName);
            if (zone != null)
            {
                SetZonePower(zone, on);
            }
        }

        /// <summary>
        /// Set power for a zone.
        /// </summary>
        public void SetZonePower(LightZone zone, bool on)
        {
            zone.isPowered = on;

            foreach (var light in zone.lights)
            {
                if (light != null)
                {
                    light.enabled = on && (mainPowerOn || isOnBackupPower);
                    if (on && zone.originalIntensities.ContainsKey(light))
                    {
                        light.intensity = zone.originalIntensities[light];
                    }
                }
            }

            foreach (var obj in zone.emissiveObjects)
            {
                if (obj != null)
                {
                    var renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        // Toggle emission
                        foreach (var mat in renderer.materials)
                        {
                            if (on)
                                mat.EnableKeyword("_EMISSION");
                            else
                                mat.DisableKeyword("_EMISSION");
                        }
                    }
                }
            }

            onZonePowerChanged?.Invoke(zone);
        }

        private void CutAllPower()
        {
            foreach (var zone in lightZones)
            {
                foreach (var light in zone.lights)
                {
                    if (light != null) light.enabled = false;
                }

                foreach (var obj in zone.emissiveObjects)
                {
                    if (obj != null)
                    {
                        var renderer = obj.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            foreach (var mat in renderer.materials)
                            {
                                mat.DisableKeyword("_EMISSION");
                            }
                        }
                    }
                }
            }
        }

        private void RestoreAllPower()
        {
            foreach (var zone in lightZones)
            {
                if (zone.isPowered)
                {
                    SetZonePower(zone, true);
                }
            }
        }

        #endregion

        #region Flicker Effects

        /// <summary>
        /// Flicker all lights.
        /// </summary>
        public void FlickerAllLights(float duration = 0.5f)
        {
            if (flickerCoroutine != null)
            {
                StopCoroutine(flickerCoroutine);
            }
            flickerCoroutine = StartCoroutine(FlickerCoroutine(null, duration));
        }

        /// <summary>
        /// Flicker a specific zone.
        /// </summary>
        public void FlickerZone(string zoneName, float duration = 0.5f)
        {
            var zone = lightZones.Find(z => z.zoneName == zoneName);
            if (zone != null)
            {
                StartCoroutine(FlickerCoroutine(zone, duration));
            }
        }

        /// <summary>
        /// Random single flicker.
        /// </summary>
        public void RandomFlicker()
        {
            if (lightZones.Count > 0)
            {
                var zone = lightZones[Random.Range(0, lightZones.Count)];
                StartCoroutine(QuickFlicker(zone));
            }
        }

        private IEnumerator FlickerCoroutine(LightZone zone, float duration)
        {
            List<Light> lights = new List<Light>();

            if (zone != null)
            {
                lights.AddRange(zone.lights);
            }
            else
            {
                foreach (var z in lightZones)
                {
                    lights.AddRange(z.lights);
                }
            }

            PlaySound(flickerSound);

            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;

                foreach (var light in lights)
                {
                    if (light != null && light.enabled)
                    {
                        float originalIntensity = 1f;
                        var ownerZone = lightZones.Find(z => z.lights.Contains(light));
                        if (ownerZone != null && ownerZone.originalIntensities.ContainsKey(light))
                        {
                            originalIntensity = ownerZone.originalIntensities[light];
                        }

                        // Random intensity
                        float flicker = Random.Range(1f - flickerIntensity, 1f);
                        light.intensity = originalIntensity * flicker;
                    }
                }

                yield return new WaitForSeconds(1f / flickerSpeed);
            }

            // Restore
            foreach (var light in lights)
            {
                if (light != null)
                {
                    var ownerZone = lightZones.Find(z => z.lights.Contains(light));
                    if (ownerZone != null && ownerZone.originalIntensities.ContainsKey(light))
                    {
                        light.intensity = ownerZone.originalIntensities[light];
                    }
                }
            }

            flickerCoroutine = null;
        }

        private IEnumerator QuickFlicker(LightZone zone)
        {
            foreach (var light in zone.lights)
            {
                if (light != null) light.enabled = false;
            }

            yield return new WaitForSeconds(0.05f);

            foreach (var light in zone.lights)
            {
                if (light != null && zone.isPowered && (mainPowerOn || isOnBackupPower))
                {
                    light.enabled = true;
                }
            }
        }

        #endregion

        #region Blackout

        /// <summary>
        /// Trigger a blackout (horror event).
        /// </summary>
        public void TriggerBlackout()
        {
            if (blackoutCoroutine != null)
            {
                StopCoroutine(blackoutCoroutine);
            }
            blackoutCoroutine = StartCoroutine(BlackoutCoroutine());
        }

        private IEnumerator BlackoutCoroutine()
        {
            // Warning flickers
            for (int i = 0; i < warningFlickers; i++)
            {
                FlickerAllLights(0.3f);
                yield return new WaitForSeconds(0.5f);
            }

            // Blackout!
            SetMainPower(false);
            onBlackoutStart?.Invoke();

            // Wait
            float duration = Random.Range(blackoutDuration.x, blackoutDuration.y);
            yield return new WaitForSeconds(duration);

            // Power back
            SetMainPower(true);
            onBlackoutEnd?.Invoke();

            blackoutCoroutine = null;
        }

        /// <summary>
        /// Cancel blackout.
        /// </summary>
        public void CancelBlackout()
        {
            if (blackoutCoroutine != null)
            {
                StopCoroutine(blackoutCoroutine);
                blackoutCoroutine = null;
                SetMainPower(true);
            }
        }

        #endregion

        #region Helpers

        private void AutoFindLights()
        {
            if (lightZones.Count == 0)
            {
                // Create default zone with all lights
                var defaultZone = new LightZone { zoneName = "Main" };

                if (!string.IsNullOrEmpty(lightTag))
                {
                    var taggedObjects = GameObject.FindGameObjectsWithTag(lightTag);
                    foreach (var obj in taggedObjects)
                    {
                        var light = obj.GetComponent<Light>();
                        if (light != null)
                        {
                            defaultZone.lights.Add(light);
                        }
                    }
                }

                // Also find all lights if none tagged
                if (defaultZone.lights.Count == 0)
                {
                    var allLights = FindObjectsOfType<Light>();
                    foreach (var light in allLights)
                    {
                        if (light.type != LightType.Directional) // Skip sun
                        {
                            defaultZone.lights.Add(light);
                        }
                    }
                }

                if (defaultZone.lights.Count > 0)
                {
                    lightZones.Add(defaultZone);
                }

                Debug.Log($"[PowerSystem] Auto-found {defaultZone.lights.Count} lights");
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion
    }
}
