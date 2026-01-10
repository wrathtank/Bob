using UnityEngine;
using TMPro;
using BobsPetroleum.Player;

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// Shows an interaction prompt when player enters the trigger zone.
    /// Attach to any interactable object with a trigger collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionTriggerPrompt : MonoBehaviour
    {
        [Header("Prompt Settings")]
        [Tooltip("The TMP text object to show/hide")]
        public TMP_Text promptText;

        [Tooltip("The GameObject containing the prompt (for show/hide)")]
        public GameObject promptObject;

        [Tooltip("Text to display (e.g., 'Press E to Interact')")]
        public string promptMessage = "Press E to Interact";

        [Tooltip("Use the IInteractable prompt if available")]
        public bool useInteractablePrompt = true;

        [Header("Billboard Settings")]
        [Tooltip("Make prompt face the camera")]
        public bool billboardToCamera = true;

        [Tooltip("Only rotate on Y axis (keep upright)")]
        public bool billboardYAxisOnly = true;

        [Header("Visibility Settings")]
        [Tooltip("Layer mask for what can trigger the prompt")]
        public LayerMask playerLayer = -1;

        [Tooltip("Fade in/out duration (0 for instant)")]
        public float fadeDuration = 0.2f;

        private IInteractable interactable;
        private CanvasGroup canvasGroup;
        private bool isPlayerInRange = false;
        private float currentAlpha = 0f;
        private float targetAlpha = 0f;

        private void Awake()
        {
            // Ensure collider is a trigger
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            // Try to get IInteractable
            interactable = GetComponent<IInteractable>();
            if (interactable == null)
            {
                interactable = GetComponentInParent<IInteractable>();
            }

            // Get or add canvas group for fading
            if (promptObject != null)
            {
                canvasGroup = promptObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null && fadeDuration > 0)
                {
                    canvasGroup = promptObject.AddComponent<CanvasGroup>();
                }
            }

            // Hide initially
            HidePrompt(true);
        }

        private void Update()
        {
            // Handle fading
            if (fadeDuration > 0 && canvasGroup != null)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / fadeDuration);
                canvasGroup.alpha = currentAlpha;

                if (currentAlpha == 0 && promptObject != null && promptObject.activeSelf)
                {
                    promptObject.SetActive(false);
                }
            }

            // Billboard effect
            if (isPlayerInRange && billboardToCamera && promptObject != null && promptObject.activeSelf)
            {
                BillboardPrompt();
            }

            // Update prompt text if using IInteractable
            if (isPlayerInRange && useInteractablePrompt && interactable != null && promptText != null)
            {
                string newPrompt = interactable.GetInteractionPrompt();
                if (!string.IsNullOrEmpty(newPrompt) && promptText.text != newPrompt)
                {
                    promptText.text = newPrompt;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's a player
            if (!IsPlayer(other)) return;

            isPlayerInRange = true;
            ShowPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            // Check if it's a player
            if (!IsPlayer(other)) return;

            isPlayerInRange = false;
            HidePrompt(false);
        }

        private bool IsPlayer(Collider other)
        {
            // Check layer
            if (playerLayer != -1 && ((1 << other.gameObject.layer) & playerLayer) == 0)
            {
                return false;
            }

            // Check for player component
            return other.GetComponent<PlayerController>() != null ||
                   other.GetComponentInParent<PlayerController>() != null;
        }

        private void ShowPrompt()
        {
            // Set text
            if (promptText != null)
            {
                if (useInteractablePrompt && interactable != null)
                {
                    string interactPrompt = interactable.GetInteractionPrompt();
                    promptText.text = !string.IsNullOrEmpty(interactPrompt) ? interactPrompt : promptMessage;
                }
                else
                {
                    promptText.text = promptMessage;
                }
            }

            // Show object
            if (promptObject != null)
            {
                promptObject.SetActive(true);
            }

            // Handle fading
            if (fadeDuration > 0 && canvasGroup != null)
            {
                targetAlpha = 1f;
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        private void HidePrompt(bool instant)
        {
            if (fadeDuration > 0 && canvasGroup != null && !instant)
            {
                targetAlpha = 0f;
            }
            else
            {
                if (promptObject != null)
                {
                    promptObject.SetActive(false);
                }
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    currentAlpha = 0f;
                }
            }
        }

        private void BillboardPrompt()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            if (billboardYAxisOnly)
            {
                Vector3 lookPos = cam.transform.position;
                lookPos.y = promptObject.transform.position.y;
                promptObject.transform.LookAt(lookPos);
                promptObject.transform.Rotate(0, 180, 0); // Face camera, not away
            }
            else
            {
                promptObject.transform.LookAt(cam.transform);
                promptObject.transform.Rotate(0, 180, 0);
            }
        }

        /// <summary>
        /// Set the prompt message at runtime.
        /// </summary>
        public void SetPromptMessage(string message)
        {
            promptMessage = message;
            if (isPlayerInRange && promptText != null)
            {
                promptText.text = message;
            }
        }

        /// <summary>
        /// Force show the prompt.
        /// </summary>
        public void ForceShow()
        {
            ShowPrompt();
        }

        /// <summary>
        /// Force hide the prompt.
        /// </summary>
        public void ForceHide()
        {
            HidePrompt(false);
        }

        /// <summary>
        /// Check if player is in range.
        /// </summary>
        public bool IsPlayerInRange => isPlayerInRange;

        private void OnDrawGizmosSelected()
        {
            // Draw trigger area
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            var collider = GetComponent<Collider>();
            if (collider is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
        }
    }
}
