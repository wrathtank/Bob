using UnityEngine;
#if UNITY_NETCODE
using Unity.Netcode;
#endif

namespace BobsPetroleum.Networking
{
    /// <summary>
    /// Easy networked object sync! Add to any object that needs to sync across players.
    /// Automatically syncs transform, state, and can handle interactions.
    ///
    /// SETUP:
    /// 1. Add NetworkObject component
    /// 2. Add this NetworkedObject component
    /// 3. Configure what to sync in inspector
    /// 4. Done!
    ///
    /// Use cases:
    /// - Pickups that need to disappear for everyone
    /// - Doors that open/close
    /// - Items that can be moved
    /// - Interactable objects
    /// </summary>
#if UNITY_NETCODE
    public class NetworkedObject : NetworkBehaviour
    {
        [Header("=== SYNC SETTINGS ===")]
        [Tooltip("Sync position")]
        public bool syncPosition = true;

        [Tooltip("Sync rotation")]
        public bool syncRotation = true;

        [Tooltip("Sync scale")]
        public bool syncScale = false;

        [Tooltip("Sync active state")]
        public bool syncActiveState = true;

        [Tooltip("Updates per second")]
        public float syncRate = 10f;

        [Tooltip("Position threshold for sync")]
        public float positionThreshold = 0.01f;

        [Header("=== INTERPOLATION ===")]
        [Tooltip("Smooth movement for remote clients")]
        public bool interpolate = true;

        [Tooltip("Interpolation speed")]
        public float interpolationSpeed = 10f;

        [Header("=== INTERACTION ===")]
        [Tooltip("Can players interact with this")]
        public bool isInteractable = false;

        [Tooltip("Only owner can interact")]
        public bool ownerOnlyInteraction = false;

        [Header("=== RIGIDBODY SYNC ===")]
        [Tooltip("Sync rigidbody physics")]
        public bool syncRigidbody = false;

        [Tooltip("Reference to rigidbody")]
        public Rigidbody rb;

        // Network variables
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<Vector3> networkScale = new NetworkVariable<Vector3>(
            Vector3.one, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> networkActive = new NetworkVariable<bool>(
            true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<int> networkState = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Runtime
        private float syncTimer;
        private Vector3 lastSyncedPosition;
        private Quaternion lastSyncedRotation;
        private Vector3 lastSyncedScale;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Initialize values
            if (IsServer)
            {
                networkPosition.Value = transform.position;
                networkRotation.Value = transform.rotation;
                networkScale.Value = transform.localScale;
                networkActive.Value = gameObject.activeSelf;

                lastSyncedPosition = transform.position;
                lastSyncedRotation = transform.rotation;
                lastSyncedScale = transform.localScale;
            }

            // Subscribe to changes
            networkPosition.OnValueChanged += OnPositionChanged;
            networkRotation.OnValueChanged += OnRotationChanged;
            networkScale.OnValueChanged += OnScaleChanged;
            networkActive.OnValueChanged += OnActiveChanged;
            networkState.OnValueChanged += OnStateChanged;

            Debug.Log($"[NetworkedObject] {gameObject.name} spawned. IsServer: {IsServer}");
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            networkPosition.OnValueChanged -= OnPositionChanged;
            networkRotation.OnValueChanged -= OnRotationChanged;
            networkScale.OnValueChanged -= OnScaleChanged;
            networkActive.OnValueChanged -= OnActiveChanged;
            networkState.OnValueChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsServer)
            {
                SyncFromServer();
            }
            else if (interpolate)
            {
                InterpolateOnClient();
            }
        }

        #region Server Sync

        private void SyncFromServer()
        {
            syncTimer += Time.deltaTime;
            float interval = 1f / syncRate;

            if (syncTimer < interval) return;
            syncTimer = 0f;

            // Position
            if (syncPosition && Vector3.Distance(transform.position, lastSyncedPosition) > positionThreshold)
            {
                networkPosition.Value = transform.position;
                lastSyncedPosition = transform.position;
            }

            // Rotation
            if (syncRotation && Quaternion.Angle(transform.rotation, lastSyncedRotation) > 0.5f)
            {
                networkRotation.Value = transform.rotation;
                lastSyncedRotation = transform.rotation;
            }

            // Scale
            if (syncScale && Vector3.Distance(transform.localScale, lastSyncedScale) > 0.01f)
            {
                networkScale.Value = transform.localScale;
                lastSyncedScale = transform.localScale;
            }

            // Active state
            if (syncActiveState && networkActive.Value != gameObject.activeSelf)
            {
                networkActive.Value = gameObject.activeSelf;
            }

            // Rigidbody velocity
            if (syncRigidbody && rb != null)
            {
                networkVelocity.Value = rb.velocity;
            }
        }

