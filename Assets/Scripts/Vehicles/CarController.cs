using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;

namespace BobsPetroleum.Vehicles
{
    /// <summary>
    /// Car controller with driving, passenger system, and collision damage.
    /// First player in drives, others are passengers.
    /// </summary>
    public class CarController : MonoBehaviour
    {
        [Header("Car Info")]
        public string carName = "Car";
        public string carId = "car_01";

        [Header("Movement")]
        [Tooltip("Maximum speed")]
        public float maxSpeed = 30f;

        [Tooltip("Acceleration force")]
        public float acceleration = 10f;

        [Tooltip("Brake force")]
        public float brakeForce = 15f;

        [Tooltip("Steering speed")]
        public float steeringSpeed = 100f;

        [Tooltip("Drag when no input")]
        public float drag = 2f;

        [Header("Fuel")]
        [Tooltip("Maximum fuel")]
        public float maxFuel = 100f;

        [Tooltip("Current fuel")]
        public float currentFuel = 100f;

        [Tooltip("Fuel consumption per second while driving")]
        public float fuelConsumption = 1f;

        [Header("Seats")]
        [Tooltip("Driver seat position")]
        public Transform driverSeat;

        [Tooltip("Passenger seat positions")]
        public Transform[] passengerSeats;

        [Header("Entry/Exit")]
        [Tooltip("Entry trigger zones (doors)")]
        public CarEntryTrigger[] entryTriggers;

        [Tooltip("Exit position")]
        public Transform exitPoint;

        [Header("Third Person Camera")]
        [Tooltip("Camera position for driver")]
        public Transform cameraPosition;

        [Tooltip("Camera look target")]
        public Transform cameraLookTarget;

        [Header("Collision Damage")]
        [Tooltip("Damage dealt to people on collision")]
        public float collisionDamage = 30f;

        [Tooltip("Minimum speed for collision damage")]
        public float minDamageSpeed = 5f;

        [Header("Audio")]
        public AudioClip engineSound;
        public AudioClip hornSound;
        public AudioClip collisionSound;

        [Header("Events")]
        public UnityEvent<PlayerController> onDriverEntered;
        public UnityEvent<PlayerController> onDriverExited;
        public UnityEvent<PlayerController> onPassengerEntered;
        public UnityEvent<PlayerController> onPassengerExited;
        public UnityEvent onOutOfFuel;

        // State
        private Rigidbody rb;
        private AudioSource audioSource;
        private PlayerController driver;
        private List<PlayerController> passengers = new List<PlayerController>();
        private float currentSpeed;
        private bool isOccupied => driver != null;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Update()
        {
            if (driver == null) return;

            // Only local driver controls the car
            if (!driver.IsOwner) return;

            HandleInput();
            HandleFuel();
            HandleAudio();
        }

        private void FixedUpdate()
        {
            if (driver == null)
            {
                // Apply drag when unoccupied
                rb.drag = drag * 2f;
                return;
            }

            if (!driver.IsOwner) return;

            ApplyMovement();
        }

        private void HandleInput()
        {
            // Acceleration/Brake
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");

            // Steering
            if (currentSpeed > 0.1f)
            {
                float steer = horizontal * steeringSpeed * Time.deltaTime;
                transform.Rotate(Vector3.up, steer);
            }

            // Horn
            if (Input.GetKeyDown(KeyCode.H) && hornSound != null)
            {
                audioSource.PlayOneShot(hornSound);
            }

            // Exit
            if (Input.GetKeyDown(KeyCode.E))
            {
                ExitVehicle(driver);
            }
        }

        private void HandleFuel()
        {
            if (currentFuel <= 0)
            {
                onOutOfFuel?.Invoke();
                return;
            }

            // Consume fuel while moving
            if (rb.velocity.magnitude > 0.1f)
            {
                currentFuel -= fuelConsumption * Time.deltaTime;
                currentFuel = Mathf.Max(0, currentFuel);
            }
        }

