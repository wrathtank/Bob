using UnityEngine;
using BobsPetroleum.Player;

namespace BobsPetroleum.Shop
{
    /// <summary>
    /// Shop shelf that holds items for customers to buy.
    /// Place in the gas station store.
    /// </summary>
    public class ShopShelf : MonoBehaviour
    {
        [Header("Item Settings")]
        [Tooltip("The item on this shelf")]
        public ShopItem shopItem;

        [Tooltip("Visual representation of the item")]
        public GameObject itemVisual;

        [Tooltip("Number of items displayed")]
        public int displayCount = 3;

        [Header("Restock")]
        [Tooltip("Auto-restock daily")]
        public bool autoRestock = true;

        [Tooltip("Current stock")]
        public int currentStock = 10;

        [Tooltip("Maximum stock")]
        public int maxStock = 10;

        /// <summary>
        /// Take an item from the shelf (called by customers).
        /// </summary>
        public bool TakeItem()
        {
            if (currentStock <= 0)
            {
                return false;
            }

            currentStock--;
            UpdateVisuals();
            return true;
        }

        /// <summary>
        /// Restock the shelf.
        /// </summary>
        public void Restock()
        {
            currentStock = maxStock;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Update visual representation based on stock
            if (itemVisual != null)
            {
                itemVisual.SetActive(currentStock > 0);
            }
        }

        /// <summary>
        /// Check if shelf has stock.
        /// </summary>
        public bool HasStock()
        {
            return currentStock > 0;
        }
    }

    [System.Serializable]
    public class ShopItem
    {
        public string itemId;
        public string itemName;
        public Sprite icon;
        public float price;
        public GameObject itemPrefab;
    }

    /// <summary>
    /// Vendor shop UI for buying from NPCs.
    /// </summary>
    public class VendorShopUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Transform itemListContainer;
        public GameObject itemButtonPrefab;
        public TMPro.TMP_Text moneyDisplay;
        public TMPro.TMP_Text vendorNameText;
        public UnityEngine.UI.Button closeButton;

        private NPC.VendorNPC vendor;
        private PlayerController player;

        public void Initialize(NPC.VendorNPC vendorNPC, PlayerController playerController)
        {
            vendor = vendorNPC;
            player = playerController;

            if (vendorNameText != null)
            {
                vendorNameText.text = vendor.vendorName;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            RefreshUI();
        }

        private void Update()
        {
            // Update money display
            if (moneyDisplay != null && player != null)
            {
                var inventory = player.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    moneyDisplay.text = $"${inventory.Money}";
                }
            }

            // Close with escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }
        }

        private void RefreshUI()
        {
            if (itemListContainer == null || itemButtonPrefab == null)
                return;

            // Clear existing
            foreach (Transform child in itemListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create item buttons
            for (int i = 0; i < vendor.inventory.Count; i++)
            {
                var item = vendor.inventory[i];
                var btnObj = Instantiate(itemButtonPrefab, itemListContainer);

                var text = btnObj.GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null)
                {
                    string stockText = item.stock < 0 ? "" : $" ({item.stock})";
                    text.text = $"{item.itemName} - ${item.price}{stockText}";
                }

                var button = btnObj.GetComponent<UnityEngine.UI.Button>();
                if (button != null)
                {
                    int index = i;
                    button.onClick.AddListener(() => PurchaseItem(index));
                    button.interactable = item.stock != 0;
                }
            }
        }

        private void PurchaseItem(int index)
        {
            if (vendor.TryPurchase(index, player))
            {
                RefreshUI();
            }
        }

        public void Close()
        {
            vendor?.CloseShop();
        }
    }
}