        #endregion

        #region Client Interpolation

        private void InterpolateOnClient()
        {
            if (syncPosition)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    networkPosition.Value,
                    Time.deltaTime * interpolationSpeed
                );
            }

            if (syncRotation)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    networkRotation.Value,
                    Time.deltaTime * interpolationSpeed
                );
            }

            if (syncScale)
            {
                transform.localScale = Vector3.Lerp(
                    transform.localScale,
                    networkScale.Value,
                    Time.deltaTime * interpolationSpeed
                );
            }
        }

        #endregion

        #region Value Change Callbacks

        private void OnPositionChanged(Vector3 oldVal, Vector3 newVal)
        {
            if (!interpolate && !IsServer)
            {
                transform.position = newVal;
            }
        }

        private void OnRotationChanged(Quaternion oldVal, Quaternion newVal)
        {
            if (!interpolate && !IsServer)
            {
                transform.rotation = newVal;
            }
        }

        private void OnScaleChanged(Vector3 oldVal, Vector3 newVal)
        {
            if (!interpolate && !IsServer)
            {
                transform.localScale = newVal;
            }
        }

        private void OnActiveChanged(bool oldVal, bool newVal)
        {
            if (!IsServer)
            {
                gameObject.SetActive(newVal);
            }
        }

        private void OnStateChanged(int oldVal, int newVal)
        {
            OnNetworkStateChanged(oldVal, newVal);
        }

        /// <summary>
        /// Override this to handle custom state changes
        /// </summary>
        protected virtual void OnNetworkStateChanged(int oldState, int newState)
        {
            // Override in derived classes
            Debug.Log($"[NetworkedObject] {gameObject.name} state changed: {oldState} -> {newState}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Request to pick up this object (clients call this)
        /// </summary>
        public void RequestPickup(ulong playerId)
        {
            if (!isInteractable) return;
            PickupServerRpc(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void PickupServerRpc(ulong playerId)
        {
            // Disable for everyone
            networkActive.Value = false;

            // Award to player
            OnPickedUp(playerId);
        }

        protected virtual void OnPickedUp(ulong playerId)
        {
            Debug.Log($"[NetworkedObject] {gameObject.name} picked up by player {playerId}");
        }

        /// <summary>
        /// Request to interact with this object
        /// </summary>
        public void RequestInteract(ulong playerId)
        {
            if (!isInteractable) return;
            InteractServerRpc(playerId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void InteractServerRpc(ulong playerId)
        {
            OnInteracted(playerId);
            InteractClientRpc(playerId);
        }

        [ClientRpc]
        private void InteractClientRpc(ulong playerId)
        {
            OnInteractedClient(playerId);
        }

        protected virtual void OnInteracted(ulong playerId)
        {
            Debug.Log($"[NetworkedObject] {gameObject.name} interacted by player {playerId}");
        }

        protected virtual void OnInteractedClient(ulong playerId)
        {
            // Override for client-side effects
        }

        /// <summary>
        /// Set custom state value (server only)
        /// </summary>
        public void SetState(int state)
        {
            if (IsServer)
            {
                networkState.Value = state;
            }
            else
            {
                SetStateServerRpc(state);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetStateServerRpc(int state)
        {
            networkState.Value = state;
        }

        /// <summary>
        /// Teleport object (server only)
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            if (IsServer)
            {
                transform.position = position;
                networkPosition.Value = position;
            }
            else
            {
                TeleportServerRpc(position);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void TeleportServerRpc(Vector3 position)
        {
            transform.position = position;
            networkPosition.Value = position;
        }

        /// <summary>
        /// Despawn this object
        /// </summary>
        public void NetworkDespawn()
        {
            if (IsServer)
            {
                NetworkObject.Despawn();
            }
            else
            {
                DespawnServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void DespawnServerRpc()
        {
            NetworkObject.Despawn();
        }

        #endregion

        #region Properties

        public int CurrentState => networkState.Value;
        public bool IsActive => networkActive.Value;

        #endregion
    }
#else
    // Fallback when Netcode not installed
    public class NetworkedObject : MonoBehaviour
    {
        [Header("=== NETCODE NOT INSTALLED ===")]
        public string note = "Install Netcode for GameObjects from Package Manager";

        public bool syncPosition = true;
        public bool syncRotation = true;
        public bool isInteractable = false;

        public bool IsServer => true;

        public void RequestPickup(ulong playerId) { }
        public void RequestInteract(ulong playerId) { }
        public void SetState(int state) { }
        public void TeleportTo(Vector3 position) { transform.position = position; }

        private void Start()
        {
            Debug.LogWarning($"[NetworkedObject] {gameObject.name}: Netcode not installed, running locally.");
        }
    }
#endif
}
