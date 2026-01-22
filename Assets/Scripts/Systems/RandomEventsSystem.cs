using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using BobsPetroleum.Core;
using BobsPetroleum.NPC;
using BobsPetroleum.Vehicles;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Random gameplay events system for variety: power outages, rush hour,
    /// weird customers, robberies, etc. Makes the game feel alive and unpredictable.
    /// </summary>
    public class RandomEventsSystem : MonoBehaviour
    {
        public static RandomEventsSystem Instance { get; private set; }

        [Header("Event Settings")]
        [Tooltip("Enable random events")]
        public bool eventsEnabled = true;

        [Tooltip("Minimum time between events")]
        public float minEventInterval = 120f;

        [Tooltip("Maximum time between events")]
        public float maxEventInterval = 600f;

        [Header("Available Events")]
        public List<GameplayEvent> gameplayEvents = new List<GameplayEvent>();

        [Header("Power Outage")]
        [Tooltip("Lights to disable during outage")]
        public List<Light> stationLights = new List<Light>();

        [Tooltip("Power outage duration range")]
        public Vector2 outageDuration = new Vector2(30f, 120f);

        [Tooltip("Generator object (optional mini-game)")]
        public GameObject generatorObject;

        [Header("Rush Hour")]
        [Tooltip("Extra customers during rush")]
        public int rushHourCustomerBoost = 5;

        [Tooltip("Rush hour duration")]
        public float rushHourDuration = 180f;

        [Header("Weird Customers")]
        [Tooltip("Weird customer prefabs")]
        public List<GameObject> weirdCustomerPrefabs = new List<GameObject>();

        [Tooltip("Weird customer spawn point")]
        public Transform weirdCustomerSpawn;

        [Header("Robbery")]
        [Tooltip("Robber NPC prefab")]
        public GameObject robberPrefab;

        [Tooltip("Robber spawn point")]
        public Transform robberSpawnPoint;

        [Tooltip("Money lost on failed robbery defense")]
        public int robberyMoneyLoss = 100;

        [Header("Gas Leak")]
        [Tooltip("Gas leak particle effect")]
        public ParticleSystem gasLeakEffect;

        [Tooltip("Gas leak damage per second")]
        public float gasLeakDamage = 5f;

        [Tooltip("Gas leak radius")]
        public float gasLeakRadius = 10f;

        [Header("Delivery")]
        [Tooltip("Delivery truck prefab")]
        public GameObject deliveryTruckPrefab;

        [Tooltip("Delivery spawn point")]
        public Transform deliverySpawnPoint;

        [Tooltip("Items delivered")]
        public List<DeliveryItem> deliveryItems = new List<DeliveryItem>();

        [Header("Weather Events")]
        [Tooltip("Rain particle system")]
        public ParticleSystem rainEffect;

        [Tooltip("Fog volume/effect")]
        public GameObject fogEffect;

        [Tooltip("Storm lightning effect")]
        public Light lightningLight;

        [Header("Audio")]
        public AudioClip powerOutageSound;
        public AudioClip powerRestoreSound;
        public AudioClip rushHourAnnouncement;
        public AudioClip robberyAlarm;
        public AudioClip gasLeakAlarm;
        public AudioClip deliveryHorn;
        public AudioClip thunderSound;

        [Header("Events")]
        public UnityEvent<string> onEventStarted;
        public UnityEvent<string> onEventEnded;
        public UnityEvent onPowerOutage;
        public UnityEvent onPowerRestored;
        public UnityEvent onRobberyStart;
        public UnityEvent<bool> onRobberyEnd; // true = defended

        // State
        private float eventTimer;
        private AudioSource audioSource;
        private bool isPowerOut = false;
        private bool isRushHour = false;
        private bool isGasLeaking = false;
        private GameplayEvent currentEvent;

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
            }
        }

        private void Start()
        {
            ResetEventTimer();

            // Initialize default events if none configured
            if (gameplayEvents.Count == 0)
            {
                InitializeDefaultEvents();
            }
        }

        private void Update()
        {
            if (!eventsEnabled) return;

            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0f && currentEvent == null)
            {
                TryTriggerRandomEvent();
                ResetEventTimer();
            }

            // Update active effects
            if (isGasLeaking)
            {
                UpdateGasLeak();
            }
        }

        private void ResetEventTimer()
        {
            eventTimer = Random.Range(minEventInterval, maxEventInterval);
        }

        private void InitializeDefaultEvents()
        {
            gameplayEvents.Add(new GameplayEvent { eventName = "Power Outage", eventType = GameplayEventType.PowerOutage, weight = 1f, dayOnly = false });
            gameplayEvents.Add(new GameplayEvent { eventName = "Rush Hour", eventType = GameplayEventType.RushHour, weight = 1.5f, dayOnly = true });
            gameplayEvents.Add(new GameplayEvent { eventName = "Weird Customer", eventType = GameplayEventType.WeirdCustomer, weight = 2f });
            gameplayEvents.Add(new GameplayEvent { eventName = "Delivery", eventType = GameplayEventType.Delivery, weight = 1.5f, dayOnly = true });
            gameplayEvents.Add(new GameplayEvent { eventName = "Rain", eventType = GameplayEventType.Rain, weight = 1f });
            gameplayEvents.Add(new GameplayEvent { eventName = "Fog", eventType = GameplayEventType.Fog, weight = 0.8f, dayOnly = false });
        }

        #region Event Triggering

        private void TryTriggerRandomEvent()
        {
            if (gameplayEvents.Count == 0) return;

            bool isDay = DayNightCycle.Instance?.IsDay() ?? true;

            // Filter available events
            List<GameplayEvent> available = new List<GameplayEvent>();
            foreach (var evt in gameplayEvents)
            {
                if (!evt.enabled) continue;
                if (evt.dayOnly && !isDay) continue;
                if (evt.nightOnly && isDay) continue;
                available.Add(evt);
            }

            if (available.Count == 0) return;

            // Weighted random selection
            float totalWeight = 0f;
            foreach (var evt in available)
            {
                totalWeight += evt.weight;
            }

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var evt in available)
            {
                cumulative += evt.weight;
                if (random <= cumulative)
                {
                    TriggerEvent(evt);
                    break;
                }
            }
        }

        private void TriggerEvent(GameplayEvent evt)
        {
            currentEvent = evt;
            onEventStarted?.Invoke(evt.eventName);

            switch (evt.eventType)
            {
                case GameplayEventType.PowerOutage:
                    StartCoroutine(PowerOutageEvent());
                    break;
                case GameplayEventType.RushHour:
                    StartCoroutine(RushHourEvent());
                    break;
                case GameplayEventType.WeirdCustomer:
                    SpawnWeirdCustomer();
                    currentEvent = null;
                    break;
                case GameplayEventType.Robbery:
                    StartCoroutine(RobberyEvent());
                    break;
                case GameplayEventType.GasLeak:
                    StartCoroutine(GasLeakEvent());
                    break;
                case GameplayEventType.Delivery:
                    StartCoroutine(DeliveryEvent());
                    break;
                case GameplayEventType.Rain:
                    StartCoroutine(RainEvent());
                    break;
                case GameplayEventType.Fog:
                    StartCoroutine(FogEvent());
                    break;
                case GameplayEventType.Storm:
                    StartCoroutine(StormEvent());
                    break;
                case GameplayEventType.CustomerRush:
                    StartCoroutine(CustomerRushEvent());
                    break;
            }
        }

        #endregion

        #region Power Outage

        private IEnumerator PowerOutageEvent()
        {
            isPowerOut = true;
            onPowerOutage?.Invoke();

            // Play sound
            PlaySound(powerOutageSound);

            // Store original intensities
            Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();
            foreach (var light in stationLights)
            {
                if (light != null)
                {
                    originalIntensities[light] = light.intensity;
                    light.intensity = 0f;
                }
            }

            // Notify HUD
            UI.HUDManager.Instance?.ShowNotification("POWER OUTAGE!");

            // Enable generator mini-game if exists
            if (generatorObject != null)
            {
                generatorObject.SetActive(true);
            }

            // Wait for duration or player fixes it
            float duration = Random.Range(outageDuration.x, outageDuration.y);
            float timer = 0f;

            while (timer < duration && isPowerOut)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            // Restore power
            RestorePower(originalIntensities);
        }

        private void RestorePower(Dictionary<Light, float> originalIntensities)
        {
            isPowerOut = false;
            onPowerRestored?.Invoke();

            PlaySound(powerRestoreSound);

            foreach (var kvp in originalIntensities)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.intensity = kvp.Value;
                }
            }

            if (generatorObject != null)
            {
                generatorObject.SetActive(false);
            }

            UI.HUDManager.Instance?.ShowNotification("Power Restored");
            onEventEnded?.Invoke("Power Outage");
            currentEvent = null;
        }

        /// <summary>
        /// Player fixed the generator.
        /// </summary>
        public void FixPowerOutage()
        {
            isPowerOut = false;
        }

        #endregion

        #region Rush Hour

        private IEnumerator RushHourEvent()
        {
            isRushHour = true;

            PlaySound(rushHourAnnouncement);
            UI.HUDManager.Instance?.ShowNotification("RUSH HOUR - More customers incoming!");

            // Boost customer spawner
            if (CustomerSpawner.Instance != null)
            {
                CustomerSpawner.Instance.spawnRateMultiplier += rushHourCustomerBoost;
            }

            yield return new WaitForSeconds(rushHourDuration);

            // End rush hour
            isRushHour = false;

            if (CustomerSpawner.Instance != null)
            {
                CustomerSpawner.Instance.spawnRateMultiplier -= rushHourCustomerBoost;
            }

            UI.HUDManager.Instance?.ShowNotification("Rush hour ended");
            onEventEnded?.Invoke("Rush Hour");
            currentEvent = null;
        }

        #endregion

        #region Weird Customer

        private void SpawnWeirdCustomer()
        {
            if (weirdCustomerPrefabs.Count == 0 || weirdCustomerSpawn == null) return;

            GameObject prefab = weirdCustomerPrefabs[Random.Range(0, weirdCustomerPrefabs.Count)];
            Instantiate(prefab, weirdCustomerSpawn.position, weirdCustomerSpawn.rotation);

            UI.HUDManager.Instance?.ShowNotification("Strange customer approaching...");
        }

        #endregion

        #region Robbery

        private IEnumerator RobberyEvent()
        {
            onRobberyStart?.Invoke();

            PlaySound(robberyAlarm);
            UI.HUDManager.Instance?.ShowNotification("ROBBERY IN PROGRESS!");

            // Spawn robber
            GameObject robber = null;
            if (robberPrefab != null && robberSpawnPoint != null)
            {
                robber = Instantiate(robberPrefab, robberSpawnPoint.position, robberSpawnPoint.rotation);
            }

            // Give player time to react
            yield return new WaitForSeconds(30f);

            // Check if robber is still alive (player defended)
            bool defended = robber == null || !robber.activeInHierarchy;

            if (!defended)
            {
                // Robbery succeeded - lose money
                var player = FindObjectOfType<Player.PlayerController>();
                var inventory = player?.GetComponent<Player.PlayerInventory>();
                inventory?.SpendMoney(robberyMoneyLoss);

                UI.HUDManager.Instance?.ShowNotification($"Robber escaped with ${robberyMoneyLoss}!");
            }
            else
            {
                UI.HUDManager.Instance?.ShowNotification("Robbery prevented!");
            }

            onRobberyEnd?.Invoke(defended);
            onEventEnded?.Invoke("Robbery");
            currentEvent = null;
        }

        #endregion

        #region Gas Leak

        private IEnumerator GasLeakEvent()
        {
            isGasLeaking = true;

            PlaySound(gasLeakAlarm);
            UI.HUDManager.Instance?.ShowNotification("WARNING: Gas Leak Detected!");

            if (gasLeakEffect != null)
            {
                gasLeakEffect.Play();
            }

            // Leak for duration
            yield return new WaitForSeconds(60f);

            // End leak
            isGasLeaking = false;

            if (gasLeakEffect != null)
            {
                gasLeakEffect.Stop();
            }

            UI.HUDManager.Instance?.ShowNotification("Gas leak contained");
            onEventEnded?.Invoke("Gas Leak");
            currentEvent = null;
        }

        private void UpdateGasLeak()
        {
            if (gasLeakEffect == null) return;

            // Damage players in radius
            var players = FindObjectsOfType<Player.PlayerController>();
            foreach (var player in players)
            {
                float dist = Vector3.Distance(player.transform.position, gasLeakEffect.transform.position);
                if (dist < gasLeakRadius)
                {
                    var health = player.GetComponent<Player.PlayerHealth>();
                    health?.TakeDamage(gasLeakDamage * Time.deltaTime);
                }
            }
        }

        #endregion

        #region Delivery

        private IEnumerator DeliveryEvent()
        {
            PlaySound(deliveryHorn);
            UI.HUDManager.Instance?.ShowNotification("Delivery truck arriving!");

            // Spawn truck
            if (deliveryTruckPrefab != null && deliverySpawnPoint != null)
            {
                GameObject truck = Instantiate(deliveryTruckPrefab, deliverySpawnPoint.position, deliverySpawnPoint.rotation);

                yield return new WaitForSeconds(5f);

                // Add items to shelves
                RestockShelves();

                yield return new WaitForSeconds(10f);

                // Truck leaves
                Destroy(truck);
            }

            onEventEnded?.Invoke("Delivery");
            currentEvent = null;
        }

        private void RestockShelves()
        {
            var shelves = FindObjectsOfType<Shop.ShopShelf>();
            foreach (var shelf in shelves)
            {
                if (shelf.autoRestock)
                {
                    shelf.Restock();
                }
            }

            UI.HUDManager.Instance?.ShowNotification("Shelves restocked!");
        }

        #endregion

        #region Weather Events

        private IEnumerator RainEvent()
        {
            if (rainEffect != null)
            {
                rainEffect.Play();
            }

            UI.HUDManager.Instance?.ShowNotification("It's starting to rain...");

            yield return new WaitForSeconds(Random.Range(120f, 300f));

            if (rainEffect != null)
            {
                rainEffect.Stop();
            }

            onEventEnded?.Invoke("Rain");
            currentEvent = null;
        }

        private IEnumerator FogEvent()
        {
            if (fogEffect != null)
            {
                fogEffect.SetActive(true);
            }

            // Also reduce render distance
            float originalFogDensity = RenderSettings.fogDensity;
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.05f;

            UI.HUDManager.Instance?.ShowNotification("Fog rolling in...");

            yield return new WaitForSeconds(Random.Range(180f, 420f));

            if (fogEffect != null)
            {
                fogEffect.SetActive(false);
            }

            RenderSettings.fogDensity = originalFogDensity;

            onEventEnded?.Invoke("Fog");
            currentEvent = null;
        }

        private IEnumerator StormEvent()
        {
            // Rain + thunder
            if (rainEffect != null)
            {
                rainEffect.Play();
                var emission = rainEffect.emission;
                emission.rateOverTime = emission.rateOverTime.constant * 2f;
            }

            UI.HUDManager.Instance?.ShowNotification("Storm approaching!");

            float duration = Random.Range(120f, 240f);
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;

                // Random lightning
                if (Random.value < 0.01f)
                {
                    StartCoroutine(LightningFlash());
                }

                yield return null;
            }

            if (rainEffect != null)
            {
                rainEffect.Stop();
            }

            onEventEnded?.Invoke("Storm");
            currentEvent = null;
        }

        private IEnumerator LightningFlash()
        {
            if (lightningLight != null)
            {
                lightningLight.enabled = true;
                lightningLight.intensity = 5f;
            }

            yield return new WaitForSeconds(0.1f);

            if (lightningLight != null)
            {
                lightningLight.enabled = false;
            }

            // Thunder sound delayed
            yield return new WaitForSeconds(Random.Range(0.5f, 2f));
            PlaySound(thunderSound);
        }

        private IEnumerator CustomerRushEvent()
        {
            UI.HUDManager.Instance?.ShowNotification("Sudden customer rush!");

            // Spawn many customers at once
            if (CustomerSpawner.Instance != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    CustomerSpawner.Instance.SpawnCustomer();
                    yield return new WaitForSeconds(0.5f);
                }
            }

            onEventEnded?.Invoke("Customer Rush");
            currentEvent = null;
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

        #endregion

        #region Public API

        /// <summary>
        /// Force trigger a specific event.
        /// </summary>
        public void TriggerEventByType(GameplayEventType eventType)
        {
            foreach (var evt in gameplayEvents)
            {
                if (evt.eventType == eventType)
                {
                    TriggerEvent(evt);
                    return;
                }
            }
        }

        /// <summary>
        /// Check if power is currently out.
        /// </summary>
        public bool IsPowerOut => isPowerOut;

        /// <summary>
        /// Check if rush hour is active.
        /// </summary>
        public bool IsRushHour => isRushHour;

        #endregion
    }

    [System.Serializable]
    public class GameplayEvent
    {
        public string eventName;
        public GameplayEventType eventType;
        public bool enabled = true;

        [Range(0f, 10f)]
        public float weight = 1f;

        public bool dayOnly = false;
        public bool nightOnly = false;
    }

    public enum GameplayEventType
    {
        PowerOutage,
        RushHour,
        WeirdCustomer,
        Robbery,
        GasLeak,
        Delivery,
        Rain,
        Fog,
        Storm,
        CustomerRush
    }

    [System.Serializable]
    public class DeliveryItem
    {
        public string itemId;
        public int quantity;
    }
}
