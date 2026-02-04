using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Handles player death and respawn with settable "home" spawn points.
    /// Gas station workers respawn at gas station, explorers respawn at their set home.
    ///
    /// SETUP:
    /// 1. Add this component to your player prefab
    /// 2. Place HomeSpot prefabs around the world
    /// 3. Players interact with HomeSpot to set their respawn
    /// 4. Done!
    /// </summary>
    public class DeathRespawnSystem : MonoBehaviour
    {
        public static DeathRespawnSystem LocalPlayer { get; private set; }

        [Header("=== HEALTH ===")]
        [Tooltip("Player's max health")]
        public float maxHealth = 100f;

        [Tooltip("Current health")]
        public float currentHealth = 100f;

        [Tooltip("Is player alive")]
        public bool isAlive = true;

        [Tooltip("Invincibility after respawn (seconds)")]
        public float respawnInvincibilityTime = 3f;

        [Header("=== RESPAWN ===")]
        [Tooltip("Default respawn point (gas station)")]
        public Transform defaultRespawnPoint;

        [Tooltip("Current home spawn point")]
        public HomeSpot currentHome;

        [Tooltip("Respawn delay after death")]
        public float respawnDelay = 3f;

        [Tooltip("Keep inventory on death")]
        public bool keepInventoryOnDeath = true;

        [Tooltip("Money lost on death (percentage 0-1)")]
        [Range(0f, 1f)]
        public float moneyLossOnDeath = 0.1f;

        [Header("=== DEATH EFFECTS ===")]
        [Tooltip("Death ragdoll prefab")]
        public GameObject ragdollPrefab;

        [Tooltip("Death particle effect")]
        public ParticleSystem deathParticles;

        [Tooltip("Death sound")]
        public AudioClip deathSound;

        [Tooltip("Respawn sound")]
        public AudioClip respawnSound;

        [Header("=== VISUALS ===")]
        [Tooltip("Player mesh/model")]
        public GameObject playerModel;

        [Tooltip("Fade screen on death")]
        public bool fadeOnDeath = true;

        [Tooltip("Death screen UI")]
        public GameObject deathScreenUI;

        [Header("=== DAMAGE SOURCES ===")]
        [Tooltip("Fall damage enabled")]
        public bool enableFallDamage = true;

        [Tooltip("Minimum fall distance for damage")]
        public float minFallDistance = 5f;

        [Tooltip("Damage per unit fallen")]
        public float fallDamageMultiplier = 10f;

        [Tooltip("Kill plane Y level (instant death below)")]
        public float killPlaneY = -50f;

        [Header("=== EVENTS ===")]
        public UnityEvent<float> onHealthChanged;
        public UnityEvent onDeath;
        public UnityEvent onRespawn;
        public UnityEvent<HomeSpot> onHomeSet;

        // Runtime
        private CharacterController characterController;
        private PlayerController playerController;
        private PlayerInventory inventory;
        private AudioSource audioSource;
        private float lastGroundedY;
        private bool isInvincible;
        private Coroutine respawnCoroutine;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController>();
            inventory = GetComponent<PlayerInventory>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            // Set as local player if this is owner (for multiplayer)
            // For single player, first one is local
            if (LocalPlayer == null)
            {
                LocalPlayer = this;
            }

            // Find default respawn if not set
            if (defaultRespawnPoint == null)
            {
                var spawnSystem = Core.CloneSpawnSystem.Instance;
                if (spawnSystem != null && spawnSystem.spawnTubes.Length > 0)
                {
                    defaultRespawnPoint = spawnSystem.spawnTubes[0].spawnPoint;
                }
            }

            currentHealth = maxHealth;
            lastGroundedY = transform.position.y;
        }

        private void Update()
        {
            if (!isAlive) return;

            // Track fall distance
            TrackFalling();

            // Check kill plane
            CheckKillPlane();
        }

        #region Damage System

        /// <summary>
        /// Deal damage to player
        /// </summary>
        public void TakeDamage(float damage, GameObject source = null)
        {
            if (!isAlive || isInvincible) return;

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            onHealthChanged?.Invoke(currentHealth / maxHealth);

            Debug.Log($"[Death] Player took {damage} damage. HP: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                Die(source);
            }
        }

        /// <summary>
        /// Heal player
        /// </summary>
        public void Heal(float amount)
        {
            if (!isAlive) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            onHealthChanged?.Invoke(currentHealth / maxHealth);

            Debug.Log($"[Death] Player healed {amount}. HP: {currentHealth}/{maxHealth}");
        }

        /// <summary>
        /// Set health directly
        /// </summary>
        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            onHealthChanged?.Invoke(currentHealth / maxHealth);
        }

        #endregion

        #region Death

        /// <summary>
        /// Kill the player
        /// </summary>
        public void Die(GameObject killer = null)
        {
            if (!isAlive) return;

            isAlive = false;
            currentHealth = 0;

            // Disable controls
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            // Play death effects
            if (deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            if (deathParticles != null)
            {
                deathParticles.Play();
            }

            // Spawn ragdoll
            if (ragdollPrefab != null)
            {
                var ragdoll = Instantiate(ragdollPrefab, transform.position, transform.rotation);
                Destroy(ragdoll, 10f);
            }

            // Hide player model
            if (playerModel != null)
            {
                playerModel.SetActive(false);
            }

            // Show death UI
            if (deathScreenUI != null)
            {
                deathScreenUI.SetActive(true);
            }

            // Lose money
            if (inventory != null && moneyLossOnDeath > 0)
            {
                int loss = Mathf.RoundToInt(inventory.GetMoney() * moneyLossOnDeath);
                inventory.AddMoney(-loss);
                Debug.Log($"[Death] Lost ${loss} on death");
            }

            onDeath?.Invoke();

            // Start respawn countdown
            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
            }
            respawnCoroutine = StartCoroutine(RespawnAfterDelay());

            Debug.Log($"[Death] Player died! Killer: {killer?.name ?? "Unknown"}");
        }

        #endregion

        #region Respawn

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        /// <summary>
        /// Respawn player at their home or default point
        /// </summary>
        public void Respawn()
        {
            // Get respawn position
            Vector3 respawnPos = GetRespawnPosition();
            Quaternion respawnRot = GetRespawnRotation();

            // Teleport
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.position = respawnPos;
            transform.rotation = respawnRot;

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            // Restore health
            currentHealth = maxHealth;
            isAlive = true;

            // Re-enable controls
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Show player model
            if (playerModel != null)
            {
                playerModel.SetActive(true);
            }

            // Hide death UI
            if (deathScreenUI != null)
            {
                deathScreenUI.SetActive(false);
            }

            // Play respawn sound
            if (respawnSound != null)
            {
                audioSource.PlayOneShot(respawnSound);
            }

            // Grant invincibility
            StartCoroutine(InvincibilityPeriod());

            onRespawn?.Invoke();
            onHealthChanged?.Invoke(1f);

            Debug.Log($"[Death] Player respawned at {respawnPos}");
        }

        private Vector3 GetRespawnPosition()
        {
            // Priority: Current home > Default spawn
            if (currentHome != null && currentHome.spawnPoint != null)
            {
                return currentHome.spawnPoint.position;
            }

            if (defaultRespawnPoint != null)
            {
                return defaultRespawnPoint.position;
            }

            // Fallback to origin
            return Vector3.zero;
        }

        private Quaternion GetRespawnRotation()
        {
            if (currentHome != null && currentHome.spawnPoint != null)
            {
                return currentHome.spawnPoint.rotation;
            }

            if (defaultRespawnPoint != null)
            {
                return defaultRespawnPoint.rotation;
            }

            return Quaternion.identity;
        }

        private IEnumerator InvincibilityPeriod()
        {
            isInvincible = true;

            // Flash effect
            float elapsed = 0f;
            while (elapsed < respawnInvincibilityTime)
            {
                elapsed += Time.deltaTime;

                // Flash player model
                if (playerModel != null)
                {
                    bool visible = Mathf.FloorToInt(elapsed * 10) % 2 == 0;
                    playerModel.SetActive(visible);
                }

                yield return null;
            }

            if (playerModel != null)
            {
                playerModel.SetActive(true);
            }

            isInvincible = false;
        }

        #endregion

        #region Home System

        /// <summary>
        /// Set player's home spawn point
        /// </summary>
        public void SetHome(HomeSpot newHome)
        {
            if (newHome == null) return;

            // Unset old home
            if (currentHome != null)
            {
                currentHome.SetOccupied(this, false);
            }

            currentHome = newHome;
            newHome.SetOccupied(this, true);

            onHomeSet?.Invoke(newHome);

            UI.HUDManager.Instance?.ShowNotification($"Home set to: {newHome.spotName}");
            Debug.Log($"[Death] Home set to: {newHome.spotName}");
        }

        /// <summary>
        /// Clear home (respawn at default)
        /// </summary>
        public void ClearHome()
        {
            if (currentHome != null)
            {
                currentHome.SetOccupied(this, false);
                currentHome = null;
            }

            UI.HUDManager.Instance?.ShowNotification("Home cleared - will respawn at gas station");
        }

        #endregion

        #region Fall Damage

        private void TrackFalling()
        {
            if (!enableFallDamage) return;

            if (characterController != null && characterController.isGrounded)
            {
                // Calculate fall distance
                float fallDistance = lastGroundedY - transform.position.y;

                if (fallDistance > minFallDistance)
                {
                    float damage = (fallDistance - minFallDistance) * fallDamageMultiplier;
                    TakeDamage(damage);
                    Debug.Log($"[Death] Fall damage: {damage} (fell {fallDistance}m)");
                }

                lastGroundedY = transform.position.y;
            }
            else
            {
                // Update highest point while airborne
                if (transform.position.y > lastGroundedY)
                {
                    lastGroundedY = transform.position.y;
                }
            }
        }

        private void CheckKillPlane()
        {
            if (transform.position.y < killPlaneY)
            {
                Die(null);
            }
        }

        #endregion

        #region Properties

        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool HasHome => currentHome != null;
        public string HomeName => currentHome?.spotName ?? "Gas Station";
        public bool IsInvincible => isInvincible;

        #endregion
    }

    /// <summary>
    /// A spot players can set as their home/respawn point
    /// </summary>
    public class HomeSpot : MonoBehaviour
    {
        [Header("=== HOME SPOT ===")]
        [Tooltip("Name of this home spot")]
        public string spotName = "Home";

        [Tooltip("Where player spawns")]
        public Transform spawnPoint;

        [Tooltip("Interaction range")]
        public float interactionRange = 2f;

        [Tooltip("Key to set as home")]
        public KeyCode setHomeKey = KeyCode.H;

        [Tooltip("Prompt text")]
        public string promptText = "Press H to set as Home";

        [Header("=== VISUALS ===")]
        [Tooltip("Bed/couch/marker mesh")]
        public GameObject homeMesh;

        [Tooltip("Occupied indicator (pillow, etc)")]
        public GameObject occupiedIndicator;

        [Tooltip("Particle effect when set")]
        public ParticleSystem setHomeEffect;

        [Header("=== AUDIO ===")]
        [Tooltip("Set home sound")]
        public AudioClip setHomeSound;

        // Runtime
        private DeathRespawnSystem occupyingPlayer;
        private AudioSource audioSource;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Create spawn point if missing
            if (spawnPoint == null)
            {
                GameObject sp = new GameObject("SpawnPoint");
                sp.transform.SetParent(transform);
                sp.transform.localPosition = Vector3.forward;
                spawnPoint = sp.transform;
            }

            UpdateVisuals();
        }

        private void Update()
        {
            // Check for nearby player
            var player = DeathRespawnSystem.LocalPlayer;
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= interactionRange)
            {
                // Show prompt
                UI.HUDManager.Instance?.ShowInteractionPrompt(promptText);

                // Check input
                if (Input.GetKeyDown(setHomeKey))
                {
                    player.SetHome(this);

                    // Effects
                    if (setHomeSound != null)
                    {
                        audioSource.PlayOneShot(setHomeSound);
                    }

                    if (setHomeEffect != null)
                    {
                        setHomeEffect.Play();
                    }
                }
            }
        }

        public void SetOccupied(DeathRespawnSystem player, bool occupied)
        {
            if (occupied)
            {
                occupyingPlayer = player;
            }
            else if (occupyingPlayer == player)
            {
                occupyingPlayer = null;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (occupiedIndicator != null)
            {
                occupiedIndicator.SetActive(occupyingPlayer != null);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            if (spawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.5f);
                Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward);
            }
        }
    }
}
