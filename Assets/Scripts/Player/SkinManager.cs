using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Skin manager with NFT verification support.
    /// Allows players to apply skins from NFT ownership or purchased in-game.
    /// </summary>
    public class SkinManager : MonoBehaviour
    {
        public static SkinManager Instance { get; private set; }

        [Header("Player References")]
        [Tooltip("Player mesh renderer(s) to apply skins to")]
        public List<Renderer> playerRenderers = new List<Renderer>();

        [Tooltip("Player skinned mesh renderer")]
        public SkinnedMeshRenderer skinnedMeshRenderer;

        [Header("Available Skins")]
        [Tooltip("All available skins in the game")]
        public List<SkinData> allSkins = new List<SkinData>();

        [Tooltip("Default skin (fallback)")]
        public SkinData defaultSkin;

        [Header("Current Skin")]
        [Tooltip("Currently applied skin")]
        public SkinData currentSkin;

        [Header("NFT Integration")]
        [Tooltip("Enable NFT skin verification")]
        public bool enableNFTSkins = true;

        [Tooltip("NFT contract address to check")]
        public string nftContractAddress = "";

        [Tooltip("Wallet address (populated by web3)")]
        public string walletAddress = "";

        [Header("UI")]
        [Tooltip("Skin selection panel")]
        public GameObject skinSelectionPanel;

        [Header("Events")]
        public UnityEvent<SkinData> onSkinChanged;
        public UnityEvent<SkinData> onSkinUnlocked;
        public UnityEvent<List<SkinData>> onNFTSkinsLoaded;

        // Runtime
        private List<SkinData> ownedSkins = new List<SkinData>();
        private List<SkinData> nftOwnedSkins = new List<SkinData>();
        private bool isNFTCheckComplete;

        // WebGL JS Interface
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void CheckNFTOwnership(string contractAddress, string callback);

        [DllImport("__Internal")]
        private static extern string GetWalletAddress();

        [DllImport("__Internal")]
        private static extern void ConnectWallet(string callback);
#endif

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // Auto-find renderers
            if (playerRenderers.Count == 0)
            {
                AutoFindRenderers();
            }
        }

        private void Start()
        {
            // Load saved skin
            LoadSavedSkin();

            // Start NFT check if enabled
            if (enableNFTSkins)
            {
                CheckNFTSkins();
            }

            // Apply default if none selected
            if (currentSkin == null && defaultSkin != null)
            {
                ApplySkin(defaultSkin);
            }
        }

        #region Skin Management

        /// <summary>
        /// Apply a skin.
        /// </summary>
        public void ApplySkin(SkinData skin)
        {
            if (skin == null) return;

            // Check if owned
            if (!skin.isFree && !IsSkinOwned(skin))
            {
                Debug.LogWarning($"[SkinManager] Cannot apply unowned skin: {skin.skinName}");
                return;
            }

            currentSkin = skin;

            // Apply material
            if (skin.skinMaterial != null)
            {
                foreach (var renderer in playerRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.material = skin.skinMaterial;
                    }
                }

                if (skinnedMeshRenderer != null)
                {
                    skinnedMeshRenderer.material = skin.skinMaterial;
                }
            }

            // Apply mesh if different
            if (skin.skinMesh != null && skinnedMeshRenderer != null)
            {
                skinnedMeshRenderer.sharedMesh = skin.skinMesh;
            }

            // Save selection
            SaveSkinSelection();

            onSkinChanged?.Invoke(skin);

            Debug.Log($"[SkinManager] Applied skin: {skin.skinName}");
        }

        /// <summary>
        /// Unlock a skin (purchased or rewarded).
        /// </summary>
        public void UnlockSkin(SkinData skin)
        {
            if (skin == null) return;
            if (ownedSkins.Contains(skin)) return;

            ownedSkins.Add(skin);
            SaveOwnedSkins();

            onSkinUnlocked?.Invoke(skin);

            Debug.Log($"[SkinManager] Unlocked skin: {skin.skinName}");
        }

        /// <summary>
        /// Purchase a skin with in-game currency.
        /// </summary>
        public bool PurchaseSkin(SkinData skin)
        {
            if (skin == null) return false;
            if (IsSkinOwned(skin)) return false;

            var inventory = FindObjectOfType<PlayerInventory>();
            if (inventory == null) return false;

            if (inventory.Money >= skin.price)
            {
                inventory.RemoveMoney(skin.price);
                UnlockSkin(skin);
                return true;
            }

            Debug.Log($"[SkinManager] Not enough money for {skin.skinName}");
            return false;
        }

        /// <summary>
        /// Check if a skin is owned.
        /// </summary>
        public bool IsSkinOwned(SkinData skin)
        {
            if (skin == null) return false;
            if (skin.isFree) return true;
            if (ownedSkins.Contains(skin)) return true;
            if (nftOwnedSkins.Contains(skin)) return true;
            return false;
        }

        /// <summary>
        /// Get all owned skins.
        /// </summary>
        public List<SkinData> GetOwnedSkins()
        {
            List<SkinData> owned = new List<SkinData>();

            foreach (var skin in allSkins)
            {
                if (IsSkinOwned(skin))
                {
                    owned.Add(skin);
                }
            }

            return owned;
        }

        #endregion

        #region NFT Integration

        /// <summary>
        /// Check NFT ownership for skins.
        /// </summary>
        public void CheckNFTSkins()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (string.IsNullOrEmpty(nftContractAddress))
            {
                Debug.LogWarning("[SkinManager] No NFT contract address set");
                return;
            }

            // Get wallet address
            walletAddress = GetWalletAddress();

            if (string.IsNullOrEmpty(walletAddress))
            {
                Debug.Log("[SkinManager] No wallet connected, requesting connection...");
                ConnectWallet("OnWalletConnected");
                return;
            }

            // Check NFT ownership
            CheckNFTOwnership(nftContractAddress, "OnNFTOwnershipReceived");
