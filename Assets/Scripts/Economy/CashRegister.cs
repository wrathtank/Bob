using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

namespace BobsPetroleum.Economy
{
    /// <summary>
    /// Cash register system for gas station gameplay.
    /// Customers bring items, player scans, takes payment, gives change.
    /// Schedule 1 style checkout experience.
    /// </summary>
    public class CashRegister : MonoBehaviour, IInteractable
    {
        public static CashRegister Instance { get; private set; }

        [Header("Register State")]
        [Tooltip("Is player currently at register")]
        public bool isPlayerAtRegister = false;

        [Tooltip("Is register open for business")]
        public bool isOpen = true;

        [Header("Current Transaction")]
        [Tooltip("Items in current transaction")]
        public List<SaleItem> currentItems = new List<SaleItem>();

        [Tooltip("Current transaction total")]
        public float currentTotal = 0f;

        [Tooltip("Amount customer is paying")]
        public float customerPayment = 0f;

        [Tooltip("Change to give back")]
        public float changeToGive = 0f;

        [Header("Cash Drawer")]
        [Tooltip("Money in the register")]
        public float registerCash = 100f;

        [Tooltip("Starting cash amount")]
        public float startingCash = 100f;

        [Header("Customer Queue")]
        [Tooltip("Current customer at register")]
        public AI.CustomerAI currentCustomer;

        [Tooltip("Queue of waiting customers")]
        public List<AI.CustomerAI> customerQueue = new List<AI.CustomerAI>();

        [Header("Interaction")]
        [Tooltip("Key to interact with register")]
        public KeyCode interactKey = KeyCode.E;

        [Tooltip("Key to scan item")]
        public KeyCode scanKey = KeyCode.Mouse0;

        [Tooltip("Key to complete transaction")]
        public KeyCode completeKey = KeyCode.Return;

        [Tooltip("Key to open drawer")]
        public KeyCode drawerKey = KeyCode.Tab;

        [Header("Register Position")]
        [Tooltip("Where player stands")]
        public Transform playerPosition;

        [Tooltip("Where items are placed for scanning")]
        public Transform scanArea;

        [Tooltip("Where scanned items go")]
        public Transform bagArea;

        [Header("UI References")]
        [Tooltip("Register UI panel")]
        public GameObject registerUI;

        [Tooltip("Item list display")]
        public Transform itemListContainer;

        [Tooltip("Item entry prefab")]
        public GameObject itemEntryPrefab;

        [Tooltip("Total display")]
        public TMP_Text totalText;

        [Tooltip("Payment display")]
        public TMP_Text paymentText;

        [Tooltip("Change display")]
        public TMP_Text changeText;

        [Tooltip("Customer name display")]
        public TMP_Text customerNameText;

        [Tooltip("Register cash display")]
        public TMP_Text registerCashText;

        [Tooltip("Transaction status text")]
        public TMP_Text statusText;

        [Header("Visual")]
        [Tooltip("Cash drawer object")]
        public GameObject cashDrawer;

        [Tooltip("Drawer open position")]
        public Vector3 drawerOpenPos;

        [Tooltip("Drawer closed position")]
        public Vector3 drawerClosedPos;

        [Tooltip("Scanner light")]
        public Light scannerLight;

        [Tooltip("Receipt printer")]
        public Transform receiptPrinter;

        [Header("Audio")]
        public AudioClip scanSound;
        public AudioClip cashRegisterSound;
        public AudioClip drawerOpenSound;
        public AudioClip drawerCloseSound;
        public AudioClip coinSound;
        public AudioClip errorSound;
        public AudioClip receiptPrintSound;

        [Header("Events")]
        public UnityEvent onTransactionStart;
        public UnityEvent onItemScanned;
        public UnityEvent<float> onTransactionComplete;
        public UnityEvent onTransactionCancelled;
        public UnityEvent<AI.CustomerAI> onCustomerServed;

        // State
        private bool isDrawerOpen = false;
        private bool isInTransaction = false;
        private AudioSource audioSource;
        private Player.PlayerController playerController;
        private TransactionState transactionState = TransactionState.Idle;

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
            registerCash = startingCash;
            UpdateUI();

            // Hide register UI
            if (registerUI != null)
            {
                registerUI.SetActive(false);
            }

            // Close drawer
            if (cashDrawer != null)
            {
                cashDrawer.transform.localPosition = drawerClosedPos;
            }
        }

        private void Update()
        {
            if (!isPlayerAtRegister) return;

            HandleInput();
        }

        #region Player Interaction

        public void Interact(Player.PlayerController player)
        {
            if (isPlayerAtRegister)
            {
                LeaveRegister();
            }
            else
            {
                EnterRegister(player);
            }
        }

