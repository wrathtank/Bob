using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// Central game manager that handles game state, day progression, and win/lose conditions.
    /// Attach to a persistent GameObject in your scene.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Settings")]
        [Tooltip("Total number of days for the game (default 7)")]
        public int totalDays = 7;

        [Tooltip("Number of hamburgers needed to revive Bob")]
        public int hamburgersToReviveBob = 10;

        [Tooltip("Current hamburgers fed to Bob")]
        public int currentHamburgersFed = 0;

        [Header("Spawn Settings")]
        [Tooltip("Where players spawn when joining or respawning")]
        public Transform playerSpawnPoint;

        [Tooltip("Starting health for players")]
        public float startingHealth = 100f;

        [Tooltip("Starting money for players")]
        public int startingMoney = 0;

        [Header("Game State")]
        public int currentDay = 1;
        public bool gameStarted = false;
        public bool gameEnded = false;
        public bool bobRevived = false;

        [Header("End Game Objects")]
        [Tooltip("GameObject to enable on game over (Bob not revived)")]
        public GameObject gameOverObject;

        [Tooltip("GameObject to enable on victory (Bob revived)")]
        public GameObject victoryObject;

        [Header("Pre-Run Rewards")]
        [Tooltip("Consumables rewarded on normal completion")]
        public List<ConsumableReward> normalRewards = new List<ConsumableReward>();

        [Tooltip("Boosted consumables rewarded when Bob is revived")]
        public List<ConsumableReward> boostedRewards = new List<ConsumableReward>();

        [Header("Events")]
        public UnityEvent onGameStart;
        public UnityEvent onDayChange;
        public UnityEvent onGameOver;
        public UnityEvent onVictory;
        public UnityEvent<int> onHamburgerFed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartGame()
        {
            gameStarted = true;
            gameEnded = false;
            currentDay = 1;
            currentHamburgersFed = 0;
            bobRevived = false;

            // Play game start sound
            AudioManager.Instance?.PlayGameStart();

            onGameStart?.Invoke();
        }

        public void AdvanceDay()
        {
            if (gameEnded) return;

            currentDay++;

            // Play day change sound
            AudioManager.Instance?.PlayDayChange();

            onDayChange?.Invoke();

            if (currentDay > totalDays)
            {
                EndGame();
            }
        }

        public void FeedBobHamburger()
        {
            if (gameEnded) return;

            currentHamburgersFed++;

            // Play hamburger fed sound
            AudioManager.Instance?.PlayHamburgerFed();

            onHamburgerFed?.Invoke(currentHamburgersFed);

            if (currentHamburgersFed >= hamburgersToReviveBob)
            {
                bobRevived = true;
            }
        }

        public void EndGame()
        {
            gameEnded = true;
            gameStarted = false;

            if (bobRevived)
            {
                Victory();
            }
            else
            {
                GameOver();
            }
        }

        private void Victory()
        {
            // Play victory sound
            AudioManager.Instance?.PlayVictory();

            onVictory?.Invoke();

            if (victoryObject != null)
                victoryObject.SetActive(true);

            // Award boosted rewards
            AwardRewards(boostedRewards);
            SaveSystem.Instance?.SavePreRunConsumables();
        }

        private void GameOver()
        {
            // Play game over sound
            AudioManager.Instance?.PlayGameOver();

            onGameOver?.Invoke();

            if (gameOverObject != null)
                gameOverObject.SetActive(true);

            // Award normal rewards
            AwardRewards(normalRewards);
            SaveSystem.Instance?.SavePreRunConsumables();
        }

        private void AwardRewards(List<ConsumableReward> rewards)
        {
            foreach (var reward in rewards)
            {
                if (Random.value <= reward.dropChance)
                {
                    SaveSystem.Instance?.AddPreRunConsumable(reward.consumableId, reward.quantity);
                }
            }
        }

        public void ResetGame()
        {
            gameStarted = false;
            gameEnded = false;
            currentDay = 1;
            currentHamburgersFed = 0;
            bobRevived = false;

            if (gameOverObject != null)
                gameOverObject.SetActive(false);
            if (victoryObject != null)
                victoryObject.SetActive(false);
        }
    }

    [System.Serializable]
    public class ConsumableReward
    {
        public string consumableId;
        public string consumableName;
        public int quantity = 1;
        [Range(0f, 1f)]
        public float dropChance = 0.5f;
    }
}
