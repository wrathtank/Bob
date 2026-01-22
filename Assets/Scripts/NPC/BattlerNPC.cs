using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;
using BobsPetroleum.Battle;
using BobsPetroleum.Animation;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// NPC that can engage in Pokemon-style battles with players.
    /// Can be stationary or wander around. Assign animal prefabs in the inspector.
    /// </summary>
    public class BattlerNPC : MonoBehaviour, IInteractable
    {
        public enum BattlerBehavior
        {
            Stationary,
            Wandering,
            Patrolling
        }

        [Header("Battler Info")]
        [Tooltip("Battler name")]
        public string battlerName = "Wild Trainer";

        [Header("Behavior")]
        [Tooltip("How the battler moves")]
        public BattlerBehavior behavior = BattlerBehavior.Stationary;

        [Tooltip("Walk speed when wandering")]
        public float walkSpeed = 2f;

        [Tooltip("Wander radius from spawn point")]
        public float wanderRadius = 15f;

        [Tooltip("Time between wander destinations")]
        public float wanderInterval = 5f;

        [Tooltip("Patrol points (for Patrolling behavior)")]
        public List<Transform> patrolPoints = new List<Transform>();

        [Tooltip("Wait time at each patrol point")]
        public float patrolWaitTime = 3f;

        [Header("NavMesh Settings")]
        [Tooltip("Agent acceleration")]
        public float acceleration = 8f;

        [Tooltip("Angular speed for turning")]
        public float angularSpeed = 120f;

        [Tooltip("Stopping distance")]
        public float stoppingDistance = 0.5f;

        [Tooltip("Dialogue before battle")]
        [TextArea(2, 4)]
        public string preBattleDialogue = "Let's battle!";

        [Tooltip("Dialogue on win")]
        [TextArea(2, 4)]
        public string winDialogue = "I won!";

        [Tooltip("Dialogue on lose")]
        [TextArea(2, 4)]
        public string loseDialogue = "You got me...";

        [Header("Animals")]
        [Tooltip("Animal prefabs this battler uses")]
        public List<BattlerAnimal> animals = new List<BattlerAnimal>();

        [Tooltip("Randomize attacks for animals")]
        public bool randomizeAttacks = true;

        [Header("Rewards")]
        [Tooltip("Money reward on defeat")]
        public int moneyReward = 50;

        [Tooltip("Items dropped on defeat")]
        public List<InventoryItem> itemRewards = new List<InventoryItem>();

        [Header("Battle Settings")]
        [Tooltip("Can be battled multiple times")]
        public bool canRebattle = false;

        [Tooltip("Cooldown before rebattle (seconds)")]
        public float rebattleCooldown = 300f;

        [Header("Interaction")]
        [Tooltip("Interaction prompt")]
        public string interactionPrompt = "Press E to Battle";

        [Tooltip("Interaction range")]
        public float interactionRange = 3f;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("Events")]
        public UnityEvent<PlayerController> onBattleStart;
        public UnityEvent onBattleWin;
        public UnityEvent onBattleLose;

        // Components
        private NavMeshAgent agent;

        // State
        private bool hasBeenDefeated = false;
        private float lastBattleTime = -999f;
        private Vector3 spawnPosition;
        private float stateTimer = 0f;
        private int currentPatrolIndex = 0;
        private bool isInBattle = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            spawnPosition = transform.position;
        }

        private void Start()
        {
            if (agent != null && behavior != BattlerBehavior.Stationary)
            {
                agent.speed = walkSpeed;
                agent.acceleration = acceleration;
                agent.angularSpeed = angularSpeed;
                agent.stoppingDistance = stoppingDistance;

                if (behavior == BattlerBehavior.Wandering)
                {
                    SetNewWanderDestination();
                }
                else if (behavior == BattlerBehavior.Patrolling && patrolPoints.Count > 0)
                {
                    agent.SetDestination(patrolPoints[0].position);
                }
            }
        }

        private void Update()
        {
            if (isInBattle || behavior == BattlerBehavior.Stationary) return;
            if (agent == null) return;

            stateTimer -= Time.deltaTime;

            switch (behavior)
            {
                case BattlerBehavior.Wandering:
                    UpdateWandering();
                    break;
                case BattlerBehavior.Patrolling:
                    UpdatePatrolling();
                    break;
            }

            // Update animation
            if (animationHandler != null)
            {
                if (agent.velocity.magnitude > 0.1f)
                {
                    animationHandler.SetAnimation("Walk", true);
                }
                else
                {
                    animationHandler.SetAnimation("Idle", true);
                }
            }
        }

        private void UpdateWandering()
        {
            if (stateTimer <= 0f || HasReachedDestination())
            {
                if (Random.value < 0.3f)
                {
                    // Idle for a bit
                    agent.isStopped = true;
                    stateTimer = Random.Range(2f, 5f);
                }
                else
                {
                    SetNewWanderDestination();
                }
            }
        }

        private void UpdatePatrolling()
        {
            if (patrolPoints.Count == 0) return;

            if (HasReachedDestination())
            {
                if (stateTimer <= 0f)
                {
                    // Move to next patrol point
                    currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                    stateTimer = patrolWaitTime;
                }
                else
                {
                    agent.isStopped = true;
                }
            }
            else
            {
                agent.isStopped = false;
            }
        }

        private void SetNewWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += spawnPosition;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }

            stateTimer = wanderInterval;
        }

        private bool HasReachedDestination()
        {
            if (agent == null) return true;
            return !agent.pathPending && agent.remainingDistance <= stoppingDistance;
        }

        public void Interact(PlayerController player)
        {
            // Check if can battle
            if (hasBeenDefeated && !canRebattle)
            {
                return;
            }

            if (canRebattle && Time.time < lastBattleTime + rebattleCooldown)
            {
                return;
            }

            StartBattle(player);
        }

        public string GetInteractionPrompt()
        {
            if (hasBeenDefeated && !canRebattle)
            {
                return "";
            }

            if (canRebattle && Time.time < lastBattleTime + rebattleCooldown)
            {
                float remaining = (lastBattleTime + rebattleCooldown) - Time.time;
                return $"Cooldown: {Mathf.CeilToInt(remaining)}s";
            }

            return interactionPrompt;
        }

        /// <summary>
        /// Start a battle with a player.
        /// </summary>
        public void StartBattle(PlayerController player)
        {
            lastBattleTime = Time.time;
            isInBattle = true;

            // Stop moving
            if (agent != null)
            {
                agent.isStopped = true;
            }

            // Face player
            Vector3 lookDir = (player.transform.position - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            animationHandler?.TriggerAnimation("BattleReady");
            onBattleStart?.Invoke(player);

            // Get player's animals
            var playerInventory = player.GetComponent<PlayerInventory>();
            var playerAnimals = playerInventory?.GetCapturedAnimals();

            if (playerAnimals == null || playerAnimals.Count == 0)
            {
                Debug.LogWarning("Player has no animals for battle!");
                return;
            }

            // Prepare NPC animals
            List<BattleAnimal> npcBattleAnimals = new List<BattleAnimal>();
            foreach (var animal in animals)
            {
                var battleAnimal = new BattleAnimal
                {
                    animalName = animal.animalName,
                    animalPrefab = animal.animalPrefab,
                    maxHealth = animal.health,
                    currentHealth = animal.health,
                    attacks = randomizeAttacks ? GetRandomAttacks() : animal.attacks
                };

                npcBattleAnimals.Add(battleAnimal);
            }

            // Convert player animals
            List<BattleAnimal> playerBattleAnimals = new List<BattleAnimal>();
            foreach (var animal in playerAnimals)
            {
                var battleAnimal = new BattleAnimal
                {
                    animalName = animal.animalName,
                    animalPrefab = animal.animalPrefab,
                    maxHealth = animal.stats?.maxHealth ?? 50f,
                    currentHealth = animal.stats?.maxHealth ?? 50f,
                    attacks = animal.attacks
                };

                playerBattleAnimals.Add(battleAnimal);
            }

            // Start battle system
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.StartBattle(
                    player,
                    this,
                    playerBattleAnimals,
                    npcBattleAnimals,
                    preBattleDialogue
                );
            }
        }

        /// <summary>
        /// Called when NPC wins the battle.
        /// </summary>
        public void OnWin()
        {
            isInBattle = false;
            animationHandler?.TriggerAnimation("Victory");
            onBattleWin?.Invoke();

            // Resume movement after delay
            Invoke(nameof(ResumeMovement), 3f);
        }

        /// <summary>
        /// Called when NPC loses the battle.
        /// </summary>
        public void OnLose(PlayerController player)
        {
            isInBattle = false;
            hasBeenDefeated = true;
            animationHandler?.TriggerAnimation("Defeat");
            onBattleLose?.Invoke();

            // Give rewards
            var playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.AddMoney(moneyReward);

                foreach (var item in itemRewards)
                {
                    playerInventory.AddItem(item);
                }
            }

            // Resume movement after delay
            Invoke(nameof(ResumeMovement), 3f);
        }

        private void ResumeMovement()
        {
            if (agent != null && behavior != BattlerBehavior.Stationary)
            {
                agent.isStopped = false;

                if (behavior == BattlerBehavior.Wandering)
                {
                    SetNewWanderDestination();
                }
            }
        }

        private List<AnimalAttack> GetRandomAttacks()
        {
            // Get random attacks from the attack pool
            if (AttackDatabase.Instance != null)
            {
                return AttackDatabase.Instance.GetRandomAttacks(4);
            }

            // Fallback to basic attacks
            return new List<AnimalAttack>
            {
                new AnimalAttack { attackName = "Tackle", damage = 10f, accuracy = 0.95f },
                new AnimalAttack { attackName = "Scratch", damage = 15f, accuracy = 0.9f }
            };
        }

        /// <summary>
        /// Reset the battler (for new game).
        /// </summary>
        public void Reset()
        {
            hasBeenDefeated = false;
            lastBattleTime = -999f;
        }

        /// <summary>
        /// Check if NPC can be battled.
        /// </summary>
        public bool CanBattle()
        {
            if (hasBeenDefeated && !canRebattle)
                return false;

            if (canRebattle && Time.time < lastBattleTime + rebattleCooldown)
                return false;

            return true;
        }
    }

    [System.Serializable]
    public class BattlerAnimal
    {
        public string animalName;
        public GameObject animalPrefab;
        public float health = 50f;
        public List<AnimalAttack> attacks = new List<AnimalAttack>();
    }
}
