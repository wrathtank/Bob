using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BobsPetroleum.Items
{
    /// <summary>
    /// Consumable item system with inventory display and thumbnails.
    /// Items bought from shop appear in inventory with their icon.
    /// </summary>
    public class ConsumableSystem : MonoBehaviour
    {
        public static ConsumableSystem Instance { get; private set; }

        [Header("Consumable Database")]
        [Tooltip("All consumable items in the game")]
        public List<ConsumableData> allConsumables = new List<ConsumableData>();

        [Header("Player Inventory")]
        [Tooltip("Items the player currently has")]
        public List<InventorySlot> inventory = new List<InventorySlot>();

        [Tooltip("Max inventory slots")]
        public int maxSlots = 20;

        [Header("UI - Inventory Panel")]
        [Tooltip("Inventory panel")]
        public GameObject inventoryPanel;

        [Tooltip("Inventory grid container")]
        public Transform inventoryGrid;

        [Tooltip("Inventory slot prefab")]
        public GameObject slotPrefab;

        [Tooltip("Toggle key")]
        public KeyCode toggleKey = KeyCode.I;

        [Header("UI - Quick Use")]
        [Tooltip("Hotbar slots (1-9)")]
        public List<Image> hotbarSlots = new List<Image>();

        [Header("Audio")]
        public AudioClip consumeSound;
        public AudioClip equipSound;
        public AudioClip errorSound;

        [Header("Events")]
        public UnityEvent<ConsumableData> onItemConsumed;
        public UnityEvent<ConsumableData> onItemAdded;
        public UnityEvent onInventoryChanged;

        // State
        private int selectedSlot = 0;
        private AudioSource audioSource;
        private bool inventoryOpen = false;

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
            // Hide inventory
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            LoadInventory();
            RefreshUI();
        }

        private void Update()
        {
            // Toggle inventory
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleInventory();
            }

            // Quick use 1-9
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    UseItemInSlot(i);
                }
            }
        }

        #region Inventory Management

        /// <summary>
        /// Add item to inventory.
        /// </summary>
        public bool AddItem(string itemId, int quantity = 1)
        {
            ConsumableData data = GetConsumableData(itemId);
            if (data == null)
            {
                Debug.LogWarning($"[Consumable] Unknown item: {itemId}");
                return false;
            }

            return AddItem(data, quantity);
        }

        /// <summary>
        /// Add item to inventory.
        /// </summary>
        public bool AddItem(ConsumableData data, int quantity = 1)
        {
            if (data == null) return false;

            // Check for existing stack
            foreach (var slot in inventory)
            {
                if (slot.itemId == data.itemId && slot.quantity < data.maxStack)
                {
                    int canAdd = Mathf.Min(quantity, data.maxStack - slot.quantity);
                    slot.quantity += canAdd;
                    quantity -= canAdd;

                    if (quantity <= 0)
                    {
                        RefreshUI();
                        SaveInventory();
                        onItemAdded?.Invoke(data);
                        onInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }

            // Add new slots
            while (quantity > 0 && inventory.Count < maxSlots)
            {
                int addAmount = Mathf.Min(quantity, data.maxStack);
                inventory.Add(new InventorySlot
                {
                    itemId = data.itemId,
                    quantity = addAmount
                });
                quantity -= addAmount;
            }

            RefreshUI();
            SaveInventory();
            onItemAdded?.Invoke(data);
            onInventoryChanged?.Invoke();

            return quantity <= 0;
        }

        /// <summary>
        /// Remove item from inventory.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            for (int i = inventory.Count - 1; i >= 0; i--)
            {
                if (inventory[i].itemId == itemId)
                {
                    if (inventory[i].quantity >= quantity)
                    {
                        inventory[i].quantity -= quantity;
                        if (inventory[i].quantity <= 0)
                        {
                            inventory.RemoveAt(i);
                        }
                        RefreshUI();
                        SaveInventory();
                        onInventoryChanged?.Invoke();
                        return true;
                    }
                    else
                    {
                        quantity -= inventory[i].quantity;
                        inventory.RemoveAt(i);
                    }
                }
            }

            RefreshUI();
            SaveInventory();
            return false;
        }

        /// <summary>
        /// Check if player has item.
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            int count = 0;
            foreach (var slot in inventory)
            {
                if (slot.itemId == itemId)
                {
                    count += slot.quantity;
                    if (count >= quantity) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get item count.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            int count = 0;
            foreach (var slot in inventory)
            {
                if (slot.itemId == itemId)
                {
                    count += slot.quantity;
                }
            }
            return count;
        }

        #endregion

        #region Consuming Items

        /// <summary>
        /// Use item in specific slot.
        /// </summary>
        public void UseItemInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= inventory.Count)
            {
                PlaySound(errorSound);
                return;
            }

            var slot = inventory[slotIndex];
            var data = GetConsumableData(slot.itemId);

            if (data != null)
            {
                ConsumeItem(data);
                RemoveItem(slot.itemId, 1);
            }
        }

        /// <summary>
        /// Consume an item and apply its effects.
        /// </summary>
        public void ConsumeItem(ConsumableData data)
        {
            if (data == null) return;

            var player = FindObjectOfType<Player.PlayerController>();
            var health = player?.GetComponent<Player.PlayerHealth>();
            var inventory = player?.GetComponent<Player.PlayerInventory>();

            // Apply effects
            foreach (var effect in data.effects)
            {
                ApplyEffect(effect, player, health, inventory);
            }

            // Play sound
            PlaySound(data.consumeSound != null ? data.consumeSound : consumeSound);

            // Show effect
            if (data.consumeEffect != null && player != null)
            {
                Instantiate(data.consumeEffect, player.transform.position, Quaternion.identity);
            }

            // Notification
            UI.HUDManager.Instance?.ShowNotification($"Used {data.itemName}");

            onItemConsumed?.Invoke(data);

            Debug.Log($"[Consumable] Used {data.itemName}");
        }

        private void ApplyEffect(ConsumableEffect effect, Player.PlayerController player,
            Player.PlayerHealth health, Player.PlayerInventory inventory)
        {
            switch (effect.effectType)
            {
                case EffectType.Heal:
                    health?.Heal(effect.value);
                    break;

                case EffectType.HealPercent:
                    if (health != null)
                        health.Heal(health.maxHealth * (effect.value / 100f));
                    break;

                case EffectType.RestoreStamina:
                    if (player != null)
                        player.currentStamina = Mathf.Min(player.currentStamina + effect.value, player.maxStamina);
                    break;

                case EffectType.SpeedBoost:
                    if (player != null)
                        StartCoroutine(TemporarySpeedBoost(player, effect.value, effect.duration));
                    break;

                case EffectType.DamageBoost:
                    // Apply to gun system
                    StartCoroutine(TemporaryDamageBoost(effect.value, effect.duration));
                    break;

                case EffectType.Invincibility:
                    if (health != null)
                        StartCoroutine(TemporaryInvincibility(health, effect.duration));
                    break;

                case EffectType.NightVision:
                    StartCoroutine(TemporaryNightVision(effect.duration));
                    break;

                case EffectType.GiveMoney:
                    inventory?.AddMoney((int)effect.value);
                    break;

                case EffectType.FeedBob:
                    Core.BobCharacter.Instance?.FeedHamburger();
                    break;
            }
        }

        #endregion

        #region Temporary Effects

        private System.Collections.IEnumerator TemporarySpeedBoost(Player.PlayerController player, float amount, float duration)
        {
            float originalSpeed = player.moveSpeed;
            player.moveSpeed += amount;
            UI.HUDManager.Instance?.ShowNotification($"Speed boost! +{amount} for {duration}s");

            yield return new WaitForSeconds(duration);

            player.moveSpeed = originalSpeed;
        }

        private System.Collections.IEnumerator TemporaryDamageBoost(float multiplier, float duration)
        {
            // Would need to implement damage multiplier in combat system
            UI.HUDManager.Instance?.ShowNotification($"Damage boost! x{multiplier} for {duration}s");
            yield return new WaitForSeconds(duration);
        }

        private System.Collections.IEnumerator TemporaryInvincibility(Player.PlayerHealth health, float duration)
        {
            health.isInvincible = true;
            UI.HUDManager.Instance?.ShowNotification($"Invincible for {duration}s!");

            yield return new WaitForSeconds(duration);

            health.isInvincible = false;
        }

        private System.Collections.IEnumerator TemporaryNightVision(float duration)
        {
            // Brighten ambient light
            var originalAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.white * 0.8f;

            UI.HUDManager.Instance?.ShowNotification($"Night vision for {duration}s!");

            yield return new WaitForSeconds(duration);

            RenderSettings.ambientLight = originalAmbient;
        }

        #endregion

        #region UI

        public void ToggleInventory()
        {
            inventoryOpen = !inventoryOpen;

            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(inventoryOpen);
            }

            // Cursor
            Cursor.lockState = inventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = inventoryOpen;

            // Pause player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = !inventoryOpen;
            }

            if (inventoryOpen)
            {
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            RefreshInventoryGrid();
            RefreshHotbar();
        }

        private void RefreshInventoryGrid()
        {
            if (inventoryGrid == null || slotPrefab == null) return;

            // Clear existing
            foreach (Transform child in inventoryGrid)
            {
                Destroy(child.gameObject);
            }

            // Create slots
            for (int i = 0; i < inventory.Count; i++)
            {
                var slot = inventory[i];
                var data = GetConsumableData(slot.itemId);
                if (data == null) continue;

                GameObject slotObj = Instantiate(slotPrefab, inventoryGrid);

                // Set icon
                var icon = slotObj.transform.Find("Icon")?.GetComponent<Image>();
                if (icon != null && data.icon != null)
                {
                    icon.sprite = data.icon;
                    icon.enabled = true;
                }

                // Set quantity
                var qtyText = slotObj.GetComponentInChildren<TMP_Text>();
                if (qtyText != null)
                {
                    qtyText.text = slot.quantity > 1 ? slot.quantity.ToString() : "";
                }

                // Click to use
                var button = slotObj.GetComponent<Button>();
                if (button != null)
                {
                    int index = i;
                    button.onClick.AddListener(() => UseItemInSlot(index));
                }
            }
        }

        private void RefreshHotbar()
        {
            for (int i = 0; i < hotbarSlots.Count; i++)
            {
                if (hotbarSlots[i] == null) continue;

                if (i < inventory.Count)
                {
                    var data = GetConsumableData(inventory[i].itemId);
                    if (data != null && data.icon != null)
                    {
                        hotbarSlots[i].sprite = data.icon;
                        hotbarSlots[i].color = Color.white;
                    }
                }
                else
                {
                    hotbarSlots[i].sprite = null;
                    hotbarSlots[i].color = new Color(1, 1, 1, 0.3f);
                }
            }
        }

        #endregion

        #region Data Lookup

        public ConsumableData GetConsumableData(string itemId)
        {
            foreach (var data in allConsumables)
            {
                if (data.itemId == itemId)
                {
                    return data;
                }
            }
            return null;
        }

        #endregion

        #region Save/Load

        private void SaveInventory()
        {
            InventorySaveData saveData = new InventorySaveData { slots = inventory };
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("ConsumableInventory", json);
            PlayerPrefs.Save();
        }

        private void LoadInventory()
        {
            string json = PlayerPrefs.GetString("ConsumableInventory", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);
                    if (saveData != null && saveData.slots != null)
                    {
                        inventory = saveData.slots;
                    }
                }
                catch { }
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

        [System.Serializable]
        private class InventorySaveData
        {
            public List<InventorySlot> slots;
        }
    }

    [System.Serializable]
    public class InventorySlot
    {
        public string itemId;
        public int quantity;
    }

    /// <summary>
    /// Consumable item data - create in Project with Create > Bob's Petroleum > Consumable.
    /// </summary>
    [CreateAssetMenu(fileName = "NewConsumable", menuName = "Bob's Petroleum/Consumable Data")]
    public class ConsumableData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName = "Item";
        [TextArea(2, 4)]
        public string description = "A consumable item";
        public Sprite icon;

        [Header("Stacking")]
        public int maxStack = 10;

        [Header("Shop")]
        public int buyPrice = 10;
        public int sellPrice = 5;
        public bool availableInShop = true;

        [Header("Effects")]
        public List<ConsumableEffect> effects = new List<ConsumableEffect>();

        [Header("Visuals")]
        public AudioClip consumeSound;
        public GameObject consumeEffect;
        public GameObject worldPrefab;
    }

    [System.Serializable]
    public class ConsumableEffect
    {
        public EffectType effectType = EffectType.Heal;
        public float value = 25f;
        public float duration = 0f; // 0 = instant
    }

    public enum EffectType
    {
        Heal,
        HealPercent,
        RestoreStamina,
        SpeedBoost,
        DamageBoost,
        Invincibility,
        NightVision,
        GiveMoney,
        FeedBob
    }
}
