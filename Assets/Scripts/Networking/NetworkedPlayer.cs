using UnityEngine;
using System.Collections.Generic;
#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace BobsPetroleum.Networking
{
    /// <summary>
    /// Add this to your player prefab for automatic network sync!
    /// Handles position, rotation, animations, and inventory sync.
    ///
    /// SETUP:
    /// 1. Add NetworkObject component to player prefab
    /// 2. Add this NetworkedPlayer component
    /// 3. Assign references in inspector
    /// 4. Done! Player state syncs automatically.
    /// </summary>
#if UNITY_NETCODE
    public class NetworkedPlayer : NetworkBehaviour
    {
        [Header("=== REQUIRED REFERENCES ===")]
        [Tooltip("CharacterController for movement")]
        public CharacterController characterController;

        [Tooltip("Camera for this player (disabled for remote players)")]
        public Camera playerCamera;

        [Tooltip("Audio listener (disabled for remote players)")]
        public AudioListener audioListener;

        [Header("=== SYNC SETTINGS ===")]
        [Tooltip("How often to sync position (per second)")]
        public float positionSyncRate = 20f;

        [Tooltip("Interpolation speed for smooth movement")]
        public float interpolationSpeed = 15f;

        [Tooltip("Sync rotation")]
        public bool syncRotation = true;

        [Tooltip("Sync animation parameters")]
        public bool syncAnimations = true;

        [Header("=== COMPONENTS TO SYNC ===")]
        [Tooltip("Animator to sync")]
        public Animator animator;

        [Tooltip("Simple animation player to sync")]
        public Animation.SimpleAnimationPlayer simpleAnimator;

        [Header("=== OWNERSHIP ===")]
        [Tooltip("Components to disable on remote players")]
        public MonoBehaviour[] ownerOnlyComponents;

        [Tooltip("GameObjects to disable on remote players")]
        public GameObject[] ownerOnlyObjects;

        [Header("=== SKIN/MODEL SYNC ===")]
        [Tooltip("SkinManager reference")]
        public Player.SkinManager skinManager;

        [Tooltip("Sync model/skin changes")]
        public bool syncModel = true;

        // Network variables
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<NetworkString64> networkAnimation = new NetworkVariable<NetworkString64>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<NetworkString64> networkModelTokenId = new NetworkVariable<NetworkString64>(
            new NetworkString64("000"), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private NetworkVariable<float> networkHealth = new NetworkVariable<float>(
            100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<int> networkMoney = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Runtime
        private float syncTimer;
        private string lastAnimation;
        private Vector3 lastSyncedPosition;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Auto-find skin manager
            if (skinManager == null)
            {
                skinManager = GetComponent<Player.SkinManager>();
            }

            if (IsOwner)
            {
                // This is our player - enable controls
                EnableOwnerComponents(true);

                // Initialize network position
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;

                // Sync our current model
                if (skinManager != null && syncModel)
                {
                    networkModelTokenId.Value = new NetworkString64(skinManager.currentModelTokenId);
                }

                Debug.Log("[NetworkedPlayer] Spawned as OWNER (local player)");
            }
            else
            {
                // This is someone else's player - disable controls
                EnableOwnerComponents(false);

                // Subscribe to model changes
                networkModelTokenId.OnValueChanged += OnModelChanged;

                // Apply their current model
                if (syncModel)
                {
                    ApplyNetworkModel(networkModelTokenId.Value.ToString());
                }

                // Subscribe to position changes for interpolation
                networkPosition.OnValueChanged += OnPositionChanged;
                networkRotation.OnValueChanged += OnRotationChanged;
                networkAnimation.OnValueChanged += OnAnimationChanged;

                Debug.Log("[NetworkedPlayer] Spawned as REMOTE player");
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (!IsOwner)
            {
                networkPosition.OnValueChanged -= OnPositionChanged;
                networkRotation.OnValueChanged -= OnRotationChanged;
                networkAnimation.OnValueChanged -= OnAnimationChanged;
                networkModelTokenId.OnValueChanged -= OnModelChanged;
            }
        }

        #region Model Sync

        /// <summary>
        /// Called when remote player changes their model
        /// </summary>
        private void OnModelChanged(NetworkString64 oldModel, NetworkString64 newModel)
        {
            string tokenId = newModel.ToString();
            ApplyNetworkModel(tokenId);
        }

        /// <summary>
        /// Apply model from network (for remote players)
        /// </summary>
        private void ApplyNetworkModel(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId)) return;

            if (skinManager != null)
            {
                skinManager.ApplyNetworkModel(tokenId);
            }
            else
            {
                // Fallback: try to load model directly
                string path = "PlayerModels/" + tokenId;
                GameObject modelPrefab = Resources.Load<GameObject>(path);
                if (modelPrefab != null)
                {
                    // Find or create model parent
                    Transform modelParent = transform.Find("ModelParent");
                    if (modelParent == null)
                    {
                        GameObject parentObj = new GameObject("ModelParent");
                        parentObj.transform.SetParent(transform);
                        parentObj.transform.localPosition = Vector3.zero;
                        modelParent = parentObj.transform;
                    }

                    // Destroy existing model
                    foreach (Transform child in modelParent)
                    {
                        Destroy(child.gameObject);
                    }

                    // Spawn new model
                    GameObject model = Instantiate(modelPrefab, modelParent);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.name = $"PlayerModel_{tokenId}";

                    // Update animator reference
                    animator = model.GetComponent<Animator>() ?? model.GetComponentInChildren<Animator>();
                    simpleAnimator = model.GetComponent<Animation.SimpleAnimationPlayer>() ??
                                    model.GetComponentInChildren<Animation.SimpleAnimationPlayer>();
                }
            }

            Debug.Log($"[NetworkedPlayer] Applied network model: {tokenId}");
        }

        /// <summary>
        /// Call this when local player changes their model
        /// </summary>
        public void SetModel(string tokenId)
        {
            if (!IsOwner) return;

            // Update network variable (syncs to all clients)
            networkModelTokenId.Value = new NetworkString64(tokenId);

            Debug.Log($"[NetworkedPlayer] Set model to: {tokenId}");
        }

        #endregion

        private void EnableOwnerComponents(bool enable)
        {
            // Camera and audio listener
            if (playerCamera != null)
            {
                playerCamera.enabled = enable;
            }

            if (audioListener != null)
            {
                audioListener.enabled = enable;
            }

            // Character controller only for owner
            if (characterController != null)
            {
                characterController.enabled = enable;
            }

            // Custom components
            if (ownerOnlyComponents != null)
            {
                foreach (var comp in ownerOnlyComponents)
                {
                    if (comp != null)
                    {
                        comp.enabled = enable;
                    }
                }
            }

            // Custom objects
            if (ownerOnlyObjects != null)
            {
                foreach (var obj in ownerOnlyObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(enable);
                    }
                }
            }
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsOwner)
            {
                // Sync our position to network
                SyncPosition();
                SyncAnimation();
            }
            else
            {
                // Interpolate to network position
                InterpolatePosition();
            }
        }

        #region Position Sync

        private void SyncPosition()
        {
            syncTimer += Time.deltaTime;
            float syncInterval = 1f / positionSyncRate;

            if (syncTimer >= syncInterval)
            {
                syncTimer = 0f;

                // Only sync if moved enough
                if (Vector3.Distance(transform.position, lastSyncedPosition) > 0.01f)
                {
                    networkPosition.Value = transform.position;
                    lastSyncedPosition = transform.position;
                }

                if (syncRotation)
                {
                    networkRotation.Value = transform.rotation;
                }
            }
        }

        private void InterpolatePosition()
        {
            // Smooth interpolation to network position
            transform.position = Vector3.Lerp(
                transform.position,
                networkPosition.Value,
                Time.deltaTime * interpolationSpeed
            );

            if (syncRotation)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    networkRotation.Value,
                    Time.deltaTime * interpolationSpeed
                );
            }
        }

        private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
        {
            // Position changed on network - will interpolate in Update
        }

        private void OnRotationChanged(Quaternion oldRot, Quaternion newRot)
        {
            // Rotation changed on network - will interpolate in Update
        }

        #endregion

        #region Animation Sync

        private void SyncAnimation()
        {
            if (!syncAnimations) return;

            // Get current animation name
            string currentAnim = GetCurrentAnimationName();

            if (currentAnim != lastAnimation)
            {
                lastAnimation = currentAnim;
                networkAnimation.Value = new NetworkString64(currentAnim);
            }
        }

        private string GetCurrentAnimationName()
        {
            if (simpleAnimator != null)
            {
                return simpleAnimator.GetCurrentAnimationName();
            }

            if (animator != null)
            {
                var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    return clipInfo[0].clip.name;
                }
            }

            return "";
        }

        private void OnAnimationChanged(NetworkString64 oldAnim, NetworkString64 newAnim)
        {
            string animName = newAnim.ToString();
            if (string.IsNullOrEmpty(animName)) return;

            // Play animation on remote player
            if (simpleAnimator != null)
            {
                simpleAnimator.Play(animName);
            }
            else if (animator != null)
            {
                animator.Play(animName);
            }
        }

        #endregion

        #region RPCs - Call these for network actions!

        /// <summary>
        /// Play animation on all clients
        /// </summary>
        public void PlayNetworkAnimation(string animName)
        {
            if (IsOwner)
            {
                networkAnimation.Value = new NetworkString64(animName);
                PlayAnimationLocal(animName);
            }
        }

        private void PlayAnimationLocal(string animName)
        {
            if (simpleAnimator != null)
            {
                simpleAnimator.Play(animName);
            }
            else if (animator != null)
            {
                animator.Play(animName);
            }
        }

        /// <summary>
        /// Take damage (server authoritative)
        /// </summary>
        [ServerRpc]
        public void TakeDamageServerRpc(float damage)
        {
            networkHealth.Value = Mathf.Max(0, networkHealth.Value - damage);

            if (networkHealth.Value <= 0)
            {
                OnDeathClientRpc();
            }
        }

        [ClientRpc]
        private void OnDeathClientRpc()
        {
            // Handle death on all clients
            PlayAnimationLocal("Death");
            Debug.Log($"[NetworkedPlayer] Player died!");
        }

        /// <summary>
        /// Heal player (server authoritative)
        /// </summary>
        [ServerRpc]
        public void HealServerRpc(float amount)
        {
            networkHealth.Value = Mathf.Min(100f, networkHealth.Value + amount);
        }

        /// <summary>
        /// Add money (server authoritative)
        /// </summary>
        [ServerRpc]
        public void AddMoneyServerRpc(int amount)
        {
            networkMoney.Value += amount;
        }

        /// <summary>
        /// Spawn effect visible to all
        /// </summary>
        [ServerRpc]
        public void SpawnEffectServerRpc(Vector3 position, int effectId)
        {
            SpawnEffectClientRpc(position, effectId);
        }

        [ClientRpc]
        private void SpawnEffectClientRpc(Vector3 position, int effectId)
        {
            // Spawn effect at position
            // effectId maps to your effect prefabs
            Debug.Log($"[NetworkedPlayer] Effect {effectId} at {position}");
        }

        #endregion

        #region Properties

        public float Health => networkHealth.Value;
        public int Money => networkMoney.Value;
        public bool IsLocalPlayer => IsOwner;

        #endregion
    }

    /// <summary>
    /// Helper struct for syncing short strings over network
    /// </summary>
    public struct NetworkString64 : INetworkSerializable
    {
        private FixedString64Bytes data;

        public NetworkString64(string value)
        {
            data = new FixedString64Bytes(value ?? "");
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref data);
        }

        public override string ToString() => data.ToString();
    }
#else
    // Fallback when Netcode not installed
    public class NetworkedPlayer : MonoBehaviour
    {
        [Header("=== NETCODE NOT INSTALLED ===")]
        [Tooltip("Install 'Netcode for GameObjects' from Package Manager")]
        public string installInstructions = "Window > Package Manager > Unity Registry > Search 'Netcode'";

        public bool IsOwner => true;
        public bool IsLocalPlayer => true;

        private void Start()
        {
            Debug.LogWarning("[NetworkedPlayer] Netcode for GameObjects not installed! Running in single-player mode.");
        }
    }
#endif
}
