using UnityEngine;
using UnityEngine.Events;

namespace BobsPetroleum.Economy
{
    /// <summary>
    /// HAMBURGER VENDOR - Buy hamburgers to feed Bob!
    /// The main way to get hamburgers in the game.
    ///
    /// SETUP:
    /// 1. Create vendor model (food truck, stand, machine)
    /// 2. Add HamburgerVendor component
    /// 3. Set price and stock
    /// 4. Done! Player can buy with E key.
    /// </summary>
    public class HamburgerVendor : Core.Interactable
    {
        [Header("=== HAMBURGER SETTINGS ===")]
        [Tooltip("Price per hamburger")]
        public int hamburgerPrice = 25;

        [Tooltip("Current stock (-1 = infinite)")]
        public int currentStock = -1;

        [Tooltip("Max stock (-1 = infinite)")]
        public int maxStock = -1;

        [Tooltip("Restock time (0 = manual)")]
        public float restockTime = 0f;

        [Tooltip("Restock amount")]
        public int restockAmount = 5;

        [Header("=== VISUALS ===")]
        [Tooltip("Hamburger display model")]
        public GameObject hamburgerDisplay;

        [Tooltip("Spawn point for purchased hamburger")]
        public Transform hamburgerSpawnPoint;

        [Tooltip("Hamburger prefab (for throwing/dropping)")]
        public GameObject hamburgerPrefab;

        [Header("=== VENDOR NPC ===")]
        [Tooltip("Vendor NPC (optional)")]
        public GameObject vendorNPC;

        [Tooltip("Vendor greeting dialogue")]
        public string[] vendorGreetings = {
            "Welcome! Best burgers in town!",
            "Hey there! Hungry?",
            "Bob's favorite - hamburgers!"
        };

        [Header("=== AUDIO ===")]
        public AudioClip purchaseSound;
        public AudioClip outOfStockSound;
        public AudioClip greetingSound;

        [Header("=== EVENTS ===")]
        public UnityEvent onPurchase;
        public UnityEvent onOutOfStock;
        public UnityEvent onRestock;

        private float restockTimer;
        private bool hasGreeted = false;

        protected override void Awake()
        {
            base.Awake();
            UpdatePrompt();
        }

        private void Update()
        {
            base.Update();

            // Restock timer
            if (restockTime > 0 && currentStock >= 0 && currentStock < maxStock)
            {
                restockTimer += Time.deltaTime;
                if (restockTimer >= restockTime)
                {
                    restockTimer = 0;
                    Restock(restockAmount);
                }
            }
        }

        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);

            // Greet player
            if (!hasGreeted && other.GetComponent<Player.PlayerController>() != null)
            {
                Greet();
                hasGreeted = true;
            }
        }

        protected override void OnTriggerExit(Collider other)
        {
            base.OnTriggerExit(other);

            if (other.GetComponent<Player.PlayerController>() != null)
            {
                hasGreeted = false;
            }
        }

        protected override void OnInteract()
        {
            TryPurchase();
        }

        /// <summary>
        /// Try to purchase a hamburger.
        /// </summary>
        public void TryPurchase()
        {
            if (currentPlayer == null) return;

            var inventory = currentPlayer.GetComponent<Player.PlayerInventory>();
            if (inventory == null) return;

            // Check stock
            if (currentStock == 0)
            {
                PlaySound(outOfStockSound);
                UI.HUDManager.Instance?.ShowNotification("Out of stock!");
                onOutOfStock?.Invoke();
                return;
            }

            // Check money
            if (inventory.Money < hamburgerPrice)
            {
                PlaySound(failSound);
                UI.HUDManager.Instance?.ShowNotification($"Need ${hamburgerPrice}!");
                return;
            }

            // Purchase!
            inventory.SpendMoney(hamburgerPrice);
            inventory.AddHamburger();

            if (currentStock > 0)
            {
                currentStock--;
            }

            // Visuals
            PlaySound(purchaseSound);
            SpawnHamburgerVisual();
            UI.HUDManager.Instance?.ShowNotification("Hamburger purchased!");
            UI.HUDManager.Instance?.FlashHamburgerPickup();

            onPurchase?.Invoke();
            UpdatePrompt();

            Debug.Log($"[HamburgerVendor] Sold hamburger for ${hamburgerPrice}. Stock: {currentStock}");
        }

        /// <summary>
        /// Spawn visual hamburger (for pickup animation).
        /// </summary>
        private void SpawnHamburgerVisual()
        {
            if (hamburgerPrefab == null || hamburgerSpawnPoint == null) return;

            var burger = Instantiate(hamburgerPrefab, hamburgerSpawnPoint.position, hamburgerSpawnPoint.rotation);

            // Destroy after short time (it's already in inventory)
            Destroy(burger, 1f);
        }

        /// <summary>
        /// Restock hamburgers.
        /// </summary>
        public void Restock(int amount)
        {
            if (maxStock < 0)
            {
                // Infinite stock
                return;
            }

            currentStock = Mathf.Min(currentStock + amount, maxStock);
            UpdatePrompt();
            onRestock?.Invoke();

            Debug.Log($"[HamburgerVendor] Restocked. Current: {currentStock}/{maxStock}");
        }

        /// <summary>
        /// Fully restock.
        /// </summary>
        public void FullRestock()
        {
            if (maxStock > 0)
            {
                currentStock = maxStock;
                UpdatePrompt();
                onRestock?.Invoke();
            }
        }

        private void Greet()
        {
            if (vendorGreetings.Length == 0) return;

            string greeting = vendorGreetings[Random.Range(0, vendorGreetings.Length)];
            UI.HUDManager.Instance?.ShowQuickMessage(greeting, 2f);
            PlaySound(greetingSound);
        }

        private void UpdatePrompt()
        {
            if (currentStock == 0)
            {
                interactionPrompt = "[E] Out of Stock";
            }
            else
            {
                string stockInfo = currentStock > 0 ? $" ({currentStock} left)" : "";
                interactionPrompt = $"[E] Buy Hamburger - ${hamburgerPrice}{stockInfo}";
            }

            if (PlayerInRange)
            {
                ShowPrompt();
            }
        }

        /// <summary>
        /// Set hamburger price.
        /// </summary>
        public void SetPrice(int newPrice)
        {
            hamburgerPrice = newPrice;
            UpdatePrompt();
        }

        /// <summary>
        /// Check if in stock.
        /// </summary>
        public bool IsInStock()
        {
            return currentStock != 0; // -1 = infinite, >0 = has stock
        }
    }
}