        private void HandleAudio()
        {
            // Engine sound based on speed
            if (engineSound != null && !audioSource.isPlaying)
            {
                audioSource.clip = engineSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            audioSource.pitch = 0.8f + (currentSpeed / maxSpeed) * 0.5f;
        }

        private void ApplyMovement()
        {
            if (currentFuel <= 0) return;

            float vertical = Input.GetAxis("Vertical");

            // Calculate force
            Vector3 force = transform.forward * vertical * acceleration;

            // Apply force
            rb.AddForce(force, ForceMode.Acceleration);

            // Limit speed
            currentSpeed = rb.velocity.magnitude;
            if (currentSpeed > maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }

            // Apply drag
            rb.drag = Mathf.Abs(vertical) < 0.1f ? drag : 0.5f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if we hit a person
            var player = collision.gameObject.GetComponent<PlayerController>();
            var npc = collision.gameObject.GetComponent<NPC.NPCHealth>();

            if (currentSpeed < minDamageSpeed) return;

            // Calculate damage based on speed
            float damage = collisionDamage * (currentSpeed / maxSpeed);

            if (player != null && player != driver && !passengers.Contains(player))
            {
                var health = player.GetComponent<PlayerHealth>();
                health?.TakeDamage(damage);

                if (collisionSound != null)
                {
                    audioSource.PlayOneShot(collisionSound);
                }
            }
            else if (npc != null)
            {
                npc.TakeDamage(damage);

                if (collisionSound != null)
                {
                    audioSource.PlayOneShot(collisionSound);
                }
            }

            // Car-to-car collision
            var otherCar = collision.gameObject.GetComponent<CarController>();
            if (otherCar != null)
            {
                if (collisionSound != null)
                {
                    audioSource.PlayOneShot(collisionSound);
                }
            }
        }

        #region Entry/Exit

        /// <summary>
        /// Player enters the vehicle.
        /// </summary>
        public bool EnterVehicle(PlayerController player)
        {
            if (driver == null)
            {
                // Become driver
                driver = player;
                player.EnterVehicle();

                // Position player
                player.transform.SetParent(driverSeat);
                player.transform.localPosition = Vector3.zero;
                player.transform.localRotation = Quaternion.identity;

                // Setup camera
                SetupDriverCamera(player);

                onDriverEntered?.Invoke(player);
                return true;
            }
            else
            {
                // Try passenger seat
                for (int i = 0; i < passengerSeats.Length; i++)
                {
                    if (!IsPassengerSeatOccupied(i))
                    {
                        passengers.Add(player);
                        player.EnterVehicle();

                        player.transform.SetParent(passengerSeats[i]);
                        player.transform.localPosition = Vector3.zero;
                        player.transform.localRotation = Quaternion.identity;

                        onPassengerEntered?.Invoke(player);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Player exits the vehicle.
        /// </summary>
        public void ExitVehicle(PlayerController player)
        {
            Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position + transform.right * 2f;

            if (player == driver)
            {
                driver = null;
                player.transform.SetParent(null);
                player.ExitVehicle(exitPos);

                // Stop engine sound
                audioSource.Stop();

                onDriverExited?.Invoke(player);
            }
            else if (passengers.Contains(player))
            {
                passengers.Remove(player);
                player.transform.SetParent(null);
                player.ExitVehicle(exitPos);

                onPassengerExited?.Invoke(player);
            }
        }

        private bool IsPassengerSeatOccupied(int seatIndex)
        {
            // Check if any passenger is using this seat
            foreach (var passenger in passengers)
            {
                if (passenger.transform.parent == passengerSeats[seatIndex])
                {
                    return true;
                }
            }
            return false;
        }

        private void SetupDriverCamera(PlayerController player)
        {
            if (player.playerCamera != null && cameraPosition != null)
            {
                player.playerCamera.transform.position = cameraPosition.position;
                if (cameraLookTarget != null)
                {
                    player.playerCamera.transform.LookAt(cameraLookTarget);
                }
            }
        }

        #endregion

        #region Fuel

        /// <summary>
        /// Refuel the car.
        /// </summary>
        public void Refuel(float amount)
        {
            currentFuel = Mathf.Min(currentFuel + amount, maxFuel);
        }

        /// <summary>
        /// Fill tank completely.
        /// </summary>
        public void FillTank()
        {
            currentFuel = maxFuel;
        }

        /// <summary>
        /// Get fuel percentage.
        /// </summary>
        public float GetFuelPercentage()
        {
            return currentFuel / maxFuel;
        }

        /// <summary>
        /// Check if needs fuel.
        /// </summary>
        public bool NeedsFuel()
        {
            return currentFuel < maxFuel * 0.3f;
        }

        #endregion

        /// <summary>
        /// Check if car is occupied.
        /// </summary>
        public bool IsOccupied => isOccupied;

        /// <summary>
        /// Get current speed.
        /// </summary>
        public float CurrentSpeed => currentSpeed;

        /// <summary>
        /// Get driver.
        /// </summary>
        public PlayerController GetDriver() => driver;

        /// <summary>
        /// Get passengers.
        /// </summary>
        public List<PlayerController> GetPassengers() => new List<PlayerController>(passengers);
    }

    /// <summary>
    /// Trigger zone for entering a car. Place near doors.
    /// </summary>
    public class CarEntryTrigger : MonoBehaviour, Player.IInteractable
    {
        [Header("Car Reference")]
        public CarController car;

        [Header("Interaction")]
        public string enterPrompt = "Press E to Enter";
        public string exitPrompt = "Press E to Exit";

        private void Start()
        {
            if (car == null)
            {
                car = GetComponentInParent<CarController>();
            }
        }

        public void Interact(PlayerController player)
        {
            if (car == null) return;

            if (player.IsInVehicle)
            {
                car.ExitVehicle(player);
            }
            else
            {
                car.EnterVehicle(player);
            }
        }

        public string GetInteractionPrompt()
        {
            return car?.IsOccupied == true ? exitPrompt : enterPrompt;
        }
    }
}
