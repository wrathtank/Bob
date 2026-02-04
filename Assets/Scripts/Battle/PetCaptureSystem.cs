using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace BobsPetroleum.Battle
{
    /// <summary>
    /// Pet capture system using a net - Pokemon style!
    /// Throw net at wild creatures to catch them.
    /// </summary>
    public class PetCaptureSystem : MonoBehaviour
    {
        public static PetCaptureSystem Instance { get; private set; }

        [Header("Capture Settings")]
        [Tooltip("Net prefab to throw")]
        public GameObject netPrefab;

        [Tooltip("Throw force")]
        public float throwForce = 20f;

        [Tooltip("Throw upward angle")]
        public float throwAngle = 15f;

        [Tooltip("Base capture chance (0-1)")]
        [Range(0f, 1f)]
        public float baseCaptureChance = 0.3f;

        [Tooltip("Capture chance bonus per damage dealt")]
        public float damageBonus = 0.02f;

        [Header("Input")]
        [Tooltip("Throw net key")]
        public KeyCode throwKey = KeyCode.F;

        [Tooltip("Switch to net mode")]
        public KeyCode netModeKey = KeyCode.N;

        [Header("Net Inventory")]
        [Tooltip("Starting nets")]
        public int startingNets = 5;

        [Tooltip("Current net count")]
        public int currentNets = 5;

        [Tooltip("Max nets can carry")]
        public int maxNets = 20;

        [Header("Throw Origin")]
        [Tooltip("Where nets are thrown from")]
        public Transform throwOrigin;

        [Header("Captured Pets")]
        [Tooltip("Player's captured pets")]
        public List<CapturedPet> capturedPets = new List<CapturedPet>();

        [Tooltip("Max pets player can have")]
        public int maxPets = 6;

        [Header("Visual")]
        [Tooltip("Net held visual")]
        public GameObject netHeldVisual;

        [Tooltip("Capture success effect")]
        public GameObject captureSuccessEffect;

        [Tooltip("Capture fail effect")]
        public GameObject captureFailEffect;

        [Header("Audio")]
        public AudioClip throwSound;
        public AudioClip captureSuccessSound;
        public AudioClip captureFailSound;
        public AudioClip noNetsSound;
        public AudioClip shakeSound;

        [Header("UI")]
        [Tooltip("Net count UI text")]
        public TMPro.TMP_Text netCountText;

        [Header("Events")]
        public UnityEvent<WildPet> onCaptureAttempt;
        public UnityEvent<WildPet, bool> onCaptureResult;
        public UnityEvent<CapturedPet> onPetCaptured;

        // State
        private bool netModeActive = false;
        private AudioSource audioSource;
        private Camera mainCam;
        private List<ThrownNet> activeNets = new List<ThrownNet>();

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

            mainCam = Camera.main;
        }

        private void Start()
        {
            currentNets = startingNets;
            LoadCapturedPets();
            UpdateNetUI();

            // Auto-find throw origin
            if (throwOrigin == null && mainCam != null)
            {
                throwOrigin = mainCam.transform;
            }

            // Update visual
            UpdateNetHeldVisual();
        }

        private void Update()
        {
            // Toggle net mode
            if (Input.GetKeyDown(netModeKey))
            {
                ToggleNetMode();
            }

            // Throw net
            if (netModeActive && Input.GetKeyDown(throwKey))
            {
                TryThrowNet();
            }
        }

        #region Net Mode

        /// <summary>
        /// Toggle net capture mode.
        /// </summary>
        public void ToggleNetMode()
        {
            netModeActive = !netModeActive;
            UpdateNetHeldVisual();

            if (netModeActive)
            {
                UI.HUDManager.Instance?.ShowNotification("Net Mode: ON - Press F to throw");
            }
            else
            {
                UI.HUDManager.Instance?.ShowNotification("Net Mode: OFF");
            }
        }

        /// <summary>
        /// Enable net mode.
        /// </summary>
        public void EnableNetMode()
        {
            netModeActive = true;
            UpdateNetHeldVisual();
        }

        /// <summary>
        /// Disable net mode.
        /// </summary>
        public void DisableNetMode()
        {
            netModeActive = false;
            UpdateNetHeldVisual();
        }

        private void UpdateNetHeldVisual()
        {
            if (netHeldVisual != null)
            {
                netHeldVisual.SetActive(netModeActive && currentNets > 0);
            }
        }

        #endregion

        #region Throwing

        /// <summary>
        /// Try to throw a net.
        /// </summary>
        public void TryThrowNet()
        {
            if (currentNets <= 0)
            {
                PlaySound(noNetsSound);
                UI.HUDManager.Instance?.ShowNotification("No nets left!");
                return;
            }

            if (throwOrigin == null) return;

            ThrowNet();
        }

        private void ThrowNet()
        {
            currentNets--;
            UpdateNetUI();
            UpdateNetHeldVisual();

            // Create net projectile
            if (netPrefab != null)
            {
                Vector3 throwDir = throwOrigin.forward;
                throwDir = Quaternion.AngleAxis(-throwAngle, throwOrigin.right) * throwDir;

                GameObject netObj = Instantiate(netPrefab, throwOrigin.position, Quaternion.LookRotation(throwDir));

                // Add physics if not present
                Rigidbody rb = netObj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = netObj.AddComponent<Rigidbody>();
                }

                rb.velocity = throwDir * throwForce;

                // Add net component
                ThrownNet thrownNet = netObj.GetComponent<ThrownNet>();
                if (thrownNet == null)
                {
                    thrownNet = netObj.AddComponent<ThrownNet>();
                }
                thrownNet.Initialize(this);
                activeNets.Add(thrownNet);

                // Destroy after time
                Destroy(netObj, 10f);
            }

            PlaySound(throwSound);
        }

        #endregion

        #region Capture

        /// <summary>
        /// Attempt to capture a wild pet (called by ThrownNet).
        /// </summary>
        public void AttemptCapture(WildPet wildPet)
        {
            if (wildPet == null) return;
            if (wildPet.isCaptured) return;

            onCaptureAttempt?.Invoke(wildPet);

            // Calculate capture chance
            float chance = CalculateCaptureChance(wildPet);

            // Roll for capture
            StartCoroutine(CaptureSequence(wildPet, chance));
        }

        private float CalculateCaptureChance(WildPet wildPet)
        {
            float chance = baseCaptureChance;

            // Increase based on damage dealt
            if (wildPet.maxHealth > 0)
            {
                float healthPercent = wildPet.currentHealth / wildPet.maxHealth;
                float healthBonus = (1f - healthPercent) * 0.5f; // Up to 50% bonus at low health
                chance += healthBonus;
            }

            // Rarity modifier
            switch (wildPet.petData.rarity)
            {
                case PetRarity.Common:
                    chance += 0.2f;
                    break;
                case PetRarity.Uncommon:
                    break;
                case PetRarity.Rare:
                    chance -= 0.1f;
                    break;
                case PetRarity.Epic:
                    chance -= 0.2f;
                    break;
                case PetRarity.Legendary:
                    chance -= 0.4f;
                    break;
            }

            return Mathf.Clamp01(chance);
        }

        private IEnumerator CaptureSequence(WildPet wildPet, float chance)
        {
            // Disable wild pet movement
            wildPet.SetCaptureState(true);

            // Shake animation (3 shakes)
            int shakes = 3;
            for (int i = 0; i < shakes; i++)
            {
                PlaySound(shakeSound);
                yield return new WaitForSeconds(0.5f);

                // Each shake has a chance to break free
                if (Random.value > chance)
                {
                    // Failed!
                    CaptureFailed(wildPet);
                    yield break;
                }
            }

            // Success!
            CaptureSuccess(wildPet);
        }

        private void CaptureSuccess(WildPet wildPet)
        {
            // Create captured pet data
            CapturedPet captured = new CapturedPet
            {
                petData = wildPet.petData,
                nickname = wildPet.petData.petName,
                level = wildPet.level,
                currentHP = wildPet.currentHealth,
                maxHP = wildPet.maxHealth
            };

            // Add to team if space
            if (capturedPets.Count < maxPets)
            {
                capturedPets.Add(captured);
                onPetCaptured?.Invoke(captured);

                UI.HUDManager.Instance?.ShowNotification($"Caught {wildPet.petData.petName}!");
            }
            else
            {
                // Send to storage
                UI.HUDManager.Instance?.ShowNotification($"Caught {wildPet.petData.petName}! (Sent to storage)");
            }

            // Effect
            if (captureSuccessEffect != null)
            {
                Instantiate(captureSuccessEffect, wildPet.transform.position, Quaternion.identity);
            }

            PlaySound(captureSuccessSound);
            onCaptureResult?.Invoke(wildPet, true);

            // Remove wild pet
            Destroy(wildPet.gameObject);

            // Save
            SaveCapturedPets();
        }

        private void CaptureFailed(WildPet wildPet)
        {
            wildPet.SetCaptureState(false);

            // Effect
            if (captureFailEffect != null)
            {
                Instantiate(captureFailEffect, wildPet.transform.position, Quaternion.identity);
            }

            PlaySound(captureFailSound);
            onCaptureResult?.Invoke(wildPet, false);

            UI.HUDManager.Instance?.ShowNotification($"{wildPet.petData.petName} broke free!");
        }

        #endregion

        #region Net Management

        /// <summary>
        /// Add nets to inventory.
        /// </summary>
        public void AddNets(int amount)
        {
            currentNets = Mathf.Min(currentNets + amount, maxNets);
            UpdateNetUI();
            UpdateNetHeldVisual();
        }

        /// <summary>
        /// Get current net count.
        /// </summary>
        public int GetNetCount()
        {
            return currentNets;
        }

        private void UpdateNetUI()
        {
            if (netCountText != null)
            {
                netCountText.text = $"Nets: {currentNets}";
            }
        }

        #endregion

        #region Pet Management

        /// <summary>
        /// Get all captured pets.
        /// </summary>
        public List<CapturedPet> GetCapturedPets()
        {
            return new List<CapturedPet>(capturedPets);
        }

        /// <summary>
        /// Release a captured pet.
        /// </summary>
        public void ReleasePet(CapturedPet pet)
        {
            capturedPets.Remove(pet);
            SaveCapturedPets();
        }

        /// <summary>
        /// Rename a captured pet.
        /// </summary>
        public void RenamePet(CapturedPet pet, string newName)
        {
            pet.nickname = newName;
            SaveCapturedPets();
        }

        #endregion

        #region Save/Load

        private void SaveCapturedPets()
        {
            CapturedPetList data = new CapturedPetList { pets = capturedPets };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString("CapturedPets", json);
            PlayerPrefs.SetInt("CurrentNets", currentNets);
            PlayerPrefs.Save();
        }

        private void LoadCapturedPets()
        {
            currentNets = PlayerPrefs.GetInt("CurrentNets", startingNets);

            string json = PlayerPrefs.GetString("CapturedPets", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    CapturedPetList data = JsonUtility.FromJson<CapturedPetList>(json);
                    if (data != null && data.pets != null)
                    {
                        capturedPets = data.pets;
                    }
                }
                catch { }
            }
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

        #region Properties

        public bool IsNetModeActive => netModeActive;
        public int NetCount => currentNets;
        public int PetCount => capturedPets.Count;

        #endregion

        [System.Serializable]
        private class CapturedPetList
        {
            public List<CapturedPet> pets;
        }
    }

    /// <summary>
    /// Thrown net projectile.
    /// </summary>
    public class ThrownNet : MonoBehaviour
    {
        private PetCaptureSystem captureSystem;
        private bool hasHit = false;

        public void Initialize(PetCaptureSystem system)
        {
            captureSystem = system;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;

            // Check for wild pet
            WildPet wildPet = collision.gameObject.GetComponent<WildPet>();
            if (wildPet != null)
            {
                hasHit = true;
                captureSystem?.AttemptCapture(wildPet);
                Destroy(gameObject);
                return;
            }

            // Hit something else - net is lost
            hasHit = true;
            Destroy(gameObject, 2f);
        }
    }

    /// <summary>
    /// Wild pet that can be captured.
    /// </summary>
    public class WildPet : MonoBehaviour
    {
        [Header("Pet Data")]
        public PetData petData;

        [Header("Stats")]
        public int level = 1;
        public float currentHealth = 100f;
        public float maxHealth = 100f;

        [Header("State")]
        public bool isCaptured = false;

        // Components
        private UnityEngine.AI.NavMeshAgent agent;
        private Animator animator;

        private void Awake()
        {
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        /// <summary>
        /// Set capture state (freeze during capture attempt).
        /// </summary>
        public void SetCaptureState(bool capturing)
        {
            isCaptured = capturing;

            if (agent != null)
            {
                agent.isStopped = capturing;
            }

            if (animator != null)
            {
                animator.speed = capturing ? 0f : 1f;
            }
        }

        /// <summary>
        /// Take damage.
        /// </summary>
        public void TakeDamage(float damage)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            if (currentHealth <= 0)
            {
                // Pet fainted - can't be captured now
                Destroy(gameObject, 2f);
            }
        }
    }

    /// <summary>
    /// Captured pet data.
    /// </summary>
    [System.Serializable]
    public class CapturedPet
    {
        public PetData petData;
        public string nickname;
        public int level = 1;
        public float currentHP;
        public float maxHP;
        public int experience;
    }

    /// <summary>
    /// Pet data configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPet", menuName = "Bob's Petroleum/Pet Data")]
    public class PetData : ScriptableObject
    {
        [Header("Info")]
        public string petId;
        public string petName = "Pet";
        public string description;
        public Sprite icon;

        [Header("Rarity")]
        public PetRarity rarity = PetRarity.Common;

        [Header("Stats")]
        public float baseHealth = 100f;
        public float baseAttack = 10f;
        public float baseDefense = 10f;
        public float baseSpeed = 10f;

        [Header("Moves")]
        public List<PetMove> availableMoves = new List<PetMove>();

        [Header("Visual")]
        public GameObject prefab;
        public RuntimeAnimatorController animatorController;

        [Header("Evolution")]
        public PetData evolvesInto;
        public int evolutionLevel = 0;
    }

    /// <summary>
    /// Pet move/attack.
    /// </summary>
    [System.Serializable]
    public class PetMove
    {
        public string moveName = "Attack";
        public string description;
        public float damage = 20f;
        public float accuracy = 1f;
        public int uses = 10;
        public MoveType moveType = MoveType.Physical;
        public AudioClip sound;
        public GameObject effect;
    }

    public enum PetRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum MoveType
    {
        Physical,
        Special,
        Status
    }
}
