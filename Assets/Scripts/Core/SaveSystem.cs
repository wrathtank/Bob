using UnityEngine;
using System.Collections.Generic;
using System;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// WebGL-compatible save system using PlayerPrefs (works with IndexedDB in WebGL).
    /// Handles pre-run consumables and player progression.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        private const string SAVE_KEY_PREFIX = "BobsPetroleum_";
        private const string CONSUMABLES_KEY = "PreRunConsumables";
        private const string PLAYER_NAME_KEY = "PlayerName";
        private const string UNLOCKED_ITEMS_KEY = "UnlockedItems";
        private const string STATS_KEY = "PlayerStats";

        [Header("Current Session Data")]
        public List<SavedConsumable> preRunConsumables = new List<SavedConsumable>();
        public List<string> unlockedItems = new List<string>();
        public string playerName = "Player";
        public PlayerStats stats = new PlayerStats();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAllData();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Pre-Run Consumables

        public void AddPreRunConsumable(string consumableId, int quantity = 1)
        {
            var existing = preRunConsumables.Find(c => c.consumableId == consumableId);
            if (existing != null)
            {
                existing.quantity += quantity;
            }
            else
            {
                preRunConsumables.Add(new SavedConsumable
                {
                    consumableId = consumableId,
                    quantity = quantity
                });
            }
        }

        public bool UsePreRunConsumable(string consumableId, int quantity = 1)
        {
            var existing = preRunConsumables.Find(c => c.consumableId == consumableId);
            if (existing != null && existing.quantity >= quantity)
            {
                existing.quantity -= quantity;
                if (existing.quantity <= 0)
                {
                    preRunConsumables.Remove(existing);
                }
                return true;
            }
            return false;
        }

        public int GetConsumableCount(string consumableId)
        {
            var existing = preRunConsumables.Find(c => c.consumableId == consumableId);
            return existing?.quantity ?? 0;
        }

        public void SavePreRunConsumables()
        {
            string json = JsonUtility.ToJson(new ConsumableListWrapper { consumables = preRunConsumables });
            PlayerPrefs.SetString(SAVE_KEY_PREFIX + CONSUMABLES_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadPreRunConsumables()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY_PREFIX + CONSUMABLES_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<ConsumableListWrapper>(json);
                preRunConsumables = wrapper?.consumables ?? new List<SavedConsumable>();
            }
        }

        #endregion

        #region Player Name

        public void SetPlayerName(string name)
        {
            playerName = name;
            PlayerPrefs.SetString(SAVE_KEY_PREFIX + PLAYER_NAME_KEY, name);
            PlayerPrefs.Save();
        }

        public string GetPlayerName()
        {
            return playerName;
        }

        private void LoadPlayerName()
        {
            playerName = PlayerPrefs.GetString(SAVE_KEY_PREFIX + PLAYER_NAME_KEY, "Player");
        }

        #endregion

        #region Unlocked Items

        public void UnlockItem(string itemId)
        {
            if (!unlockedItems.Contains(itemId))
            {
                unlockedItems.Add(itemId);
                SaveUnlockedItems();
            }
        }

        public bool IsItemUnlocked(string itemId)
        {
            return unlockedItems.Contains(itemId);
        }

        private void SaveUnlockedItems()
        {
            string json = JsonUtility.ToJson(new StringListWrapper { items = unlockedItems });
            PlayerPrefs.SetString(SAVE_KEY_PREFIX + UNLOCKED_ITEMS_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadUnlockedItems()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY_PREFIX + UNLOCKED_ITEMS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                var wrapper = JsonUtility.FromJson<StringListWrapper>(json);
                unlockedItems = wrapper?.items ?? new List<string>();
            }
        }

        #endregion

        #region Player Stats

        public void UpdateStats(int gamesPlayed = 0, int gamesWon = 0, int totalGarbageCollected = 0, int totalMoney = 0)
        {
            stats.gamesPlayed += gamesPlayed;
            stats.gamesWon += gamesWon;
            stats.totalGarbageCollected += totalGarbageCollected;
            stats.totalMoneyEarned += totalMoney;
            SaveStats();
        }

        private void SaveStats()
        {
            string json = JsonUtility.ToJson(stats);
            PlayerPrefs.SetString(SAVE_KEY_PREFIX + STATS_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadStats()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY_PREFIX + STATS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                stats = JsonUtility.FromJson<PlayerStats>(json);
            }
        }

        #endregion

        #region Utility

        public void LoadAllData()
        {
            LoadPreRunConsumables();
            LoadPlayerName();
            LoadUnlockedItems();
            LoadStats();
        }

        public void SaveAllData()
        {
            SavePreRunConsumables();
            SaveUnlockedItems();
            SaveStats();
            PlayerPrefs.SetString(SAVE_KEY_PREFIX + PLAYER_NAME_KEY, playerName);
            PlayerPrefs.Save();
        }

        public void ClearAllData()
        {
            PlayerPrefs.DeleteAll();
            preRunConsumables.Clear();
            unlockedItems.Clear();
            playerName = "Player";
            stats = new PlayerStats();
        }

        #endregion
    }

    [Serializable]
    public class SavedConsumable
    {
        public string consumableId;
        public int quantity;
    }

    [Serializable]
    public class ConsumableListWrapper
    {
        public List<SavedConsumable> consumables;
    }

    [Serializable]
    public class StringListWrapper
    {
        public List<string> items;
    }

    [Serializable]
    public class PlayerStats
    {
        public int gamesPlayed;
        public int gamesWon;
        public int totalGarbageCollected;
        public int totalMoneyEarned;
    }
}
