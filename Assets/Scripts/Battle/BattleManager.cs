using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using BobsPetroleum.Player;
using BobsPetroleum.NPC;

namespace BobsPetroleum.Battle
{
    /// <summary>
    /// Pokemon-style battle manager. Handles turn-based animal battles.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("Battle Settings")]
        [Tooltip("Time between turns")]
        public float turnDelay = 1f;

        [Tooltip("Time for attack animations")]
        public float attackAnimationTime = 1.5f;

        [Header("Battle Arena")]
        [Tooltip("Position for player's animal")]
        public Transform playerAnimalPosition;

        [Tooltip("Position for enemy animal")]
        public Transform enemyAnimalPosition;

        [Tooltip("Camera position during battle")]
        public Transform battleCameraPosition;

        [Header("UI")]
        [Tooltip("Battle UI prefab")]
        public GameObject battleUIPrefab;

        [Header("Audio")]
        public AudioClip battleStartSound;
        public AudioClip attackSound;
        public AudioClip hitSound;
        public AudioClip victorySound;
        public AudioClip defeatSound;

        [Header("Events")]
        public UnityEvent onBattleStart;
        public UnityEvent<BattleAnimal> onAnimalSwitched;
        public UnityEvent<AnimalAttack, BattleAnimal> onAttackUsed;
        public UnityEvent<BattleAnimal> onAnimalFainted;
        public UnityEvent<bool> onBattleEnd; // true = player won

        // Battle state
        private bool isBattleActive = false;
        private PlayerController playerController;
        private BattlerNPC enemyBattler;
        private List<BattleAnimal> playerAnimals;
        private List<BattleAnimal> enemyAnimals;
        private BattleAnimal currentPlayerAnimal;
        private BattleAnimal currentEnemyAnimal;
        private int currentPlayerIndex = 0;
        private int currentEnemyIndex = 0;
        private bool isPlayerTurn = true;
        private GameObject currentUI;
        private AudioSource audioSource;

        // Spawned animal objects
        private GameObject playerAnimalObject;
        private GameObject enemyAnimalObject;

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

        /// <summary>
        /// Start a battle between player and NPC battler.
        /// </summary>
        public void StartBattle(PlayerController player, BattlerNPC enemy,
            List<BattleAnimal> playerTeam, List<BattleAnimal> enemyTeam,
            string introDialogue = "")
        {
            if (isBattleActive) return;

            isBattleActive = true;
            playerController = player;
            enemyBattler = enemy;
            playerAnimals = new List<BattleAnimal>(playerTeam);
            enemyAnimals = new List<BattleAnimal>(enemyTeam);

            // Setup first animals
            currentPlayerIndex = 0;
            currentEnemyIndex = 0;
            currentPlayerAnimal = playerAnimals[currentPlayerIndex];
            currentEnemyAnimal = enemyAnimals[currentEnemyIndex];

            // Disable player control
            player.enabled = false;

            // Spawn battle UI
            if (battleUIPrefab != null)
            {
                currentUI = Instantiate(battleUIPrefab);
                var battleUI = currentUI.GetComponent<BattleUI>();
                if (battleUI != null)
                {
                    battleUI.Initialize(this);
                }
            }

            // Spawn animal visuals
            SpawnAnimalVisuals();

            // Play start sound
            if (battleStartSound != null)
            {
                audioSource.PlayOneShot(battleStartSound);
            }

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            isPlayerTurn = true;
            onBattleStart?.Invoke();
        }

        private void SpawnAnimalVisuals()
        {
            // Clean up existing
            if (playerAnimalObject != null) Destroy(playerAnimalObject);
            if (enemyAnimalObject != null) Destroy(enemyAnimalObject);

            // Spawn player animal
            if (currentPlayerAnimal.animalPrefab != null && playerAnimalPosition != null)
            {
                playerAnimalObject = Instantiate(currentPlayerAnimal.animalPrefab,
                    playerAnimalPosition.position, playerAnimalPosition.rotation);
            }

            // Spawn enemy animal
            if (currentEnemyAnimal.animalPrefab != null && enemyAnimalPosition != null)
            {
                enemyAnimalObject = Instantiate(currentEnemyAnimal.animalPrefab,
                    enemyAnimalPosition.position, enemyAnimalPosition.rotation);
                enemyAnimalObject.transform.rotation = Quaternion.LookRotation(-enemyAnimalPosition.forward);
            }

            onAnimalSwitched?.Invoke(currentPlayerAnimal);
        }

