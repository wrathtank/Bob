using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// System for crafting and smoking cigars with special powers.
    /// </summary>
    public class CigarSystem : MonoBehaviour
    {
        public static CigarSystem Instance { get; private set; }

        [Header("Available Cigars")]
        [Tooltip("All cigar types that can be crafted")]
        public List<CigarType> cigarTypes = new List<CigarType>();

        [Header("Events")]
        public UnityEvent<CigarType> onCigarCrafted;
        public UnityEvent<CigarType, PlayerController> onCigarSmoked;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        /// <summary>
        /// Craft a cigar for a player.
        /// </summary>
        public bool CraftCigar(string cigarId, PlayerController player)
        {
            var cigar = cigarTypes.Find(c => c.cigarId == cigarId);
            if (cigar == null) return false;

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return false;

            // Check if player has required materials
            foreach (var material in cigar.requiredMaterials)
            {
                if (!inventory.HasItem(material.itemId, material.quantity))
                {
                    return false;
                }
            }

            // Consume materials
            foreach (var material in cigar.requiredMaterials)
            {
                inventory.RemoveItem(material.itemId, material.quantity);
            }

            // Add cigar to inventory
            inventory.AddItem(new InventoryItem
            {
                itemId = cigar.cigarId,
                itemName = cigar.cigarName,
                description = cigar.description,
                icon = cigar.icon,
                quantity = 1,
                isStackable = true,
                isConsumable = true
            });

            onCigarCrafted?.Invoke(cigar);
            return true;
        }

        /// <summary>
        /// Smoke a cigar and apply effects.
        /// </summary>
        public bool SmokeCigar(string cigarId, PlayerController player)
        {
            var cigar = cigarTypes.Find(c => c.cigarId == cigarId);
            if (cigar == null) return false;

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return false;

            // Check if player has cigar
            if (!inventory.HasItem(cigarId))
            {
                return false;
            }

            // Remove cigar
            inventory.RemoveItem(cigarId);

            // Apply effects
            ApplyCigarEffects(cigar, player);

            onCigarSmoked?.Invoke(cigar, player);
            return true;
        }

        private void ApplyCigarEffects(CigarType cigar, PlayerController player)
        {
            // Apply movement effects
            if (cigar.speedMultiplier != 1f || cigar.jumpMultiplier != 1f)
            {
                player.ApplyCigarEffect(cigar.speedMultiplier, cigar.jumpMultiplier, cigar.effectDuration);
            }

            // Apply health effects
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                if (cigar.healthBoost > 0)
                {
                    health.Heal(cigar.healthBoost);
                }

                if (cigar.invincibilityDuration > 0)
                {
                    health.SetInvincible(true, cigar.invincibilityDuration);
                }
            }

            // Apply money bonus
            if (cigar.moneyBonus > 0)
            {
                var inventory = player.GetComponent<PlayerInventory>();
                inventory?.AddMoney(cigar.moneyBonus);
            }

            // Visual/Audio effects
            if (cigar.smokeEffect != null)
            {
                var effect = Instantiate(cigar.smokeEffect, player.transform.position + Vector3.up * 1.5f, Quaternion.identity);
                effect.transform.SetParent(player.transform);
                Destroy(effect, cigar.effectDuration);
            }

            if (cigar.smokeSound != null)
            {
                AudioSource.PlayClipAtPoint(cigar.smokeSound, player.transform.position);
            }
        }

        /// <summary>
        /// Get cigar by ID.
        /// </summary>
        public CigarType GetCigar(string cigarId)
        {
            return cigarTypes.Find(c => c.cigarId == cigarId);
        }

        /// <summary>
        /// Get all available cigar types.
        /// </summary>
        public List<CigarType> GetAllCigars()
        {
            return new List<CigarType>(cigarTypes);
        }
    }

    [System.Serializable]
    public class CigarType
    {
        [Header("Info")]
        public string cigarId;
        public string cigarName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Crafting")]
        public List<CraftingMaterial> requiredMaterials = new List<CraftingMaterial>();

        [Header("Effects")]
        [Tooltip("Effect duration in seconds")]
        public float effectDuration = 30f;

        [Tooltip("Speed multiplier (1 = normal)")]
        public float speedMultiplier = 1f;

        [Tooltip("Jump multiplier (1 = normal)")]
        public float jumpMultiplier = 1f;

        [Tooltip("Health restored")]
        public float healthBoost = 0f;

        [Tooltip("Invincibility duration")]
        public float invincibilityDuration = 0f;

        [Tooltip("Money bonus")]
        public int moneyBonus = 0;

        [Header("Visuals/Audio")]
        public GameObject smokeEffect;
        public AudioClip smokeSound;
    }

    [System.Serializable]
    public class CraftingMaterial
    {
        public string itemId;
        public string itemName;
        public int quantity = 1;
    }

    /// <summary>
    /// Cigar crafting station. Players interact to craft cigars.
    /// </summary>
    public class CigarCraftingStation : MonoBehaviour, IInteractable
    {
        [Header("UI")]
        public GameObject craftingUIPrefab;

        [Header("Interaction")]
        public string interactionPrompt = "Press E to Craft Cigars";

        private GameObject currentUI;

        public void Interact(PlayerController player)
        {
            OpenCraftingUI(player);
        }

        public string GetInteractionPrompt()
        {
            return interactionPrompt;
        }

        private void OpenCraftingUI(PlayerController player)
        {
            if (craftingUIPrefab != null && currentUI == null)
            {
                currentUI = Instantiate(craftingUIPrefab);
                var ui = currentUI.GetComponent<CigarCraftingUI>();
                if (ui != null)
                {
                    ui.Initialize(player, this);
                }

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void CloseUI()
        {
            if (currentUI != null)
            {
                Destroy(currentUI);
                currentUI = null;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// UI for cigar crafting.
    /// </summary>
    public class CigarCraftingUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public Transform cigarListContainer;
        public GameObject cigarListItemPrefab;

        private PlayerController player;
        private CigarCraftingStation station;

        public void Initialize(PlayerController playerController, CigarCraftingStation craftingStation)
        {
            player = playerController;
            station = craftingStation;
            RefreshList();
        }

        private void RefreshList()
        {
            if (cigarListContainer == null || cigarListItemPrefab == null)
                return;

            // Clear
            foreach (Transform child in cigarListContainer)
            {
                Destroy(child.gameObject);
            }

            // Populate
            if (CigarSystem.Instance != null)
            {
                foreach (var cigar in CigarSystem.Instance.GetAllCigars())
                {
                    var item = Instantiate(cigarListItemPrefab, cigarListContainer);
                    var text = item.GetComponentInChildren<TMPro.TMP_Text>();
                    if (text != null)
                    {
                        text.text = cigar.cigarName;
                    }

                    var button = item.GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        string cigarId = cigar.cigarId;
                        button.onClick.AddListener(() => CraftCigar(cigarId));
                    }
                }
            }
        }

        private void CraftCigar(string cigarId)
        {
            if (CigarSystem.Instance != null && player != null)
            {
                CigarSystem.Instance.CraftCigar(cigarId, player);
                RefreshList();
            }
        }

        public void Close()
        {
            station?.CloseUI();
        }
    }
}
