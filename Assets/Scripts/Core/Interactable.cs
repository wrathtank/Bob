using UnityEngine;
using UnityEngine.Events;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// BASE INTERACTABLE - Inherit from this for all interactable objects!
    /// Handles interaction detection, prompts, and callbacks.
    ///
    /// SETUP:
    /// 1. Add this component (or inherit from it)
    /// 2. Set interaction prompt
    /// 3. Add collider (trigger)
    /// 4. Done! Player can interact with E key.
    ///
    /// FEATURES:
    /// - Auto-shows interaction prompt in HUD
    /// - Cooldown between interactions
    /// - Can require items/money
    /// - Can be disabled/enabled
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [Header("=== INTERACTION SETTINGS ===")]
        [Tooltip("Text shown when player can interact")]
        public string interactionPrompt = "[E] Interact";

        [Tooltip("Alternative prompt (when state changes)")]
        public string alternatePrompt = "";

        [Tooltip("Use alternate prompt?")]
        public bool useAlternatePrompt = false;

        [Tooltip("Interaction key")]
        public KeyCode interactionKey = KeyCode.E;

        [Tooltip("Max interaction distance")]
        public float interactionDistance = 3f;

        [Tooltip("Cooldown between interactions")]
        public float cooldown = 0.5f;

        [Tooltip("Can interact multiple times?")]
        public bool canRepeat = true;

        [Header("=== REQUIREMENTS ===")]
        [Tooltip("Required item ID (empty = none)")]
        public string requiredItemId = "";

        [Tooltip("Required money amount (0 = none)")]
        public int requiredMoney = 0;

        [Tooltip("Consume required item on use?")]
        public bool consumeItem = true;

        [Header("=== STATE ===")]
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private bool playerInRange = false;
        [SerializeField] private bool hasInteracted = false;

        public bool IsEnabled => isEnabled;
        public bool PlayerInRange => playerInRange;
        public bool HasInteracted => hasInteracted;

        [Header("=== AUDIO ===")]
        [Tooltip("Sound on successful interaction")]
        public AudioClip interactSound;

        [Tooltip("Sound when can't interact (missing requirement)")]
        public AudioClip failSound;

        [Header("=== EVENTS ===")]
        [Tooltip("Called when player interacts")]
        public UnityEvent onInteract;

        [Tooltip("Called when player enters range")]
        public UnityEvent onPlayerEnter;

        [Tooltip("Called when player leaves range")]
        public UnityEvent onPlayerExit;

        [Tooltip("Called when interaction fails")]
        public UnityEvent onInteractFailed;

        // Internal
        protected AudioSource audioSource;
        protected Player.PlayerController currentPlayer;
        private float lastInteractTime;

        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }

            // Ensure we have a trigger collider
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = interactionDistance;
            }
        }

        protected virtual void Update()
        {
            if (!isEnabled || !playerInRange || currentPlayer == null) return;

            // Check for interaction input
            if (Input.GetKeyDown(interactionKey))
            {
                TryInteract();
            }
        }

        #region Trigger Detection

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!isEnabled) return;

            var player = other.GetComponent<Player.PlayerController>();
            if (player != null)
            {
                playerInRange = true;
                currentPlayer = player;
                ShowPrompt();
                onPlayerEnter?.Invoke();
            }
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<Player.PlayerController>();
            if (player != null && player == currentPlayer)
            {
                playerInRange = false;
                currentPlayer = null;
                HidePrompt();
                onPlayerExit?.Invoke();
            }
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Try to interact with this object.
        /// </summary>
        public virtual void TryInteract()
        {
            if (!CanInteract())
            {
                OnInteractFailed();
                return;
            }

            // Check cooldown
            if (Time.time - lastInteractTime < cooldown)
            {
                return;
            }

            // Check requirements
            if (!CheckRequirements())
            {
                OnInteractFailed();
                return;
            }

            // Consume requirements
            ConsumeRequirements();

            // Perform interaction
            lastInteractTime = Time.time;
            hasInteracted = true;

            PlaySound(interactSound);
            OnInteract();
            onInteract?.Invoke();

            // Disable if can't repeat
            if (!canRepeat)
            {
                SetEnabled(false);
            }
        }

        /// <summary>
        /// Override this for custom interaction behavior.
        /// </summary>
        protected virtual void OnInteract()
        {
            Debug.Log($"[Interactable] {gameObject.name} interacted!");
        }

        /// <summary>
        /// Called when interaction fails.
        /// </summary>
        protected virtual void OnInteractFailed()
        {
            PlaySound(failSound);
            onInteractFailed?.Invoke();

            // Show why it failed
            if (!string.IsNullOrEmpty(requiredItemId))
            {
                UI.HUDManager.Instance?.ShowNotification($"Requires: {requiredItemId}");
            }
            else if (requiredMoney > 0)
            {
                UI.HUDManager.Instance?.ShowNotification($"Requires: ${requiredMoney}");
            }
        }

        /// <summary>
        /// Check if interaction is possible.
        /// </summary>
        public virtual bool CanInteract()
        {
            return isEnabled && playerInRange && currentPlayer != null;
        }

        #endregion

        #region Requirements

        protected virtual bool CheckRequirements()
        {
            if (currentPlayer == null) return false;

            var inventory = currentPlayer.GetComponent<Player.PlayerInventory>();
            if (inventory == null) return true; // No inventory = no requirements

            // Check item
            if (!string.IsNullOrEmpty(requiredItemId))
            {
                if (!inventory.HasItem(requiredItemId))
                {
                    return false;
                }
            }

            // Check money
            if (requiredMoney > 0)
            {
                if (inventory.Money < requiredMoney)
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void ConsumeRequirements()
        {
            if (currentPlayer == null) return;

            var inventory = currentPlayer.GetComponent<Player.PlayerInventory>();
            if (inventory == null) return;

            // Consume item
            if (consumeItem && !string.IsNullOrEmpty(requiredItemId))
            {
                inventory.RemoveItem(requiredItemId);
            }

            // Spend money
            if (requiredMoney > 0)
            {
                inventory.SpendMoney(requiredMoney);
            }
        }

        #endregion

        #region Prompt

        protected virtual void ShowPrompt()
        {
            string prompt = useAlternatePrompt && !string.IsNullOrEmpty(alternatePrompt)
                ? alternatePrompt
                : interactionPrompt;

            UI.HUDManager.Instance?.ShowInteractionPrompt(prompt);
        }

        protected virtual void HidePrompt()
        {
            UI.HUDManager.Instance?.HideInteractionPrompt();
        }

        /// <summary>
        /// Get current prompt text.
        /// </summary>
        public string GetPrompt()
        {
            return useAlternatePrompt && !string.IsNullOrEmpty(alternatePrompt)
                ? alternatePrompt
                : interactionPrompt;
        }

        /// <summary>
        /// Set the prompt text.
        /// </summary>
        public void SetPrompt(string prompt)
        {
            interactionPrompt = prompt;
            if (playerInRange) ShowPrompt();
        }

        #endregion

        #region State Control

        /// <summary>
        /// Enable/disable this interactable.
        /// </summary>
        public virtual void SetEnabled(bool enabled)
        {
            isEnabled = enabled;

            if (!enabled && playerInRange)
            {
                HidePrompt();
            }
            else if (enabled && playerInRange)
            {
                ShowPrompt();
            }
        }

        /// <summary>
        /// Reset interaction state.
        /// </summary>
        public virtual void ResetInteraction()
        {
            hasInteracted = false;
            lastInteractTime = 0f;
        }

        /// <summary>
        /// Force player out of range.
        /// </summary>
        public void ForcePlayerExit()
        {
            if (playerInRange)
            {
                playerInRange = false;
                currentPlayer = null;
                HidePrompt();
            }
        }

        #endregion

        #region Audio

        protected void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Gizmos

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = isEnabled ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }

        #endregion
    }

    /// <summary>
    /// Simple pickup interactable - picks up and adds to inventory.
    /// </summary>
    public class PickupInteractable : Interactable
    {
        [Header("=== PICKUP SETTINGS ===")]
        [Tooltip("Item ID to give")]
        public string itemId = "item";

        [Tooltip("Quantity to give")]
        public int quantity = 1;

        [Tooltip("Money to give (0 = item only)")]
        public int moneyAmount = 0;

        [Tooltip("Destroy after pickup?")]
        public bool destroyOnPickup = true;

        [Tooltip("Item icon for notification")]
        public Sprite itemIcon;

        protected override void OnInteract()
        {
            if (currentPlayer == null) return;

            var inventory = currentPlayer.GetComponent<Player.PlayerInventory>();
            if (inventory == null) return;

            // Give item
            if (!string.IsNullOrEmpty(itemId))
            {
                inventory.AddItem(itemId, quantity);
                UI.HUDManager.Instance?.ShowItemPickup(itemId, itemIcon, quantity);
            }

            // Give money
            if (moneyAmount > 0)
            {
                inventory.AddMoney(moneyAmount);
            }

            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Door interactable - opens/closes doors.
    /// </summary>
    public class DoorInteractable : Interactable
    {
        [Header("=== DOOR SETTINGS ===")]
        [Tooltip("Is door currently open?")]
        public bool isOpen = false;

        [Tooltip("Locked state")]
        public bool isLocked = false;

        [Tooltip("Key item ID to unlock")]
        public string keyItemId = "";

        [Tooltip("Door animator")]
        public Animator doorAnimator;

        [Tooltip("Open animation trigger")]
        public string openTrigger = "Open";

        [Tooltip("Close animation trigger")]
        public string closeTrigger = "Close";

        [Header("=== DOOR AUDIO ===")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip lockedSound;

        protected override void Awake()
        {
            base.Awake();
            doorAnimator = doorAnimator ?? GetComponent<Animator>();
            UpdatePrompt();
        }

        protected override void OnInteract()
        {
            if (isLocked)
            {
                // Try to unlock
                if (!string.IsNullOrEmpty(keyItemId))
                {
                    var inventory = currentPlayer?.GetComponent<Player.PlayerInventory>();
                    if (inventory != null && inventory.HasItem(keyItemId))
                    {
                        inventory.RemoveItem(keyItemId);
                        isLocked = false;
                        UI.HUDManager.Instance?.ShowNotification("Door unlocked!");
                    }
                    else
                    {
                        PlaySound(lockedSound);
                        UI.HUDManager.Instance?.ShowNotification("Door is locked!");
                        return;
                    }
                }
                else
                {
                    PlaySound(lockedSound);
                    UI.HUDManager.Instance?.ShowNotification("Door is locked!");
                    return;
                }
            }

            // Toggle door
            isOpen = !isOpen;

            if (doorAnimator != null)
            {
                doorAnimator.SetTrigger(isOpen ? openTrigger : closeTrigger);
            }

            PlaySound(isOpen ? openSound : closeSound);
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            if (isLocked)
            {
                interactionPrompt = "[E] Locked";
            }
            else
            {
                interactionPrompt = isOpen ? "[E] Close" : "[E] Open";
            }

            if (playerInRange) ShowPrompt();
        }
    }
}
