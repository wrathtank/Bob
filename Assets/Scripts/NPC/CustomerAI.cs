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
        [Tooltip("Wander radius from current position")]
        public float wanderRadius = 20f;

        [Tooltip("Time between wander destinations")]
        public float wanderInterval = 5f;

        [Tooltip("Minimum distance to destination")]
        public float arrivalDistance = 1f;

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
        }

        private void Start()
        {
            // Start wandering
            SetWandering();
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;

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
