using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace BobsPetroleum.Economy
{
    /// <summary>
    /// ITEM SHELF - Display and sell items!
    /// Customers browse here, player can restock.
    ///
    /// SETUP:
    /// 1. Create shelf model
    /// 2. Add ItemShelf component
    /// 3. Create item slots as children
    /// 4. Configure stock items
    /// </summary>
    public class ItemShelf : MonoBehaviour
    {
        [System.Serializable]
        public class ShelfItem
        {
            public string itemId = "chips";
            public string displayName = "Chips";
            public float price = 2.99f;
            public int maxStock = 10;
            public int currentStock = 10;
            public GameObject displayPrefab;
            public Transform displaySlot;
            [HideInInspector] public GameObject displayInstance;
        }

        [Header("=== SHELF ITEMS ===")]
        [Tooltip("Items on this shelf")]
        public List<ShelfItem> items = new List<ShelfItem>();

        [Header("=== SETTINGS ===")]
        [Tooltip("Auto-restock time (0 = manual only)")]
        public float autoRestockTime = 0f;

        [Tooltip("Restock amount per cycle")]
        public int restockAmount = 1;

        [Header("=== CUSTOMER INTERACTION ===")]
        [Tooltip("Customers can take items")]
        public bool customersCanTake = true;

        [Tooltip("Chance customer takes item (per visit)")]
        [Range(0f, 1f)]
        public float takeChance = 0.5f;

        [Header("=== EVENTS ===")]
        public UnityEvent<ShelfItem> onItemTaken;
        public UnityEvent<ShelfItem> onItemRestocked;
        public UnityEvent onShelfEmpty;

        private float restockTimer;

        private void Start()
        {
            // Spawn initial displays
            foreach (var item in items)
            {
                UpdateItemDisplay(item);
            }
        }

        private void Update()
        {
            // Auto restock
            if (autoRestockTime > 0)
            {
                restockTimer += Time.deltaTime;
                if (restockTimer >= autoRestockTime)
                {
                    restockTimer = 0;
                    RestockAll(restockAmount);
                }
            }
        }

        #region Stock Management

        /// <summary>
        /// Take an item from shelf (used by customers/player).
        /// </summary>
        public ShelfItem TakeItem(string itemId)
        {
            var item = items.Find(i => i.itemId == itemId && i.currentStock > 0);
            if (item != null)
            {
                item.currentStock--;
                UpdateItemDisplay(item);
                onItemTaken?.Invoke(item);

                // Check if shelf is empty
                if (IsShelfEmpty())
                {
                    onShelfEmpty?.Invoke();
                }

                return item;
            }
            return null;
        }

        /// <summary>
        /// Take a random item (used by browsing customers).
        /// </summary>
        public ShelfItem TakeRandomItem()
        {
            var available = items.FindAll(i => i.currentStock > 0);
            if (available.Count > 0)
            {
                var item = available[Random.Range(0, available.Count)];
                return TakeItem(item.itemId);
            }
            return null;
        }

        /// <summary>
        /// Restock a specific item.
        /// </summary>
        public void RestockItem(string itemId, int amount = 1)
        {
            var item = items.Find(i => i.itemId == itemId);
            if (item != null)
            {
                item.currentStock = Mathf.Min(item.currentStock + amount, item.maxStock);
                UpdateItemDisplay(item);
                onItemRestocked?.Invoke(item);
            }
        }

        /// <summary>
        /// Restock all items.
        /// </summary>
        public void RestockAll(int amountPerItem = 1)
        {
            foreach (var item in items)
            {
                item.currentStock = Mathf.Min(item.currentStock + amountPerItem, item.maxStock);
                UpdateItemDisplay(item);
            }
        }

        /// <summary>
        /// Fully restock all items.
        /// </summary>
        public void FullyRestock()
        {
            foreach (var item in items)
            {
                item.currentStock = item.maxStock;
                UpdateItemDisplay(item);
            }
        }

        /// <summary>
        /// Check if shelf is completely empty.
        /// </summary>
        public bool IsShelfEmpty()
        {
            return items.TrueForAll(i => i.currentStock <= 0);
        }

        /// <summary>
        /// Get total value of items on shelf.
        /// </summary>
        public float GetTotalValue()
        {
            float total = 0;
            foreach (var item in items)
            {
                total += item.price * item.currentStock;
            }
            return total;
        }

        #endregion

        #region Display

        private void UpdateItemDisplay(ShelfItem item)
        {
            if (item.displaySlot == null || item.displayPrefab == null) return;

            // Destroy old display
            if (item.displayInstance != null)
            {
                Destroy(item.displayInstance);
            }

            // Create new display if in stock
            if (item.currentStock > 0)
            {
                item.displayInstance = Instantiate(item.displayPrefab, item.displaySlot);
                item.displayInstance.transform.localPosition = Vector3.zero;
                item.displayInstance.transform.localRotation = Quaternion.identity;

                // Scale based on stock? (optional visual feedback)
                float stockPercent = (float)item.currentStock / item.maxStock;
                // Could scale or change material based on stock level
            }
        }

        #endregion

        #region Customer Interaction

        /// <summary>
        /// Called when a customer browses this shelf.
        /// </summary>
        public ShelfItem CustomerBrowse()
        {
            if (!customersCanTake) return null;

            if (Random.value < takeChance)
            {
                return TakeRandomItem();
            }
            return null;
        }

        #endregion
    }
}
