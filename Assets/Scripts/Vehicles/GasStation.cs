using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;

namespace BobsPetroleum.Vehicles
{
    /// <summary>
    /// Gas station pump system. One car at a time.
    /// Players interact to pump gas for customer cars.
    /// </summary>
    public class GasStation : MonoBehaviour
    {
        public static GasStation Instance { get; private set; }

        [Header("Station Settings")]
        [Tooltip("Price per unit of fuel")]
        public float fuelPrice = 2f;

        [Tooltip("Fuel dispensed per second")]
        public float fuelRate = 10f;

        [Header("Pump Positions")]
        [Tooltip("Position for car to stop")]
        public Transform pumpPosition;

        [Tooltip("Position for player to stand")]
        public Transform attendantPosition;

        [Header("Queue")]
        [Tooltip("Queue positions for waiting cars")]
        public Transform[] queuePositions;

        [Header("Audio")]
        public AudioClip pumpingSound;
        public AudioClip completeSound;

        [Header("Events")]
        public UnityEvent<CarController> onCarArrived;
        public UnityEvent<CarController> onCarServiced;
        public UnityEvent<CarController> onCarLeft;
        public UnityEvent<float> onPaymentReceived;

        // State
        private CarController currentCar;
        private Queue<CarController> carQueue = new Queue<CarController>();
        private bool isPumping = false;
        private float fuelPumped = 0f;
        private AudioSource audioSource;

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

        /// <summary>
        /// Car requests service at the gas station.
        /// </summary>
        public bool RequestService(CarController car)
        {
            if (currentCar == null)
            {
                // Station is free
                currentCar = car;
                MoveToPump(car);
                onCarArrived?.Invoke(car);
                return true;
            }
            else if (carQueue.Count < queuePositions.Length)
            {
                // Add to queue
                carQueue.Enqueue(car);
                MoveToQueue(car, carQueue.Count - 1);
                return true;
            }

            return false; // Station full
        }

