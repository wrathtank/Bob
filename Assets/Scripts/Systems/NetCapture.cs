using UnityEngine;
using UnityEngine.Events;
using BobsPetroleum.Player;
using BobsPetroleum.NPC;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Net item for capturing animals. Use from inventory to attempt capture.
    /// </summary>
    public class NetCapture : MonoBehaviour
    {
        [Header("Net Settings")]
        [Tooltip("Capture range")]
        public float captureRange = 5f;

        [Tooltip("Capture angle (degrees)")]
        public float captureAngle = 45f;

        [Tooltip("Net throw speed")]
        public float throwSpeed = 20f;

        [Tooltip("Net prefab to throw")]
        public GameObject netProjectilePrefab;

        [Header("Layers")]
        public LayerMask animalLayer;

        [Header("Audio")]
        public AudioClip throwSound;
        public AudioClip captureSuccessSound;
        public AudioClip captureFailSound;

        [Header("Events")]
        public UnityEvent onNetThrown;
        public UnityEvent<WanderingAnimalAI> onCaptureSuccess;
        public UnityEvent onCaptureFail;

        private AudioSource audioSource;
        private PlayerController owner;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Set the net owner.
        /// </summary>
        public void SetOwner(PlayerController player)
        {
            owner = player;
        }

        /// <summary>
        /// Use the net to attempt capture.
        /// </summary>
        public void UseNet(PlayerController player, Vector3 direction)
        {
            // Play throw sound
            if (throwSound != null)
            {
                audioSource.PlayOneShot(throwSound);
            }

            onNetThrown?.Invoke();

            // Spawn net projectile
            if (netProjectilePrefab != null)
            {
                Vector3 spawnPos = player.transform.position + Vector3.up * 1.5f + direction * 0.5f;
                var netObj = Instantiate(netProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

                var netProjectile = netObj.GetComponent<NetProjectile>();
                if (netProjectile != null)
                {
                    netProjectile.Initialize(player, direction * throwSpeed, this);
                }
                else
                {
                    // Simple raycast capture
                    RaycastCapture(player, direction);
                }
            }
            else
            {
                // Instant raycast capture
                RaycastCapture(player, direction);
            }
        }

        private void RaycastCapture(PlayerController player, Vector3 direction)
        {
            RaycastHit hit;
            if (Physics.Raycast(player.transform.position + Vector3.up * 1.5f, direction, out hit, captureRange, animalLayer))
            {
                var animal = hit.collider.GetComponent<WanderingAnimalAI>();
                if (animal != null)
                {
                    AttemptCapture(animal, player);
                }
            }
        }

        /// <summary>
        /// Attempt to capture an animal.
        /// </summary>
        public void AttemptCapture(WanderingAnimalAI animal, PlayerController player)
        {
            bool success = animal.TryCapture(player);

            if (success)
            {
                if (captureSuccessSound != null)
                {
                    audioSource.PlayOneShot(captureSuccessSound);
                }
                onCaptureSuccess?.Invoke(animal);
            }
            else
            {
                if (captureFailSound != null)
                {
                    audioSource.PlayOneShot(captureFailSound);
                }
                onCaptureFail?.Invoke();
            }
        }
    }

    /// <summary>
    /// Net projectile that flies through the air and captures animals on contact.
    /// </summary>
    public class NetProjectile : MonoBehaviour
    {
        [Header("Settings")]
        public float lifetime = 3f;
        public float gravity = 9.8f;

        private PlayerController thrower;
        private Vector3 velocity;
        private NetCapture netSystem;
        private bool hasHit = false;

        public void Initialize(PlayerController player, Vector3 initialVelocity, NetCapture system)
        {
            thrower = player;
            velocity = initialVelocity;
            netSystem = system;

            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (hasHit) return;

            // Apply gravity
            velocity.y -= gravity * Time.deltaTime;

            // Move
            transform.position += velocity * Time.deltaTime;

            // Rotate to face direction
            if (velocity != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(velocity);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            var animal = other.GetComponent<WanderingAnimalAI>();
            if (animal != null)
            {
                hasHit = true;
                netSystem?.AttemptCapture(animal, thrower);
                Destroy(gameObject);
            }
            else if (!other.isTrigger)
            {
                // Hit ground or obstacle
                hasHit = true;
                Destroy(gameObject, 1f);
            }
        }
    }
}
