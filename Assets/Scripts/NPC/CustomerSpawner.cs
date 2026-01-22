using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Core;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// Spawns customer NPCs that wander and visit the gas station.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        public static CustomerSpawner Instance { get; private set; }

        [Header("Customer Prefabs")]
        [Tooltip("Customer prefabs to spawn")]
        public List<GameObject> customerPrefabs = new List<GameObject>();

        [Header("Spawn Settings")]
        [Tooltip("Spawn points for customers")]
        public Transform[] spawnPoints;

        [Tooltip("Maximum customers alive")]
        public int maxCustomers = 10;

        [Tooltip("Spawn interval (seconds)")]
        public float spawnInterval = 15f;

        [Tooltip("Customers per spawn")]
        public int customersPerSpawn = 1;

        [Header("Store Configuration")]
        [Tooltip("Store entrance")]
        public Transform storeEntrance;

        [Tooltip("Cash register")]
        public Transform cashRegister;

        [Tooltip("Store exit")]
        public Transform storeExit;

        [Tooltip("Shopping shelves")]
        public Transform[] storeShelves;

        [Header("Day Settings")]
        [Tooltip("Spawn customers only during day")]
        public bool dayTimeOnly = true;

        [Header("Events")]
        public UnityEvent<GameObject> onCustomerSpawned;
        public UnityEvent<GameObject> onCustomerLeft;

        private List<GameObject> activeCustomers = new List<GameObject>();
        private float nextSpawnTime;
        private bool isDayTime = true;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            // Clean up
            activeCustomers.RemoveAll(c => c == null);

            // Spawn
            if (Time.time >= nextSpawnTime)
            {
                if (!dayTimeOnly || isDayTime)
                {
                    SpawnCustomers();
                }
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        /// <summary>
        /// Spawn customers.
        /// </summary>
        public void SpawnCustomers()
        {
            for (int i = 0; i < customersPerSpawn; i++)
            {
                SpawnCustomer();
            }
        }

        /// <summary>
        /// Spawn a single customer.
        /// </summary>
        public GameObject SpawnCustomer()
        {
            if (activeCustomers.Count >= maxCustomers)
            {
                return null;
            }

            if (spawnPoints.Length == 0 || customerPrefabs.Count == 0)
            {
                return null;
            }

            // Pick random spawn and prefab
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];

            // Spawn
            GameObject customer = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            activeCustomers.Add(customer);

            // Configure customer AI
            var customerAI = customer.GetComponent<CustomerAI>();
            if (customerAI != null)
            {
                customerAI.SetStorePoints(storeEntrance, cashRegister, storeExit, storeShelves);
                customerAI.onLeave.AddListener(() => OnCustomerLeave(customer));
            }

            onCustomerSpawned?.Invoke(customer);
            return customer;
        }

        private void OnCustomerLeave(GameObject customer)
        {
            activeCustomers.Remove(customer);
            onCustomerLeft?.Invoke(customer);
        }

        /// <summary>
        /// Set day/night mode.
        /// </summary>
        public void SetDayTime(bool isDay)
        {
            isDayTime = isDay;
        }

        /// <summary>
        /// Get current customer count.
        /// </summary>
        public int GetCustomerCount()
        {
            return activeCustomers.Count;
        }

        /// <summary>
        /// Force all customers to leave.
        /// </summary>
        public void ClearAllCustomers()
        {
            foreach (var customer in activeCustomers)
            {
                if (customer != null)
                {
                    var customerAI = customer.GetComponent<CustomerAI>();
                    if (customerAI != null)
                    {
                        customerAI.ForceLeave();
                    }
                    else
                    {
                        Destroy(customer);
                    }
                }
            }
        }
    }
}
