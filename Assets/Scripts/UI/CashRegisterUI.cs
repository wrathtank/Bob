using UnityEngine;
using TMPro;
using UnityEngine.Events;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Cash register display for showing transaction totals.
    /// Attach to a cash register object with a TMP display.
    /// </summary>
    public class CashRegisterUI : MonoBehaviour
    {
        [Header("Display")]
        [Tooltip("TextMeshPro component for the register display")]
        public TMP_Text displayText;

        [Tooltip("Format for displaying price (use {0} for value)")]
        public string priceFormat = "${0:F2}";

        [Tooltip("Message when idle")]
        public string idleMessage = "READY";

        [Tooltip("Message after successful transaction")]
        public string successMessage = "THANK YOU";

        [Tooltip("Duration to show success message")]
        public float successMessageDuration = 2f;

        [Header("Audio")]
        [Tooltip("Sound when item is scanned")]
        public AudioClip scanSound;

        [Tooltip("Sound when transaction completes")]
        public AudioClip transactionSound;

        [Header("Events")]
        public UnityEvent<float> onItemScanned;
        public UnityEvent<float> onTransactionComplete;

        private AudioSource audioSource;
        private float currentTotal = 0f;
        private bool inTransaction = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            ResetDisplay();
        }

        /// <summary>
        /// Start a new transaction.
        /// </summary>
        public void StartTransaction()
        {
            currentTotal = 0f;
            inTransaction = true;
            UpdateDisplay();
        }

        /// <summary>
        /// Add an item to the current transaction.
        /// </summary>
        public void ScanItem(float price)
        {
            if (!inTransaction)
            {
                StartTransaction();
            }

            currentTotal += price;
            UpdateDisplay();

            if (scanSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(scanSound);
            }

            onItemScanned?.Invoke(price);
        }

        /// <summary>
        /// Complete the current transaction.
        /// </summary>
        public float CompleteTransaction()
        {
            if (!inTransaction) return 0f;

            float total = currentTotal;
            inTransaction = false;

            if (transactionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(transactionSound);
            }

            onTransactionComplete?.Invoke(total);

            // Show success message
            if (displayText != null)
            {
                displayText.text = successMessage;
            }

            Invoke(nameof(ResetDisplay), successMessageDuration);

            return total;
        }

        /// <summary>
        /// Cancel the current transaction.
        /// </summary>
        public void CancelTransaction()
        {
            currentTotal = 0f;
            inTransaction = false;
            ResetDisplay();
        }

        /// <summary>
        /// Get the current transaction total.
        /// </summary>
        public float GetCurrentTotal()
        {
            return currentTotal;
        }

        private void UpdateDisplay()
        {
            if (displayText != null)
            {
                displayText.text = string.Format(priceFormat, currentTotal);
            }
        }

        private void ResetDisplay()
        {
            currentTotal = 0f;
            if (displayText != null)
            {
                displayText.text = idleMessage;
            }
        }

        /// <summary>
        /// Set display to show a custom message.
        /// </summary>
        public void SetDisplayMessage(string message)
        {
            if (displayText != null)
            {
                displayText.text = message;
            }
        }

        public bool IsInTransaction => inTransaction;
    }
}
