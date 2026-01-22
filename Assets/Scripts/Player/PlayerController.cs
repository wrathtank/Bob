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

        [Tooltip("Crouch speed")]
        public float crouchSpeed = 2.5f;

        [Tooltip("Jump force")]
        public float jumpForce = 5f;

        [Tooltip("Gravity multiplier")]
        public float gravityMultiplier = 2f;

        [Header("Crouch Settings")]
        [Tooltip("Enable crouching")]
        public bool crouchEnabled = true;

        [Tooltip("Crouch key")]
        public KeyCode crouchKey = KeyCode.LeftControl;

        [Tooltip("Toggle crouch mode (vs hold)")]
        public bool crouchToggle = false;

        [Tooltip("Standing height")]
        public float standingHeight = 2f;

        [Tooltip("Crouching height")]
        public float crouchHeight = 1f;

        [Tooltip("Crouch transition speed")]
        public float crouchTransitionSpeed = 8f;

        [Header("Stamina Settings")]
        [Tooltip("Enable stamina system")]
        public bool staminaEnabled = true;

        [Tooltip("Maximum stamina")]
        public float maxStamina = 100f;

        [Tooltip("Stamina drain per second while sprinting")]
        public float staminaDrain = 20f;

        [Tooltip("Stamina recovery per second")]
        public float staminaRecovery = 15f;

        [Tooltip("Delay before stamina recovers")]
        public float staminaRecoveryDelay = 1f;

        [Header("Mouse Look")]
        [Tooltip("Mouse sensitivity")]
        public float mouseSensitivity = 2f;

        [Tooltip("Minimum vertical look angle")]
        public float minLookAngle = -90f;

        [Tooltip("Maximum vertical look angle")]
        public float maxLookAngle = 90f;

        [Header("Head Bob")]
        [Tooltip("Enable head bob")]
        public bool headBobEnabled = true;

        [Tooltip("Head bob frequency while walking")]
        public float walkBobFrequency = 8f;

        [Tooltip("Head bob frequency while running")]
        public float runBobFrequency = 12f;

        [Tooltip("Head bob amplitude")]
        public float headBobAmplitude = 0.05f;

        [Header("Camera")]
        [Tooltip("First-person camera (child of player)")]
        public Camera playerCamera;

        [Tooltip("Camera holder for head bob")]
        public Transform cameraHolder;

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

        [Header("Audio - Footsteps")]
        [Tooltip("Footstep sounds")]
        public AudioClip[] footstepSounds;

        [Tooltip("Run footstep sounds (optional, uses regular if empty)")]
        public AudioClip[] runFootstepSounds;

        [Tooltip("Crouch footstep sounds (optional)")]
        public AudioClip[] crouchFootstepSounds;

        [Tooltip("Jump sound")]
        public AudioClip jumpSound;

        [Tooltip("Land sound")]
        public AudioClip landSound;

        [Tooltip("Hard land sound (high velocity)")]
        public AudioClip hardLandSound;

        [Tooltip("Fart sound")]
        public AudioClip fartSound;

        [Tooltip("Footstep interval while walking")]
        public float walkFootstepInterval = 0.5f;

        [Tooltip("Footstep interval while running")]
        public float runFootstepInterval = 0.35f;

        [Tooltip("Footstep volume")]
        [Range(0f, 1f)]
        public float footstepVolume = 0.7f;

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
        public UnityEvent<float> onStaminaChanged;
        public UnityEvent onStaminaDepleted;

        // Components
        private CharacterController characterController;
        private AudioSource audioSource;
        private Vector3 velocity;
        private float verticalRotation = 0f;
        private bool isGrounded;
        private bool wasGrounded;
        private float lastFartTime = -999f;
        private bool isRagdolling = false;
        private bool isInVehicle = false;

        // Crouch state
        private bool isCrouching = false;
        private float currentHeight;
        private float targetHeight;

        // Stamina state
        private float currentStamina;
        private float lastSprintTime;
        private bool canSprint = true;

        // Footstep state
        private float footstepTimer = 0f;
        private float lastFootstepTime = 0f;

        // Head bob state
        private float headBobTimer = 0f;
        private Vector3 cameraDefaultLocalPos;

        // Fall damage tracking
        private float fallStartY = 0f;
        private bool isFalling = false;

        // Network sync
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
            }

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            // Initialize states
            currentHeight = standingHeight;
            targetHeight = standingHeight;
            currentStamina = maxStamina;

            // Store default camera position for head bob
            if (cameraHolder != null)
            {
                cameraDefaultLocalPos = cameraHolder.localPosition;
            }
            else if (playerCamera != null)
            {
                cameraDefaultLocalPos = playerCamera.transform.localPosition;
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
            HandleCrouch();
            HandleStamina();
            HandleMovement();
            HandleFootsteps();
            HandleHeadBob();
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

        private void HandleCrouch()
        {
            if (!crouchEnabled) return;

            // Handle crouch input
            if (crouchToggle)
            {
                if (Input.GetKeyDown(crouchKey))
                {
                    isCrouching = !isCrouching;
                }
            }
            else
            {
                isCrouching = Input.GetKey(crouchKey);
            }

            // Set target height
            targetHeight = isCrouching ? crouchHeight : standingHeight;

            // Check if can stand up
            if (!isCrouching && currentHeight < standingHeight)
            {
                // Raycast to check for ceiling
                if (Physics.Raycast(transform.position, Vector3.up, standingHeight - crouchHeight + 0.1f))
                {
                    isCrouching = true;
                    targetHeight = crouchHeight;
                }
            }

            // Smoothly transition height
            if (Mathf.Abs(currentHeight - targetHeight) > 0.01f)
            {
                currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
                characterController.height = currentHeight;
                characterController.center = new Vector3(0, currentHeight / 2f, 0);

                // Adjust camera position
                if (playerCamera != null)
                {
                    Vector3 camPos = playerCamera.transform.localPosition;
                    camPos.y = currentHeight - 0.2f; // Eye height
                    playerCamera.transform.localPosition = camPos;
                }
            }
        }

        private void HandleStamina()
        {
            if (!staminaEnabled) return;

            bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isGrounded && !isCrouching;
            bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

            if (isSprinting && isMoving && canSprint)
            {
                // Drain stamina
                currentStamina -= staminaDrain * Time.deltaTime;
                lastSprintTime = Time.time;

                if (currentStamina <= 0)
                {
                    currentStamina = 0;
                    canSprint = false;
                    onStaminaDepleted?.Invoke();
                }

                onStaminaChanged?.Invoke(currentStamina / maxStamina);
            }
            else
            {
                // Recover stamina after delay
                if (Time.time > lastSprintTime + staminaRecoveryDelay)
                {
                    if (currentStamina < maxStamina)
                    {
                        currentStamina += staminaRecovery * Time.deltaTime;
                        currentStamina = Mathf.Min(currentStamina, maxStamina);

                        if (currentStamina >= maxStamina * 0.2f)
                        {
                            canSprint = true;
                        }

                        onStaminaChanged?.Invoke(currentStamina / maxStamina);
                    }
                }
            }
        }

        private void HandleMovement()
        {
            wasGrounded = isGrounded;
            isGrounded = characterController.isGrounded;

            // Track falling
            if (!isGrounded && velocity.y < 0 && !isFalling)
            {
                isFalling = true;
                fallStartY = transform.position.y;
            }

            // Landing detection
            if (isGrounded && !wasGrounded)
            {
                float fallDistance = fallStartY - transform.position.y;
                isFalling = false;

                // Play appropriate landing sound
                if (fallDistance > 3f && hardLandSound != null)
                {
                    audioSource.PlayOneShot(hardLandSound, footstepVolume);
                }
                else if (landSound != null)
                {
                    audioSource.PlayOneShot(landSound, footstepVolume);
                }

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

            // Determine speed based on crouch and sprint state
            float currentSpeed;
            bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && canSprint;

            if (isCrouching)
            {
                currentSpeed = crouchSpeed;
            }
            else if (wantsSprint && (!staminaEnabled || currentStamina > 0))
            {
                currentSpeed = runSpeed;
            }
            else
            {
                currentSpeed = walkSpeed;
            }

            currentSpeed *= speedMultiplier;

            // Apply movement
            characterController.Move(move * currentSpeed * Time.deltaTime);

            // Animation triggers
            if (move.magnitude > 0.1f)
            {
                if (isCrouching)
                    animationHandler?.SetAnimation("CrouchWalk", true);
                else if (wantsSprint && (!staminaEnabled || currentStamina > 0))
                    animationHandler?.SetAnimation("Run", true);
                else
                    animationHandler?.SetAnimation("Walk", true);
            }
            else
            {
                if (isCrouching)
                    animationHandler?.SetAnimation("CrouchIdle", true);
                else
                    animationHandler?.SetAnimation("Idle", true);
            }

            // Jumping (can't jump while crouching)
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                velocity.y = Mathf.Sqrt(jumpForce * jumpMultiplier * -2f * Physics.gravity.y);

                // Play jump sound
                if (jumpSound != null)
                {
                    audioSource.PlayOneShot(jumpSound, footstepVolume);
                }

                onJump?.Invoke();
                animationHandler?.TriggerAnimation("Jump");
            }

            // Apply gravity
            velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleFootsteps()
        {
            if (!isGrounded) return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

            if (!isMoving) return;

            // Determine footstep interval based on movement type
            float interval;
            AudioClip[] clips;

            if (isCrouching)
            {
                interval = walkFootstepInterval * 1.5f; // Slower crouch steps
                clips = crouchFootstepSounds != null && crouchFootstepSounds.Length > 0 ? crouchFootstepSounds : footstepSounds;
            }
            else if (Input.GetKey(KeyCode.LeftShift) && canSprint)
            {
                interval = runFootstepInterval;
                clips = runFootstepSounds != null && runFootstepSounds.Length > 0 ? runFootstepSounds : footstepSounds;
            }
            else
            {
                interval = walkFootstepInterval;
                clips = footstepSounds;
            }

            footstepTimer += Time.deltaTime;

            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                PlayFootstep(clips);
            }
        }

        private void PlayFootstep(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null)
            {
                float volume = isCrouching ? footstepVolume * 0.5f : footstepVolume;
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private void HandleHeadBob()
        {
            if (!headBobEnabled || !isGrounded) return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;

            Transform camTransform = cameraHolder != null ? cameraHolder : playerCamera?.transform;
            if (camTransform == null) return;

            if (isMoving && !isCrouching)
            {
                bool isRunning = Input.GetKey(KeyCode.LeftShift) && canSprint;
                float frequency = isRunning ? runBobFrequency : walkBobFrequency;
                float amplitude = isRunning ? headBobAmplitude * 1.5f : headBobAmplitude;

                headBobTimer += Time.deltaTime * frequency;

                float bobY = Mathf.Sin(headBobTimer) * amplitude;
                float bobX = Mathf.Cos(headBobTimer * 0.5f) * amplitude * 0.5f;

                camTransform.localPosition = cameraDefaultLocalPos + new Vector3(bobX, bobY, 0);
            }
            else
            {
                // Reset to default position smoothly
                headBobTimer = 0f;
                camTransform.localPosition = Vector3.Lerp(camTransform.localPosition, cameraDefaultLocalPos, Time.deltaTime * 5f);
            }
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

            // Play fart sound
            if (fartSound != null)
            {
                audioSource.PlayOneShot(fartSound);
            }

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

        /// <summary>
        /// Check if player is crouching.
        /// </summary>
        public bool IsCrouching => isCrouching;

        /// <summary>
        /// Get current stamina (0 to maxStamina).
        /// </summary>
        public float CurrentStamina => currentStamina;

        /// <summary>
        /// Get stamina percentage (0-1).
        /// </summary>
        public float StaminaPercentage => currentStamina / maxStamina;

        /// <summary>
        /// Check if player can sprint.
        /// </summary>
        public bool CanSprint => canSprint;
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
