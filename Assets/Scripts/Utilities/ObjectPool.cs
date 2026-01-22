using UnityEngine;
using System.Collections.Generic;

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// Generic object pooling for performance optimization.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 10;
            public bool expandable = true;
        }

        [Header("Pools")]
        public List<Pool> pools = new List<Pool>();

        private Dictionary<string, Queue<GameObject>> poolDictionary;
        private Dictionary<string, Pool> poolSettings;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            poolSettings = new Dictionary<string, Pool>();

            foreach (var pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();
                poolSettings[pool.tag] = pool;

                for (int i = 0; i < pool.initialSize; i++)
                {
                    GameObject obj = CreateNewObject(pool);
                    objectPool.Enqueue(obj);
                }

                poolDictionary[pool.tag] = objectPool;
            }
        }

        private GameObject CreateNewObject(Pool pool)
        {
            GameObject obj = Instantiate(pool.prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            return obj;
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
                return null;
            }

            Queue<GameObject> pool = poolDictionary[tag];

            GameObject obj;

            if (pool.Count > 0)
            {
                obj = pool.Dequeue();
            }
            else if (poolSettings[tag].expandable)
            {
                obj = CreateNewObject(poolSettings[tag]);
            }
            else
            {
                return null;
            }

            obj.SetActive(true);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.SetParent(null);

            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnSpawnFromPool();

            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void Despawn(string tag, GameObject obj)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Destroy(obj);
                return;
            }

            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnReturnToPool();

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            poolDictionary[tag].Enqueue(obj);
        }

        /// <summary>
        /// Despawn after delay.
        /// </summary>
        public void DespawnDelayed(string tag, GameObject obj, float delay)
        {
            StartCoroutine(DespawnCoroutine(tag, obj, delay));
        }

        private System.Collections.IEnumerator DespawnCoroutine(string tag, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Despawn(tag, obj);
        }
    }

    /// <summary>
    /// Interface for poolable objects.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }
}
