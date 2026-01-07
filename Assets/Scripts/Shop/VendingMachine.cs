using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;
using BobsPetroleum.Systems;

namespace BobsPetroleum.Shop
{
    /// <summary>
    /// Vending machine for buying items. Used for hamburgers to revive Bob.
    /// </summary>
    public class VendingMachine : MonoBehaviour, IInteractable
    {
        [Header("Vending Machine Settings")]
        [Tooltip("Items available in this vending machine")]
        public List<VendingItem> items = new List<VendingItem>();

        [Tooltip("Interaction prompt")]
        public string interactionPrompt = "Press E to Use Vending Machine";

        [Header("UI")]
        [Tooltip("Vending machine UI prefab")]
        public GameObject vendingUIPrefab;

        [Header("Audio")]
        public AudioClip selectSound;
        public AudioClip purchaseSound;
        public AudioClip dispenseSound;
        public AudioClip errorSound;

        [Header("Dispense Point")]
        [Tooltip("Where items are dispensed")]
        public Transform dispensePoint;

        [Header("Events")]
        public UnityEvent onMachineOpened;
        public UnityEvent onMachineClosed;
        public UnityEvent<VendingItem> onItemPurchased;

        private AudioSource audioSource;
        private GameObject currentUI;
        private PlayerController currentUser;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public void Interact(PlayerController player)
        {
            OpenVendingMachine(player);
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        /// <summary>
        /// Open the vending machine UI.
        /// </summary>
        public void OpenVendingMachine(PlayerController player)
        {
            if (currentUI != null) return;

            currentUser = player;

            if (vendingUIPrefab != null)
            {
                currentUI = Instantiate(vendingUIPrefab);
                var ui = currentUI.GetComponent<VendingMachineUI>();
                if (ui != null)
                {
                    ui.Initialize(this, player);
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            onMachineOpened?.Invoke();
        }

        /// <summary>
        /// Close the vending machine UI.
        /// </summary>
        public void CloseVendingMachine()
        {
            if (currentUI != null)
            {
                Destroy(currentUI);
                currentUI = null;
            }

            currentUser = null;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            onMachineClosed?.Invoke();
        }

        /// <summary>
        /// Purchase an item from the vending machine.
        /// </summary>
        public bool PurchaseItem(int itemIndex, PlayerController player)
        {
            if (itemIndex < 0 || itemIndex >= items.Count)
            {
                return false;
            }

            var item = items[itemIndex];
            var inventory = player.GetComponent<PlayerInventory>();

            if (inventory == null)
            {
                return false;
            }

            // Check if player can afford
            if (!inventory.CanAfford(item.price))
            {
                if (errorSound != null)
                {
                    audioSource.PlayOneShot(errorSound);
                }
                return false;
            }

            // Check stock
            if (item.stock == 0)
            {
                if (errorSound != null)
                {
                    audioSource.PlayOneShot(errorSound);
                }
                return false;
            }

            // Process purchase
            inventory.RemoveMoney(item.price);

            // Reduce stock
            if (item.stock > 0)
            {
                item.stock--;
            }

            // Add to inventory
            inventory.AddItem(new InventoryItem
            {
                itemId = item.itemId,
                itemName = item.itemName,
                description = item.description,
                icon = item.icon,
                quantity = 1,
                isStackable = true,
                isConsumable = item.isConsumable
            });

            // Play sounds
            if (purchaseSound != null)
            {
                audioSource.PlayOneShot(purchaseSound);
            }

            Invoke(nameof(PlayDispenseSound), 0.5f);

            // Spawn visual item at dispense point (optional)
            if (item.visualPrefab != null && dispensePoint != null)
            {
                var visual = Instantiate(item.visualPrefab, dispensePoint.position, Quaternion.identity);
                Destroy(visual, 3f);
            }

            onItemPurchased?.Invoke(item);
            return true;
        }

        private void PlayDispenseSound()
        {
            if (dispenseSound != null)
            {
                audioSource.PlayOneShot(dispenseSound);
            }
        }

        /// <summary>
        /// Restock the vending machine.
        /// </summary>
        public void Restock()
        {
            foreach (var item in items)
            {
                if (item.maxStock > 0)
                {
                    item.stock = item.maxStock;
                }
            }
        }

        /// <summary>
        /// Get all items.
        /// </summary>
        public List<VendingItem> GetItems()
        {
            return items;
        }
    }

    [System.Serializable]
    public class VendingItem
    {
        public string itemId;
        public string itemName;
        [TextArea(1, 3)]
        public string description;
        public Sprite icon;
        public int price;
        public bool isConsumable = true;

        [Tooltip("Current stock (-1 for unlimited)")]
        public int stock = -1;

        [Tooltip("Max stock for restocking")]
        public int maxStock = -1;

        [Tooltip("Visual prefab to spawn on dispense")]
        public GameObject visualPrefab;
    }

    /// <summary>
    /// UI for vending machine.
    /// </summary>
    public class VendingMachineUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Transform itemListContainer;
        public GameObject itemButtonPrefab;
        public TMPro.TMP_Text moneyDisplay;
        public Button closeButton;

        private VendingMachine vendingMachine;
        private PlayerController player;

        public void Initialize(VendingMachine machine, PlayerController playerController)
        {
            vendingMachine = machine;
            player = playerController;

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
            var items = vendingMachine.GetItems();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var btnObj = Instantiate(itemButtonPrefab, itemListContainer);

                var text = btnObj.GetComponentInChildren<TMPro.TMP_Text>();
                if (text != null)
                {
                    string stockText = item.stock < 0 ? "" : $" ({item.stock})";
                    text.text = $"{item.itemName} - ${item.price}{stockText}";
                }

                var button = btnObj.GetComponent<Button>();
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
            if (vendingMachine.PurchaseItem(index, player))
            {
                RefreshUI();
            }
        }

        public void Close()
        {
            vendingMachine?.CloseVendingMachine();
        }
    }
}
