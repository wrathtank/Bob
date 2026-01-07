using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;
using BobsPetroleum.Battle;
using BobsPetroleum.Animation;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// NPC that can engage in Pokemon-style battles with players.
    /// Assign animal prefabs in the inspector.
    /// </summary>
    public class BattlerNPC : MonoBehaviour, IInteractable
    {
        [Header("Battler Info")]
        [Tooltip("Battler name")]
        public string battlerName = "Wild Trainer";

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

        private bool hasBeenDefeated = false;
        private float lastBattleTime = -999f;

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
            animationHandler?.TriggerAnimation("Victory");
            onBattleWin?.Invoke();
        }

        /// <summary>
        /// Called when NPC loses the battle.
        /// </summary>
        public void OnLose(PlayerController player)
        {
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