        /// <summary>
        /// Start pumping gas (player interaction).
        /// </summary>
        public void StartPumping(PlayerController player)
        {
            if (currentCar == null || isPumping) return;

            isPumping = true;
            fuelPumped = 0f;

            if (pumpingSound != null)
            {
                audioSource.clip = pumpingSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        /// <summary>
        /// Stop pumping gas.
        /// </summary>
        public void StopPumping(PlayerController player)
        {
            if (!isPumping) return;

            isPumping = false;
            audioSource.Stop();

            // Calculate payment
            float payment = fuelPumped * fuelPrice;

            // Pay the player
            var inventory = player.GetComponent<PlayerInventory>();
            inventory?.AddMoney(Mathf.RoundToInt(payment));

            onPaymentReceived?.Invoke(payment);

            if (completeSound != null)
            {
                audioSource.PlayOneShot(completeSound);
            }

            // Service complete
            CompleteService();
        }

        private void Update()
        {
            if (isPumping && currentCar != null)
            {
                // Pump fuel
                float fuelNeeded = currentCar.maxFuel - currentCar.currentFuel;

                if (fuelNeeded > 0)
                {
                    float fuelToPump = fuelRate * Time.deltaTime;
                    fuelToPump = Mathf.Min(fuelToPump, fuelNeeded);

                    currentCar.Refuel(fuelToPump);
                    fuelPumped += fuelToPump;
                }
                else
                {
                    // Tank is full
                    // Auto-stop or wait for player
                }
            }
        }

        private void CompleteService()
        {
            if (currentCar != null)
            {
                onCarServiced?.Invoke(currentCar);

                // Tell car to leave
                var aiDriver = currentCar.GetComponent<NPCCarDriver>();
                aiDriver?.LeaveStation();

                onCarLeft?.Invoke(currentCar);
                currentCar = null;

                // Process queue
                ProcessQueue();
            }
        }

        private void ProcessQueue()
        {
            if (carQueue.Count > 0)
            {
                currentCar = carQueue.Dequeue();
                MoveToPump(currentCar);
                onCarArrived?.Invoke(currentCar);

                // Move other cars forward in queue
                int index = 0;
                foreach (var car in carQueue)
                {
                    MoveToQueue(car, index);
                    index++;
                }
            }
        }

        private void MoveToPump(CarController car)
        {
            var aiDriver = car.GetComponent<NPCCarDriver>();
            if (aiDriver != null)
            {
                aiDriver.DriveToPosition(pumpPosition.position, pumpPosition.rotation);
            }
        }

        private void MoveToQueue(CarController car, int queueIndex)
        {
            if (queueIndex >= 0 && queueIndex < queuePositions.Length)
            {
                var aiDriver = car.GetComponent<NPCCarDriver>();
                if (aiDriver != null)
                {
                    aiDriver.DriveToPosition(queuePositions[queueIndex].position, queuePositions[queueIndex].rotation);
                }
            }
        }

        /// <summary>
        /// Check if station has a car waiting.
        /// </summary>
        public bool HasCarWaiting()
        {
            return currentCar != null;
        }

        /// <summary>
        /// Get current car at pump.
        /// </summary>
        public CarController GetCurrentCar()
        {
            return currentCar;
        }

        /// <summary>
        /// Get queue count.
        /// </summary>
        public int GetQueueCount()
        {
            return carQueue.Count;
        }

        /// <summary>
        /// Check if pumping.
        /// </summary>
        public bool IsPumping => isPumping;

        /// <summary>
        /// Get current fuel pumped.
        /// </summary>
        public float GetFuelPumped() => fuelPumped;

        /// <summary>
        /// Get current cost.
        /// </summary>
        public float GetCurrentCost() => fuelPumped * fuelPrice;
    }

    /// <summary>
    /// Gas pump interaction trigger.
    /// </summary>
    public class GasPumpTrigger : MonoBehaviour, Player.IInteractable
    {
        [Header("Station Reference")]
        public GasStation station;

        private bool isPlayerPumping = false;
        private PlayerController pumpingPlayer;

        private void Start()
        {
            if (station == null)
            {
                station = GetComponentInParent<GasStation>();
            }
        }

        public void Interact(PlayerController player)
        {
            if (station == null) return;

            if (isPlayerPumping)
            {
                station.StopPumping(player);
                isPlayerPumping = false;
                pumpingPlayer = null;
            }
            else if (station.HasCarWaiting())
            {
                station.StartPumping(player);
                isPlayerPumping = true;
                pumpingPlayer = player;
            }
        }

        public string GetInteractionPrompt()
        {
            if (isPlayerPumping)
            {
                return $"Press E to Stop (${station?.GetCurrentCost():F2})";
            }

            if (station?.HasCarWaiting() == true)
            {
                return "Press E to Pump Gas";
            }

            return "No Car at Pump";
        }
    }

    /// <summary>
    /// NPC car AI driver for gas station visits.
    /// </summary>
    public class NPCCarDriver : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Chance to need gas (0-1)")]
        [Range(0f, 1f)]
        public float needsGasChance = 0.3f;

        [Tooltip("Speed")]
        public float driveSpeed = 10f;

        private CarController car;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool isDriving = false;
        private bool needsGas = false;

        private void Awake()
        {
            car = GetComponent<CarController>();
            needsGas = Random.value < needsGasChance;
        }

        private void Update()
        {
            if (!isDriving) return;

            // Simple drive to position
            Vector3 direction = (targetPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance > 1f)
            {
                transform.position += direction * driveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
            }
            else
            {
                isDriving = false;
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        /// <summary>
        /// Drive to a position.
        /// </summary>
        public void DriveToPosition(Vector3 position, Quaternion rotation)
        {
            targetPosition = position;
            targetRotation = rotation;
            isDriving = true;
        }

        /// <summary>
        /// Leave the station.
        /// </summary>
        public void LeaveStation()
        {
            // Drive away and despawn
            Vector3 leavePos = transform.position + transform.forward * 100f;
            DriveToPosition(leavePos, transform.rotation);

            Destroy(gameObject, 15f);
        }

        /// <summary>
        /// Check if this car needs gas.
        /// </summary>
        public bool NeedsGas()
        {
            return needsGas || (car != null && car.NeedsFuel());
        }
    }
}
