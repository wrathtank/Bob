using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using BobsPetroleum.Animation;
using BobsPetroleum.Player;
using BobsPetroleum.Battle;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// AI for wandering animals (rats, etc.) that can be captured with a net.
    /// Used for the Pokemon-style battle system.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class WanderingAnimalAI : MonoBehaviour
    {
        public enum AnimalState
        {
            Idle,
            Wandering,
            Fleeing,
            Captured
        }

        [Header("Animal Info")]
        [Tooltip("Animal name")]
        public string animalName = "Wild Rat";

        [Tooltip("Animal type for battle system")]
        public string animalType = "Rodent";

        [Header("Stats")]
        [Tooltip("Base health for battles")]
        public float baseHealth = 30f;

        [Tooltip("Catch difficulty (0-1, higher = harder)")]
        [Range(0f, 1f)]
        public float catchDifficulty = 0.5f;

        [Header("Movement")]
        [Tooltip("Walk speed")]
        public float walkSpeed = 2f;

        [Tooltip("Flee speed")]
        public float fleeSpeed = 5f;

        [Tooltip("Wander radius")]
        public float wanderRadius = 10f;

        [Tooltip("Wander interval")]
        public float wanderInterval = 3f;

        [Header("Detection")]
        [Tooltip("Distance to start fleeing from players")]
        public float fleeDistance = 5f;

        [Tooltip("Distance to stop fleeing")]
        public float safeDistance = 15f;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("Events")]
        public UnityEvent onStartFleeing;
        public UnityEvent onCaptured;
        public UnityEvent onEscaped;

        // Components
        private NavMeshAgent agent;
        private AnimalState currentState = AnimalState.Wandering;
        private float stateTimer = 0f;
        private Transform threatTarget;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            agent.speed = walkSpeed;
            SetWandering();
        }

        private void Update()
        {
            if (currentState == AnimalState.Captured) return;

            CheckForThreats();

            switch (currentState)
            {
                case AnimalState.Idle:
                    UpdateIdle();
                    break;
                case AnimalState.Wandering:
                    UpdateWandering();
                    break;
                case AnimalState.Fleeing:
                    UpdateFleeing();
                    break;
            }

            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (animationHandler == null) return;

            if (agent.velocity.magnitude > 0.1f)
            {
                if (currentState == AnimalState.Fleeing)
                    animationHandler.SetAnimation("Run", true);
                else
                    animationHandler.SetAnimation("Walk", true);
            }
            else
            {
                animationHandler.SetAnimation("Idle", true);
            }
        }

        #region State Updates

        private void UpdateIdle()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                SetWandering();
            }
        }

        private void UpdateWandering()
        {
            stateTimer -= Time.deltaTime;

            if (stateTimer <= 0f || HasReachedDestination())
            {
                if (Random.value < 0.3f)
                {
                    SetIdle(Random.Range(1f, 3f));
                }
                else
                {
                    SetNewWanderDestination();
                }
            }
        }

        private void UpdateFleeing()
        {
            if (threatTarget == null)
            {
                SetWandering();
                return;
            }

            float distance = Vector3.Distance(transform.position, threatTarget.position);

            if (distance > safeDistance)
            {
                SetWandering();
                return;
            }

            // Keep fleeing
            Vector3 fleeDirection = (transform.position - threatTarget.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDirection * 10f;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleeTarget, out hit, 10f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }

        #endregion

        #region State Transitions

        private void SetIdle(float duration)
        {
            currentState = AnimalState.Idle;
            agent.isStopped = true;
            stateTimer = duration;
        }

        private void SetWandering()
        {
            currentState = AnimalState.Wandering;
            agent.isStopped = false;
            agent.speed = walkSpeed;
            threatTarget = null;
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

        private void SetFleeing(Transform threat)
        {
            if (currentState == AnimalState.Fleeing) return;

            currentState = AnimalState.Fleeing;
            threatTarget = threat;
            agent.isStopped = false;
            agent.speed = fleeSpeed;
            onStartFleeing?.Invoke();
        }

        #endregion

        private void CheckForThreats()
        {
            // Find nearest player
            var players = FindObjectsOfType<PlayerController>();
            float nearestDistance = float.MaxValue;
            Transform nearestPlayer = null;

            foreach (var player in players)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlayer = player.transform;
                }
            }

            if (nearestPlayer != null && nearestDistance < fleeDistance)
            {
                SetFleeing(nearestPlayer);
            }
        }

        /// <summary>
        /// Attempt to capture this animal with a net.
        /// </summary>
        public bool TryCapture(PlayerController player)
        {
            // Calculate capture chance
            float captureChance = 1f - catchDifficulty;

            // Bonus if animal is idle
            if (currentState == AnimalState.Idle)
            {
                captureChance += 0.2f;
            }

            // Penalty if fleeing
            if (currentState == AnimalState.Fleeing)
            {
                captureChance -= 0.3f;
            }

            captureChance = Mathf.Clamp01(captureChance);

            if (Random.value <= captureChance)
            {
                // Captured!
                Capture(player);
                return true;
            }
            else
            {
                // Escaped!
                onEscaped?.Invoke();
                SetFleeing(player.transform);
                return false;
            }
        }

        private void Capture(PlayerController player)
        {
            currentState = AnimalState.Captured;
            agent.isStopped = true;
            onCaptured?.Invoke();
            animationHandler?.TriggerAnimation("Captured");

            // Add to player inventory
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                var capturedAnimal = new CapturedAnimal
                {
                    animalId = System.Guid.NewGuid().ToString(),
                    animalName = animalName,
                    animalPrefab = gameObject,
                    stats = new AnimalStats
                    {
                        animalType = animalType,
                        maxHealth = baseHealth
                    },
                    attacks = GetRandomAttacks()
                };

                inventory.AddAnimal(capturedAnimal);
            }

            // Destroy this wandering instance
            Destroy(gameObject, 1f);
        }

        private System.Collections.Generic.List<AnimalAttack> GetRandomAttacks()
        {
            if (AttackDatabase.Instance != null)
            {
                return AttackDatabase.Instance.GetRandomAttacks(4);
            }

            return new System.Collections.Generic.List<AnimalAttack>
            {
                new AnimalAttack { attackName = "Bite", damage = 10f, accuracy = 0.95f },
                new AnimalAttack { attackName = "Scratch", damage = 8f, accuracy = 1f }
            };
        }

        private bool HasReachedDestination()
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            {
                return true;
            }
            return false;
        }
    }
}