        public string GetInteractionPrompt()
        {
            if (isPlayerAtRegister)
            {
                return "Press E to leave register";
            }
            return "Press E to use register";
        }

        /// <summary>
        /// Player enters register mode.
        /// </summary>
        public void EnterRegister(Player.PlayerController player)
        {
            isPlayerAtRegister = true;
            playerController = player;

            // Disable player movement
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // Position player
            if (playerPosition != null && player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = playerPosition.position;
                player.transform.rotation = playerPosition.rotation;
                if (cc != null) cc.enabled = true;
            }

            // Show UI
            if (registerUI != null)
            {
                registerUI.SetActive(true);
            }

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Check for waiting customer
            if (currentCustomer == null && customerQueue.Count > 0)
            {
                ServeNextCustomer();
            }

            UpdateUI();
            SetStatus("Ready for customers");

            Debug.Log("[CashRegister] Player entered register");
        }

        /// <summary>
        /// Player leaves register.
        /// </summary>
        public void LeaveRegister()
        {
            // Can't leave during transaction
            if (isInTransaction && currentItems.Count > 0)
            {
                PlaySound(errorSound);
                SetStatus("Complete transaction first!");
                return;
            }

            isPlayerAtRegister = false;

            // Re-enable player
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Hide UI
            if (registerUI != null)
            {
                registerUI.SetActive(false);
            }

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Close drawer if open
            if (isDrawerOpen)
            {
                ToggleDrawer();
            }

            Debug.Log("[CashRegister] Player left register");
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Leave register
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LeaveRegister();
                return;
            }

            // Toggle drawer
            if (Input.GetKeyDown(drawerKey))
            {
                ToggleDrawer();
            }

