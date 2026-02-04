using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

namespace BobsPetroleum.Economy
{
    /// <summary>
    /// Universal shop system for weapons, vehicles, items, and more.
    /// Easy to configure different shop types in inspector.
    /// </summary>
    public class ShopSystem : MonoBehaviour
    {
        public static ShopSystem Instance { get; private set; }

        [Header("Shop Type")]
        public ShopType shopType = ShopType.General;

        [Tooltip("Shop name displayed in UI")]
        public string shopName = "Bob's Shop";

        [Header("Inventory")]
        [Tooltip("Items for sale")]
        public List<ShopItem> shopItems = new List<ShopItem>();

        [Header("UI References")]
        [Tooltip("Shop panel")]
        public GameObject shopPanel;

        [Tooltip("Item list container")]
        public Transform itemListContainer;

        [Tooltip("Item button prefab")]
        public GameObject itemButtonPrefab;

        [Tooltip("Item preview panel")]
        public GameObject itemPreviewPanel;

        [Tooltip("Preview name text")]
        public TMP_Text previewNameText;

        [Tooltip("Preview description text")]
        public TMP_Text previewDescriptionText;

        [Tooltip("Preview price text")]
        public TMP_Text previewPriceText;

        [Tooltip("Preview image")]
        public UnityEngine.UI.Image previewImage;

        [Tooltip("Buy button")]
        public UnityEngine.UI.Button buyButton;

        [Tooltip("Player money display")]
        public TMP_Text playerMoneyText;

        [Header("Preview 3D")]
        [Tooltip("3D preview spawn point")]
        public Transform previewSpawnPoint;

        [Tooltip("Preview camera")]
        public Camera previewCamera;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip purchaseSound;
        public AudioClip failSound;
        public AudioClip hoverSound;

        [Header("Events")]
        public UnityEvent onShopOpened;
        public UnityEvent onShopClosed;
        public UnityEvent<ShopItem> onItemPurchased;
        public UnityEvent<ShopItem> onItemSelected;

        // Runtime
        private ShopItem selectedItem;
        private Player.PlayerInventory playerInventory;
        private AudioSource audioSource;
        private GameObject currentPreviewObject;
        private bool isOpen;

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

        private void Start()
        {
            // Hide shop by default
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            // Find player inventory
            playerInventory = FindObjectOfType<Player.PlayerInventory>();
        }

        #region Shop Control

        /// <summary>
        /// Open the shop.
        /// </summary>
        public void OpenShop()
        {
            if (isOpen) return;

            isOpen = true;

            // Find player inventory if not cached
            if (playerInventory == null)
            {
                playerInventory = FindObjectOfType<Player.PlayerInventory>();
            }

            // Show panel
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable player controls
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = false;
            }

            // Populate items
            PopulateShopItems();

            // Update money display
            UpdateMoneyDisplay();

            PlaySound(openSound);
            onShopOpened?.Invoke();

            Debug.Log($"[Shop] Opened {shopName}");
        }

        /// <summary>
        /// Close the shop.
        /// </summary>
        public void CloseShop()
        {
            if (!isOpen) return;

            isOpen = false;

            // Hide panel
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Re-enable player controls
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = true;
            }

            // Clean up preview
            ClearPreview();

            PlaySound(closeSound);
            onShopClosed?.Invoke();

