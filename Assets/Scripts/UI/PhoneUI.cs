using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;
using BobsPetroleum.Core;
using BobsPetroleum.Player;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Mobile phone UI that displays when player presses Tab.
    /// Shows health, money, day, party members, and inventory.
    /// Attach to your phone prefab.
    /// </summary>
    public class PhoneUI : MonoBehaviour
    {
        public static PhoneUI Instance { get; private set; }

        [Header("Phone Display")]
        [Tooltip("The phone GameObject to show/hide")]
        public GameObject phoneObject;

        [Tooltip("Key to toggle phone (default Tab)")]
        public KeyCode toggleKey = KeyCode.Tab;

        [Header("Stats Display")]
        [Tooltip("TextMeshPro for health display")]
        public TMP_Text healthText;

        [Tooltip("TextMeshPro for money display")]
        public TMP_Text moneyText;

        [Tooltip("TextMeshPro for current day display")]
        public TMP_Text dayText;

        [Tooltip("Format string for health (use {0} for value)")]
        public string healthFormat = "Health: {0}";

        [Tooltip("Format string for money (use {0} for value)")]
        public string moneyFormat = "${0}";

        [Tooltip("Format string for day (use {0} for current, {1} for total)")]
        public string dayFormat = "Day {0}/{1}";

        [Header("Party Display")]
        [Tooltip("Container for party member list")]
        public Transform partyListContainer;

        [Tooltip("Prefab for party member entry")]
        public GameObject partyMemberPrefab;

        [Header("Inventory Display")]
        [Tooltip("Container for inventory items")]
        public Transform inventoryContainer;

        [Tooltip("Prefab for inventory item entry")]
        public GameObject inventoryItemPrefab;

        [Header("Captured Animals Display")]
        [Tooltip("Container for captured animals list")]
        public Transform animalsContainer;

        [Tooltip("Prefab for animal entry")]
        public GameObject animalEntryPrefab;

        [Header("Events")]
        public UnityEvent onPhoneOpened;
        public UnityEvent onPhoneClosed;

        private bool isOpen = false;
        private PlayerController localPlayer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (phoneObject != null)
            {
                phoneObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePhone();
            }

            if (isOpen)
            {
                RefreshDisplay();
            }
        }

        /// <summary>
        /// Toggle phone visibility.
        /// </summary>
        public void TogglePhone()
        {
            if (isOpen)
            {
                ClosePhone();
            }
            else
            {
                OpenPhone();
            }
        }

        /// <summary>
        /// Open the phone UI.
        /// </summary>
        public void OpenPhone()
        {
            if (phoneObject != null)
            {
                phoneObject.SetActive(true);
                isOpen = true;
                RefreshDisplay();
                onPhoneOpened?.Invoke();

                // Optionally unlock cursor
                // Cursor.lockState = CursorLockMode.None;
                // Cursor.visible = true;
            }
        }

        /// <summary>
        /// Close the phone UI.
        /// </summary>
        public void ClosePhone()
        {
            if (phoneObject != null)
            {
                phoneObject.SetActive(false);
                isOpen = false;
                onPhoneClosed?.Invoke();

                // Optionally lock cursor back
                // Cursor.lockState = CursorLockMode.Locked;
                // Cursor.visible = false;
            }
        }

        /// <summary>
        /// Set the local player reference.
        /// </summary>
        public void SetLocalPlayer(PlayerController player)
        {
            localPlayer = player;
        }

        /// <summary>
        /// Refresh all displayed information.
        /// </summary>
        public void RefreshDisplay()
        {
            RefreshStats();
            RefreshPartyList();
            RefreshInventory();
            RefreshAnimals();
        }

        private void RefreshStats()
        {
            // Health
            if (healthText != null && localPlayer != null)
            {
                var health = localPlayer.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    healthText.text = string.Format(healthFormat, Mathf.CeilToInt(health.CurrentHealth));
                }
            }

            // Money
            if (moneyText != null && localPlayer != null)
            {
                var inventory = localPlayer.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    moneyText.text = string.Format(moneyFormat, inventory.Money);
                }
            }

            // Day
            if (dayText != null && GameManager.Instance != null)
            {
                dayText.text = string.Format(dayFormat,
                    GameManager.Instance.currentDay,
                    GameManager.Instance.totalDays);
            }
        }

        private void RefreshPartyList()
        {
            if (partyListContainer == null || partyMemberPrefab == null)
                return;

            // Clear existing
            foreach (Transform child in partyListContainer)
            {
                Destroy(child.gameObject);
            }

            // Add party members
            if (BobsNetworkManager.Instance != null)
            {
                var players = BobsNetworkManager.Instance.GetConnectedPlayers();
                foreach (var player in players)
                {
                    var entry = Instantiate(partyMemberPrefab, partyListContainer);
                    var text = entry.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        text.text = player.playerName;
                    }
                }
            }
        }

        private void RefreshInventory()
        {
            if (inventoryContainer == null || inventoryItemPrefab == null)
                return;

            // Clear existing
            foreach (Transform child in inventoryContainer)
            {
                Destroy(child.gameObject);
            }

            // Add inventory items
            if (localPlayer != null)
            {
                var inventory = localPlayer.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    foreach (var item in inventory.GetAllItems())
                    {
                        var entry = Instantiate(inventoryItemPrefab, inventoryContainer);
                        var text = entry.GetComponentInChildren<TMP_Text>();
                        if (text != null)
                        {
                            text.text = $"{item.itemName} x{item.quantity}";
                        }
                    }
                }
            }
        }

        private void RefreshAnimals()
        {
            if (animalsContainer == null || animalEntryPrefab == null)
                return;

            // Clear existing
            foreach (Transform child in animalsContainer)
            {
                Destroy(child.gameObject);
            }

            // Add captured animals
            if (localPlayer != null)
            {
                var inventory = localPlayer.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    foreach (var animal in inventory.GetCapturedAnimals())
                    {
                        var entry = Instantiate(animalEntryPrefab, animalsContainer);
                        var text = entry.GetComponentInChildren<TMP_Text>();
                        if (text != null)
                        {
                            text.text = animal.animalName;
                        }
                    }
                }
            }
        }

        public bool IsOpen => isOpen;
    }
}
