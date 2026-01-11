using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// Spawns wandering animals randomly in the world for capture.
    /// Handles spawn zones, limits, and day/night variations.
    /// </summary>
    public class AnimalSpawner : MonoBehaviour
    {
        public static AnimalSpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [Tooltip("Animal prefabs to spawn")]
        public List<AnimalSpawnEntry> animalPrefabs = new List<AnimalSpawnEntry>();

        [Tooltip("Maximum animals in world at once")]
        public int maxAnimals = 15;

        [Tooltip("Spawn interval (seconds)")]
        public float spawnInterval = 30f;

        [Tooltip("Minimum spawn distance from players")]
        public float minSpawnDistance = 20f;

        [Tooltip("Maximum spawn distance from players")]
        public float maxSpawnDistance = 50f;

        [Header("Spawn Zones")]
        [Tooltip("Use specific spawn zones (empty = spawn anywhere on NavMesh)")]
        public List<Transform> spawnZones = new List<Transform>();

        [Tooltip("Spawn zone radius")]
        public float spawnZoneRadius = 20f;

        [Header("Day/Night")]
        [Tooltip("Spawn multiplier during day")]
        [Range(0f, 2f)]
        public float daySpawnMultiplier = 1f;

        [Tooltip("Spawn multiplier during night")]
        [Range(0f, 2f)]
        public float nightSpawnMultiplier = 0.5f;

        [Tooltip("Nocturnal animals only spawn at night")]
        public bool enableNocturnalAnimals = true;

        [Header("Despawn")]
        [Tooltip("Despawn animals too far from players")]
        public bool despawnDistantAnimals = true;

        [Tooltip("Distance at which to despawn")]
        public float despawnDistance = 100f;

        // State
        private List<GameObject> spawnedAnimals = new List<GameObject>();
        private float spawnTimer = 0f;
        private bool isDay = true;

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
            for (int i = 0; i < maxAnimals / 2; i++)
            {
                TrySpawnAnimal();
            }
        }

        private void Update()
        {
            // Clean up destroyed animals from list
            spawnedAnimals.RemoveAll(a => a == null);

            // Despawn distant animals
            if (despawnDistantAnimals)
            {
                DespawnDistantAnimals();
            }

            // Spawn timer
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                float multiplier = isDay ? daySpawnMultiplier : nightSpawnMultiplier;
                spawnTimer = spawnInterval / multiplier;

                if (spawnedAnimals.Count < maxAnimals)
                {
                    TrySpawnAnimal();
                }
            }
        }

        private void TrySpawnAnimal()
        {
            if (animalPrefabs.Count == 0) return;

            // Select animal based on spawn chance and day/night
            AnimalSpawnEntry selectedAnimal = SelectAnimal();
            if (selectedAnimal == null) return;

            // Find spawn position
            Vector3? spawnPos = FindSpawnPosition();
            if (!spawnPos.HasValue) return;

            // Spawn
            GameObject animal = Instantiate(selectedAnimal.prefab, spawnPos.Value, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
            spawnedAnimals.Add(animal);

            // Apply random scale variation
            if (selectedAnimal.scaleVariation > 0)
            {
                float scale = 1f + Random.Range(-selectedAnimal.scaleVariation, selectedAnimal.scaleVariation);
                animal.transform.localScale *= scale;
            }
        }

        private AnimalSpawnEntry SelectAnimal()
        {
            // Filter by day/night
            List<AnimalSpawnEntry> validAnimals = new List<AnimalSpawnEntry>();
            foreach (var entry in animalPrefabs)
            {
                if (entry.prefab == null) continue;

                // Check nocturnal
                if (entry.nocturnalOnly && isDay && enableNocturnalAnimals)
                    continue;

                // Check diurnal
                if (entry.diurnalOnly && !isDay)
                    continue;

                validAnimals.Add(entry);
            }

            if (validAnimals.Count == 0) return null;

            // Weighted random selection
            float totalWeight = 0f;
            foreach (var entry in validAnimals)
            {
                totalWeight += entry.spawnChance;
            }

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var entry in validAnimals)
            {
                cumulative += entry.spawnChance;
                if (random <= cumulative)
                {
                    return entry;
                }
            }

            return validAnimals[validAnimals.Count - 1];
        }

        private Vector3? FindSpawnPosition()
        {
            // Try to find valid spawn position
            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector3 randomPos;

                if (spawnZones.Count > 0)
                {
                    // Use spawn zones
                    Transform zone = spawnZones[Random.Range(0, spawnZones.Count)];
                    randomPos = zone.position + Random.insideUnitSphere * spawnZoneRadius;
                    randomPos.y = zone.position.y;
                }
                else
                {
                    // Find player and spawn relative to them
                    var players = FindObjectsOfType<Player.PlayerController>();
                    if (players.Length == 0)
                    {
                        randomPos = transform.position + Random.insideUnitSphere * maxSpawnDistance;
                    }
                    else
                    {
                        Player.PlayerController player = players[Random.Range(0, players.Length)];
                        Vector2 circle = Random.insideUnitCircle.normalized;
                        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
                        randomPos = player.transform.position + new Vector3(circle.x, 0, circle.y) * distance;
                    }
                }

                // Check NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomPos, out hit, 10f, NavMesh.AllAreas))
                {
                    // Check distance from players
                    if (IsValidSpawnDistance(hit.position))
                    {
                        return hit.position;
                    }
                }
            }

            return null;
        }

        private bool IsValidSpawnDistance(Vector3 position)
        {
            var players = FindObjectsOfType<Player.PlayerController>();
            foreach (var player in players)
            {
                float distance = Vector3.Distance(position, player.transform.position);
                if (distance < minSpawnDistance)
                {
                    return false;
                }
            }
            return true;
        }

        private void DespawnDistantAnimals()
        {
            var players = FindObjectsOfType<Player.PlayerController>();
            if (players.Length == 0) return;

            for (int i = spawnedAnimals.Count - 1; i >= 0; i--)
            {
                if (spawnedAnimals[i] == null) continue;

                bool tooFar = true;
                foreach (var player in players)
                {
                    float distance = Vector3.Distance(spawnedAnimals[i].transform.position, player.transform.position);
                    if (distance < despawnDistance)
                    {
                        tooFar = false;
                        break;
                    }
                }

                if (tooFar)
                {
                    Destroy(spawnedAnimals[i]);
                    spawnedAnimals.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Set day/night mode for spawning.
        /// </summary>
        public void SetDayTime(bool day)
        {
            isDay = day;
        }

        /// <summary>
        /// Force spawn an animal of a specific type.
        /// </summary>
        public GameObject SpawnAnimal(int prefabIndex, Vector3 position)
        {
            if (prefabIndex < 0 || prefabIndex >= animalPrefabs.Count) return null;

            var entry = animalPrefabs[prefabIndex];
            if (entry.prefab == null) return null;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, 5f, NavMesh.AllAreas))
            {
                GameObject animal = Instantiate(entry.prefab, hit.position, Quaternion.identity);
                spawnedAnimals.Add(animal);
                return animal;
            }

            return null;
        }

        /// <summary>
        /// Get current animal count.
        /// </summary>
        public int GetAnimalCount()
        {
            spawnedAnimals.RemoveAll(a => a == null);
            return spawnedAnimals.Count;
        }

        /// <summary>
        /// Clear all spawned animals.
        /// </summary>
        public void ClearAllAnimals()
        {
            foreach (var animal in spawnedAnimals)
            {
                if (animal != null)
                {
                    Destroy(animal);
                }
            }
            spawnedAnimals.Clear();
        }
    }

    [System.Serializable]
    public class AnimalSpawnEntry
    {
        [Tooltip("Animal prefab")]
        public GameObject prefab;

        [Tooltip("Animal name (for display)")]
        public string animalName;

        [Tooltip("Spawn weight (higher = more common)")]
        [Range(0f, 10f)]
        public float spawnChance = 1f;

        [Tooltip("Only spawns at night")]
        public bool nocturnalOnly = false;

        [Tooltip("Only spawns during day")]
        public bool diurnalOnly = false;

        [Tooltip("Random scale variation (+/-)")]
        [Range(0f, 0.5f)]
        public float scaleVariation = 0.1f;
    }
}
