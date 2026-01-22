using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Unity.Netcode;
using BobsPetroleum.Battle;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Player inventory system for items, weapons, consumables, and captured animals.
    /// </summary>
    public class PlayerInventory : NetworkBehaviour
    {
        [Header("Inventory Settings")]
        [Tooltip("Maximum inventory slots")]
        public int maxSlots = 20;

        [Tooltip("Starting money")]
        public int startingMoney = 0;

        [Header("Weapon Slots")]
        [Tooltip("Maximum weapons player can carry")]
        public int maxWeapons = 3;

        [Tooltip("Currently equipped weapon index")]
        public int equippedWeaponIndex = 0;

        [Header("Animal Slots")]
        [Tooltip("Maximum captured animals")]
        public int maxAnimals = 6;

        [Header("Events")]
        public UnityEvent<int> onMoneyChanged;
        public UnityEvent<InventoryItem> onItemAdded;
        public UnityEvent<InventoryItem> onItemRemoved;
        public UnityEvent<WeaponItem> onWeaponEquipped;
        public UnityEvent<CapturedAnimal> onAnimalCaptured;

        // Network synced money
        private NetworkVariable<int> networkMoney = new NetworkVariable<int>();

        // Inventory data
        private List<InventoryItem> items = new List<InventoryItem>();
        private List<WeaponItem> weapons = new List<WeaponItem>();
        private List<CapturedAnimal> capturedAnimals = new List<CapturedAnimal>();

        public int Money => networkMoney.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                networkMoney.Value = startingMoney;
            }

            networkMoney.OnValueChanged += (old, newVal) => onMoneyChanged?.Invoke(newVal);
        }

        #region Money

        /// <summary>
        /// Add money to the player.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void AddMoneyServerRpc(int amount)
        {
            AddMoney(amount);
        }

        public void AddMoney(int amount)
        {
            if (!IsServer) return;
            networkMoney.Value += amount;
        }

        /// <summary>
        /// Remove money from the player. Returns true if successful.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RemoveMoneyServerRpc(int amount)
        {
            RemoveMoney(amount);
        }

        public bool RemoveMoney(int amount)
        {
            if (!IsServer) return false;

            if (networkMoney.Value >= amount)
            {
                networkMoney.Value -= amount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if player can afford an amount.
        /// </summary>
        public bool CanAfford(int amount)
        {
            return networkMoney.Value >= amount;
        }

        #endregion

        #region Items

        /// <summary>
        /// Add an item to inventory.
        /// </summary>
        public bool AddItem(InventoryItem item)
        {
            // Check for existing stack
            var existing = items.Find(i => i.itemId == item.itemId && i.isStackable);
            if (existing != null)
            {
                existing.quantity += item.quantity;
                onItemAdded?.Invoke(item);
                return true;
            }

            // Check for space
            if (items.Count >= maxSlots)
            {
                return false;
            }

            items.Add(item);
            onItemAdded?.Invoke(item);
            return true;
        }

        /// <summary>
        /// Remove an item from inventory.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var item = items.Find(i => i.itemId == itemId);
            if (item == null || item.quantity < quantity)
            {
                return false;
            }

            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                items.Remove(item);
            }

            onItemRemoved?.Invoke(item);
            return true;
        }

        /// <summary>
        /// Check if player has an item.
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            var item = items.Find(i => i.itemId == itemId);
            return item != null && item.quantity >= quantity;
        }

        /// <summary>
        /// Get item count.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            var item = items.Find(i => i.itemId == itemId);
            return item?.quantity ?? 0;
        }

        /// <summary>
        /// Get all items.
        /// </summary>
        public List<InventoryItem> GetAllItems()
        {
            return new List<InventoryItem>(items);
        }

        /// <summary>
        /// Use a consumable item.
        /// </summary>
        public bool UseItem(string itemId)
        {
            var item = items.Find(i => i.itemId == itemId);
            if (item == null || !item.isConsumable)
            {
                return false;
            }

            // Apply item effect (implement in derived class or via events)
            item.OnUse?.Invoke(GetComponent<PlayerController>());

            RemoveItem(itemId, 1);
            return true;
        }

        #endregion

        #region Weapons

        /// <summary>
        /// Add a weapon to inventory.
        /// </summary>
        public bool AddWeapon(WeaponItem weapon)
        {
            if (weapons.Count >= maxWeapons)
            {
                return false;
            }

            weapons.Add(weapon);

            // Auto-equip if first weapon
            if (weapons.Count == 1)
            {
                EquipWeapon(0);
            }

            return true;
        }

        /// <summary>
        /// Remove a weapon from inventory.
        /// </summary>
        public bool RemoveWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count)
            {
                return false;
            }

            weapons.RemoveAt(index);

            if (equippedWeaponIndex >= weapons.Count)
            {
                equippedWeaponIndex = weapons.Count - 1;
            }

            return true;
        }

        /// <summary>
        /// Equip a weapon by index.
        /// </summary>
        public void EquipWeapon(int index)
        {
            if (index < 0 || index >= weapons.Count)
            {
                return;
            }

            // Deactivate current weapon
            if (equippedWeaponIndex >= 0 && equippedWeaponIndex < weapons.Count)
            {
                weapons[equippedWeaponIndex].weaponObject?.SetActive(false);
            }

            equippedWeaponIndex = index;

            // Activate new weapon
            weapons[equippedWeaponIndex].weaponObject?.SetActive(true);
            onWeaponEquipped?.Invoke(weapons[equippedWeaponIndex]);
        }

        /// <summary>
        /// Get currently equipped weapon.
        /// </summary>
        public WeaponItem GetEquippedWeapon()
        {
            if (equippedWeaponIndex >= 0 && equippedWeaponIndex < weapons.Count)
            {
                return weapons[equippedWeaponIndex];
            }
            return null;
        }

        /// <summary>
        /// Get all weapons.
        /// </summary>
        public List<WeaponItem> GetAllWeapons()
        {
            return new List<WeaponItem>(weapons);
        }

        #endregion

        #region Animals

        /// <summary>
        /// Add a captured animal.
        /// </summary>
        public bool AddAnimal(CapturedAnimal animal)
        {
            if (capturedAnimals.Count >= maxAnimals)
            {
                return false;
            }

            capturedAnimals.Add(animal);
            onAnimalCaptured?.Invoke(animal);
            return true;
        }

        /// <summary>
        /// Remove a captured animal.
        /// </summary>
        public bool RemoveAnimal(int index)
        {
            if (index < 0 || index >= capturedAnimals.Count)
            {
                return false;
            }

            capturedAnimals.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Get all captured animals.
        /// </summary>
        public List<CapturedAnimal> GetCapturedAnimals()
        {
            return new List<CapturedAnimal>(capturedAnimals);
        }

        /// <summary>
        /// Get animal count.
        /// </summary>
        public int GetAnimalCount()
        {
            return capturedAnimals.Count;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Clear all inventory (on death).
        /// </summary>
        public void ClearInventory()
        {
            items.Clear();
            weapons.Clear();
            // Note: Animals are kept on death (optional)
        }

        /// <summary>
        /// Clear everything including animals and money.
        /// </summary>
        public void ClearAll()
        {
            items.Clear();
            weapons.Clear();
            capturedAnimals.Clear();

            if (IsServer)
            {
                networkMoney.Value = 0;
            }
        }

        #endregion
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public string itemName;
        public string description;
        public Sprite icon;
        public int quantity = 1;
        public bool isStackable = true;
        public bool isConsumable = false;
        public UnityEvent<PlayerController> OnUse;
    }

    [System.Serializable]
    public class WeaponItem
    {
        public string weaponId;
        public string weaponName;
        public GameObject weaponObject;
        public float damage = 10f;
        public float attackSpeed = 1f;
        public float range = 2f;
        public bool isNFTWeapon = false;
        public string nftTokenId;
    }

    [System.Serializable]
    public class CapturedAnimal
    {
        public string animalId;
        public string animalName;
        public GameObject animalPrefab;
        public AnimalStats stats;
        public List<AnimalAttack> attacks = new List<AnimalAttack>();
    }
}
