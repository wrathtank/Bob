using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using BobsPetroleum.Core;
using BobsPetroleum.Animation;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// First-person player controller with movement, interaction, and special abilities.
    /// Includes the fart ability that launches player as ragdoll.
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Walking speed")]
        public float walkSpeed = 5f;

        [Tooltip("Running speed")]
        public float runSpeed = 8f;

        [Tooltip("Jump force")]
        public float jumpForce = 5f;

        [Tooltip("Gravity multiplier")]
        public float gravityMultiplier = 2f;

        [Header("Mouse Look")]
        [Tooltip("Mouse sensitivity")]
        public float mouseSensitivity = 2f;

        [Tooltip("Minimum vertical look angle")]
        public float minLookAngle = -90f;

        [Tooltip("Maximum vertical look angle")]
        public float maxLookAngle = 90f;

        [Header("Camera")]
        [Tooltip("First-person camera (child of player)")]
        public Camera playerCamera;

        [Header("Interaction")]
        [Tooltip("Interaction range")]
        public float interactionRange = 3f;

        [Tooltip("Interaction key")]
        public KeyCode interactKey = KeyCode.E;

        [Tooltip("Layer mask for interactable objects")]
        public LayerMask interactionLayer;

        [Header("Fart Ability")]
        [Tooltip("Enable fart launch ability")]
        public bool fartAbilityEnabled = true;

        [Tooltip("Key to activate fart")]
        public KeyCode fartKey = KeyCode.F;

        [Tooltip("Force applied when farting")]
        public float fartForce = 20f;

        [Tooltip("Upward force component")]
        public float fartUpwardForce = 5f;

        [Tooltip("Cooldown between farts")]
        public float fartCooldown = 5f;

        [Tooltip("Duration of ragdoll state")]
        public float ragdollDuration = 2f;

        [Tooltip("Ragdoll controller (optional)")]
        public RagdollController ragdollController;

        [Header("Cigar Effects")]
        [Tooltip("Current speed multiplier from cigars")]
        public float speedMultiplier = 1f;

        [Tooltip("Current jump multiplier from cigars")]
        public float jumpMultiplier = 1f;

        [Header("Animation Events")]
        [Tooltip("Animation event handler for this player")]
        public AnimationEventHandler animationHandler;

        [Header("Events")]
        public UnityEvent onJump;
        public UnityEvent onLand;
        public UnityEvent onFart;
        public UnityEvent<IInteractable> onInteract;

        // Components
        private CharacterController characterController;
        private Vector3 velocity;
        private float verticalRotation = 0f;
        private bool isGrounded;
        private bool wasGrounded;
        private float lastFartTime = -999f;
        private bool isRagdolling = false;
        private bool isInVehicle = false;

        // Network sync
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                // Enable camera only for local player
                if (playerCamera != null)
                {
                    playerCamera.enabled = true;
                    playerCamera.GetComponent<AudioListener>().enabled = true;
                }

                // Lock cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Register with PhoneUI
                UI.PhoneUI.Instance?.SetLocalPlayer(this);
            }
            else
            {
                // Disable camera for remote players
                if (playerCamera != null)
                {
                    playerCamera.enabled = false;
                    var listener = playerCamera.GetComponent<AudioListener>();
                    if (listener != null) listener.enabled = false;
                }
            }
        }

        private void Update()
        {
            if (!IsOwner || isRagdolling || isInVehicle) return;

            HandleMouseLook();
            HandleMovement();
            HandleInteraction();
            HandleFartAbility();
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Horizontal rotation (body)
            transform.Rotate(Vector3.up * mouseX);

            // Vertical rotation (camera)
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            wasGrounded = isGrounded;
            isGrounded = characterController.isGrounded;

            // Landing detection
            if (isGrounded && !wasGrounded)
            {
                onLand?.Invoke();
                animationHandler?.TriggerAnimation("Land");
            }

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Calculate movement direction
            Vector3 move = transform.right * horizontal + transform.forward * vertical;

            // Determine speed
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
            currentSpeed *= speedMultiplier;

            // Apply movement
            characterController.Move(move * currentSpeed * Time.deltaTime);

            // Animation triggers
            if (move.magnitude > 0.1f)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    animationHandler?.SetAnimation("Run", true);
                else
                    animationHandler?.SetAnimation("Walk", true);
            }
            else
            {
                animationHandler?.SetAnimation("Idle", true);
            }

            // Jumping
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * jumpMultiplier * -2f * Physics.gravity.y);
                onJump?.Invoke();
                animationHandler?.TriggerAnimation("Jump");
            }

            // Apply gravity
            velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleInteraction()
        {
            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionRange, interactionLayer))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(this);
                    onInteract?.Invoke(interactable);
                }
            }
        }

        private void HandleFartAbility()
        {
            if (!fartAbilityEnabled) return;

            if (Input.GetKeyDown(fartKey) && Time.time >= lastFartTime + fartCooldown)
            {
                ActivateFart();
            }
        }

        private void ActivateFart()
        {
            lastFartTime = Time.time;
            onFart?.Invoke();
            animationHandler?.TriggerAnimation("Fart");

            // Calculate launch direction (forward + up)
            Vector3 launchDirection = (transform.forward + Vector3.up * (fartUpwardForce / fartForce)).normalized;

            if (ragdollController != null)
            {
                // Enable ragdoll and apply force
                isRagdolling = true;
                ragdollController.EnableRagdoll();
                ragdollController.ApplyForce(launchDirection * fartForce, ForceMode.Impulse);

                // Schedule recovery
                Invoke(nameof(RecoverFromRagdoll), ragdollDuration);
            }
            else
            {
                // Simple launch without ragdoll
                velocity = launchDirection * fartForce;
            }
        }

        private void RecoverFromRagdoll()
        {
            if (ragdollController != null)
            {
                ragdollController.DisableRagdoll();

                // Reposition character controller to ragdoll position
                Vector3 ragdollPos = ragdollController.GetHipsPosition();
                characterController.enabled = false;
                transform.position = ragdollPos;
                characterController.enabled = true;
            }

            isRagdolling = false;
            animationHandler?.TriggerAnimation("GetUp");
        }

        /// <summary>
        /// Apply cigar effects to the player.
        /// </summary>
        public void ApplyCigarEffect(float speedMult, float jumpMult, float duration)
        {
            speedMultiplier = speedMult;
            jumpMultiplier = jumpMult;
            Invoke(nameof(ResetCigarEffects), duration);
        }

        private void ResetCigarEffects()
        {
            speedMultiplier = 1f;
            jumpMultiplier = 1f;
        }

        /// <summary>
        /// Enter a vehicle.
        /// </summary>
        public void EnterVehicle()
        {
            isInVehicle = true;
            characterController.enabled = false;
        }

        /// <summary>
        /// Exit a vehicle.
        /// </summary>
        public void ExitVehicle(Vector3 exitPosition)
        {
            isInVehicle = false;
            transform.position = exitPosition;
            characterController.enabled = true;
        }

        /// <summary>
        /// Teleport player to a position.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }

        /// <summary>
        /// Get looking direction.
        /// </summary>
        public Vector3 GetLookDirection()
        {
            return playerCamera != null ? playerCamera.transform.forward : transform.forward;
        }

        /// <summary>
        /// Check if player is grounded.
        /// </summary>
        public bool IsGrounded => isGrounded;

        /// <summary>
        /// Check if player is in vehicle.
        /// </summary>
        public bool IsInVehicle => isInVehicle;

        /// <summary>
        /// Check if player is ragdolling.
        /// </summary>
        public bool IsRagdolling => isRagdolling;
    }

    /// <summary>
    /// Interface for interactable objects.
    /// </summary>
    public interface IInteractable
    {
        void Interact(PlayerController player);
        string GetInteractionPrompt();
    }
}
