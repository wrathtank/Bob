using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace BobsPetroleum.Economy
{
    /// <summary>
    /// GAS PUMP - Where customers (and players) get gas!
    /// Tracks gallons pumped and calculates cost.
    ///
    /// SETUP:
    /// 1. Create pump model with nozzle
    /// 2. Add GasPump component
    /// 3. Assign UI elements and nozzle transform
    /// 4. Done! Works with CustomerAI automatically.
    ///
    /// INTERACTION:
    /// - Player can interact to start/stop pumping
    /// - CustomerAI uses it automatically
    /// - Tracks gallons and cost
    /// </summary>
    public class GasPump : MonoBehaviour
    {
        [Header("=== PUMP SETTINGS ===")]
        [Tooltip("Price per gallon")]
        public float pricePerGallon = 3.50f;

        [Tooltip("Gallons pumped per second")]
        public float gallonsPerSecond = 0.5f;

        [Tooltip("Maximum gallons in tank")]
        public float maxGallons = 500f;

        [Tooltip("Current gallons available")]
        public float currentGallons = 500f;

        [Header("=== STATE ===")]
        [SerializeField] private bool isPumping = false;
        [SerializeField] private bool isOccupied = false;
        [SerializeField] private float sessionGallons = 0f;
        [SerializeField] private float sessionCost = 0f;

        public bool IsPumping => isPumping;
        public bool IsOccupied => isOccupied;
        public bool IsDonePumping => !isPumping && sessionGallons > 0;
        public float SessionGallons => sessionGallons;
        public float SessionCost => sessionCost;

        [Header("=== VISUAL REFERENCES ===")]
        [Tooltip("Nozzle object (for pickup animation)")]
        public Transform nozzle;

        [Tooltip("Nozzle resting position")]
        public Transform nozzleRestPosition;

        [Tooltip("Nozzle pumping position (at car)")]
        public Transform nozzlePumpPosition;

        [Tooltip("Hose renderer (optional)")]
        public LineRenderer hoseRenderer;

        [Header("=== UI DISPLAY ===")]
        [Tooltip("Gallons display text")]
        public TMP_Text gallonsText;

        [Tooltip("Cost display text")]
        public TMP_Text costText;

        [Tooltip("Price per gallon display")]
        public TMP_Text priceText;

        [Tooltip("Pump number display")]
        public TMP_Text pumpNumberText;

        [Tooltip("Pump number")]
        public int pumpNumber = 1;

        [Header("=== AUDIO ===")]
        [Tooltip("Pumping loop sound")]
        public AudioClip pumpingSound;

        [Tooltip("Start pump sound")]
        public AudioClip startSound;

        [Tooltip("Stop pump sound")]
        public AudioClip stopSound;

        [Tooltip("Empty tank sound")]
        public AudioClip emptySound;

        [Header("=== EFFECTS ===")]
        [Tooltip("Fuel particles (optional)")]
        public ParticleSystem fuelParticles;

        [Header("=== INTERACTION ===")]
        [Tooltip("Interaction prompt")]
        public string interactionPrompt = "[E] Use Pump";

        [Tooltip("Interaction radius")]
        public float interactionRadius = 2f;

        [Header("=== EVENTS ===")]
        public UnityEvent onPumpingStarted;
        public UnityEvent onPumpingStopped;
        public UnityEvent onTankEmpty;
        public UnityEvent<float> onGallonsPumped;

        // Internal
        private AudioSource audioSource;
        private AI.CustomerAI currentCustomer;
        private Player.PlayerController currentPlayer;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.loop = true;
            }
        }

        private void Start()
        {
            // Initialize displays
            UpdateDisplay();

            if (priceText != null)
                priceText.text = $"${pricePerGallon:F2}/gal";

            if (pumpNumberText != null)
                pumpNumberText.text = $"Pump {pumpNumber}";

            // Reset nozzle position
            if (nozzle != null && nozzleRestPosition != null)
            {
                nozzle.position = nozzleRestPosition.position;
                nozzle.rotation = nozzleRestPosition.rotation;
            }
        }

        private void Update()
        {
            if (isPumping)
            {
                // Pump gas
                float gallonsToPump = gallonsPerSecond * Time.deltaTime;

                if (currentGallons >= gallonsToPump)
                {
                    currentGallons -= gallonsToPump;
                    sessionGallons += gallonsToPump;
                    sessionCost = sessionGallons * pricePerGallon;

                    onGallonsPumped?.Invoke(gallonsToPump);
                }
                else
                {
                    // Tank empty!
                    StopPumping();
                    PlaySound(emptySound);
                    onTankEmpty?.Invoke();
                }

                UpdateDisplay();
            }

            // Update hose
            UpdateHose();
        }

        #region Pumping Control

        /// <summary>
        /// Start pumping gas (called by CustomerAI or player).
        /// </summary>
        public void StartPumping(AI.CustomerAI customer = null)
        {
            if (isPumping || currentGallons <= 0) return;

            isPumping = true;
            isOccupied = true;
            currentCustomer = customer;
            sessionGallons = 0f;
            sessionCost = 0f;

            // Move nozzle
            if (nozzle != null && nozzlePumpPosition != null)
            {
                nozzle.position = nozzlePumpPosition.position;
                nozzle.rotation = nozzlePumpPosition.rotation;
            }

            // Audio
            PlaySound(startSound);
            if (pumpingSound != null)
            {
                audioSource.clip = pumpingSound;
                audioSource.Play();
            }

            // Particles
            if (fuelParticles != null)
                fuelParticles.Play();

            onPumpingStarted?.Invoke();
            Debug.Log($"[GasPump {pumpNumber}] Started pumping");
        }

        /// <summary>
        /// Start pumping for player.
        /// </summary>
        public void StartPumpingForPlayer(Player.PlayerController player)
        {
            if (isPumping || currentGallons <= 0) return;

            currentPlayer = player;
            StartPumping(null);
        }

        /// <summary>
        /// Stop pumping and return cost.
        /// </summary>
        public float StopPumping()
        {
            if (!isPumping) return sessionCost;

            isPumping = false;

            // Return nozzle
            if (nozzle != null && nozzleRestPosition != null)
            {
                nozzle.position = nozzleRestPosition.position;
                nozzle.rotation = nozzleRestPosition.rotation;
            }

            // Stop audio
            audioSource.Stop();
            PlaySound(stopSound);

            // Stop particles
            if (fuelParticles != null)
                fuelParticles.Stop();

            float finalCost = sessionCost;

            onPumpingStopped?.Invoke();
            Debug.Log($"[GasPump {pumpNumber}] Stopped. Gallons: {sessionGallons:F2}, Cost: ${sessionCost:F2}");

            return finalCost;
        }

        /// <summary>
        /// Reset pump for next customer.
        /// </summary>
        public void ResetPump()
        {
            isOccupied = false;
            currentCustomer = null;
            currentPlayer = null;
            sessionGallons = 0f;
            sessionCost = 0f;
            UpdateDisplay();
        }

        /// <summary>
        /// Refill the pump tank (for supply deliveries).
        /// </summary>
        public void RefillTank(float gallons)
        {
            currentGallons = Mathf.Min(currentGallons + gallons, maxGallons);
            Debug.Log($"[GasPump {pumpNumber}] Refilled. Current: {currentGallons:F0}/{maxGallons:F0} gallons");
        }

        #endregion

        #region Player Interaction

        /// <summary>
        /// Called when player interacts with pump.
        /// </summary>
        public void OnPlayerInteract(Player.PlayerController player)
        {
            if (!isOccupied)
            {
                // Start pumping
                StartPumpingForPlayer(player);
            }
            else if (currentPlayer == player && isPumping)
            {
                // Stop pumping
                float cost = StopPumping();

                // Charge player
                var inventory = player.GetComponent<Player.PlayerInventory>();
                if (inventory != null)
                {
                    inventory.SpendMoney(Mathf.CeilToInt(cost));
                }

                ResetPump();
            }
        }

        /// <summary>
        /// Get interaction prompt.
        /// </summary>
        public string GetInteractionPrompt()
        {
            if (!isOccupied)
            {
                return interactionPrompt;
            }
            else if (isPumping)
            {
                return $"[E] Stop (${sessionCost:F2})";
            }
            return "";
        }

        #endregion

        #region Display

        private void UpdateDisplay()
        {
            if (gallonsText != null)
            {
                gallonsText.text = $"{sessionGallons:F2} gal";
            }

            if (costText != null)
            {
                costText.text = $"${sessionCost:F2}";
            }
        }

        private void UpdateHose()
        {
            if (hoseRenderer == null || nozzle == null || nozzleRestPosition == null) return;

            // Simple line from rest to nozzle
            hoseRenderer.positionCount = 2;
            hoseRenderer.SetPosition(0, nozzleRestPosition.position);
            hoseRenderer.SetPosition(1, nozzle.position);
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

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Interaction radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);

            // Nozzle positions
            if (nozzleRestPosition != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(nozzleRestPosition.position, 0.1f);
            }

            if (nozzlePumpPosition != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(nozzlePumpPosition.position, 0.1f);
            }
        }

        #endregion
    }
}
