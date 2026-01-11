using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using BobsPetroleum.Core;
using BobsPetroleum.Player;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Horror events system for random scares, paranormal activity, and creepy moments.
    /// Creates tension and atmosphere like Schedule 1 / horror games.
    /// </summary>
    public class HorrorEventsSystem : MonoBehaviour
    {
        public static HorrorEventsSystem Instance { get; private set; }

        [Header("Event Settings")]
        [Tooltip("Enable horror events")]
        public bool eventsEnabled = true;

        [Tooltip("Only trigger events at night")]
        public bool nightOnly = true;

        [Tooltip("Minimum time between events (seconds)")]
        public float minEventInterval = 60f;

        [Tooltip("Maximum time between events (seconds)")]
        public float maxEventInterval = 300f;

        [Tooltip("Event chance multiplier at night")]
        public float nightMultiplier = 2f;

        [Tooltip("Current horror intensity (0-1, increases over days)")]
        [Range(0f, 1f)]
        public float horrorIntensity = 0.3f;

        [Header("Event Types")]
        public List<HorrorEvent> horrorEvents = new List<HorrorEvent>();

        [Header("Audio Events")]
        [Tooltip("Random creepy ambient sounds")]
        public AudioClip[] creepySounds;

        [Tooltip("Distant screams/moans")]
        public AudioClip[] distantSounds;

        [Tooltip("Whisper sounds")]
        public AudioClip[] whisperSounds;

        [Tooltip("Jump scare stingers")]
        public AudioClip[] jumpScareStingers;

        [Tooltip("Phone ring sound (creepy call)")]
        public AudioClip phoneRingSound;

        [Tooltip("Static/interference sound")]
        public AudioClip staticSound;

        [Header("Visual Effects")]
        [Tooltip("Screen distortion material")]
        public Material distortionMaterial;

        [Tooltip("Flicker all lights in scene")]
        public bool canFlickerLights = true;

        [Tooltip("Shadow figure prefab")]
        public GameObject shadowFigurePrefab;

        [Tooltip("Ghost prefab (appears briefly)")]
        public GameObject ghostPrefab;

        [Header("Environmental")]
        [Tooltip("Doors that can slam")]
        public List<HorrorDoor> horrorDoors = new List<HorrorDoor>();

        [Tooltip("Objects that can fall")]
        public List<GameObject> fallableObjects = new List<GameObject>();

        [Tooltip("TVs/monitors that can show static")]
        public List<Renderer> tvScreens = new List<Renderer>();

        [Tooltip("Static TV material")]
        public Material staticMaterial;

        [Header("Events")]
        public UnityEvent<string> onHorrorEventTriggered;
        public UnityEvent onJumpScare;

        // State
        private float eventTimer;
        private AudioSource audioSource;
        private AudioSource ambientAudioSource;
        private PlayerController player;
        private bool isEventInProgress = false;
        private List<Material> originalTVMaterials = new List<Material>();

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
                audioSource.spatialBlend = 0f; // 2D for jump scares
            }

            // Create ambient source for 3D sounds
            GameObject ambientObj = new GameObject("HorrorAmbientSource");
            ambientObj.transform.SetParent(transform);
            ambientAudioSource = ambientObj.AddComponent<AudioSource>();
            ambientAudioSource.spatialBlend = 1f;
            ambientAudioSource.maxDistance = 50f;
        }

        private void Start()
        {
            ResetEventTimer();

            // Store original TV materials
            foreach (var tv in tvScreens)
            {
                if (tv != null)
                {
                    originalTVMaterials.Add(tv.material);
                }
            }

            // Find player
            player = FindObjectOfType<PlayerController>();
        }

        private void Update()
        {
            if (!eventsEnabled) return;

            // Update intensity based on day
            if (GameManager.Instance != null)
            {
                float dayProgress = (float)GameManager.Instance.currentDay / GameManager.Instance.totalDays;
                horrorIntensity = Mathf.Lerp(0.2f, 1f, dayProgress);
            }

            // Check if night
            bool isNight = DayNightCycle.Instance?.IsNight() ?? false;
            if (nightOnly && !isNight) return;

            // Event timer
            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0f && !isEventInProgress)
            {
                TryTriggerRandomEvent();
                ResetEventTimer();
            }
        }

        private void ResetEventTimer()
        {
            float interval = Random.Range(minEventInterval, maxEventInterval);

            // Reduce interval based on intensity
            interval *= (1f - horrorIntensity * 0.5f);

            // Night modifier
            if (DayNightCycle.Instance?.IsNight() == true)
            {
                interval /= nightMultiplier;
            }

            eventTimer = interval;
        }

        #region Event Triggering

        private void TryTriggerRandomEvent()
        {
            if (horrorEvents.Count == 0) return;

            // Weight by intensity
            List<HorrorEvent> availableEvents = new List<HorrorEvent>();
            foreach (var evt in horrorEvents)
            {
                if (evt.minIntensity <= horrorIntensity && evt.enabled)
                {
                    availableEvents.Add(evt);
                }
            }

            if (availableEvents.Count == 0) return;

            // Random weighted selection
            float totalWeight = 0f;
            foreach (var evt in availableEvents)
            {
                totalWeight += evt.weight;
            }

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var evt in availableEvents)
            {
                cumulative += evt.weight;
                if (random <= cumulative)
                {
                    TriggerEvent(evt);
                    break;
                }
            }
        }

        private void TriggerEvent(HorrorEvent evt)
        {
            isEventInProgress = true;
            onHorrorEventTriggered?.Invoke(evt.eventName);

            switch (evt.eventType)
            {
                case HorrorEventType.CreepySound:
                    StartCoroutine(PlayCreepySound());
                    break;
                case HorrorEventType.DistantSound:
                    StartCoroutine(PlayDistantSound());
                    break;
                case HorrorEventType.Whispers:
                    StartCoroutine(PlayWhispers());
                    break;
                case HorrorEventType.LightsFlicker:
                    StartCoroutine(FlickerAllLights());
                    break;
                case HorrorEventType.DoorSlam:
                    TriggerDoorSlam();
                    break;
                case HorrorEventType.ObjectFall:
                    TriggerObjectFall();
                    break;
                case HorrorEventType.TVStatic:
                    StartCoroutine(TVStaticEvent());
                    break;
                case HorrorEventType.ShadowFigure:
                    SpawnShadowFigure();
                    break;
                case HorrorEventType.GhostAppearance:
                    SpawnGhost();
                    break;
                case HorrorEventType.CreepyPhoneCall:
                    StartCoroutine(CreepyPhoneCall());
                    break;
                case HorrorEventType.FlashlightMalfunction:
                    TriggerFlashlightMalfunction();
                    break;
                case HorrorEventType.JumpScare:
                    StartCoroutine(JumpScare());
                    break;
            }

            StartCoroutine(EventCooldown(evt.cooldown));
        }

        private IEnumerator EventCooldown(float cooldown)
        {
            yield return new WaitForSeconds(cooldown);
            isEventInProgress = false;
        }

        #endregion

        #region Audio Events

        private IEnumerator PlayCreepySound()
        {
            if (creepySounds.Length == 0) yield break;

            AudioClip clip = creepySounds[Random.Range(0, creepySounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
            yield return null;
        }

        private IEnumerator PlayDistantSound()
        {
            if (distantSounds.Length == 0) yield break;

            // Play from random position around player
            if (player != null)
            {
                Vector3 randomPos = player.transform.position + Random.insideUnitSphere * 30f;
                randomPos.y = player.transform.position.y;
                ambientAudioSource.transform.position = randomPos;

                AudioClip clip = distantSounds[Random.Range(0, distantSounds.Length)];
                ambientAudioSource.PlayOneShot(clip, 0.5f);
            }
            yield return null;
        }

        private IEnumerator PlayWhispers()
        {
            if (whisperSounds.Length == 0) yield break;

            // Play whispers close to player
            AudioClip clip = whisperSounds[Random.Range(0, whisperSounds.Length)];
            audioSource.PlayOneShot(clip, 0.3f);

            yield return new WaitForSeconds(clip.length);

            // Maybe play another
            if (Random.value < 0.3f && whisperSounds.Length > 1)
            {
                yield return new WaitForSeconds(Random.Range(1f, 3f));
                clip = whisperSounds[Random.Range(0, whisperSounds.Length)];
                audioSource.PlayOneShot(clip, 0.2f);
            }
        }

        #endregion

        #region Visual Events

        private IEnumerator FlickerAllLights()
        {
            if (!canFlickerLights) yield break;

            Light[] lights = FindObjectsOfType<Light>();
            Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();

            foreach (var light in lights)
            {
                originalIntensities[light] = light.intensity;
            }

            // Flicker sequence
            int flickerCount = Random.Range(3, 8);
            for (int i = 0; i < flickerCount; i++)
            {
                // Off
                foreach (var light in lights)
                {
                    if (light != null)
                    {
                        light.intensity = Random.value < 0.7f ? 0f : originalIntensities[light] * 0.2f;
                    }
                }

                // Also flicker player flashlight
                var flashlight = player?.GetComponentInChildren<Flashlight>();
                flashlight?.TriggerHorrorFlicker();

                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));

                // Restore
                foreach (var light in lights)
                {
                    if (light != null)
                    {
                        light.intensity = originalIntensities[light];
                    }
                }

                yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
            }

            // Final restoration
            foreach (var light in lights)
            {
                if (light != null)
                {
                    light.intensity = originalIntensities[light];
                }
            }
        }

        private void TriggerDoorSlam()
        {
            if (horrorDoors.Count == 0) return;

            // Find closest door to player
            HorrorDoor closestDoor = null;
            float closestDist = float.MaxValue;

            foreach (var door in horrorDoors)
            {
                if (door == null || !door.canSlam) continue;

                float dist = Vector3.Distance(door.transform.position, player?.transform.position ?? Vector3.zero);
                if (dist < closestDist && dist < door.maxSlamDistance)
                {
                    closestDist = dist;
                    closestDoor = door;
                }
            }

            closestDoor?.Slam();
        }

        private void TriggerObjectFall()
        {
            if (fallableObjects.Count == 0) return;

            // Pick random object
            GameObject obj = fallableObjects[Random.Range(0, fallableObjects.Count)];
            if (obj == null) return;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = obj.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.AddForce(Vector3.down * 2f + Random.insideUnitSphere, ForceMode.Impulse);
        }

        private IEnumerator TVStaticEvent()
        {
            if (tvScreens.Count == 0 || staticMaterial == null) yield break;

            // Turn on static
            foreach (var tv in tvScreens)
            {
                if (tv != null)
                {
                    tv.material = staticMaterial;
                }
            }

            // Play static sound
            if (staticSound != null)
            {
                audioSource.PlayOneShot(staticSound, 0.5f);
            }

            yield return new WaitForSeconds(Random.Range(3f, 8f));

            // Restore
            for (int i = 0; i < tvScreens.Count && i < originalTVMaterials.Count; i++)
            {
                if (tvScreens[i] != null)
                {
                    tvScreens[i].material = originalTVMaterials[i];
                }
            }
        }

        private void SpawnShadowFigure()
        {
            if (shadowFigurePrefab == null || player == null) return;

            // Spawn behind player, in peripheral vision
            Vector3 spawnPos = player.transform.position - player.transform.forward * 15f;
            spawnPos += player.transform.right * Random.Range(-10f, 10f);

            GameObject shadow = Instantiate(shadowFigurePrefab, spawnPos, Quaternion.identity);

            // Face player
            shadow.transform.LookAt(player.transform);

            // Destroy after short time
            Destroy(shadow, 2f);
        }

        private void SpawnGhost()
        {
            if (ghostPrefab == null || player == null) return;

            // Spawn in front of player briefly
            Vector3 spawnPos = player.transform.position + player.transform.forward * 8f;

            GameObject ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
            ghost.transform.LookAt(player.transform);

            // Quick fade out
            StartCoroutine(FadeAndDestroyGhost(ghost));
        }

        private IEnumerator FadeAndDestroyGhost(GameObject ghost)
        {
            yield return new WaitForSeconds(0.5f);

            // Fade out
            Renderer renderer = ghost.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = renderer.material;
                Color color = mat.color;

                float timer = 0f;
                float duration = 0.3f;

                while (timer < duration)
                {
                    timer += Time.deltaTime;
                    color.a = Mathf.Lerp(1f, 0f, timer / duration);
                    mat.color = color;
                    yield return null;
                }
            }

            Destroy(ghost);
        }

        private IEnumerator CreepyPhoneCall()
        {
            if (phoneRingSound == null) yield break;

            // Phone rings
            for (int i = 0; i < 3; i++)
            {
                audioSource.PlayOneShot(phoneRingSound);
                yield return new WaitForSeconds(2f);
            }

            // Then silence... creepy
        }

        private void TriggerFlashlightMalfunction()
        {
            var flashlight = player?.GetComponentInChildren<Flashlight>();
            if (flashlight != null && flashlight.IsOn)
            {
                flashlight.ForceOffTemporarily(Random.Range(1f, 3f));
            }
        }

        private IEnumerator JumpScare()
        {
            onJumpScare?.Invoke();

            // Screen flash
            if (distortionMaterial != null)
            {
                // Apply distortion briefly
            }

            // Loud stinger
            if (jumpScareStingers.Length > 0)
            {
                AudioClip stinger = jumpScareStingers[Random.Range(0, jumpScareStingers.Length)];
                audioSource.PlayOneShot(stinger, 1f);
            }

            // Spawn something scary in front of player
            if (ghostPrefab != null && player != null)
            {
                Vector3 spawnPos = player.transform.position + player.transform.forward * 2f;
                spawnPos.y = player.transform.position.y;

                GameObject scare = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
                scare.transform.LookAt(player.transform);

                yield return new WaitForSeconds(0.3f);

                Destroy(scare);
            }

            yield return null;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force trigger a specific event type.
        /// </summary>
        public void TriggerEventByType(HorrorEventType eventType)
        {
            foreach (var evt in horrorEvents)
            {
                if (evt.eventType == eventType)
                {
                    TriggerEvent(evt);
                    return;
                }
            }
        }

        /// <summary>
        /// Increase horror intensity (called by game progression).
        /// </summary>
        public void IncreaseIntensity(float amount)
        {
            horrorIntensity = Mathf.Clamp01(horrorIntensity + amount);
        }

        /// <summary>
        /// Enable/disable horror events.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            eventsEnabled = enabled;
        }

        /// <summary>
        /// Force trigger lights flicker (for external use).
        /// </summary>
        public void ForceFlickerLights()
        {
            StartCoroutine(FlickerAllLights());
        }

        #endregion
    }

    [System.Serializable]
    public class HorrorEvent
    {
        public string eventName;
        public HorrorEventType eventType;
        public bool enabled = true;

        [Range(0f, 10f)]
        public float weight = 1f;

        [Range(0f, 1f)]
        [Tooltip("Minimum horror intensity to trigger")]
        public float minIntensity = 0f;

        [Tooltip("Cooldown after this event")]
        public float cooldown = 5f;
    }

    public enum HorrorEventType
    {
        CreepySound,
        DistantSound,
        Whispers,
        LightsFlicker,
        DoorSlam,
        ObjectFall,
        TVStatic,
        ShadowFigure,
        GhostAppearance,
        CreepyPhoneCall,
        FlashlightMalfunction,
        JumpScare
    }

    /// <summary>
    /// Door that can slam for horror effect.
    /// </summary>
    public class HorrorDoor : MonoBehaviour
    {
        [Header("Settings")]
        public bool canSlam = true;
        public float maxSlamDistance = 20f;

        [Header("Audio")]
        public AudioClip slamSound;

        [Header("Animation")]
        public Animator animator;
        public string slamTrigger = "Slam";

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }
        }

        public void Slam()
        {
            if (animator != null)
            {
                animator.SetTrigger(slamTrigger);
            }

            if (slamSound != null)
            {
                audioSource.PlayOneShot(slamSound);
            }
        }
    }
}
