using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace BobsPetroleum.AI
{
    /// <summary>
    /// CUSTOMER SPAWNER - Automatically spawns customers!
    /// Controls flow of customers based on time of day.
    ///
    /// SETUP:
    /// 1. Create empty GameObject
    /// 2. Add CustomerSpawner
    /// 3. Assign waypoints and prefabs
    /// 4. Done! Customers spawn automatically.
    ///
    /// WAYPOINTS NEEDED:
    /// - Spawn point (road entrance)
    /// - Pump waypoints (one per pump)
    /// - Store entrance
    /// - Register waypoint
    /// - Browse waypoints (shelves)
    /// - Exit waypoint (road exit)
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public static CustomerSpawner Instance { get; private set; }

        [Header("=== PREFABS ===")]
        [Tooltip("Customer prefab (NPC with CustomerAI)")]
        public GameObject customerPrefab;

        [Tooltip("Vehicle prefabs (random selection)")]
        public List<GameObject> vehiclePrefabs = new List<GameObject>();

        [Header("=== SPAWN SETTINGS ===")]
        [Tooltip("Spawn point (where cars enter)")]
        public Transform spawnPoint;

        [Tooltip("Exit point (where cars leave)")]
        public Transform exitPoint;

        [Tooltip("Maximum customers at once")]
        public int maxCustomers = 4;

        [Tooltip("Base time between spawns (seconds)")]
        public float baseSpawnInterval = 30f;

        [Tooltip("Random variance in spawn time")]
        public float spawnVariance = 10f;

        [Header("=== WAYPOINTS ===")]
        [Tooltip("Gas pump waypoints (cars park here)")]
        public List<Transform> pumpWaypoints = new List<Transform>();

        [Tooltip("Store entrance door")]
        public Transform storeEntrance;

        [Tooltip("Cash register position")]
        public Transform registerWaypoint;

        [Tooltip("Browse positions (shelves, coolers)")]
        public List<Transform> browseWaypoints = new List<Transform>();

        [Header("=== TIME-BASED SPAWNING ===")]
        [Tooltip("Spawn rate multiplier during day")]
        [Range(0f, 3f)]
        public float dayMultiplier = 1.5f;

        [Tooltip("Spawn rate multiplier at night")]
        [Range(0f, 3f)]
        public float nightMultiplier = 0.3f;

        [Tooltip("Spawn rate multiplier during rush hour")]
        [Range(0f, 3f)]
        public float rushHourMultiplier = 2.5f;

        [Tooltip("Rush hour times (7-9 AM, 5-7 PM)")]
        public bool enableRushHour = true;

        [Header("=== CUSTOMER VARIETY ===")]
        [Tooltip("Min wallet amount")]
        public int minWallet = 20;

        [Tooltip("Max wallet amount")]
        public int maxWallet = 100;

        [Tooltip("Min patience (seconds)")]
        public float minPatience = 30f;

        [Tooltip("Max patience (seconds)")]
        public float maxPatience = 120f;

        [Header("=== NIGHT BEHAVIOR ===")]
        [Tooltip("Spawn creepy customers at night")]
        public bool spawnCreepyCustomers = true;

        [Tooltip("Chance of creepy customer at night")]
        [Range(0f, 1f)]
        public float creepyChance = 0.2f;

        [Tooltip("Creepy customer prefab (optional)")]
        public GameObject creepyCustomerPrefab;

        [Header("=== EVENTS ===")]
        public UnityEvent<CustomerAI> onCustomerSpawned;
        public UnityEvent<CustomerAI> onCustomerLeft;
        public UnityEvent onMaxCustomersReached;

        // Tracking
        private List<CustomerAI> activeCustomers = new List<CustomerAI>();
        private float spawnTimer;
        private int totalCustomersToday;
        private bool isSpawningEnabled = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Start()
        {
            // Start spawn loop
            StartCoroutine(SpawnLoop());
        }

        private void Update()
        {
            // Clean up null references
            activeCustomers.RemoveAll(c => c == null);
        }

        #region Spawning

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                if (isSpawningEnabled && activeCustomers.Count < maxCustomers)
                {
                    // Calculate spawn interval based on time of day
                    float interval = CalculateSpawnInterval();

                    yield return new WaitForSeconds(interval);

                    if (activeCustomers.Count < maxCustomers)
                    {
                        SpawnCustomer();
                    }
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }

        private float CalculateSpawnInterval()
        {
            float multiplier = 1f;

            // Get time of day
            var dayNight = Systems.DayNightCycle.Instance;
            if (dayNight != null)
            {
                float hour = dayNight.CurrentHour;

                // Night time (10 PM - 6 AM)
                if (hour >= 22 || hour < 6)
                {
                    multiplier = nightMultiplier;
                }
                // Rush hour (7-9 AM or 5-7 PM)
                else if (enableRushHour && ((hour >= 7 && hour < 9) || (hour >= 17 && hour < 19)))
                {
                    multiplier = rushHourMultiplier;
                }
                // Normal day
                else
                {
                    multiplier = dayMultiplier;
                }
            }

            // Higher multiplier = more customers = shorter interval
            float interval = baseSpawnInterval / Mathf.Max(0.1f, multiplier);
            interval += Random.Range(-spawnVariance, spawnVariance);

            return Mathf.Max(5f, interval); // Minimum 5 seconds
        }

        /// <summary>
        /// Spawn a customer with vehicle.
        /// </summary>
        public CustomerAI SpawnCustomer()
        {
            if (customerPrefab == null || spawnPoint == null)
            {
                Debug.LogWarning("[CustomerSpawner] Missing prefab or spawn point!");
                return null;
            }

            if (activeCustomers.Count >= maxCustomers)
            {
                onMaxCustomersReached?.Invoke();
                return null;
            }

            // Choose prefab (creepy at night?)
            GameObject prefabToUse = customerPrefab;
            bool isCreepy = false;

            if (spawnCreepyCustomers && creepyCustomerPrefab != null)
            {
                var dayNight = Systems.DayNightCycle.Instance;
                if (dayNight != null && dayNight.IsNight)
                {
                    if (Random.value < creepyChance)
                    {
                        prefabToUse = creepyCustomerPrefab;
                        isCreepy = true;
                    }
                }
            }

            // Spawn vehicle
            GameObject vehicle = null;
            if (vehiclePrefabs.Count > 0)
            {
                GameObject vehiclePrefab = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Count)];
                vehicle = Instantiate(vehiclePrefab, spawnPoint.position, spawnPoint.rotation);
            }

            // Spawn customer
            Vector3 customerPos = vehicle != null ? vehicle.transform.position : spawnPoint.position;
            GameObject customerObj = Instantiate(prefabToUse, customerPos, Quaternion.identity);
            CustomerAI customer = customerObj.GetComponent<CustomerAI>();

            if (customer == null)
            {
                Debug.LogError("[CustomerSpawner] Customer prefab missing CustomerAI component!");
                Destroy(customerObj);
                Destroy(vehicle);
                return null;
            }

            // Configure customer
            customer.vehicle = vehicle;
            customer.walletAmount = Random.Range(minWallet, maxWallet + 1);
            customer.patience = Random.Range(minPatience, maxPatience);
            customer.gasNeeded = Random.Range(5f, 20f);

            // Assign waypoints
            if (pumpWaypoints.Count > 0)
            {
                customer.pumpWaypoint = pumpWaypoints[Random.Range(0, pumpWaypoints.Count)];
            }
            customer.storeEntrance = storeEntrance;
            customer.registerWaypoint = registerWaypoint;
            customer.exitWaypoint = exitPoint;
            customer.browseWaypoints = new List<Transform>(browseWaypoints);

            // Track
            activeCustomers.Add(customer);
            totalCustomersToday++;

            onCustomerSpawned?.Invoke(customer);

            Debug.Log($"[CustomerSpawner] Spawned customer #{totalCustomersToday} (Creepy: {isCreepy})");

            return customer;
        }

        /// <summary>
        /// Called when a customer leaves.
        /// </summary>
        public void OnCustomerLeft(CustomerAI customer)
        {
            activeCustomers.Remove(customer);
            onCustomerLeft?.Invoke(customer);
        }

        #endregion

        #region Control

        /// <summary>
        /// Enable/disable customer spawning.
        /// </summary>
        public void SetSpawningEnabled(bool enabled)
        {
            isSpawningEnabled = enabled;
            Debug.Log($"[CustomerSpawner] Spawning {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Force spawn a customer immediately.
        /// </summary>
        public void ForceSpawn()
        {
            SpawnCustomer();
        }

        /// <summary>
        /// Despawn all customers.
        /// </summary>
        public void DespawnAll()
        {
            foreach (var customer in activeCustomers.ToArray())
            {
                if (customer != null)
                {
                    if (customer.vehicle != null)
                        Destroy(customer.vehicle);
                    Destroy(customer.gameObject);
                }
            }
            activeCustomers.Clear();
        }

        /// <summary>
        /// Get current customer count.
        /// </summary>
        public int GetActiveCustomerCount() => activeCustomers.Count;

        /// <summary>
        /// Get total customers served today.
        /// </summary>
        public int GetTotalCustomersToday() => totalCustomersToday;

        /// <summary>
        /// Reset daily counter (call at start of new day).
        /// </summary>
        public void ResetDailyCount()
        {
            totalCustomersToday = 0;
        }

        #endregion
    }
}