#else
            // In editor, simulate NFT check
            Debug.Log("[SkinManager] NFT check simulated in editor");
            isNFTCheckComplete = true;
#endif
        }

        /// <summary>
        /// Called from JavaScript when wallet connects.
        /// </summary>
        public void OnWalletConnected(string address)
        {
            walletAddress = address;
            Debug.Log($"[SkinManager] Wallet connected: {address}");

            // Now check NFT ownership
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(nftContractAddress))
            {
                CheckNFTOwnership(nftContractAddress, "OnNFTOwnershipReceived");
            }
#endif
        }

        /// <summary>
        /// Called from JavaScript with NFT ownership data.
        /// Expected format: JSON array of token IDs owned.
        /// </summary>
        public void OnNFTOwnershipReceived(string jsonData)
        {
            isNFTCheckComplete = true;

            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.Log("[SkinManager] No NFTs owned");
                return;
            }

            try
            {
                // Parse token IDs
                NFTOwnershipData data = JsonUtility.FromJson<NFTOwnershipData>(jsonData);

                if (data != null && data.tokenIds != null)
                {
                    nftOwnedSkins.Clear();

                    foreach (int tokenId in data.tokenIds)
                    {
                        // Find skin by NFT token ID
                        SkinData matchingSkin = allSkins.Find(s => s.nftTokenId == tokenId);
                        if (matchingSkin != null)
                        {
                            nftOwnedSkins.Add(matchingSkin);
                            Debug.Log($"[SkinManager] NFT skin unlocked: {matchingSkin.skinName}");
                        }
                    }

                    onNFTSkinsLoaded?.Invoke(nftOwnedSkins);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SkinManager] Error parsing NFT data: {e.Message}");
            }
        }

        #endregion

        #region Save/Load

        private void SaveSkinSelection()
        {
            if (currentSkin != null)
            {
                PlayerPrefs.SetString("CurrentSkin", currentSkin.skinId);
                PlayerPrefs.Save();
            }
        }

        private void LoadSavedSkin()
        {
            string skinId = PlayerPrefs.GetString("CurrentSkin", "");

            if (!string.IsNullOrEmpty(skinId))
            {
                SkinData savedSkin = allSkins.Find(s => s.skinId == skinId);
                if (savedSkin != null && IsSkinOwned(savedSkin))
                {
                    ApplySkin(savedSkin);
                    return;
                }
            }

            // Apply default
            if (defaultSkin != null)
            {
                ApplySkin(defaultSkin);
            }
        }

        private void SaveOwnedSkins()
        {
            List<string> ownedIds = new List<string>();
            foreach (var skin in ownedSkins)
            {
                if (skin != null)
                {
                    ownedIds.Add(skin.skinId);
                }
            }

            string json = JsonUtility.ToJson(new StringList { items = ownedIds });
            PlayerPrefs.SetString("OwnedSkins", json);
            PlayerPrefs.Save();
        }

        private void LoadOwnedSkins()
        {
            string json = PlayerPrefs.GetString("OwnedSkins", "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    StringList data = JsonUtility.FromJson<StringList>(json);
                    if (data != null && data.items != null)
                    {
                        foreach (string skinId in data.items)
                        {
                            SkinData skin = allSkins.Find(s => s.skinId == skinId);
                            if (skin != null && !ownedSkins.Contains(skin))
                            {
                                ownedSkins.Add(skin);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        #endregion

        #region UI

        /// <summary>
        /// Open skin selection panel.
        /// </summary>
        public void OpenSkinSelection()
        {
            if (skinSelectionPanel != null)
            {
                skinSelectionPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Close skin selection panel.
        /// </summary>
        public void CloseSkinSelection()
        {
            if (skinSelectionPanel != null)
            {
                skinSelectionPanel.SetActive(false);
            }
        }

        #endregion

        #region Helpers

        private void AutoFindRenderers()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    playerRenderers.Add(r);
                }
            }

            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
            }
        }

        #endregion

        [System.Serializable]
        private class NFTOwnershipData
        {
            public int[] tokenIds;
        }

        [System.Serializable]
        private class StringList
        {
            public List<string> items;
        }
    }

    /// <summary>
    /// Skin data configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkin", menuName = "Bob's Petroleum/Skin Data")]
    public class SkinData : ScriptableObject
    {
        [Header("Info")]
        public string skinId;
        public string skinName = "Default Skin";
        public string description = "A basic skin";
        public Sprite icon;

        [Header("Rarity")]
        public SkinRarity rarity = SkinRarity.Common;

        [Header("Visuals")]
        public Material skinMaterial;
        public Mesh skinMesh;
        public Color tintColor = Color.white;

        [Header("Acquisition")]
        public bool isFree = false;
        public int price = 100;

        [Header("NFT")]
        public bool isNFTSkin = false;
        public int nftTokenId = -1;
        public string nftContractAddress;

        [Header("Preview")]
        public GameObject previewPrefab;
    }

    public enum SkinRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        NFT
    }
}
