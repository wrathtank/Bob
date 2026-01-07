using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Core;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// Spawns zombies randomly around the map.
    /// Configure spawn points and zombie prefabs in inspector.
    /// </summary>
    public class ZombieSpawner : MonoBehaviour
    {
        public static ZombieSpawner Instance { get; private set; }

        [Header("Zombie Prefabs")]
        [Tooltip("Zombie prefabs to spawn (picked randomly)")]
        public List<ZombiePrefabEntry> zombiePrefabs = new List<ZombiePrefabEntry>();

        [Header("Spawn Settings")]
        [Tooltip("Spawn points for zombies")]
        public Transform[] spawnPoints;

        [Tooltip("Spawn radius around spawn points")]
        public float spawnRadius = 5f;

        [Tooltip("Maximum zombies alive at once")]
        public int maxZombies = 10;

        [Tooltip("Initial zombies to spawn")]
        public int initialSpawnCount = 3;

        [Tooltip("Time between spawn waves")]
        public float spawnInterval = 30f;

        [Tooltip("Zombies per spawn wave")]
        public int zombiesPerWave = 2;

        [Header("Day Scaling")]
        [Tooltip("Multiply max zombies by day number")]
        public bool scaleWithDay = true;

        [Tooltip("Additional zombies per day")]
        public int zombiesPerDay = 2;

        [Header("Night Spawning")]
        [Tooltip("Spawn more at night")]
        public bool moreAtNight = true;

        [Tooltip("Night spawn multiplier")]
        public float nightSpawnMultiplier = 2f;

        [Header("Events")]
        public UnityEvent<GameObject> onZombieSpawned;
        public UnityEvent<GameObject> onZombieDied;

        private List<GameObject> activeZombies = new List<GameObject>();
        private float nextSpawnTime;
        private bool isNight = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Initial spawn
            for (int i = 0; i < initialSpawnCount; i++)
            {
                SpawnZombie();
            }

            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            // Clean up dead zombies
            activeZombies.RemoveAll(z => z == null);

            // Spawn wave
            if (Time.time >= nextSpawnTime)
            {
                SpawnWave();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        /// <summary>
        /// Spawn a single zombie at a random spawn point.
        /// </summary>
        public GameObject SpawnZombie()
        {
            if (activeZombies.Count >= GetMaxZombies())
            {
                return null;
            }

            // Pick random spawn point
            if (spawnPoints.Length == 0)
            {
                Debug.LogWarning("No spawn points configured!");
                return null;
            }

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // Random offset within radius
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = spawnPoint.position + new Vector3(randomOffset.x, 0, randomOffset.y);

            // Pick random zombie prefab
            GameObject prefab = GetRandomZombiePrefab();
            if (prefab == null)
            {
                Debug.LogWarning("No zombie prefabs configured!");
                return null;
            }

            // Spawn
            GameObject zombie = Instantiate(prefab, spawnPos, Quaternion.identity);
            activeZombies.Add(zombie);

            // Subscribe to death event
            var health = zombie.GetComponent<NPCHealth>();
            if (health != null)
            {
                health.onDeath.AddListener(() => OnZombieDeath(zombie));
            }

            onZombieSpawned?.Invoke(zombie);
            return zombie;
        }

        /// <summary>
        /// Spawn a wave of zombies.
        /// </summary>
        public void SpawnWave()
        {
            int count = zombiesPerWave;

            if (moreAtNight && isNight)
            {
                count = Mathf.CeilToInt(count * nightSpawnMultiplier);
            }

            for (int i = 0; i < count; i++)
            {
                SpawnZombie();
            }
        }

        /// <summary>
        /// Spawn a zombie at a specific location.
        /// </summary>
        public GameObject SpawnZombieAt(Vector3 position)
        {
            if (activeZombies.Count >= GetMaxZombies())
            {
                return null;
            }

            GameObject prefab = GetRandomZombiePrefab();
            if (prefab == null) return null;

            GameObject zombie = Instantiate(prefab, position, Quaternion.identity);
            activeZombies.Add(zombie);

            var health = zombie.GetComponent<NPCHealth>();
            if (health != null)
            {
                health.onDeath.AddListener(() => OnZombieDeath(zombie));
            }

            onZombieSpawned?.Invoke(zombie);
            return zombie;
        }

        private void OnZombieDeath(GameObject zombie)
        {
            activeZombies.Remove(zombie);
            onZombieDied?.Invoke(zombie);
        }

        private int GetMaxZombies()
        {
            int max = maxZombies;

            if (scaleWithDay && GameManager.Instance != null)
            {
                max += (GameManager.Instance.currentDay - 1) * zombiesPerDay;
            }

            return max;
        }

        private GameObject GetRandomZombiePrefab()
        {
            if (zombiePrefabs.Count == 0) return null;

            // Calculate total weight
            float totalWeight = 0f;
            foreach (var entry in zombiePrefabs)
            {
                totalWeight += entry.spawnWeight;
            }

            // Pick random based on weight
            float random = Random.Range(0f, totalWeight);
            float accumulated = 0f;

            foreach (var entry in zombiePrefabs)
            {
                accumulated += entry.spawnWeight;
                if (random <= accumulated)
                {
                    return entry.prefab;
                }
            }

            return zombiePrefabs[0].prefab;
        }

        /// <summary>
        /// Set night mode (affects spawn rates).
        /// </summary>
        public void SetNightMode(bool night)
        {
            isNight = night;
        }

        /// <summary>
        /// Kill all active zombies.
        /// </summary>
        public void KillAllZombies()
        {
            foreach (var zombie in activeZombies)
            {
                if (zombie != null)
                {
                    var health = zombie.GetComponent<NPCHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(9999f);
                    }
                    else
                    {
                        Destroy(zombie);
                    }
                }
            }

            activeZombies.Clear();
        }

        /// <summary>
        /// Get current zombie count.
        /// </summary>
        public int GetZombieCount()
        {
            return activeZombies.Count;
        }
    }

    [System.Serializable]
    public class ZombiePrefabEntry
    {
        public GameObject prefab;
        [Range(0f, 1f)]
        public float spawnWeight = 1f;
    }
}
