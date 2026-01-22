using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BobsPetroleum.Player;

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// Shows interaction prompts when player looks at interactable objects.
    /// Attach to player. Automatically works with any IInteractable in the game.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [Tooltip("Interaction range")]
        public float interactionRange = 3f;

        [Tooltip("Layer mask for interactables")]
        public LayerMask interactionLayer = -1;

        [Tooltip("Raycast from this camera (auto-detects if not set)")]
        public Camera playerCamera;

        [Header("Prompt UI")]
        [Tooltip("Text component for prompt message")]
        public TMP_Text promptText;

        [Tooltip("GameObject to show/hide (parent of the text)")]
        public GameObject promptObject;

        [Tooltip("Default prompt if IInteractable returns empty")]
        public string defaultPrompt = "Press E to Interact";

        [Header("Crosshair (Optional)")]
        [Tooltip("Normal crosshair image")]
        public Image crosshairNormal;

        [Tooltip("Crosshair when looking at interactable")]
        public Image crosshairInteract;

        [Tooltip("Or just change crosshair color")]
        public bool changeCrosshairColor = false;

        [Tooltip("Crosshair color when looking at interactable")]
        public Color interactCrosshairColor = Color.green;

        [Header("Animation")]
        [Tooltip("Fade in/out duration (0 for instant)")]
        public float fadeDuration = 0.15f;

        [Tooltip("Scale pop effect")]
        public bool scalePopEffect = true;

        [Tooltip("Pop scale multiplier")]
        public float popScale = 1.1f;

        // State
        private IInteractable currentInteractable;
        private CanvasGroup canvasGroup;
        private float currentAlpha = 0f;
        private float targetAlpha = 0f;
        private Vector3 originalScale;
        private Color originalCrosshairColor;
        private bool isShowing = false;

        private void Awake()
        {
            // Get canvas group for fading
            if (promptObject != null)
            {
                canvasGroup = promptObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null && fadeDuration > 0)
                {
                    canvasGroup = promptObject.AddComponent<CanvasGroup>();
                }

                originalScale = promptObject.transform.localScale;
            }

            // Store original crosshair color
            if (crosshairNormal != null)
            {
                originalCrosshairColor = crosshairNormal.color;
            }
        }

        private void Start()
        {
            if (playerCamera == null)
            {
                // Try to get from PlayerController
                var controller = GetComponent<PlayerController>();
                if (controller != null && controller.playerCamera != null)
                {
                    playerCamera = controller.playerCamera;
                }
                else
                {
                    playerCamera = Camera.main;
                }
            }

            HidePrompt(true);
        }

        private void Update()
        {
            CheckForInteractable();
            UpdateFade();
        }

        private void CheckForInteractable()
        {
            if (playerCamera == null || !playerCamera.enabled)
            {
                if (isShowing) HidePrompt(false);
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
            {
                // Check for IInteractable on hit object or parent
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponentInParent<IInteractable>();
                }

                if (interactable != null)
                {
                    currentInteractable = interactable;
                    string prompt = interactable.GetInteractionPrompt();

                    // Use default if empty
                    if (string.IsNullOrEmpty(prompt))
                    {
                        prompt = defaultPrompt;
                    }

                    ShowPrompt(prompt);
                    UpdateCrosshair(true);
                    return;
                }
            }

            // Nothing found
            if (currentInteractable != null || isShowing)
            {
                currentInteractable = null;
                HidePrompt(false);
                UpdateCrosshair(false);
            }
        }

        private void ShowPrompt(string text)
        {
            // Update text
            if (promptText != null)
            {
                promptText.text = text;
            }

            // Show object
            if (promptObject != null && !promptObject.activeSelf)
            {
                promptObject.SetActive(true);

                // Pop effect
                if (scalePopEffect)
                {
                    promptObject.transform.localScale = originalScale * popScale;
                }
            }

            isShowing = true;
            targetAlpha = 1f;
        }

        private void HidePrompt(bool instant)
        {
            isShowing = false;
            targetAlpha = 0f;

            if (instant)
            {
                currentAlpha = 0f;
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                if (promptObject != null) promptObject.SetActive(false);
            }
        }

        private void UpdateFade()
        {
            if (canvasGroup == null) return;

            // Lerp alpha
            if (fadeDuration > 0)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / fadeDuration);
            }
            else
            {
                currentAlpha = targetAlpha;
            }

            canvasGroup.alpha = currentAlpha;

            // Scale animation
            if (scalePopEffect && promptObject != null)
            {
                float scaleT = currentAlpha;
                promptObject.transform.localScale = Vector3.Lerp(
                    originalScale * popScale,
                    originalScale,
                    scaleT
                );
            }

            // Hide when fully faded
            if (currentAlpha <= 0 && promptObject != null && promptObject.activeSelf)
            {
                promptObject.SetActive(false);
            }
        }

        private void UpdateCrosshair(bool interacting)
        {
            if (crosshairNormal != null && crosshairInteract != null)
            {
                // Swap crosshairs
                crosshairNormal.enabled = !interacting;
                crosshairInteract.enabled = interacting;
            }
            else if (changeCrosshairColor && crosshairNormal != null)
            {
                // Just change color
                crosshairNormal.color = interacting ? interactCrosshairColor : originalCrosshairColor;
            }
        }

        /// <summary>
        /// Get the current interactable being looked at.
        /// </summary>
        public IInteractable GetCurrentInteractable()
        {
            return currentInteractable;
        }

        /// <summary>
        /// Check if currently looking at an interactable.
        /// </summary>
        public bool IsLookingAtInteractable()
        {
            return currentInteractable != null;
        }

        /// <summary>
        /// Manually set the camera reference.
        /// </summary>
        public void SetCamera(Camera cam)
        {
            playerCamera = cam;
        }
    }
}
