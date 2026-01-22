using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using BobsPetroleum.Animation;
using BobsPetroleum.Shop;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// AI for customer NPCs that enter the gas station and buy items.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAI : MonoBehaviour
    {
        public enum CustomerState
        {
            Wandering,
            GoingToStore,
            Shopping,
            GoingToRegister,
            Paying,
            Leaving
        }

        [Header("State")]
        public CustomerState currentState = CustomerState.Wandering;

        [Header("Movement")]
        [Tooltip("Walking speed")]
        public float walkSpeed = 3f;

        [Tooltip("Wander radius from current position")]
        public float wanderRadius = 20f;

        [Tooltip("Time between wander destinations")]
        public float wanderInterval = 5f;

        [Tooltip("Minimum distance to destination")]
        public float arrivalDistance = 1f;

        [Header("NavMesh Settings")]
        [Tooltip("Agent acceleration")]
        public float acceleration = 8f;

        [Tooltip("Angular speed for turning")]
        public float angularSpeed = 120f;

        [Tooltip("Stopping distance from destination")]
        public float stoppingDistance = 0.5f;

        [Tooltip("Obstacle avoidance radius")]
        public float avoidanceRadius = 0.5f;

        [Tooltip("Obstacle avoidance priority (lower = higher priority)")]
        [Range(0, 99)]
        public int avoidancePriority = 50;

        [Header("Shopping")]
        [Tooltip("Chance to visit store (0-1)")]
        [Range(0f, 1f)]
        public float shopChance = 0.3f;

        [Tooltip("Maximum items to buy")]
        public int maxItemsToBuy = 3;

        [Tooltip("Time spent at each shelf")]
        public float shoppingTime = 2f;

        [Header("Target Points")]
        [Tooltip("Store entrance point")]
        public Transform storeEntrance;

        [Tooltip("Cash register point")]
        public Transform registerPoint;

        [Tooltip("Exit point")]
        public Transform exitPoint;

        [Tooltip("Shelf points to visit")]
        public Transform[] shelfPoints;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("Audio")]
        [Tooltip("Footstep sounds")]
        public AudioClip[] footstepSounds;

        [Tooltip("Idle chatter sounds")]
        public AudioClip[] chatterSounds;

        [Tooltip("Payment/thank you sounds")]
        public AudioClip[] paymentSounds;

        [Tooltip("Item pickup sound")]
        public AudioClip pickupSound;

        [Tooltip("Greeting sound when entering store")]
        public AudioClip greetingSound;

        [Tooltip("Footstep interval")]
        public float footstepInterval = 0.5f;

        [Range(0f, 1f)]
        public float footstepVolume = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Chance to play idle chatter")]
        public float chatterChance = 0.1f;

        [Header("Events")]
        public UnityEvent onEnterStore;
        public UnityEvent onStartShopping;
        public UnityEvent<ShopItem> onPickItem;
        public UnityEvent onReachRegister;
        public UnityEvent<float> onPay;
        public UnityEvent onLeave;

        // Components
        private NavMeshAgent agent;
        private NPCHealth health;
        private AudioSource audioSource;

        // Audio state
        private float footstepTimer = 0f;
        private float chatterTimer = 0f;

        // Shopping data
        private int itemsToBuy = 0;
        private int itemsCollected = 0;
        private float totalCost = 0f;
        private int currentShelfIndex = 0;
        private float stateTimer = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<NPCHealth>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
            }
        }

        private void Start()
        {
            // Apply NavMesh settings
            agent.speed = walkSpeed;
            agent.acceleration = acceleration;
            agent.angularSpeed = angularSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.radius = avoidanceRadius;
            agent.avoidancePriority = avoidancePriority;

            // Start wandering
            SetWandering();
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;

            // Handle audio
            HandleFootsteps();
            HandleChatter();

            switch (currentState)
            {
                case CustomerState.Wandering:
                    UpdateWandering();
                    break;
                case CustomerState.GoingToStore:
                    UpdateGoingToStore();
                    break;
                case CustomerState.Shopping:
                    UpdateShopping();
                    break;
                case CustomerState.GoingToRegister:
                    UpdateGoingToRegister();
                    break;
                case CustomerState.Paying:
                    UpdatePaying();
                    break;
                case CustomerState.Leaving:
                    UpdateLeaving();
                    break;
            }

            // Update animation based on movement
            UpdateAnimation();
        }

        private void HandleFootsteps()
        {
            if (footstepSounds == null || footstepSounds.Length == 0) return;
            if (agent.velocity.magnitude < 0.1f) return;

            footstepTimer += Time.deltaTime;
            if (footstepTimer >= footstepInterval)
            {
                footstepTimer = 0f;
                AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip, footstepVolume);
                }
            }
        }

        private void HandleChatter()
        {
            if (chatterSounds == null || chatterSounds.Length == 0) return;
            if (currentState != CustomerState.Shopping && currentState != CustomerState.Wandering) return;

            chatterTimer -= Time.deltaTime;
            if (chatterTimer <= 0f)
            {
                chatterTimer = Random.Range(5f, 15f);

                if (Random.value < chatterChance)
                {
                    AudioClip clip = chatterSounds[Random.Range(0, chatterSounds.Length)];
                    if (clip != null)
                    {
                        audioSource.PlayOneShot(clip, 0.6f);
                    }
                }
            }
        }

        private void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private void PlayRandomSound(AudioClip[] clips, float volume = 1f)
        {
            if (clips != null && clips.Length > 0)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                PlaySound(clip, volume);
            }
        }

        private void UpdateAnimation()
        {
            if (animationHandler == null) return;

            if (agent.velocity.magnitude > 0.1f)
            {
                animationHandler.SetAnimation("Walk", true);
            }
            else
            {
                animationHandler.SetAnimation("Idle", true);
            }
        }

        #region State Updates

        private void UpdateWandering()
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0f || HasReachedDestination())
            {
                // Decide whether to shop
                if (Random.value < shopChance && storeEntrance != null)
                {
                    GoToStore();
                }
                else
                {
                    SetNewWanderDestination();
                }
            }
        }

        private void UpdateGoingToStore()
        {
            if (HasReachedDestination())
            {
                StartShopping();
            }
        }

        private void UpdateShopping()
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0f)
            {
                // Pick item from current shelf
                PickItemFromShelf();

                itemsCollected++;

                if (itemsCollected >= itemsToBuy || currentShelfIndex >= shelfPoints.Length)
                {
                    // Done shopping, go to register
                    GoToRegister();
                }
                else
                {
                    // Go to next shelf
                    GoToNextShelf();
                }
            }
        }

        private void UpdateGoingToRegister()
        {
            if (HasReachedDestination())
            {
                StartPaying();
            }
        }

        private void UpdatePaying()
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0f)
            {
                CompletePurchase();
                Leave();
            }
        }

        private void UpdateLeaving()
        {
            if (HasReachedDestination())
            {
                // Customer has left, destroy or return to pool
                Destroy(gameObject);
            }
        }

        #endregion

        #region State Transitions

        private void SetWandering()
        {
            currentState = CustomerState.Wandering;
            SetNewWanderDestination();
        }

        private void SetNewWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            stateTimer = wanderInterval;
        }

        private void GoToStore()
        {
            currentState = CustomerState.GoingToStore;
            agent.SetDestination(storeEntrance.position);
        }

        private void StartShopping()
        {
            currentState = CustomerState.Shopping;
            itemsToBuy = Random.Range(1, maxItemsToBuy + 1);
            itemsCollected = 0;
            totalCost = 0f;
            currentShelfIndex = 0;

            // Play greeting sound
            PlaySound(greetingSound, 0.8f);

            onEnterStore?.Invoke();
            onStartShopping?.Invoke();

            GoToNextShelf();
        }

        private void GoToNextShelf()
        {
            if (currentShelfIndex < shelfPoints.Length)
            {
                agent.SetDestination(shelfPoints[currentShelfIndex].position);
                stateTimer = shoppingTime;
                currentShelfIndex++;
            }
        }

        private void PickItemFromShelf()
        {
            // Get shelf info
            if (currentShelfIndex > 0 && currentShelfIndex <= shelfPoints.Length)
            {
                var shelf = shelfPoints[currentShelfIndex - 1].GetComponent<ShopShelf>();
                if (shelf != null && shelf.shopItem != null)
                {
                    totalCost += shelf.shopItem.price;

                    // Play pickup sound
                    PlaySound(pickupSound, 0.7f);

                    onPickItem?.Invoke(shelf.shopItem);
                }
            }
        }

        private void GoToRegister()
        {
            currentState = CustomerState.GoingToRegister;

            if (registerPoint != null)
            {
                agent.SetDestination(registerPoint.position);
            }
        }

        private void StartPaying()
        {
            currentState = CustomerState.Paying;
            stateTimer = 2f; // Payment time
            onReachRegister?.Invoke();

            // Trigger cash register
            var register = registerPoint?.GetComponent<UI.CashRegisterUI>();
            if (register != null)
            {
                register.StartTransaction();
                register.ScanItem(totalCost);
            }
        }

        private void CompletePurchase()
        {
            var register = registerPoint?.GetComponent<UI.CashRegisterUI>();
            if (register != null)
            {
                float paid = register.CompleteTransaction();

                // Play payment/thank you sound
                PlayRandomSound(paymentSounds, 0.8f);

                onPay?.Invoke(paid);
            }
        }

        private void Leave()
        {
            currentState = CustomerState.Leaving;
            onLeave?.Invoke();

            if (exitPoint != null)
            {
                agent.SetDestination(exitPoint.position);
            }
            else
            {
                // Just wander away
                Vector3 awayDir = (transform.position - storeEntrance.position).normalized;
                agent.SetDestination(transform.position + awayDir * 50f);
            }
        }

        #endregion

        private bool HasReachedDestination()
        {
            if (!agent.pathPending && agent.remainingDistance <= arrivalDistance)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set the store points for this customer.
        /// </summary>
        public void SetStorePoints(Transform entrance, Transform register, Transform exit, Transform[] shelves)
        {
            storeEntrance = entrance;
            registerPoint = register;
            exitPoint = exit;
            shelfPoints = shelves;
        }

        /// <summary>
        /// Force customer to leave immediately.
        /// </summary>
        public void ForceLeave()
        {
            Leave();
        }
    }
}
