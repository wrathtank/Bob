using UnityEngine;
using TMPro;
using BobsPetroleum.Player;

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// Shows interaction prompts when player looks at interactable objects.
    /// Attach to player or main camera.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Interaction range")]
        public float interactionRange = 3f;

        [Tooltip("Layer mask for interactables")]
        public LayerMask interactionLayer;

        [Header("UI")]
        [Tooltip("Text component for prompt")]
        public TMP_Text promptText;

        [Tooltip("GameObject to show/hide")]
        public GameObject promptObject;

        [Header("References")]
        [Tooltip("Camera to raycast from")]
        public Camera playerCamera;

        private IInteractable currentInteractable;

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            HidePrompt();
        }

        private void Update()
        {
            CheckForInteractable();
        }

        private void CheckForInteractable()
        {
            if (playerCamera == null)
            {
                HidePrompt();
                return;
            }

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null)
                {
                    currentInteractable = interactable;
                    ShowPrompt(interactable.GetInteractionPrompt());
                    return;
                }
            }

            currentInteractable = null;
            HidePrompt();
        }

        private void ShowPrompt(string text)
        {
            if (promptObject != null)
            {
                promptObject.SetActive(true);
            }

            if (promptText != null)
            {
                promptText.text = text;
            }
        }

        private void HidePrompt()
        {
            if (promptObject != null)
            {
                promptObject.SetActive(false);
            }

            if (promptText != null)
            {
                promptText.text = "";
            }
        }

        /// <summary>
        /// Get current interactable (for manual interaction handling).
        /// </summary>
        public IInteractable GetCurrentInteractable()
        {
            return currentInteractable;
        }
    }
}
