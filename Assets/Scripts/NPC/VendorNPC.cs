using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;
using BobsPetroleum.Animation;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// Vendor NPC that sells items to players.
    /// Can sell weapons, consumables, cars, nets, etc.
    /// </summary>
    public class VendorNPC : MonoBehaviour, IInteractable
    {
        [Header("Vendor Info")]
        [Tooltip("Vendor name")]
        public string vendorName = "Shop Keeper";

        [Tooltip("Vendor type")]
        public VendorType vendorType = VendorType.General;

        [Header("Inventory")]
        [Tooltip("Items this vendor sells")]
        public List<VendorItem> inventory = new List<VendorItem>();

        [Header("Interaction")]
        [Tooltip("Interaction prompt")]
        public string interactionPrompt = "Press E to Shop";

        [Header("UI")]
        [Tooltip("Shop UI prefab to spawn")]
        public GameObject shopUIPrefab;

        [Tooltip("Parent for shop UI")]
        public Transform shopUIParent;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("Audio")]
        public AudioClip greetingSound;
        public AudioClip purchaseSound;
        public AudioClip insufficientFundsSound;

        [Header("Events")]
        public UnityEvent<PlayerController> onPlayerInteract;
        public UnityEvent<VendorItem> onItemPurchased;
        public UnityEvent onShopOpened;
        public UnityEvent onShopClosed;

        private AudioSource audioSource;
        private GameObject currentShopUI;
        private PlayerController currentCustomer;

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
            currentCustomer = player;
            OpenShop(player);
            onPlayerInteract?.Invoke(player);

            animationHandler?.TriggerAnimation("Greet");

            if (greetingSound != null)
            {
                audioSource.PlayOneShot(greetingSound);
            }
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        /// <summary>
        /// Open the shop UI for a player.
        /// </summary>
        public void OpenShop(PlayerController player)
        {
            if (shopUIPrefab != null && currentShopUI == null)
            {
                Transform parent = shopUIParent != null ? shopUIParent : player.transform;
                currentShopUI = Instantiate(shopUIPrefab, parent);

                var shopUI = currentShopUI.GetComponent<Shop.VendorShopUI>();
                if (shopUI != null)
                {
                    shopUI.Initialize(this, player);
                }

                // Unlock cursor
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                onShopOpened?.Invoke();
            }
        }

        /// <summary>
        /// Close the shop UI.
        /// </summary>
        public void CloseShop()
        {
            if (currentShopUI != null)
            {
                Destroy(currentShopUI);
                currentShopUI = null;
            }

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            currentCustomer = null;
            onShopClosed?.Invoke();
        }

        /// <summary>
        /// Try to purchase an item.
        /// </summary>
        public bool TryPurchase(int itemIndex, PlayerController player)
        {
            if (itemIndex < 0 || itemIndex >= inventory.Count)
            {
                return false;
            }

            var item = inventory[itemIndex];
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (playerInventory == null)
            {
                return false;
            }

            // Check if player can afford
            if (!playerInventory.CanAfford(item.price))
            {
                if (insufficientFundsSound != null)
                {
                    audioSource.PlayOneShot(insufficientFundsSound);
                }
                return false;
            }

            // Check stock
            if (item.stock == 0)
            {
                return false;
            }

            // Process purchase based on item type
            bool success = ProcessPurchase(item, playerInventory);

            if (success)
            {
                playerInventory.RemoveMoney(item.price);

                // Reduce stock
                if (item.stock > 0)
                {
                    item.stock--;
                }

                if (purchaseSound != null)
                {
                    audioSource.PlayOneShot(purchaseSound);
                }

                animationHandler?.TriggerAnimation("Sell");
                onItemPurchased?.Invoke(item);
            }

            return success;
        }

        private bool ProcessPurchase(VendorItem item, PlayerInventory playerInventory)
        {
            switch (item.itemType)
            {
                case VendorItemType.Consumable:
                    return playerInventory.AddItem(new InventoryItem
                    {
                        itemId = item.itemId,
                        itemName = item.itemName,
                        description = item.description,
                        icon = item.icon,
                        quantity = 1,
                        isStackable = true,
                        isConsumable = true
                    });

                case VendorItemType.Weapon:
                    if (item.itemPrefab != null)
                    {
                        var weapon = Instantiate(item.itemPrefab, playerInventory.transform);
                        weapon.SetActive(false);

                        return playerInventory.AddWeapon(new WeaponItem
                        {
                            weaponId = item.itemId,
                            weaponName = item.itemName,
                            weaponObject = weapon,
                            damage = item.weaponDamage,
                            attackSpeed = item.weaponAttackSpeed,
                            range = item.weaponRange
                        });
                    }
                    return false;

                case VendorItemType.Net:
                    return playerInventory.AddItem(new InventoryItem
                    {
                        itemId = item.itemId,
                        itemName = item.itemName,
                        description = "Use to capture animals",
                        icon = item.icon,
                        quantity = 1,
                        isStackable = true,
                        isConsumable = true
                    });

                case VendorItemType.Car:
                    // Cars are handled specially - spawn at a car spawn point
                    // This would need integration with a car spawning system
                    return true;

                default:
                    return playerInventory.AddItem(new InventoryItem
                    {
                        itemId = item.itemId,
                        itemName = item.itemName,
                        description = item.description,
                        icon = item.icon,
                        quantity = 1,
                        isStackable = true
                    });
            }
        }

        /// <summary>
        /// Restock the vendor (for day changes).
        /// </summary>
        public void Restock()
        {
            foreach (var item in inventory)
            {
                if (item.maxStock > 0)
                {
                    item.stock = item.maxStock;
                }
            }
        }

        /// <summary>
        /// Add an item to the vendor's inventory.
        /// </summary>
        public void AddItem(VendorItem item)
        {
            inventory.Add(item);
        }

        /// <summary>
        /// Remove an item from the vendor's inventory.
        /// </summary>
        public void RemoveItem(string itemId)
        {
            inventory.RemoveAll(i => i.itemId == itemId);
        }
    }

    public enum VendorType
    {
        General,
        Armory,
        CarDealer,
        AnimalSupplier
    }

    public enum VendorItemType
    {
        Consumable,
        Weapon,
        Net,
        Car,
        Misc
    }

    [System.Serializable]
    public class VendorItem
    {
        public string itemId;
        public string itemName;
        public string description;
        public Sprite icon;
        public int price;
        public VendorItemType itemType;

        [Tooltip("Prefab to spawn (for weapons, cars, etc.)")]
        public GameObject itemPrefab;

        [Tooltip("Current stock (-1 for unlimited)")]
        public int stock = -1;

        [Tooltip("Maximum stock for restocking")]
        public int maxStock = -1;

        // Weapon stats
        [Header("Weapon Stats (if weapon)")]
        public float weaponDamage = 10f;
        public float weaponAttackSpeed = 1f;
        public float weaponRange = 2f;
    }
}
