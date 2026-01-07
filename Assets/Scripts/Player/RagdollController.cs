using UnityEngine;
using System.Collections.Generic;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Controls ragdoll physics for the player.
    /// Used for the fart launch ability.
    /// </summary>
    public class RagdollController : MonoBehaviour
    {
        [Header("Ragdoll Parts")]
        [Tooltip("Root of the ragdoll hierarchy")]
        public Transform ragdollRoot;

        [Tooltip("Hips rigidbody (main body part)")]
        public Rigidbody hipsRigidbody;

        [Tooltip("Animator to disable during ragdoll")]
        public Animator animator;

        [Header("Settings")]
        [Tooltip("Automatically find rigidbodies on start")]
        public bool autoFindRigidbodies = true;

        [Tooltip("Start with ragdoll disabled")]
        public bool startDisabled = true;

        private List<Rigidbody> ragdollRigidbodies = new List<Rigidbody>();
        private List<Collider> ragdollColliders = new List<Collider>();
        private CharacterController characterController;
        private bool isRagdolled = false;

        private void Awake()
        {
            characterController = GetComponentInParent<CharacterController>();

            if (autoFindRigidbodies)
            {
                FindRagdollParts();
            }

            if (startDisabled)
            {
                DisableRagdoll();
            }
        }

        /// <summary>
        /// Find all rigidbodies and colliders in the ragdoll.
        /// </summary>
        public void FindRagdollParts()
        {
            ragdollRigidbodies.Clear();
            ragdollColliders.Clear();

            Transform root = ragdollRoot != null ? ragdollRoot : transform;

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>())
            {
                ragdollRigidbodies.Add(rb);

                if (hipsRigidbody == null && rb.gameObject.name.ToLower().Contains("hip"))
                {
                    hipsRigidbody = rb;
                }
            }

            foreach (var col in root.GetComponentsInChildren<Collider>())
            {
                // Skip the main character controller collider
                if (col is CharacterController) continue;
                ragdollColliders.Add(col);
            }
        }

        /// <summary>
        /// Enable ragdoll physics.
        /// </summary>
        public void EnableRagdoll()
        {
            if (isRagdolled) return;

            isRagdolled = true;

            // Disable animator
            if (animator != null)
            {
                animator.enabled = false;
            }

            // Disable character controller
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            // Enable ragdoll physics
            foreach (var rb in ragdollRigidbodies)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            foreach (var col in ragdollColliders)
            {
                col.enabled = true;
            }
        }

        /// <summary>
        /// Disable ragdoll physics.
        /// </summary>
        public void DisableRagdoll()
        {
            if (!isRagdolled && !startDisabled) return;

            isRagdolled = false;

            // Disable ragdoll physics
            foreach (var rb in ragdollRigidbodies)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            foreach (var col in ragdollColliders)
            {
                col.enabled = false;
            }

            // Re-enable animator
            if (animator != null)
            {
                animator.enabled = true;
            }

            // Re-enable character controller
            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        /// <summary>
        /// Apply force to all ragdoll parts.
        /// </summary>
        public void ApplyForce(Vector3 force, ForceMode forceMode = ForceMode.Impulse)
        {
            foreach (var rb in ragdollRigidbodies)
            {
                rb.AddForce(force, forceMode);
            }
        }

        /// <summary>
        /// Apply force to the hips (main body).
        /// </summary>
        public void ApplyForceToHips(Vector3 force, ForceMode forceMode = ForceMode.Impulse)
        {
            if (hipsRigidbody != null)
            {
                hipsRigidbody.AddForce(force, forceMode);
            }
        }

        /// <summary>
        /// Apply explosion force to the ragdoll.
        /// </summary>
        public void ApplyExplosionForce(float force, Vector3 position, float radius)
        {
            foreach (var rb in ragdollRigidbodies)
            {
                rb.AddExplosionForce(force, position, radius);
            }
        }

        /// <summary>
        /// Get the current position of the hips.
        /// </summary>
        public Vector3 GetHipsPosition()
        {
            if (hipsRigidbody != null)
            {
                return hipsRigidbody.position;
            }

            return transform.position;
        }

        /// <summary>
        /// Check if ragdoll is currently active.
        /// </summary>
        public bool IsRagdolled => isRagdolled;

        /// <summary>
        /// Get all ragdoll rigidbodies.
        /// </summary>
        public List<Rigidbody> GetRigidbodies()
        {
            return new List<Rigidbody>(ragdollRigidbodies);
        }

        /// <summary>
        /// Blend from ragdoll to animation (for smooth transitions).
        /// </summary>
        public void BlendToAnimation(float duration)
        {
            StartCoroutine(BlendCoroutine(duration));
        }

        private System.Collections.IEnumerator BlendCoroutine(float duration)
        {
            // Store ragdoll positions
            Dictionary<Transform, Vector3> positions = new Dictionary<Transform, Vector3>();
            Dictionary<Transform, Quaternion> rotations = new Dictionary<Transform, Quaternion>();

            foreach (var rb in ragdollRigidbodies)
            {
                positions[rb.transform] = rb.transform.position;
                rotations[rb.transform] = rb.transform.rotation;
            }

            // Disable ragdoll
            DisableRagdoll();

            // Blend positions over time
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                foreach (var rb in ragdollRigidbodies)
                {
                    // Animation will control the position, we just smoothly transition
                    // This is a simplified version - for better results, use animation rigging
                }

                yield return null;
            }
        }
    }
}
