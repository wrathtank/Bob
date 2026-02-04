using UnityEngine;
using UnityEngine.Events;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// Bob - the dying owner you need to save with hamburgers!
    /// Feed him to keep him alive and eventually revive him.
    /// </summary>
    public class BobCharacter : MonoBehaviour
    {
        public static BobCharacter Instance { get; private set; }

        [Header("Bob's Health")]
        [Tooltip("Bob's current health/hunger")]
        public float currentHealth = 50f;

        [Tooltip("Bob's maximum health")]
        public float maxHealth = 100f;

        [Tooltip("Health drain per second (he's dying!)")]
        public float healthDrainRate = 0.5f;

        [Tooltip("Health restored per hamburger")]
        public float healthPerHamburger = 20f;

        [Tooltip("Is Bob alive?")]
        public bool isAlive = true;

        [Tooltip("Is Bob revived (win condition)?")]
        public bool isRevived = false;

        [Header("Hamburger Feeding")]
        [Tooltip("Where hamburger model spawns when fed")]
        public Transform hamburgerDropSpot;

        [Tooltip("Hamburger prefab to spawn")]
        public GameObject hamburgerPrefab;

        [Tooltip("Time hamburger is visible")]
        public float hamburgerDisplayTime = 2f;

        [Tooltip("Hamburger drop height")]
        public float dropHeight = 0.5f;

        [Header("Money Dispenser")]
        [Tooltip("Where money/rewards spawn")]
        public Transform moneyDispenseSpot;

        [Tooltip("Money prefab (coin/bill)")]
        public GameObject moneyPrefab;

        [Tooltip("Money given per hamburger")]
        public int moneyReward = 10;

        [Header("Interaction")]
        [Tooltip("Interaction range")]
        public float interactionRange = 3f;

        [Tooltip("Interact key")]
        public KeyCode feedKey = KeyCode.E;

        [Tooltip("Interaction prompt")]
        public string feedPrompt = "Press E to feed Bob a hamburger";

        [Header("Animation")]
        [Tooltip("Bob's animator")]
        public Animator animator;

        [Tooltip("Simple animation player")]
        public Animation.SimpleAnimationPlayer simpleAnimator;

        [Tooltip("Idle animation")]
        public string idleAnimation = "Idle";

        [Tooltip("Eating animation")]
        public string eatingAnimation = "Eat";

        [Tooltip("Happy/revived animation")]
        public string happyAnimation = "Happy";

        [Tooltip("Dying animation")]
        public string dyingAnimation = "Dying";

        [Tooltip("Death animation")]
        public string deathAnimation = "Death";

        [Header("Audio")]
        [Tooltip("Hamburger drop sound")]
        public AudioClip hamburgerDropSound;

        [Tooltip("Eating/chomping sound")]
        public AudioClip eatingSound;

        [Tooltip("Bob happy sound (after eating)")]
        public AudioClip happySound;

        [Tooltip("Money dispense sound")]
        public AudioClip moneySound;

        [Tooltip("Bob groaning (low health)")]
        public AudioClip groanSound;

        [Tooltip("Bob death sound")]
        public AudioClip deathSound;

        [Tooltip("Revival fanfare")]
        public AudioClip revivalSound;

        [Header("Visual Feedback")]
        [Tooltip("Health bar above Bob")]
        public UnityEngine.UI.Image healthBarFill;

        [Tooltip("Speech bubble object")]
        public GameObject speechBubble;

        [Tooltip("Speech text")]
        public TMPro.TMP_Text speechText;

        [Tooltip("Particle effect when fed")]
        public ParticleSystem feedParticles;

        [Tooltip("Particle effect on revival")]
        public ParticleSystem revivalParticles;

        [Header("Bob Quotes")]
        [Tooltip("Things Bob says when hungry")]
        public string[] hungryQuotes = {
            "I'm so hungry...",
            "Need... hamburger...",
            "Feed me please...",
            "*stomach growls*"
        };

        [Tooltip("Things Bob says when fed")]
        public string[] fedQuotes = {
            "Mmm delicious!",
            "Thank you!",
            "That hit the spot!",
            "More please!"
        };

        [Tooltip("Things Bob says when revived")]
        public string[] revivedQuotes = {
            "I'M ALIVE!",
            "You saved me!",
            "I feel great!",
            "Thank you so much!"
        };

        [Header("Events")]
        public UnityEvent onHamburgerFed;
        public UnityEvent onBobDied;
        public UnityEvent onBobRevived;
        public UnityEvent<float> onHealthChanged;
        public UnityEvent<int> onMoneyDispensed;

        // Runtime
        private AudioSource audioSource;
        private float groanTimer = 0f;
        private float speechTimer = 0f;
        private Player.PlayerController nearbyPlayer;
        private GameObject currentHamburger;
        private int hamburgersFed = 0;

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

            // Auto-create drop spots if not assigned
            CreateDropSpotsIfMissing();
        }

        private void Start()
        {
            UpdateHealthBar();
            PlayAnimation(idleAnimation);

            if (speechBubble != null)
            {
                speechBubble.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isAlive || isRevived) return;

            // Drain health over time
            DrainHealth();

            // Random groaning when low health
            HandleGroaning();

            // Check for player interaction
            CheckPlayerInteraction();

            // Update speech bubble timer
            UpdateSpeechBubble();
        }

        #region Health System

        private void DrainHealth()
        {
            if (healthDrainRate <= 0) return;

            currentHealth -= healthDrainRate * Time.deltaTime;
            currentHealth = Mathf.Max(0, currentHealth);

            UpdateHealthBar();
            onHealthChanged?.Invoke(currentHealth / maxHealth);

            // Check death
            if (currentHealth <= 0 && isAlive)
            {
                Die();
            }

            // Play dying animation when very low
            if (currentHealth < maxHealth * 0.2f && simpleAnimator != null)
            {
                if (!simpleAnimator.IsPlaying(dyingAnimation))
                {
                    PlayAnimation(dyingAnimation);
                }
            }
        }

        private void HandleGroaning()
        {
            if (currentHealth > maxHealth * 0.3f) return;

            groanTimer -= Time.deltaTime;
            if (groanTimer <= 0f)
            {
                groanTimer = Random.Range(10f, 30f);
                PlaySound(groanSound);
                ShowSpeech(hungryQuotes[Random.Range(0, hungryQuotes.Length)]);
            }
        }

        /// <summary>
        /// Feed Bob a hamburger!
        /// </summary>
        public void FeedHamburger()
        {
            if (!isAlive) return;

            hamburgersFed++;

            // Spawn hamburger visual
            SpawnHamburger();

            // Heal Bob
            float oldHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + healthPerHamburger, maxHealth);

            // Play eating animation
            PlayAnimation(eatingAnimation);

            // Play sounds
            PlaySound(hamburgerDropSound);
            Invoke(nameof(PlayEatingSound), 0.3f);

            // Dispense money reward
            DispenseMoney();

            // Show happiness
            ShowSpeech(fedQuotes[Random.Range(0, fedQuotes.Length)]);

            // Particles
            if (feedParticles != null)
            {
                feedParticles.Play();
            }

            // Update game manager
            GameManager.Instance?.FeedBobHamburger();

            UpdateHealthBar();
            onHamburgerFed?.Invoke();
            onHealthChanged?.Invoke(currentHealth / maxHealth);

            // Check for revival
            CheckRevival();

            Debug.Log($"[Bob] Fed hamburger #{hamburgersFed}. Health: {currentHealth}/{maxHealth}");
        }

        private void CheckRevival()
        {
            // Revival at full health
            if (currentHealth >= maxHealth && !isRevived)
            {
                Revive();
            }
        }

        /// <summary>
        /// Bob is revived! Victory!
        /// </summary>
        public void Revive()
        {
            isRevived = true;

            // Play revival animation
            PlayAnimation(happyAnimation);

            // Play sounds
            PlaySound(revivalSound);
            PlaySound(happySound);

            // Particles
            if (revivalParticles != null)
            {
                revivalParticles.Play();
            }

            // Show speech
            ShowSpeech(revivedQuotes[Random.Range(0, revivedQuotes.Length)], 5f);

            // Notify game manager
            GameManager.Instance?.EndGame();

            onBobRevived?.Invoke();

            Debug.Log("[Bob] REVIVED! Player wins!");
        }

        /// <summary>
        /// Bob dies... Game over.
        /// </summary>
        public void Die()
        {
            isAlive = false;

            // Play death animation
            PlayAnimation(deathAnimation);

            // Play sound
            PlaySound(deathSound);

            // Notify
            onBobDied?.Invoke();

            // Game over
            GameManager.Instance?.EndGame();

            Debug.Log("[Bob] Died... Game over!");
        }

        #endregion

        #region Hamburger Spawning

        private void SpawnHamburger()
        {
            // Clean up old hamburger
            if (currentHamburger != null)
            {
                Destroy(currentHamburger);
            }

            if (hamburgerPrefab == null || hamburgerDropSpot == null) return;

            // Spawn above drop spot
            Vector3 spawnPos = hamburgerDropSpot.position + Vector3.up * dropHeight;
            currentHamburger = Instantiate(hamburgerPrefab, spawnPos, Quaternion.identity);

            // Drop animation
            StartCoroutine(DropHamburger(currentHamburger));
        }

        private System.Collections.IEnumerator DropHamburger(GameObject burger)
        {
            if (burger == null) yield break;

            Vector3 startPos = burger.transform.position;
            Vector3 endPos = hamburgerDropSpot.position;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Bounce ease
                t = 1f - Mathf.Pow(1f - t, 2f);

                if (burger != null)
                {
                    burger.transform.position = Vector3.Lerp(startPos, endPos, t);
                }

                yield return null;
            }

            // Destroy after display time
            yield return new WaitForSeconds(hamburgerDisplayTime);

            if (burger != null)
            {
                Destroy(burger);
            }
        }

        #endregion

        #region Money Dispensing

        private void DispenseMoney()
        {
            // Add to player inventory
            var playerInventory = FindObjectOfType<Player.PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.AddMoney(moneyReward);
            }

            // Visual money spawn
            if (moneyPrefab != null && moneyDispenseSpot != null)
            {
                GameObject money = Instantiate(moneyPrefab, moneyDispenseSpot.position, Quaternion.identity);

                // Add some random velocity
                Rigidbody rb = money.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(2f, 4f),
                        Random.Range(-1f, 1f)
                    );
                }

                // Destroy after time
                Destroy(money, 10f);
            }

            PlaySound(moneySound);
            onMoneyDispensed?.Invoke(moneyReward);

            Debug.Log($"[Bob] Dispensed ${moneyReward} reward");
        }

        #endregion

        #region Player Interaction

        private void CheckPlayerInteraction()
        {
            // Find nearby player
            Player.PlayerController player = FindObjectOfType<Player.PlayerController>();
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= interactionRange)
            {
                nearbyPlayer = player;

                // Show prompt
                UI.HUDManager.Instance?.ShowInteractionPrompt(feedPrompt);

                // Check input
                if (Input.GetKeyDown(feedKey))
                {
                    TryFeedBob(player);
                }
            }
            else
            {
                if (nearbyPlayer != null)
                {
                    nearbyPlayer = null;
                    UI.HUDManager.Instance?.HideInteractionPrompt();
                }
            }
        }

        private void TryFeedBob(Player.PlayerController player)
        {
            // Check if player has hamburger
            var inventory = player.GetComponent<Player.PlayerInventory>();
            if (inventory == null) return;

            if (inventory.HasItem("hamburger"))
            {
                inventory.RemoveItem("hamburger", 1);
                FeedHamburger();
            }
            else
            {
                // No hamburger
                ShowSpeech("I need a hamburger!", 2f);
                UI.HUDManager.Instance?.ShowNotification("You need a hamburger to feed Bob!");
            }
        }

        #endregion

        #region Animation

        private void PlayAnimation(string animName)
        {
            if (simpleAnimator != null)
            {
                simpleAnimator.Play(animName);
            }
            else if (animator != null)
            {
                animator.Play(animName);
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

        private void PlayEatingSound()
        {
            PlaySound(eatingSound);

            // Delayed happy sound
            Invoke(nameof(PlayHappySound), 1f);
        }

        private void PlayHappySound()
        {
            PlaySound(happySound);
        }

        #endregion

        #region UI

        private void UpdateHealthBar()
        {
            if (healthBarFill != null && maxHealth > 0)
            {
                healthBarFill.fillAmount = currentHealth / maxHealth;

                // Color based on health
                if (currentHealth < maxHealth * 0.3f)
                {
                    healthBarFill.color = Color.red;
                }
                else if (currentHealth < maxHealth * 0.6f)
                {
                    healthBarFill.color = Color.yellow;
                }
                else
                {
                    healthBarFill.color = Color.green;
                }
            }
        }

        private void ShowSpeech(string text, float duration = 3f)
        {
            if (speechBubble != null && speechText != null)
            {
                speechBubble.SetActive(true);
                speechText.text = text;
                speechTimer = duration;
            }
        }

        private void UpdateSpeechBubble()
        {
            if (speechBubble == null) return;

            if (speechTimer > 0)
            {
                speechTimer -= Time.deltaTime;
                if (speechTimer <= 0)
                {
                    speechBubble.SetActive(false);
                }
            }
        }

        #endregion

        #region Setup Helpers

        private void CreateDropSpotsIfMissing()
        {
            if (hamburgerDropSpot == null)
            {
                GameObject spot = new GameObject("HamburgerDropSpot");
                spot.transform.SetParent(transform);
                spot.transform.localPosition = new Vector3(0f, 1f, 0.5f); // In front of Bob
                hamburgerDropSpot = spot.transform;
            }

            if (moneyDispenseSpot == null)
            {
                GameObject spot = new GameObject("MoneyDispenseSpot");
                spot.transform.SetParent(transform);
                spot.transform.localPosition = new Vector3(0.5f, 1f, 0f); // Side of Bob
                moneyDispenseSpot = spot.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // Draw drop spots
            Gizmos.color = Color.red;
            if (hamburgerDropSpot != null)
            {
                Gizmos.DrawWireCube(hamburgerDropSpot.position, Vector3.one * 0.3f);
                Gizmos.DrawLine(hamburgerDropSpot.position, hamburgerDropSpot.position + Vector3.up * dropHeight);
            }

            Gizmos.color = Color.green;
            if (moneyDispenseSpot != null)
            {
                Gizmos.DrawWireSphere(moneyDispenseSpot.position, 0.2f);
            }
        }

        #endregion

        #region Properties

        public float HealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsLowHealth => currentHealth < maxHealth * 0.3f;
        public int HamburgersFed => hamburgersFed;

        #endregion
    }
}
