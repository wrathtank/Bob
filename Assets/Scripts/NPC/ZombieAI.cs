using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using BobsPetroleum.Animation;
using BobsPetroleum.Player;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// AI for zombie NPCs that roam and attack players/customers.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ZombieAI : MonoBehaviour
    {
        public enum ZombieState
        {
            Idle,
            Wandering,
            Chasing,
            Attacking
        }

        [Header("State")]
        public ZombieState currentState = ZombieState.Wandering;

        [Header("Detection")]
        [Tooltip("Range to detect targets")]
        public float detectionRange = 15f;

        [Tooltip("Range to lose interest in target")]
        public float loseInterestRange = 25f;

        [Tooltip("Layers to detect as targets")]
        public LayerMask targetLayers;

        [Tooltip("Prioritize players over NPCs")]
        public bool prioritizePlayers = true;

        [Header("Movement")]
        [Tooltip("Normal movement speed")]
        public float walkSpeed = 2f;

        [Tooltip("Chase speed")]
        public float chaseSpeed = 4f;

        [Tooltip("Wander radius")]
        public float wanderRadius = 15f;

        [Tooltip("Time between wander destinations")]
        public float wanderInterval = 4f;

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

        [Tooltip("Auto brake when reaching destination")]
        public bool autoBraking = true;

        [Header("Combat")]
        [Tooltip("Attack range")]
        public float attackRange = 2f;

        [Tooltip("Damage per attack")]
        public float attackDamage = 15f;

        [Tooltip("Attack cooldown")]
        public float attackCooldown = 1.5f;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("Audio")]
        public AudioClip[] groans;
        public AudioClip attackSound;
        public AudioClip[] footstepSounds;
        public AudioClip[] hitSounds;
        public AudioClip deathSound;
        [Range(0f, 1f)]
        public float groanChance = 0.1f;
        [Tooltip("Footstep interval while walking")]
        public float footstepInterval = 0.6f;
        [Range(0f, 1f)]
        public float footstepVolume = 0.5f;

        [Header("Events")]
        public UnityEvent<Transform> onTargetDetected;
        public UnityEvent onTargetLost;
        public UnityEvent onAttack;

        // Components
        private NavMeshAgent agent;
        private NPCHealth health;
        private AudioSource audioSource;

        // State data
        private Transform currentTarget;
        private float lastAttackTime = -999f;
        private float stateTimer = 0f;
        private float groanTimer = 0f;
        private float footstepTimer = 0f;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<NPCHealth>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
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
            agent.autoBraking = autoBraking;

            SetWandering();
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;

            // Random groan
            HandleGroaning();

            // Footsteps
            HandleFootsteps();

            // State machine
            switch (currentState)
            {
                case ZombieState.Idle:
                    UpdateIdle();
                    break;
                case ZombieState.Wandering:
                    UpdateWandering();
                    break;
                case ZombieState.Chasing:
                    UpdateChasing();
                    break;
                case ZombieState.Attacking:
                    UpdateAttacking();
                    break;
            }

            // Always check for targets
            CheckForTargets();

            // Update animation
            UpdateAnimation();
        }

        private void HandleGroaning()
        {
            groanTimer -= Time.deltaTime;
            if (groanTimer <= 0f)
            {
                groanTimer = Random.Range(3f, 10f);

                if (Random.value < groanChance && groans.Length > 0)
                {
                    audioSource.PlayOneShot(groans[Random.Range(0, groans.Length)]);
                }
            }
        }

        private void HandleFootsteps()
        {
            if (footstepSounds == null || footstepSounds.Length == 0) return;
            if (agent.velocity.magnitude < 0.1f) return;

            float interval = currentState == ZombieState.Chasing ? footstepInterval * 0.7f : footstepInterval;

            footstepTimer += Time.deltaTime;
            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip, footstepVolume);
                }
            }
        }

        /// <summary>
        /// Called when zombie takes damage (hook this to NPCHealth.onDamaged).
        /// </summary>
        public void OnTakeDamage(float damage)
        {
            if (hitSounds != null && hitSounds.Length > 0)
            {
                AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
            animationHandler?.TriggerAnimation("Hurt");
        }

        /// <summary>
        /// Called when zombie dies (hook this to NPCHealth.onDeath).
        /// </summary>
        public void OnDeath()
        {
            if (deathSound != null)
            {
                audioSource.PlayOneShot(deathSound);
            }
            animationHandler?.TriggerAnimation("Death");
            agent.isStopped = true;
        }

        private void UpdateAnimation()
        {
            if (animationHandler == null) return;

            if (currentState == ZombieState.Attacking)
            {
                // Attack animation handled separately
            }
            else if (agent.velocity.magnitude > 0.1f)
            {
                if (currentState == ZombieState.Chasing)
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
                // Random chance to idle
                if (Random.value < 0.2f)
                {
                    SetIdle(Random.Range(2f, 5f));
                }
                else
                {
                    SetNewWanderDestination();
                }
            }
        }

        private void UpdateChasing()
        {
            if (currentTarget == null)
            {
                OnLoseTarget();
                return;
            }

            // Check if target is dead
            var targetHealth = currentTarget.GetComponent<PlayerHealth>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                OnLoseTarget();
                return;
            }

            var targetNPCHealth = currentTarget.GetComponent<NPCHealth>();
            if (targetNPCHealth != null && targetNPCHealth.IsDead)
            {
                OnLoseTarget();
                return;
            }

            float distance = Vector3.Distance(transform.position, currentTarget.position);

            // Lost interest?
            if (distance > loseInterestRange)
            {
                OnLoseTarget();
                return;
            }

            // In attack range?
            if (distance <= attackRange)
            {
                SetAttacking();
                return;
            }

            // Keep chasing
            agent.SetDestination(currentTarget.position);
        }

        private void UpdateAttacking()
        {
            if (currentTarget == null)
            {
                OnLoseTarget();
                return;
            }

            float distance = Vector3.Distance(transform.position, currentTarget.position);

            // Target escaped?
            if (distance > attackRange)
            {
                SetChasing(currentTarget);
                return;
            }

            // Face target
            Vector3 lookDir = (currentTarget.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // Attack
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack();
            }
        }

        #endregion

        #region State Transitions

        private void SetIdle(float duration)
        {
            currentState = ZombieState.Idle;
            agent.isStopped = true;
            stateTimer = duration;
        }

        private void SetWandering()
        {
            currentState = ZombieState.Wandering;
            agent.isStopped = false;
            agent.speed = walkSpeed;
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

        private void SetChasing(Transform target)
        {
            currentState = ZombieState.Chasing;
            currentTarget = target;
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            onTargetDetected?.Invoke(target);
        }

        private void SetAttacking()
        {
            currentState = ZombieState.Attacking;
            agent.isStopped = true;
        }

        private void OnLoseTarget()
        {
            currentTarget = null;
            onTargetLost?.Invoke();
            SetWandering();
        }

        #endregion

        #region Detection and Combat

        private void CheckForTargets()
        {
            // Already have a target?
            if (currentTarget != null && currentState == ZombieState.Chasing)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRange, targetLayers);

            Transform bestTarget = null;
            float bestDistance = float.MaxValue;
            bool foundPlayer = false;

            foreach (var hit in hits)
            {
                // Skip self
                if (hit.transform == transform) continue;

                // Check if valid target
                bool isPlayer = hit.GetComponent<PlayerController>() != null;
                bool isCustomer = hit.GetComponent<CustomerAI>() != null;

                if (!isPlayer && !isCustomer) continue;

                // Check if alive
                var playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null && playerHealth.IsDead) continue;

                var npcHealth = hit.GetComponent<NPCHealth>();
                if (npcHealth != null && npcHealth.IsDead) continue;

                float distance = Vector3.Distance(transform.position, hit.transform.position);

                // Prioritize players
                if (prioritizePlayers)
                {
                    if (isPlayer && !foundPlayer)
                    {
                        foundPlayer = true;
                        bestDistance = distance;
                        bestTarget = hit.transform;
                    }
                    else if (isPlayer && distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = hit.transform;
                    }
                    else if (!foundPlayer && distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = hit.transform;
                    }
                }
                else
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestTarget = hit.transform;
                    }
                }
            }

            if (bestTarget != null)
            {
                SetChasing(bestTarget);
            }
        }

        private void PerformAttack()
        {
            lastAttackTime = Time.time;
            onAttack?.Invoke();
            animationHandler?.TriggerAnimation("Attack");

            if (attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }

            // Deal damage after short delay (animation timing)
            Invoke(nameof(DealDamage), 0.3f);
        }

        private void DealDamage()
        {
            if (currentTarget == null) return;

            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance > attackRange * 1.5f) return;

            // Damage player
            var playerHealth = currentTarget.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                return;
            }

            // Damage NPC
            var npcHealth = currentTarget.GetComponent<NPCHealth>();
            if (npcHealth != null)
            {
                npcHealth.TakeDamage(attackDamage);
            }
        }

        #endregion

        private bool HasReachedDestination()
        {
            if (!agent.pathPending && agent.remainingDistance <= 1f)
            {
                return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Lose interest range
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, loseInterestRange);
        }
    }
}
