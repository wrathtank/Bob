using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BobsPetroleum.Web3
{
    /// <summary>
    /// Wallet connector for WebGL builds. Uses JavaScript interop for WalletConnect/Web3.
    /// Attach to a 3D object (like a plane) for players to interact with.
    /// </summary>
    public class WalletConnector : MonoBehaviour, Player.IInteractable
    {
        public static WalletConnector Instance { get; private set; }

        [Header("Connection Settings")]
        [Tooltip("Chain ID for your L1 (default Ethereum Mainnet)")]
        public int chainId = 1;

        [Tooltip("RPC URL for your L1")]
        public string rpcUrl = "";

        [Tooltip("Your L1 chain name")]
        public string chainName = "Ethereum";

        [Header("State")]
        public bool isConnected = false;
        public string connectedAddress = "";

        [Header("Interaction")]
        public string interactionPrompt = "Press E to Connect Wallet";

        [Header("NFT Contracts")]
        [Tooltip("ERC-1155 contract address for weapons/boosts")]
        public string erc1155ContractAddress = "";

        [Tooltip("ERC-721 contract address for 1:1 skins")]
        public string erc721ContractAddress = "";

        [Header("Events")]
        public UnityEvent<string> onWalletConnected;
        public UnityEvent onWalletDisconnected;
        public UnityEvent<string> onConnectionError;
        public UnityEvent<List<OwnedNFT>> onNFTsLoaded;

        // Cached NFTs
        private List<OwnedNFT> ownedNFTs = new List<OwnedNFT>();
        private Dictionary<string, bool> nftOwnershipCache = new Dictionary<string, bool>();

        #region JavaScript Interop

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void JS_ConnectWallet(string gameObjectName, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void JS_DisconnectWallet();

        [DllImport("__Internal")]
        private static extern string JS_GetConnectedAddress();

        [DllImport("__Internal")]
        private static extern void JS_CheckNFTOwnership(string contractAddress, string tokenId, string gameObjectName, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void JS_GetOwnedNFTs(string contractAddress, string gameObjectName, string callbackMethod);
#endif

        #endregion

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Check if already connected (page refresh)
            CheckExistingConnection();
        }

        public void Interact(Player.PlayerController player)
        {
            if (isConnected)
            {
                DisconnectWallet();
            }
            else
            {
                ConnectWallet();
            }
        }

        public string GetInteractionPrompt()
        {
            return isConnected ? $"Connected: {TruncateAddress(connectedAddress)} (E to Disconnect)" : interactionPrompt;
        }

        #region Wallet Connection

        /// <summary>
        /// Initiate wallet connection.
        /// </summary>
        public void ConnectWallet()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_ConnectWallet(gameObject.name, "OnWalletConnectCallback");
#else
            // Mock connection for editor testing
            SimulateConnection("0x1234567890abcdef1234567890abcdef12345678");
#endif
        }

        /// <summary>
        /// Disconnect wallet.
        /// </summary>
        public void DisconnectWallet()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_DisconnectWallet();
#endif
            isConnected = false;
            connectedAddress = "";
            ownedNFTs.Clear();
            nftOwnershipCache.Clear();
            onWalletDisconnected?.Invoke();
        }

        private void CheckExistingConnection()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string address = JS_GetConnectedAddress();
            if (!string.IsNullOrEmpty(address))
            {
                OnWalletConnectCallback(address);
            }
#endif
        }

        // Called from JavaScript
        public void OnWalletConnectCallback(string result)
        {
            if (result.StartsWith("error:"))
            {
                string error = result.Substring(6);
                onConnectionError?.Invoke(error);
                return;
            }

            isConnected = true;
            connectedAddress = result;
            onWalletConnected?.Invoke(result);

            // Load NFTs
            LoadOwnedNFTs();
        }

        // For editor testing
        private void SimulateConnection(string address)
        {
            isConnected = true;
            connectedAddress = address;
            onWalletConnected?.Invoke(address);
        }

        #endregion

        #region NFT Operations

        /// <summary>
        /// Load all owned NFTs from configured contracts.
        /// </summary>
        public void LoadOwnedNFTs()
        {
            ownedNFTs.Clear();

            if (!string.IsNullOrEmpty(erc1155ContractAddress))
            {
                LoadNFTsFromContract(erc1155ContractAddress, NFTType.ERC1155);
            }

            if (!string.IsNullOrEmpty(erc721ContractAddress))
            {
                LoadNFTsFromContract(erc721ContractAddress, NFTType.ERC721);
            }
        }

        private void LoadNFTsFromContract(string contractAddress, NFTType type)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JS_GetOwnedNFTs(contractAddress, gameObject.name, "OnNFTsLoadedCallback");