            Debug.Log($"[Shop] Closed {shopName}");
        }

        /// <summary>
        /// Toggle shop open/closed.
        /// </summary>
        public void ToggleShop()
        {
            if (isOpen)
            {
                CloseShop();
            }
            else
            {
                OpenShop();
            }
        }

        #endregion

        #region Item Display

        private void PopulateShopItems()
        {
            if (itemListContainer == null || itemButtonPrefab == null) return;

            // Clear existing
            foreach (Transform child in itemListContainer)
            {
                Destroy(child.gameObject);
            }

            // Create item buttons
            foreach (var item in shopItems)
            {
                if (item == null) continue;

                GameObject button = Instantiate(itemButtonPrefab, itemListContainer);

                // Set up button
                var buttonComponent = button.GetComponent<UnityEngine.UI.Button>();
                if (buttonComponent != null)
                {
                    ShopItem capturedItem = item; // Capture for closure
                    buttonComponent.onClick.AddListener(() => SelectItem(capturedItem));
                }

                // Set name
                var nameText = button.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = $"{item.itemName} - ${item.price}";
                }

                // Set icon
                var icon = button.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
                if (icon != null && item.icon != null)
                {
                    icon.sprite = item.icon;
                }

                // Mark if owned
                if (IsItemOwned(item))
                {
                    var image = button.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        image.color = new Color(0.5f, 1f, 0.5f); // Green tint
                    }

                    if (nameText != null)
                    {
                        nameText.text = $"{item.itemName} [OWNED]";
                    }
                }
            }
        }

        /// <summary>
        /// Select an item to preview.
        /// </summary>
        public void SelectItem(ShopItem item)
        {
            if (item == null) return;

            selectedItem = item;

            PlaySound(hoverSound);

            // Update preview UI
            if (previewNameText != null)
            {
                previewNameText.text = item.itemName;
            }

            if (previewDescriptionText != null)
            {
                previewDescriptionText.text = item.description;
            }

            if (previewPriceText != null)
            {
                previewPriceText.text = IsItemOwned(item) ? "OWNED" : $"${item.price}";
            }

            if (previewImage != null && item.icon != null)
            {
                previewImage.sprite = item.icon;
                previewImage.gameObject.SetActive(true);
            }

            // Update buy button
            if (buyButton != null)
            {
                bool canBuy = !IsItemOwned(item) && playerInventory != null &&
                             playerInventory.Money >= item.price;

                buyButton.interactable = canBuy;

                var buyText = buyButton.GetComponentInChildren<TMP_Text>();
                if (buyText != null)
                {
                    if (IsItemOwned(item))
                    {
                        buyText.text = "OWNED";
                    }
                    else if (playerInventory != null && playerInventory.Money < item.price)
                    {
                        buyText.text = "NOT ENOUGH $";
                    }
                    else
                    {
                        buyText.text = "BUY";
                    }
                }
            }

            // Show preview panel
            if (itemPreviewPanel != null)
            {
                itemPreviewPanel.SetActive(true);
            }

            // Spawn 3D preview
            Show3DPreview(item);

            onItemSelected?.Invoke(item);
        }

        private void Show3DPreview(ShopItem item)
        {
            ClearPreview();

            if (item.previewPrefab != null && previewSpawnPoint != null)
            {
                currentPreviewObject = Instantiate(item.previewPrefab, previewSpawnPoint);
                currentPreviewObject.transform.localPosition = Vector3.zero;
                currentPreviewObject.transform.localRotation = Quaternion.identity;

                // Add rotation script for viewing
                var rotator = currentPreviewObject.AddComponent<PreviewRotator>();
            }
        }

        private void ClearPreview()
        {
            if (currentPreviewObject != null)
            {
                Destroy(currentPreviewObject);
            }
        }

        #endregion

        #region Purchasing

        /// <summary>
        /// Purchase the currently selected item.
        /// </summary>
        public void PurchaseSelectedItem()
        {
            if (selectedItem == null) return;
            PurchaseItem(selectedItem);
        }

        /// <summary>
        /// Purchase a specific item.
        /// </summary>
        public bool PurchaseItem(ShopItem item)
        {
            if (item == null) return false;

            // Check if already owned
            if (IsItemOwned(item))
            {
                Debug.Log($"[Shop] Already own {item.itemName}");
                return false;
            }

            // Check money
            if (playerInventory == null || playerInventory.Money < item.price)
            {
                Debug.Log($"[Shop] Not enough money for {item.itemName}");
                PlaySound(failSound);
                return false;
            }

            // Deduct money
            playerInventory.RemoveMoney(item.price);

            // Grant item based on type
            GrantItem(item);

            // Save as owned
            MarkItemOwned(item);

            // Update UI
            UpdateMoneyDisplay();
            PopulateShopItems();
            SelectItem(item); // Refresh selection

            PlaySound(purchaseSound);
            onItemPurchased?.Invoke(item);

            // Show notification
            UI.HUDManager.Instance?.ShowNotification($"Purchased {item.itemName}!");

            Debug.Log($"[Shop] Purchased {item.itemName} for ${item.price}");
            return true;
        }

        private void GrantItem(ShopItem item)
        {
            switch (item.itemType)
            {
                case ShopItemType.Weapon:
                    // Add to gun system
                    if (item.weaponData != null)
                    {
                        Combat.SimpleGunSystem.Instance?.AddWeapon(item.weaponData);
                    }
                    break;

                case ShopItemType.Vehicle:
                    // Spawn vehicle or add to garage
                    if (item.prefab != null)
                    {
                        // Find vehicle spawn point or player position
                        var player = FindObjectOfType<Player.PlayerController>();
                        Vector3 spawnPos = player != null ?
                            player.transform.position + player.transform.forward * 5f :
                            Vector3.zero;

                        Instantiate(item.prefab, spawnPos, Quaternion.identity);
                    }
                    break;

                case ShopItemType.Pet:
                    // Add to player's pets
                    if (item.petData != null)
                    {
                        Battle.BattleSystem.Instance?.AddPetToTeam(item.petData);
                    }
                    break;

                case ShopItemType.Skin:
                    // Unlock skin
                    if (item.skinData != null)
                    {
                        Player.SkinManager.Instance?.UnlockSkin(item.skinData);
                    }
                    break;

                case ShopItemType.Consumable:
                    // Add to inventory
                    if (playerInventory != null)
                    {
                        playerInventory.AddItem(item.itemId, item.quantity);
                    }
                    break;

                case ShopItemType.Upgrade:
                    // Apply upgrade
                    ApplyUpgrade(item);
                    break;
            }
        }

        private void ApplyUpgrade(ShopItem item)
        {
            // Example upgrade handling
            switch (item.upgradeType)
            {
                case UpgradeType.MaxHealth:
                    var health = FindObjectOfType<Player.PlayerHealth>();
                    if (health != null)
                    {
                        health.maxHealth += item.upgradeValue;
                    }
                    break;

                case UpgradeType.Speed:
                    var controller = FindObjectOfType<Player.PlayerController>();
                    if (controller != null)
                    {
                        controller.moveSpeed += item.upgradeValue;
                    }
                    break;

                case UpgradeType.Stamina:
                    var player = FindObjectOfType<Player.PlayerController>();
                    if (player != null)
                    {
                        player.maxStamina += item.upgradeValue;
                    }
                    break;
            }
        }

        #endregion

        #region Ownership

        private bool IsItemOwned(ShopItem item)
        {
            if (item == null) return false;
            return PlayerPrefs.GetInt($"Shop_Owned_{item.itemId}", 0) == 1;
        }

        private void MarkItemOwned(ShopItem item)
        {
            if (item == null) return;
            PlayerPrefs.SetInt($"Shop_Owned_{item.itemId}", 1);
            PlayerPrefs.Save();
        }

        #endregion

        #region UI Updates

        private void UpdateMoneyDisplay()
        {
            if (playerMoneyText != null && playerInventory != null)
            {
                playerMoneyText.text = $"${playerInventory.Money}";
            }
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Properties

        public bool IsOpen => isOpen;
        public ShopItem SelectedItem => selectedItem;

        #endregion
    }

    /// <summary>
    /// Item available for purchase in shop.
    /// </summary>
    [System.Serializable]
    public class ShopItem
    {
        [Header("Info")]
        public string itemId;
        public string itemName = "Item";
        [TextArea(2, 4)]
        public string description = "An item for sale";
        public Sprite icon;

        [Header("Price")]
        public int price = 100;
        public int quantity = 1;

        [Header("Type")]
        public ShopItemType itemType = ShopItemType.Consumable;

        [Header("References (Based on Type)")]
        public GameObject prefab;
        public GameObject previewPrefab;
        public Combat.WeaponData weaponData;
        public Battle.PetData petData;
        public Player.SkinData skinData;

        [Header("Upgrade Settings")]
        public UpgradeType upgradeType;
        public float upgradeValue;
    }

    public enum ShopType
    {
        General,
        Weapons,
        Vehicles,
        Pets,
        Skins,
        Upgrades
    }

    public enum ShopItemType
    {
        Consumable,
        Weapon,
        Vehicle,
        Pet,
        Skin,
        Upgrade
    }

    public enum UpgradeType
    {
        None,
        MaxHealth,
        Speed,
        Stamina,
        Damage,
        Defense
    }

    /// <summary>
    /// Simple preview rotation script.
    /// </summary>
    public class PreviewRotator : MonoBehaviour
    {
        public float rotationSpeed = 30f;

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
