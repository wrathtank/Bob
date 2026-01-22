using UnityEngine;
using UnityEngine.Events;
using TMPro;
using BobsPetroleum.Core;
using BobsPetroleum.Player;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Bob's revival system. Feed hamburgers from the vending machine to revive Bob.
    /// </summary>
    public class BobRevivalSystem : MonoBehaviour
    {
        public static BobRevivalSystem Instance { get; private set; }

        [Header("Bob Setup")]
        [Tooltip("Bob's hospital bed object")]
        public GameObject bobOnBed;

        [Tooltip("Bob's revived object (active when revived)")]
        public GameObject bobRevived;

        [Header("Hamburger Requirements")]
        [Tooltip("Total hamburgers needed")]
        public int hamburgersNeeded = 10;

        [Tooltip("Current hamburgers fed")]
        public int hamburgersFed = 0;

        [Header("Interaction")]
        [Tooltip("Point where player feeds Bob")]
        public Transform feedingPoint;

        [Tooltip("Interaction range")]
        public float interactionRange = 2f;

        [Header("UI")]
        [Tooltip("Text showing hamburger progress")]
        public TMP_Text progressText;

        [Tooltip("Format string (use {0} for current, {1} for total)")]
        public string progressFormat = "Burgers: {0}/{1}";

        [Header("Effects")]
        public ParticleSystem feedEffect;
        public AudioClip feedSound;
        public AudioClip reviveSound;

        [Header("Events")]
        public UnityEvent<int> onHamburgerFed;
        public UnityEvent onBobRevived;

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

        private void Start()
        {
            // Sync with GameManager
            if (GameManager.Instance != null)
            {
                hamburgersNeeded = GameManager.Instance.hamburgersToReviveBob;
            }

            UpdateUI();

            // Setup initial state
            if (bobOnBed != null) bobOnBed.SetActive(true);
            if (bobRevived != null) bobRevived.SetActive(false);
        }

        /// <summary>
        /// Try to feed Bob a hamburger.
        /// </summary>
        public bool FeedBob(PlayerController player)
        {
            if (hamburgersFed >= hamburgersNeeded)
            {
                return false; // Already revived
            }

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return false;

            // Check if player has hamburger
            if (!inventory.HasItem("hamburger"))
            {
                return false;
            }

            // Remove hamburger
            inventory.RemoveItem("hamburger");

            // Feed Bob
            hamburgersFed++;
            GameManager.Instance?.FeedBobHamburger();

            // Effects
            if (feedEffect != null)
            {
                feedEffect.Play();
            }

            if (feedSound != null)
            {
                audioSource.PlayOneShot(feedSound);
            }

            onHamburgerFed?.Invoke(hamburgersFed);
            UpdateUI();

            // Check if revived
            if (hamburgersFed >= hamburgersNeeded)
            {
                ReviveBob();
            }

            return true;
        }

        private void ReviveBob()
        {
            // Swap models
            if (bobOnBed != null) bobOnBed.SetActive(false);
            if (bobRevived != null) bobRevived.SetActive(true);

            // Effects
            if (reviveSound != null)
            {
                audioSource.PlayOneShot(reviveSound);
            }

            onBobRevived?.Invoke();
        }

        private void UpdateUI()
        {
            if (progressText != null)
            {
                progressText.text = string.Format(progressFormat, hamburgersFed, hamburgersNeeded);
            }
        }

        /// <summary>
        /// Reset for new game.
        /// </summary>
        public void Reset()
        {
            hamburgersFed = 0;
            UpdateUI();

            if (bobOnBed != null) bobOnBed.SetActive(true);
            if (bobRevived != null) bobRevived.SetActive(false);
        }

        /// <summary>
        /// Check if Bob is revived.
        /// </summary>
        public bool IsBobRevived()
        {
            return hamburgersFed >= hamburgersNeeded;
        }

        /// <summary>
        /// Get progress percentage.
        /// </summary>
        public float GetProgress()
        {
            return (float)hamburgersFed / hamburgersNeeded;
        }
    }

    /// <summary>
    /// Trigger zone for feeding Bob.
    /// </summary>
    public class BobFeedingZone : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        public string interactionPrompt = "Press E to Feed Bob";

        public void Interact(PlayerController player)
        {
            BobRevivalSystem.Instance?.FeedBob(player);
        }

        public string GetInteractionPrompt()
        {
            var inventory = FindObjectOfType<PlayerInventory>();
            if (inventory != null && inventory.HasItem("hamburger"))
            {
                return interactionPrompt;
            }
            return "Need Hamburger to Feed Bob";
        }
    }
}