            // State-based input
            switch (transactionState)
            {
                case TransactionState.Idle:
                    // Waiting for customer
                    break;

                case TransactionState.Scanning:
                    // Scan items
                    if (Input.GetKeyDown(scanKey))
                    {
                        ScanNextItem();
                    }
                    // Done scanning
                    if (Input.GetKeyDown(completeKey))
                    {
                        if (currentItems.Count > 0)
                        {
                            RequestPayment();
                        }
                    }
                    break;

                case TransactionState.Payment:
                    // Accept payment
                    if (Input.GetKeyDown(completeKey))
                    {
                        AcceptPayment();
                    }
                    break;

                case TransactionState.Change:
                    // Give change
                    if (Input.GetKeyDown(completeKey))
                    {
                        GiveChange();
                    }
                    break;
            }
        }

        #endregion

        #region Customer Management

        /// <summary>
        /// Customer joins the queue.
        /// </summary>
        public void CustomerJoinQueue(AI.CustomerAI customer)
        {
            if (customer == null) return;
            if (customerQueue.Contains(customer)) return;

            customerQueue.Add(customer);

            // If no current customer and player at register, serve immediately
            if (currentCustomer == null && isPlayerAtRegister)
            {
                ServeNextCustomer();
            }

            Debug.Log($"[CashRegister] Customer joined queue. Queue size: {customerQueue.Count}");
        }

        /// <summary>
        /// Customer leaves queue.
        /// </summary>
        public void CustomerLeaveQueue(AI.CustomerAI customer)
        {
            customerQueue.Remove(customer);

            if (currentCustomer == customer)
            {
                CancelTransaction();
                currentCustomer = null;
            }
        }

        /// <summary>
        /// Serve the next customer in queue.
        /// </summary>
        public void ServeNextCustomer()
        {
            if (customerQueue.Count == 0)
            {
                currentCustomer = null;
                transactionState = TransactionState.Idle;
                SetStatus("Waiting for customers...");
                UpdateUI();
                return;
            }

            currentCustomer = customerQueue[0];
            customerQueue.RemoveAt(0);

            StartTransaction();
        }

        #endregion

        #region Transaction Flow

        /// <summary>
        /// Start a new transaction with current customer.
        /// </summary>
        public void StartTransaction()
        {
            if (currentCustomer == null) return;

            isInTransaction = true;
            transactionState = TransactionState.Scanning;
            currentItems.Clear();
            currentTotal = 0f;
            customerPayment = 0f;
            changeToGive = 0f;

            // Get items from customer
            // Customer's items would be in their inventory
            // For now, generate random items
            GenerateCustomerItems();

            UpdateUI();
            SetStatus($"Scan items for {currentCustomer.customerName}");

            onTransactionStart?.Invoke();

            Debug.Log($"[CashRegister] Started transaction with {currentCustomer.customerName}");
        }

        private void GenerateCustomerItems()
        {
            // This would normally come from the customer's shopping cart
            // For demo, generate 1-5 random items
            int itemCount = Random.Range(1, 6);

            string[] itemNames = { "Soda", "Chips", "Candy", "Magazine", "Jerky", "Coffee", "Donut", "Gum" };
            float[] prices = { 2.50f, 3.00f, 1.50f, 5.00f, 4.50f, 2.00f, 1.00f, 0.75f };

            for (int i = 0; i < itemCount; i++)
            {
                int idx = Random.Range(0, itemNames.Length);
                SaleItem item = new SaleItem
                {
                    itemName = itemNames[idx],
                    price = prices[idx],
                    quantity = 1,
                    isScanned = false
                };
                currentItems.Add(item);
            }
        }

        /// <summary>
        /// Scan the next unscanned item.
        /// </summary>
        public void ScanNextItem()
        {
            // Find next unscanned item
            SaleItem itemToScan = null;
            foreach (var item in currentItems)
            {
                if (!item.isScanned)
                {
                    itemToScan = item;
                    break;
                }
            }

            if (itemToScan == null)
            {
                SetStatus("All items scanned! Press Enter to total");
                return;
            }

            // Scan it
            itemToScan.isScanned = true;
            currentTotal += itemToScan.price * itemToScan.quantity;

            // Effects
            PlaySound(scanSound);
            FlashScannerLight();

            UpdateUI();
            SetStatus($"Scanned: {itemToScan.itemName} - ${itemToScan.price:F2}");

            onItemScanned?.Invoke();

            // Check if all scanned
            bool allScanned = true;
            foreach (var item in currentItems)
            {
                if (!item.isScanned)
                {
                    allScanned = false;
                    break;
                }
            }

            if (allScanned)
            {
                SetStatus("All items scanned! Press Enter to request payment");
            }
        }

        /// <summary>
        /// Request payment from customer.
        /// </summary>
        public void RequestPayment()
        {
            transactionState = TransactionState.Payment;

            // Customer pays (usually rounds up or exact)
            float roundedTotal = Mathf.Ceil(currentTotal);
            customerPayment = Random.value > 0.3f ? roundedTotal : roundedTotal + Random.Range(0, 3) * 5f;

            UpdateUI();
            SetStatus($"Customer pays ${customerPayment:F2}. Press Enter to accept");

            PlaySound(cashRegisterSound);
        }

        /// <summary>
        /// Accept payment from customer.
        /// </summary>
        public void AcceptPayment()
        {
            if (customerPayment < currentTotal)
            {
                PlaySound(errorSound);
                SetStatus("Not enough payment!");
                return;
            }

            // Calculate change
            changeToGive = customerPayment - currentTotal;

            // Add to register
            registerCash += customerPayment;

            // Open drawer
            if (!isDrawerOpen)
            {
                ToggleDrawer();
            }

            transactionState = TransactionState.Change;

            UpdateUI();

            if (changeToGive > 0)
            {
                SetStatus($"Give ${changeToGive:F2} change. Press Enter when done");
            }
            else
            {
                SetStatus("Exact change! Press Enter to complete");
            }

            PlaySound(coinSound);
        }

        /// <summary>
        /// Give change and complete transaction.
        /// </summary>
        public void GiveChange()
        {
            // Subtract change from register
            registerCash -= changeToGive;

            // Print receipt
            PrintReceipt();

            // Complete
            CompleteTransaction();
        }

        /// <summary>
        /// Complete the transaction.
        /// </summary>
        public void CompleteTransaction()
        {
            float earnedAmount = currentTotal;

            // Add to player's money
            var playerInventory = FindObjectOfType<Player.PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.AddMoney((int)earnedAmount);
            }

            // Notify customer
            if (currentCustomer != null)
            {
                currentCustomer.OnTransactionComplete();
                onCustomerServed?.Invoke(currentCustomer);
            }

            // Close drawer
            if (isDrawerOpen)
            {
                ToggleDrawer();
            }

            // Clear transaction
            isInTransaction = false;
            currentItems.Clear();
            currentTotal = 0f;
            customerPayment = 0f;
            changeToGive = 0f;
            currentCustomer = null;

            onTransactionComplete?.Invoke(earnedAmount);

            // Show notification
            UI.HUDManager.Instance?.ShowNotification($"Earned ${earnedAmount:F2}!");

            UpdateUI();
            SetStatus("Transaction complete!");

            Debug.Log($"[CashRegister] Transaction complete. Earned ${earnedAmount:F2}");

            // Serve next customer after delay
            Invoke(nameof(ServeNextCustomer), 1f);
        }

        /// <summary>
        /// Cancel current transaction.
        /// </summary>
        public void CancelTransaction()
        {
            isInTransaction = false;
            transactionState = TransactionState.Idle;
            currentItems.Clear();
            currentTotal = 0f;
            customerPayment = 0f;
            changeToGive = 0f;

            UpdateUI();
            SetStatus("Transaction cancelled");

            onTransactionCancelled?.Invoke();

            Debug.Log("[CashRegister] Transaction cancelled");
        }

        #endregion

        #region Cash Drawer

        /// <summary>
        /// Toggle cash drawer open/closed.
        /// </summary>
        public void ToggleDrawer()
        {
            isDrawerOpen = !isDrawerOpen;

            if (cashDrawer != null)
            {
                cashDrawer.transform.localPosition = isDrawerOpen ? drawerOpenPos : drawerClosedPos;
            }

            PlaySound(isDrawerOpen ? drawerOpenSound : drawerCloseSound);
        }

        #endregion

        #region Visual Effects

        private void FlashScannerLight()
        {
            if (scannerLight != null)
            {
                StartCoroutine(FlashLight());
            }
        }

        private System.Collections.IEnumerator FlashLight()
        {
            scannerLight.enabled = true;
            yield return new WaitForSeconds(0.1f);
            scannerLight.enabled = false;
        }

        private void PrintReceipt()
        {
            PlaySound(receiptPrintSound);
            // Could instantiate a receipt object at receiptPrinter position
        }

        #endregion

        #region UI

        private void UpdateUI()
        {
            // Total
            if (totalText != null)
            {
                totalText.text = $"Total: ${currentTotal:F2}";
            }

            // Payment
            if (paymentText != null)
            {
                paymentText.text = $"Payment: ${customerPayment:F2}";
            }

            // Change
            if (changeText != null)
            {
                changeText.text = $"Change: ${changeToGive:F2}";
            }

            // Customer name
            if (customerNameText != null)
            {
                customerNameText.text = currentCustomer != null ?
                    $"Customer: {currentCustomer.customerName}" : "No customer";
            }

            // Register cash
            if (registerCashText != null)
            {
                registerCashText.text = $"Register: ${registerCash:F2}";
            }

            // Item list
            UpdateItemList();
        }

        private void UpdateItemList()
        {
            if (itemListContainer == null) return;

            // Clear existing
            foreach (Transform child in itemListContainer)
            {
                Destroy(child.gameObject);
            }

            // Add items
            foreach (var item in currentItems)
            {
                if (itemEntryPrefab != null)
                {
                    GameObject entry = Instantiate(itemEntryPrefab, itemListContainer);
                    var text = entry.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        string status = item.isScanned ? "✓" : "○";
                        text.text = $"{status} {item.itemName} x{item.quantity} - ${item.price:F2}";
                    }
                }
                else
                {
                    // Fallback: create simple text
                    GameObject entry = new GameObject("Item");
                    entry.transform.SetParent(itemListContainer);
                    var text = entry.AddComponent<TMP_Text>();
                    string status = item.isScanned ? "✓" : "○";
                    text.text = $"{status} {item.itemName} x{item.quantity} - ${item.price:F2}";
                    text.fontSize = 14;
                }
            }
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
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

        #region Daily Operations

        /// <summary>
        /// Open register for the day.
        /// </summary>
        public void OpenRegister()
        {
            isOpen = true;
            registerCash = startingCash;
            SetStatus("Register open for business");
        }

        /// <summary>
        /// Close register (end of day).
        /// </summary>
        public void CloseRegister()
        {
            isOpen = false;

            // Calculate daily earnings
            float dailyEarnings = registerCash - startingCash;

            // Add to player's bank/save
            Debug.Log($"[CashRegister] Daily earnings: ${dailyEarnings:F2}");

            SetStatus("Register closed");
        }

        /// <summary>
        /// Get daily earnings.
        /// </summary>
        public float GetDailyEarnings()
        {
            return registerCash - startingCash;
        }

        #endregion

        #region Properties

        public bool IsPlayerAtRegister => isPlayerAtRegister;
        public bool IsInTransaction => isInTransaction;
        public bool IsDrawerOpen => isDrawerOpen;
        public int QueueLength => customerQueue.Count;
        public float RegisterCash => registerCash;

        #endregion
    }

    [System.Serializable]
    public class SaleItem
    {
        public string itemName;
        public float price;
        public int quantity;
        public bool isScanned;
        public Sprite icon;
    }

    public enum TransactionState
    {
        Idle,
        Scanning,
        Payment,
        Change
    }
}