        /// <summary>
        /// Player selects an attack.
        /// </summary>
        public void PlayerSelectAttack(int attackIndex)
        {
            if (!isBattleActive || !isPlayerTurn) return;

            if (attackIndex < 0 || attackIndex >= currentPlayerAnimal.attacks.Count)
            {
                return;
            }

            var attack = currentPlayerAnimal.attacks[attackIndex];
            StartCoroutine(ExecuteTurn(attack));
        }

        /// <summary>
        /// Player switches animal.
        /// </summary>
        public void PlayerSwitchAnimal(int animalIndex)
        {
            if (!isBattleActive || !isPlayerTurn) return;

            if (animalIndex < 0 || animalIndex >= playerAnimals.Count)
            {
                return;
            }

            var newAnimal = playerAnimals[animalIndex];
            if (newAnimal.currentHealth <= 0) return;

            currentPlayerIndex = animalIndex;
            currentPlayerAnimal = newAnimal;

            // Respawn visual
            if (playerAnimalObject != null) Destroy(playerAnimalObject);
            if (currentPlayerAnimal.animalPrefab != null && playerAnimalPosition != null)
            {
                playerAnimalObject = Instantiate(currentPlayerAnimal.animalPrefab,
                    playerAnimalPosition.position, playerAnimalPosition.rotation);
            }

            onAnimalSwitched?.Invoke(currentPlayerAnimal);

            // Enemy gets a free turn
            StartCoroutine(EnemyTurn());
        }

        private IEnumerator ExecuteTurn(AnimalAttack playerAttack)
        {
            isPlayerTurn = false;

            // Player attacks
            yield return ExecuteAttack(currentPlayerAnimal, currentEnemyAnimal, playerAttack, false);

            // Check if enemy fainted
            if (currentEnemyAnimal.currentHealth <= 0)
            {
                yield return OnAnimalFainted(false);
                yield break;
            }

            yield return new WaitForSeconds(turnDelay);

            // Enemy turn
            yield return EnemyTurn();
        }

        private IEnumerator EnemyTurn()
        {
            // AI selects random attack
            if (currentEnemyAnimal.attacks.Count > 0)
            {
                int attackIndex = Random.Range(0, currentEnemyAnimal.attacks.Count);
                var attack = currentEnemyAnimal.attacks[attackIndex];

                yield return ExecuteAttack(currentEnemyAnimal, currentPlayerAnimal, attack, true);
            }

            // Check if player animal fainted
            if (currentPlayerAnimal.currentHealth <= 0)
            {
                yield return OnAnimalFainted(true);
                yield break;
            }

            // Back to player turn
            isPlayerTurn = true;
        }

