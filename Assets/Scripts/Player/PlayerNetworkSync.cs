using UnityEngine;
using Unity.Netcode;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Handles network synchronization for player name and other data.
    /// </summary>
    public class PlayerNetworkSync : NetworkBehaviour
    {
        [Header("Display")]
        [Tooltip("TextMesh for displaying player name above head")]
        public TMPro.TMP_Text nameDisplay;

        [Tooltip("Canvas containing the name display")]
        public Canvas nameCanvas;

        [Tooltip("Hide name for local player")]
        public bool hideOwnName = true;

        private NetworkVariable<Unity.Collections.FixedString64Bytes> networkPlayerName =
            new NetworkVariable<Unity.Collections.FixedString64Bytes>();

        public string PlayerName => networkPlayerName.Value.ToString();

        public override void OnNetworkSpawn()
        {
            networkPlayerName.OnValueChanged += OnNameChanged;

            if (IsOwner)
            {
                // Set name from save system
                string savedName = Core.SaveSystem.Instance?.GetPlayerName() ?? "Player";
                SetPlayerNameServerRpc(savedName);

                // Hide own name if configured
                if (hideOwnName && nameCanvas != null)
                {
                    nameCanvas.gameObject.SetActive(false);
                }

                // Update network manager with our name
                Core.BobsNetworkManager.Instance?.UpdatePlayerName(OwnerClientId, savedName);
            }
            else
            {
                // Show name for other players
                if (nameCanvas != null)
                {
                    nameCanvas.gameObject.SetActive(true);
                }

                UpdateNameDisplay(networkPlayerName.Value.ToString());
            }
        }

        public override void OnNetworkDespawn()
        {
            networkPlayerName.OnValueChanged -= OnNameChanged;
        }

        private void LateUpdate()
        {
            // Billboard the name canvas to face camera
            if (nameCanvas != null && nameCanvas.gameObject.activeSelf)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    nameCanvas.transform.LookAt(
                        nameCanvas.transform.position + mainCam.transform.forward);
                }
            }
        }

        [ServerRpc]
        private void SetPlayerNameServerRpc(string name)
        {
            networkPlayerName.Value = name;
        }

        private void OnNameChanged(Unity.Collections.FixedString64Bytes oldName,
            Unity.Collections.FixedString64Bytes newName)
        {
            UpdateNameDisplay(newName.ToString());
        }

        private void UpdateNameDisplay(string name)
        {
            if (nameDisplay != null)
            {
                nameDisplay.text = name;
            }
        }

        /// <summary>
        /// Change player name.
        /// </summary>
        public void SetName(string name)
        {
            if (IsOwner)
            {
                SetPlayerNameServerRpc(name);
                Core.SaveSystem.Instance?.SetPlayerName(name);
            }
        }
    }
}