#else
            // Mock NFTs for editor testing
            AddMockNFTs(type);
#endif
        }

        // Called from JavaScript
        public void OnNFTsLoadedCallback(string jsonResult)
        {
            // Parse JSON result
            // Expected format: { "nfts": [{ "tokenId": "1", "amount": 1, "type": "ERC1155" }, ...] }
            try
            {
                var result = JsonUtility.FromJson<NFTListResult>(jsonResult);
                if (result != null && result.nfts != null)
                {
                    foreach (var nft in result.nfts)
                    {
                        ownedNFTs.Add(nft);
                        string cacheKey = $"{nft.contractAddress}_{nft.tokenId}";
                        nftOwnershipCache[cacheKey] = true;
                    }
                }

                onNFTsLoaded?.Invoke(ownedNFTs);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to parse NFT result: {e.Message}");
            }
        }

        /// <summary>
        /// Check if player owns a specific NFT.
        /// </summary>
        public bool OwnsNFT(string contractAddress, string tokenId)
        {
            string cacheKey = $"{contractAddress}_{tokenId}";
            return nftOwnershipCache.ContainsKey(cacheKey) && nftOwnershipCache[cacheKey];
        }

        /// <summary>
        /// Check NFT ownership asynchronously.
        /// </summary>
        public void CheckNFTOwnershipAsync(string contractAddress, string tokenId, System.Action<bool> callback)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // Store callback for later
            pendingOwnershipCallbacks[$"{contractAddress}_{tokenId}"] = callback;
            JS_CheckNFTOwnership(contractAddress, tokenId, gameObject.name, "OnOwnershipCheckCallback");
#else
            // Mock ownership for editor
            callback?.Invoke(true);
#endif
        }

        private Dictionary<string, System.Action<bool>> pendingOwnershipCallbacks = new Dictionary<string, System.Action<bool>>();

        // Called from JavaScript
        public void OnOwnershipCheckCallback(string result)
        {
            // Expected format: "contractAddress_tokenId:true/false"
            var parts = result.Split(':');
            if (parts.Length == 2)
            {
                string key = parts[0];
                bool owns = parts[1].ToLower() == "true";

                nftOwnershipCache[key] = owns;

                if (pendingOwnershipCallbacks.ContainsKey(key))
                {
                    pendingOwnershipCallbacks[key]?.Invoke(owns);
                    pendingOwnershipCallbacks.Remove(key);
                }
            }
        }

        /// <summary>
        /// Get all owned NFTs.
        /// </summary>
        public List<OwnedNFT> GetOwnedNFTs()
        {
            return new List<OwnedNFT>(ownedNFTs);
        }

        /// <summary>
        /// Get owned NFTs of a specific type.
        /// </summary>
        public List<OwnedNFT> GetOwnedNFTs(NFTType type)
        {
            return ownedNFTs.FindAll(n => n.nftType == type);
        }

        #endregion

        #region Utility

        private string TruncateAddress(string address)
        {
            if (string.IsNullOrEmpty(address) || address.Length < 10)
                return address;

            return $"{address.Substring(0, 6)}...{address.Substring(address.Length - 4)}";
        }

        private void AddMockNFTs(NFTType type)
        {
            // Add some mock NFTs for testing in editor
            ownedNFTs.Add(new OwnedNFT
            {
                tokenId = "1",
                contractAddress = type == NFTType.ERC1155 ? erc1155ContractAddress : erc721ContractAddress,
                amount = 1,
                nftType = type,
                metadata = new NFTMetadata
                {
                    name = "Test NFT",
                    description = "A test NFT for development"
                }
            });

            onNFTsLoaded?.Invoke(ownedNFTs);
        }

        #endregion
    }

    public enum NFTType
    {
        ERC721,
        ERC1155
    }

    [System.Serializable]
    public class OwnedNFT
    {
        public string tokenId;
        public string contractAddress;
        public int amount;
        public NFTType nftType;
        public NFTMetadata metadata;
    }

    [System.Serializable]
    public class NFTMetadata
    {
        public string name;
        public string description;
        public string image;
        public List<NFTAttribute> attributes;
    }

    [System.Serializable]
    public class NFTAttribute
    {
        public string trait_type;
        public string value;
    }

    [System.Serializable]
    public class NFTListResult
    {
        public List<OwnedNFT> nfts;
    }
}
