using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BobsPetroleum.Vehicles
{
    /// <summary>
    /// Spawns NPC cars that drive around town and visit the gas station.
    /// </summary>
    public class NPCCarSpawner : MonoBehaviour
    {
        public static NPCCarSpawner Instance { get; private set; }

        [Header("Car Prefabs")]
        [Tooltip("NPC car prefabs to spawn")]
        public List<GameObject> carPrefabs = new List<GameObject>();

        [Header("Spawn Settings")]
        [Tooltip("Spawn points around the map")]
        public Transform[] spawnPoints;

        [Tooltip("Maximum NPC cars")]
        public int maxCars = 5;

        [Tooltip("Spawn interval (seconds)")]
        public float spawnInterval = 30f;

        [Header("Waypoints")]
        [Tooltip("Waypoints for cars to follow")]
        public Transform[] cityWaypoints;

        [Header("Gas Station")]
        [Tooltip("Reference to gas station")]
        public GasStation gasStation;

        [Header("Events")]
        public UnityEvent<GameObject> onCarSpawned;
        public UnityEvent<GameObject> onCarDespawned;

        private List<GameObject> activeCars = new List<GameObject>();
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
            nextSpawnTime = Time.time + spawnInterval;
        }

        private void Update()
        {
            // Clean up
            activeCars.RemoveAll(c => c == null);

            // Spawn
            if (Time.time >= nextSpawnTime && activeCars.Count < maxCars)
            {
                SpawnCar();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }

        /// <summary>
        /// Spawn a single NPC car.
        /// </summary>
        public GameObject SpawnCar()
        {
            if (spawnPoints.Length == 0 || carPrefabs.Count == 0)
            {
                return null;
            }

            // Pick random spawn and prefab
            Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];

            // Spawn
            GameObject car = Instantiate(prefab, spawn.position, spawn.rotation);
            activeCars.Add(car);

            // Setup NPC driver
            var driver = car.GetComponent<NPCCarDriver>();
            if (driver == null)
            {
                driver = car.AddComponent<NPCCarDriver>();
            }

            // Setup patrol AI
            var patrol = car.GetComponent<NPCCarPatrol>();
            if (patrol == null)
            {
                patrol = car.AddComponent<NPCCarPatrol>();
            }

            patrol.waypoints = cityWaypoints;
            patrol.gasStation = gasStation;

            onCarSpawned?.Invoke(car);
            return car;
        }

        /// <summary>
        /// Get active car count.
        /// </summary>
        public int GetCarCount()
        {
            return activeCars.Count;
        }
    }

    /// <summary>
    /// NPC car patrol AI - drives around waypoints and visits gas station.
    /// </summary>
    public class NPCCarPatrol : MonoBehaviour
    {
        [Header("Waypoints")]
        public Transform[] waypoints;

        [Header("Gas Station")]
        public GasStation gasStation;

        [Header("Settings")]
        public float driveSpeed = 8f;
        public float waypointThreshold = 3f;
        public float gasCheckInterval = 30f;

        private int currentWaypointIndex = 0;
        private float gasCheckTimer = 0f;
        private bool goingToGasStation = false;
        private CarController car;
        private NPCCarDriver driver;

        private void Awake()
        {
            car = GetComponent<CarController>();
            driver = GetComponent<NPCCarDriver>();
        }

        private void Start()
        {
            // Start at random waypoint
            if (waypoints != null && waypoints.Length > 0)
            {
                currentWaypointIndex = Random.Range(0, waypoints.Length);
            }

            gasCheckTimer = gasCheckInterval;
        }

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            // Check if need gas
            gasCheckTimer -= Time.deltaTime;
            if (gasCheckTimer <= 0 && !goingToGasStation)
            {
                gasCheckTimer = gasCheckInterval;

                if (driver != null && driver.NeedsGas() && gasStation != null)
                {
                    // Try to go to gas station
                    if (gasStation.RequestService(car))
                    {
                        goingToGasStation = true;
                        return;
                    }
                }
            }

            if (goingToGasStation) return;

            // Drive to current waypoint
            Transform targetWaypoint = waypoints[currentWaypointIndex];
            Vector3 direction = (targetWaypoint.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetWaypoint.position);

            if (distance > waypointThreshold)
            {
                // Drive towards waypoint
                transform.position += direction * driveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.deltaTime * 2f
                );
            }
            else
            {
                // Reached waypoint, go to next
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }
        }

        /// <summary>
        /// Called when finished at gas station.
        /// </summary>
        public void OnGasStationComplete()
        {
            goingToGasStation = false;
        }
    }
}