        private IEnumerator ExecuteAttack(BattleAnimal attacker, BattleAnimal defender,
            AnimalAttack attack, bool isEnemyAttacking)
        {
            // Check accuracy
            if (Random.value > attack.accuracy)
            {
                // Miss!
                Debug.Log($"{attacker.animalName}'s {attack.attackName} missed!");
                yield break;
            }

            // Play attack sound
            if (attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }

            // Play attack animation on attacker
            var attackerAnim = isEnemyAttacking ? enemyAnimalObject : playerAnimalObject;
            if (attackerAnim != null)
            {
                var animHandler = attackerAnim.GetComponent<Animation.AnimationEventHandler>();
                animHandler?.TriggerAnimation("Attack");
            }

            yield return new WaitForSeconds(attackAnimationTime * 0.5f);

            // Calculate damage
            float damage = attack.damage;

            // Apply type effectiveness (simplified)
            // You could expand this with type charts

            // Apply damage
            defender.currentHealth -= damage;
            defender.currentHealth = Mathf.Max(0, defender.currentHealth);

            // Play hit effect
            if (hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            var defenderObj = isEnemyAttacking ? playerAnimalObject : enemyAnimalObject;
            if (defenderObj != null)
            {
                var animHandler = defenderObj.GetComponent<Animation.AnimationEventHandler>();
                animHandler?.TriggerAnimation("Hurt");
            }

            onAttackUsed?.Invoke(attack, defender);

            yield return new WaitForSeconds(attackAnimationTime * 0.5f);
        }

        private IEnumerator OnAnimalFainted(bool isPlayerAnimal)
        {
            BattleAnimal fainted = isPlayerAnimal ? currentPlayerAnimal : currentEnemyAnimal;
            onAnimalFainted?.Invoke(fainted);

            // Play faint animation
            var faintedObj = isPlayerAnimal ? playerAnimalObject : enemyAnimalObject;
            if (faintedObj != null)
            {
                var animHandler = faintedObj.GetComponent<Animation.AnimationEventHandler>();
                animHandler?.TriggerAnimation("Faint");
            }

            yield return new WaitForSeconds(1f);

            if (isPlayerAnimal)
            {
                // Try to send out next player animal
                BattleAnimal nextAnimal = GetNextAliveAnimal(playerAnimals, currentPlayerIndex);

                if (nextAnimal == null)
                {
                    // Player lost
                    EndBattle(false);
                }
                else
                {
                    // Auto switch to next animal
                    currentPlayerIndex = playerAnimals.IndexOf(nextAnimal);
                    currentPlayerAnimal = nextAnimal;

                    if (playerAnimalObject != null) Destroy(playerAnimalObject);
                    if (currentPlayerAnimal.animalPrefab != null)
                    {
                        playerAnimalObject = Instantiate(currentPlayerAnimal.animalPrefab,
                            playerAnimalPosition.position, playerAnimalPosition.rotation);
                    }

                    onAnimalSwitched?.Invoke(currentPlayerAnimal);
                    isPlayerTurn = true;
                }
            }
            else
            {
                // Try to send out next enemy animal
                BattleAnimal nextAnimal = GetNextAliveAnimal(enemyAnimals, currentEnemyIndex);

                if (nextAnimal == null)
                {
                    // Player won
                    EndBattle(true);
                }
                else
                {
                    currentEnemyIndex = enemyAnimals.IndexOf(nextAnimal);
                    currentEnemyAnimal = nextAnimal;

                    if (enemyAnimalObject != null) Destroy(enemyAnimalObject);
                    if (currentEnemyAnimal.animalPrefab != null)
                    {
                        enemyAnimalObject = Instantiate(currentEnemyAnimal.animalPrefab,
                            enemyAnimalPosition.position, enemyAnimalPosition.rotation);
                        enemyAnimalObject.transform.rotation = Quaternion.LookRotation(-enemyAnimalPosition.forward);
                    }

                    isPlayerTurn = true;
                }
            }
        }

        private BattleAnimal GetNextAliveAnimal(List<BattleAnimal> animals, int currentIndex)
        {
            for (int i = 0; i < animals.Count; i++)
            {
                int index = (currentIndex + i + 1) % animals.Count;
                if (animals[index].currentHealth > 0)
                {
                    return animals[index];
                }
            }
            return null;
        }

        private void EndBattle(bool playerWon)
        {
            isBattleActive = false;

            // Play end sound
            if (playerWon && victorySound != null)
            {
                audioSource.PlayOneShot(victorySound);
            }
            else if (!playerWon && defeatSound != null)
            {
                audioSource.PlayOneShot(defeatSound);
            }

            // Notify battler
            if (playerWon)
            {
                enemyBattler?.OnLose(playerController);
            }
            else
            {
                enemyBattler?.OnWin();
            }

            // Clean up
            if (playerAnimalObject != null) Destroy(playerAnimalObject);
            if (enemyAnimalObject != null) Destroy(enemyAnimalObject);
            if (currentUI != null) Destroy(currentUI);

            // Re-enable player
            playerController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            onBattleEnd?.Invoke(playerWon);
        }

        /// <summary>
        /// Run from battle (forfeit).
        /// </summary>
        public void RunFromBattle()
        {
            if (!isBattleActive) return;

            EndBattle(false);
        }

        // Getters for UI
        public BattleAnimal GetCurrentPlayerAnimal() => currentPlayerAnimal;
        public BattleAnimal GetCurrentEnemyAnimal() => currentEnemyAnimal;
        public List<BattleAnimal> GetPlayerAnimals() => playerAnimals;
        public bool IsPlayerTurn => isPlayerTurn;
        public bool IsBattleActive => isBattleActive;
    }

    [System.Serializable]
    public class BattleAnimal
    {
        public string animalName;
        public GameObject animalPrefab;
        public float maxHealth;
        public float currentHealth;
        public List<AnimalAttack> attacks = new List<AnimalAttack>();
    }

    [System.Serializable]
    public class AnimalAttack
    {
        public string attackName;
        public float damage;
        [Range(0f, 1f)]
        public float accuracy = 0.9f;
        public AttackType attackType = AttackType.Normal;
        public string animationTrigger = "Attack";
        public AudioClip attackSound;
        public GameObject effectPrefab;
    }

    public enum AttackType
    {
        Normal,
        Fire,
        Water,
        Electric,
        Grass,
        Poison
    }

    [System.Serializable]
    public class AnimalStats
    {
        public string animalType;
        public float maxHealth = 50f;
        public float attack = 10f;
        public float defense = 10f;
        public float speed = 10f;
    }
}
