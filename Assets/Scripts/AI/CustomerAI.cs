using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace BobsPetroleum.AI
{
    /// <summary>
    /// CUSTOMER AI - NPCs that come to your gas station!
    /// They drive up, get gas, buy snacks, pay, and leave.
    ///
    /// SETUP:
    /// 1. Create NPC model with NavMeshAgent
    /// 2. Add CustomerAI component
    /// 3. Assign waypoints in CustomerSpawner
    /// 4. Done! Customers auto-spawn and behave.
    ///
    /// BEHAVIOR:
    /// 1. Spawn with car at entrance
    /// 2. Drive to gas pump
    /// 3. Walk inside to browse/pay
    /// 4. Return to car and leave
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAI : MonoBehaviour
    {
        public enum CustomerState
        {
            Spawning,
            DrivingToPump,
            GettingGas,
            WalkingToStore,
            Browsing,
            WaitingInLine,
            AtRegister,
            Paying,
            WalkingToCar,
            Leaving,
            Angry,
            Fleeing
        }

        [Header("=== CURRENT STATE ===")]
        [SerializeField] private CustomerState currentState = CustomerState.Spawning;
        public CustomerState CurrentState => currentState;

        [Header("=== CUSTOMER DATA ===")]
        [Tooltip("How much money this customer has")]
        public int walletAmount = 50;

        [Tooltip("Items this customer wants to buy")]
        public List<string> shoppingList = new List<string>();

        [Tooltip("Gas amount needed (gallons)")]
        public float gasNeeded = 10f;

        [Tooltip("Customer patience (seconds before angry)")]
        public float patience = 60f;

        [Header("=== TIMING ===")]
        [Tooltip("Time spent browsing shelves")]
        public float browseTime = 10f;

        [Tooltip("Time spent at register")]
        public float payTime = 3f;

        [Tooltip("Time spent getting gas")]
        public float gasTime = 15f;

        [Header("=== REFERENCES ===")]
        [Tooltip("Customer's vehicle (spawned with them)")]
        public GameObject vehicle;

        [Tooltip("Current target waypoint")]
        public Transform currentTarget;

        [Header("=== AUDIO ===")]
        public AudioClip greetingSound;
        public AudioClip angrySound;
        public AudioClip thankYouSound;

        [Header("=== EVENTS ===")]
        public UnityEvent onArrivedAtPump;
        public UnityEvent onEnteredStore;
        public UnityEvent onStartedPaying;
        public UnityEvent onFinishedPaying;
        public UnityEvent onLeftStore;
        public UnityEvent onBecameAngry;

        // Components
        private NavMeshAgent agent;
        private Animator animator;
        private AudioSource audioSource;

        // State tracking
        private float stateTimer;
        private float patienceTimer;
        private Economy.GasPump currentPump;
        private Economy.CashRegister currentRegister;
        private bool hasPaid = false;
        private float totalBill = 0f;

        // Waypoints (set by spawner)
        [HideInInspector] public Transform pumpWaypoint;
        [HideInInspector] public Transform storeEntrance;
        [HideInInspector] public Transform registerWaypoint;
        [HideInInspector] public Transform exitWaypoint;
        [HideInInspector] public List<Transform> browseWaypoints = new List<Transform>();

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }
        }

        private void Start()
        {
            // Generate random shopping list if empty
            if (shoppingList.Count == 0)
            {
                GenerateShoppingList();
            }

            // Start customer behavior
            StartCoroutine(CustomerBehavior());
        }

        private void Update()
        {
            // Track patience
            if (currentState != CustomerState.Leaving && currentState != CustomerState.Fleeing)
            {
                patienceTimer += Time.deltaTime;
                if (patienceTimer >= patience && currentState != CustomerState.Angry)
                {
                    BecomeAngry();
                }
            }

            // Update animator
            UpdateAnimator();
        }

        #region State Machine

        private IEnumerator CustomerBehavior()
        {
            // State: Spawning -> Driving to pump
            yield return StartCoroutine(DriveToGasPump());

            // State: Getting gas
            yield return StartCoroutine(GetGas());

            // State: Walk to store
            yield return StartCoroutine(WalkToStore());

            // State: Browse
            yield return StartCoroutine(BrowseStore());

            // State: Wait in line / Pay
            yield return StartCoroutine(PayAtRegister());

            // State: Walk back to car
            yield return StartCoroutine(WalkToCar());

            // State: Leave
            yield return StartCoroutine(DriveAway());
        }

        private IEnumerator DriveToGasPump()
        {
            SetState(CustomerState.DrivingToPump);

            if (vehicle != null && pumpWaypoint != null)
            {
                // Move vehicle to pump
                float driveTime = 3f;
                Vector3 startPos = vehicle.transform.position;
                Vector3 endPos = pumpWaypoint.position;

                float t = 0;
                while (t < driveTime)
                {
                    t += Time.deltaTime;
                    vehicle.transform.position = Vector3.Lerp(startPos, endPos, t / driveTime);
                    transform.position = vehicle.transform.position + new Vector3(1.5f, 0, 0);
                    yield return null;
                }
            }

            onArrivedAtPump?.Invoke();
        }

        private IEnumerator GetGas()
        {
            SetState(CustomerState.GettingGas);

            // Find nearby pump
            currentPump = FindNearestPump();

            if (currentPump != null)
            {
                // Start pumping
                currentPump.StartPumping(this);

                float timer = 0;
                while (timer < gasTime)
                {
                    timer += Time.deltaTime;

                    // Check if pump finished early
                    if (currentPump.IsDonePumping)
                        break;

                    yield return null;
                }

                // Calculate gas cost
                totalBill += currentPump.StopPumping();
            }
            else
            {
                // No pump available, wait a bit
                yield return new WaitForSeconds(2f);
            }
        }

        private IEnumerator WalkToStore()
        {
            SetState(CustomerState.WalkingToStore);

            // Exit vehicle
            if (vehicle != null)
            {
                transform.position = vehicle.transform.position + new Vector3(2f, 0, 0);
            }

            // Walk to store entrance
            if (storeEntrance != null)
            {
                agent.enabled = true;
                agent.SetDestination(storeEntrance.position);

                while (!HasReachedDestination())
                {
                    yield return null;
                }
            }

            // Play greeting
            PlaySound(greetingSound);
            onEnteredStore?.Invoke();
        }

        private IEnumerator BrowseStore()
        {
            SetState(CustomerState.Browsing);

            // Visit random browse waypoints
            int browsesToDo = Random.Range(1, Mathf.Min(4, browseWaypoints.Count + 1));

            for (int i = 0; i < browsesToDo; i++)
            {
                if (browseWaypoints.Count > 0)
                {
                    Transform browsePoint = browseWaypoints[Random.Range(0, browseWaypoints.Count)];
                    agent.SetDestination(browsePoint.position);

                    while (!HasReachedDestination())
                    {
                        yield return null;
                    }

                    // Browse at this spot
                    yield return new WaitForSeconds(browseTime / browsesToDo);

                    // Maybe pick up an item
                    if (Random.value > 0.5f && shoppingList.Count > 0)
                    {
                        string item = shoppingList[Random.Range(0, shoppingList.Count)];
                        // Add item cost to bill (simplified)
                        totalBill += Random.Range(1f, 10f);
                    }
                }
            }
        }

        private IEnumerator PayAtRegister()
        {
            SetState(CustomerState.WaitingInLine);

            // Walk to register
            if (registerWaypoint != null)
            {
                agent.SetDestination(registerWaypoint.position);

                while (!HasReachedDestination())
                {
                    yield return null;
                }
            }

            // Find register
            currentRegister = FindNearestRegister();

            if (currentRegister != null)
            {
                SetState(CustomerState.AtRegister);
                onStartedPaying?.Invoke();

                // Wait for player to process transaction
                // This is handled by CashRegister calling CompletePurchase()
                float waitTime = 0;
                while (!hasPaid && waitTime < patience)
                {
                    waitTime += Time.deltaTime;
                    yield return null;
                }

                if (!hasPaid)
                {
                    // Player took too long!
                    BecomeAngry();
                    yield break;
                }
            }

            SetState(CustomerState.Paying);
            yield return new WaitForSeconds(payTime);

            PlaySound(thankYouSound);
            onFinishedPaying?.Invoke();
        }

        private IEnumerator WalkToCar()
        {
            SetState(CustomerState.WalkingToCar);

            if (vehicle != null)
            {
                agent.SetDestination(vehicle.transform.position);

                while (!HasReachedDestination())
                {
                    yield return null;
                }

                // Enter vehicle
                agent.enabled = false;
                transform.position = vehicle.transform.position;
            }

            onLeftStore?.Invoke();
        }

        private IEnumerator DriveAway()
        {
            SetState(CustomerState.Leaving);

            if (vehicle != null && exitWaypoint != null)
            {
                float driveTime = 3f;
                Vector3 startPos = vehicle.transform.position;
                Vector3 endPos = exitWaypoint.position;

                float t = 0;
                while (t < driveTime)
                {
                    t += Time.deltaTime;
                    vehicle.transform.position = Vector3.Lerp(startPos, endPos, t / driveTime);
                    transform.position = vehicle.transform.position;
                    yield return null;
                }
            }

            // Despawn
            CustomerSpawner.Instance?.OnCustomerLeft(this);
            Destroy(vehicle);
            Destroy(gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Called by CashRegister when player completes transaction.
        /// </summary>
        public void CompletePurchase(float amountPaid)
        {
            hasPaid = true;

            // Check if correct change given
            if (amountPaid >= totalBill)
            {
                // Happy customer!
                Debug.Log($"[CustomerAI] Customer paid ${totalBill:F2}, received ${amountPaid:F2}");
            }
            else
            {
                // Short changed!
                Debug.Log($"[CustomerAI] Customer short changed! Bill: ${totalBill:F2}, Paid: ${amountPaid:F2}");
            }
        }

        /// <summary>
        /// Get the customer's current bill total.
        /// </summary>
        public float GetBillTotal() => totalBill;

        /// <summary>
        /// Make customer angry (leaves without paying, might cause trouble).
        /// </summary>
        public void BecomeAngry()
        {
            if (currentState == CustomerState.Angry || currentState == CustomerState.Fleeing)
                return;

            SetState(CustomerState.Angry);
            PlaySound(angrySound);
            onBecameAngry?.Invoke();

            // Stop current behavior and flee
            StopAllCoroutines();
            StartCoroutine(FleeStore());
        }

        private IEnumerator FleeStore()
        {
            SetState(CustomerState.Fleeing);

            // Run to car
            if (vehicle != null)
            {
                agent.speed *= 2f; // Run!
                agent.SetDestination(vehicle.transform.position);

                while (!HasReachedDestination())
                {
                    yield return null;
                }
            }

            // Drive away angrily
            yield return StartCoroutine(DriveAway());
        }

        #endregion

        #region Helpers

        private void SetState(CustomerState newState)
        {
            currentState = newState;
            stateTimer = 0f;
            Debug.Log($"[CustomerAI] State: {newState}");
        }

        private void GenerateShoppingList()
        {
            string[] possibleItems = { "Chips", "Soda", "Candy", "Cigarettes", "Beer", "Snacks", "Coffee", "Hot Dog" };
            int itemCount = Random.Range(0, 4);

            for (int i = 0; i < itemCount; i++)
            {
                shoppingList.Add(possibleItems[Random.Range(0, possibleItems.Length)]);
            }
        }

        private bool HasReachedDestination()
        {
            if (!agent.enabled) return true;
            if (agent.pathPending) return false;
            return agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        }

        private Economy.GasPump FindNearestPump()
        {
            Economy.GasPump[] pumps = FindObjectsOfType<Economy.GasPump>();
            Economy.GasPump nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var pump in pumps)
            {
                if (!pump.IsOccupied)
                {
                    float dist = Vector3.Distance(transform.position, pump.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = pump;
                    }
                }
            }

            return nearest;
        }

        private Economy.CashRegister FindNearestRegister()
        {
            Economy.CashRegister[] registers = FindObjectsOfType<Economy.CashRegister>();
            if (registers.Length > 0)
            {
                return registers[0]; // Just use first one for now
            }
            return null;
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;

            bool isMoving = agent.enabled && agent.velocity.magnitude > 0.1f;
            animator.SetBool("IsWalking", isMoving);
            animator.SetBool("IsAngry", currentState == CustomerState.Angry);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion
    }
}
