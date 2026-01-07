using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Garbage collection system. Garbage items give money when collected and dumped.
    /// </summary>
    public class GarbageSystem : MonoBehaviour
    {
        public static GarbageSystem Instance { get; private set; }

        [Header("Garbage Prefabs")]
        [Tooltip("Garbage prefabs to spawn")]
        public List<GarbagePrefab> garbagePrefabs = new List<GarbagePrefab>();

        [Header("Spawn Points")]
        [Tooltip("Points where garbage can spawn (set in inspector)")]
        public Transform[] garbageSpawnPoints;

        [Tooltip("Random offset from spawn points")]
        public float spawnOffset = 2f;

        [Header("Spawn Settings")]
        [Tooltip("Initial garbage count")]
        public int initialGarbageCount = 20;

        [Tooltip("Maximum garbage on map")]
        public int maxGarbage = 50;

        [Tooltip("Spawn interval (seconds)")]
        public float spawnInterval = 60f;

        [Tooltip("Garbage per spawn")]
        public int garbagePerSpawn = 5;

        [Header("Events")]
        public UnityEvent<GarbageItem> onGarbageSpawned;
        public UnityEvent<GarbageItem> onGarbageCollected;

        private List<GarbageItem> activeGarbage = new List<GarbageItem>();
        private float nextSpawnTime;

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
            for (int i = 0; i < initialGarbageCount; i++)
            {
                SpawnGarbage();
            }

            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            // Clean up collected garbage
            activeGarbage.RemoveAll(g => g == null);

            // Spawn more garbage
            if (Time.time >= nextSpawnTime)
            {
                for (int i = 0; i < garbagePerSpawn; i++)
                {
                    SpawnGarbage();
                }
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        /// <summary>
        /// Spawn a single garbage item at a random spawn point.
        /// </summary>
        public GarbageItem SpawnGarbage()
        {
            if (activeGarbage.Count >= maxGarbage)
            {
                return null;
            }

            if (garbageSpawnPoints.Length == 0 || garbagePrefabs.Count == 0)
            {
                return null;
            }

            // Pick random spawn point
            Transform spawnPoint = garbageSpawnPoints[Random.Range(0, garbageSpawnPoints.Length)];

            // Random offset
            Vector2 offset = Random.insideUnitCircle * spawnOffset;
            Vector3 spawnPos = spawnPoint.position + new Vector3(offset.x, 0, offset.y);

            // Pick random prefab based on weight
            GameObject prefab = GetRandomGarbagePrefab();
            if (prefab == null) return null;

            // Spawn
            GameObject garbageObj = Instantiate(prefab, spawnPos, Random.rotation);
            var garbageItem = garbageObj.GetComponent<GarbageItem>();

            if (garbageItem == null)
            {
                garbageItem = garbageObj.AddComponent<GarbageItem>();
            }

            activeGarbage.Add(garbageItem);
            onGarbageSpawned?.Invoke(garbageItem);

            return garbageItem;
        }

        private GameObject GetRandomGarbagePrefab()
        {
            float totalWeight = 0f;
            foreach (var entry in garbagePrefabs)
            {
                totalWeight += entry.spawnWeight;
            }

            float random = Random.Range(0f, totalWeight);
            float accumulated = 0f;

            foreach (var entry in garbagePrefabs)
            {
                accumulated += entry.spawnWeight;
                if (random <= accumulated)
                {
                    return entry.prefab;
                }
            }

            return garbagePrefabs[0].prefab;
        }

        /// <summary>
        /// Called when garbage is collected.
        /// </summary>
        public void OnGarbageCollected(GarbageItem garbage)
        {
            activeGarbage.Remove(garbage);
            onGarbageCollected?.Invoke(garbage);
        }

        /// <summary>
        /// Get total garbage count.
        /// </summary>
        public int GetGarbageCount()
        {
            return activeGarbage.Count;
        }
    }

    [System.Serializable]
    public class GarbagePrefab
    {
        public GameObject prefab;
        [Range(0f, 1f)]
        public float spawnWeight = 1f;
        public int value = 5;
    }

    /// <summary>
    /// Attach to garbage prefabs. Makes them collectible.
    /// </summary>
    public class GarbageItem : MonoBehaviour, IInteractable
    {
        [Header("Garbage Settings")]
        [Tooltip("Money value of this garbage")]
        public int value = 5;

        [Tooltip("Interaction prompt")]
        public string interactionPrompt = "Press E to Collect";

        [Header("Audio")]
        public AudioClip collectSound;

        public void Interact(PlayerController player)
        {
            Collect(player);
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        /// <summary>
        /// Collect this garbage.
        /// </summary>
        public void Collect(PlayerController player)
        {
            // Add to player's garbage inventory
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddItem(new InventoryItem
                {
                    itemId = "garbage",
                    itemName = "Garbage",
                    quantity = 1,
                    isStackable = true
                });
            }

            // Play sound
            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // Notify system
            GarbageSystem.Instance?.OnGarbageCollected(this);

            // Destroy
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Garbage dump zone. Players dump garbage here for money.
    /// </summary>
    public class GarbageDumpZone : MonoBehaviour
    {
        [Header("Dump Settings")]
        [Tooltip("Money per garbage item")]
        public int moneyPerGarbage = 10;

        [Tooltip("Dump all at once or one by one")]
        public bool dumpAllAtOnce = true;

        [Header("Audio")]
        public AudioClip dumpSound;

        [Header("Events")]
        public UnityEvent<int> onGarbageDumped;

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                DumpGarbage(player);
            }
        }

        /// <summary>
        /// Dump garbage for a player.
        /// </summary>
        public void DumpGarbage(PlayerController player)
        {
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            int garbageCount = inventory.GetItemCount("garbage");
            if (garbageCount == 0) return;

            if (dumpAllAtOnce)
            {
                // Dump all
                inventory.RemoveItem("garbage", garbageCount);
                int reward = garbageCount * moneyPerGarbage;
                inventory.AddMoney(reward);
                onGarbageDumped?.Invoke(reward);
            }
            else
            {
                // Dump one
                inventory.RemoveItem("garbage", 1);
                inventory.AddMoney(moneyPerGarbage);
                onGarbageDumped?.Invoke(moneyPerGarbage);
            }

            if (dumpSound != null)
            {
                AudioSource.PlayClipAtPoint(dumpSound, transform.position);
            }
        }
    }
}
