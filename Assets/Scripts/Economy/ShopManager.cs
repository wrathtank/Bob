using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BobsPetroleum.Economy
{
    /// <summary>
    /// Shop open/closed system. When closed, NPCs won't enter.
    /// Player can toggle shop status to go explore the world.
    /// Schedule 1 style shop management.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Shop State")]
        [Tooltip("Is the shop currently open?")]
        public bool isOpen = true;

        [Tooltip("Toggle key")]
        public KeyCode toggleKey = KeyCode.O;

        [Header("Visual Indicators")]
        [Tooltip("Open sign object (enabled when open)")]
        public GameObject openSign;

        [Tooltip("Closed sign object (enabled when closed)")]
        public GameObject closedSign;

        [Tooltip("Shop door (optional - closes when shop closed)")]
        public Transform shopDoor;

        [Tooltip("Door closed rotation")]
        public Vector3 doorClosedRotation = Vector3.zero;

        [Tooltip("Door open rotation")]
        public Vector3 doorOpenRotation = new Vector3(0, 90, 0);

        [Header("Lights")]
        [Tooltip("Shop lights to turn off when closed")]
        public List<Light> shopLights = new List<Light>();

        [Tooltip("Keep some lights on when closed (security lights)")]
        public List<Light> alwaysOnLights = new List<Light>();

        [Header("Audio")]
        [Tooltip("Bell sound when opening")]
        public AudioClip openSound;

        [Tooltip("Sound when closing")]
        public AudioClip closeSound;

        [Tooltip("Announcement when opening")]
        public AudioClip openAnnouncement;

        [Tooltip("Announcement when closing")]
        public AudioClip closeAnnouncement;

        [Header("Auto Schedule")]
        [Tooltip("Auto-open at day start")]
        public bool autoOpenAtDay = true;

        [Tooltip("Auto-close at night")]
        public bool autoCloseAtNight = true;

        [Header("Customer Behavior")]
        [Tooltip("Customers inside when closing will finish and leave")]
        public bool letCustomersFinish = true;

        [Tooltip("Time before force-removing customers (seconds)")]
        public float forceCloseTime = 60f;

        [Header("Events")]
        public UnityEvent onShopOpen;
        public UnityEvent onShopClose;
        public UnityEvent<bool> onShopStateChanged;

        // Runtime
        private AudioSource audioSource;
        private List<NPC.CustomerAI> customersInShop = new List<NPC.CustomerAI>();
        private float closingTimer;

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
            // Apply initial state
            ApplyShopState();

            // Subscribe to day/night events
            if (Systems.DayNightCycle.Instance != null)
            {
                Systems.DayNightCycle.Instance.onDayStart.AddListener(OnDayStart);
                Systems.DayNightCycle.Instance.onNightStart.AddListener(OnNightStart);
            }
        }

        private void OnDestroy()
        {
            if (Systems.DayNightCycle.Instance != null)
            {
                Systems.DayNightCycle.Instance.onDayStart.RemoveListener(OnDayStart);
                Systems.DayNightCycle.Instance.onNightStart.RemoveListener(OnNightStart);
            }
        }

        private void Update()
        {
            // Toggle input
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleShop();
            }

            // Force close timer
            if (!isOpen && letCustomersFinish && customersInShop.Count > 0)
            {
                closingTimer -= Time.deltaTime;
                if (closingTimer <= 0f)
                {
                    ForceRemoveAllCustomers();
                }
            }
        }

        #region Shop Control

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

        /// <summary>
        /// Open the shop.
        /// </summary>
        public void OpenShop()
        {
            if (isOpen) return;

            isOpen = true;
            ApplyShopState();

            // Play sounds
            PlaySound(openSound);
            if (openAnnouncement != null)
            {
                Invoke(nameof(PlayOpenAnnouncement), 0.5f);
            }

            // Notify
            onShopOpen?.Invoke();
            onShopStateChanged?.Invoke(true);

            // Show notification
            UI.HUDManager.Instance?.ShowNotification("Shop is now OPEN!");

            Debug.Log("[ShopManager] Shop opened");
        }

        /// <summary>
        /// Close the shop.
        /// </summary>
        public void CloseShop()
        {
            if (!isOpen) return;

            isOpen = false;
            ApplyShopState();

            // Start closing timer
            closingTimer = forceCloseTime;

            // Play sounds
            PlaySound(closeSound);
            if (closeAnnouncement != null)
            {
                Invoke(nameof(PlayCloseAnnouncement), 0.5f);
            }

            // Notify customers to leave
            NotifyCustomersToLeave();

            // Notify
            onShopClose?.Invoke();
            onShopStateChanged?.Invoke(false);

            // Show notification
            UI.HUDManager.Instance?.ShowNotification("Shop is now CLOSED. Time to explore!");

            Debug.Log("[ShopManager] Shop closed");
        }

        private void ApplyShopState()
        {
            // Signs
            if (openSign != null) openSign.SetActive(isOpen);
            if (closedSign != null) closedSign.SetActive(!isOpen);

            // Door
            if (shopDoor != null)
            {
                shopDoor.localEulerAngles = isOpen ? doorOpenRotation : doorClosedRotation;
            }

            // Lights
            foreach (var light in shopLights)
            {
                if (light != null)
                {
                    light.enabled = isOpen;
                }
            }
        }

        #endregion

        #region Customer Management

        /// <summary>
        /// Check if shop is accepting customers.
        /// </summary>
        public bool IsAcceptingCustomers()
        {
            return isOpen;
        }

        /// <summary>
        /// Register a customer entering the shop.
        /// </summary>
        public void RegisterCustomerEntered(NPC.CustomerAI customer)
        {
            if (!customersInShop.Contains(customer))
            {
                customersInShop.Add(customer);
            }
        }

        /// <summary>
        /// Register a customer leaving the shop.
        /// </summary>
        public void RegisterCustomerLeft(NPC.CustomerAI customer)
        {
            customersInShop.Remove(customer);
        }

        /// <summary>
        /// Get number of customers in shop.
        /// </summary>
        public int GetCustomerCount()
        {
            // Clean up null references
            customersInShop.RemoveAll(c => c == null);
            return customersInShop.Count;
        }

        private void NotifyCustomersToLeave()
        {
            foreach (var customer in customersInShop)
            {
                if (customer != null)
                {
                    customer.LeaveShop();
                }
            }
        }

        private void ForceRemoveAllCustomers()
        {
            foreach (var customer in customersInShop)
            {
                if (customer != null)
                {
                    // Teleport them out or destroy
                    customer.ForceLeave();
                }
            }
            customersInShop.Clear();
        }

        #endregion

        #region Day/Night Events

        private void OnDayStart()
        {
            if (autoOpenAtDay)
            {
                OpenShop();
            }
        }

        private void OnNightStart()
        {
            if (autoCloseAtNight)
            {
                CloseShop();
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

        private void PlayOpenAnnouncement()
        {
            PlaySound(openAnnouncement);
        }

        private void PlayCloseAnnouncement()
        {
            PlaySound(closeAnnouncement);
        }

        #endregion

        #region Queries

        /// <summary>
        /// Is shop currently open?
        /// </summary>
        public bool IsShopOpen => isOpen;

        /// <summary>
        /// How many customers are inside?
        /// </summary>
        public int CustomersInside => GetCustomerCount();

        #endregion
    }
}
