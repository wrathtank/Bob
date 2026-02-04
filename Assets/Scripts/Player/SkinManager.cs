using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Skin manager with NFT verification support for 250 unique character models.
    /// All models use IDENTICAL animation names - just swap the prefab!
    ///
    /// MODEL NAMING: Models are named by their NFT token ID (e.g., "001", "002", "250")
    ///
    /// ANIMATION REQUIREMENT: All 250 models MUST have these animations:
    /// - Idle, Walk, Run, Sprint
    /// - Jump, Fall, Land
    /// - Attack, Die, Interact
    /// See PlayerAnimationNames class for full list.
    /// </summary>
    public class SkinManager : MonoBehaviour
    {
        public static SkinManager Instance { get; private set; }

        [Header("=== MODEL SWAPPING ===")]
        [Tooltip("Parent transform where player model is spawned")]
        public Transform modelParent;

        [Tooltip("Currently active model instance")]
        public GameObject currentModelInstance;

        [Tooltip("Animator on current model")]
        public Animator currentAnimator;

        [Tooltip("SimpleAnimationPlayer on current model")]
        public Animation.SimpleAnimationPlayer currentSimpleAnimator;

        [Header("=== MODEL LIBRARY (250 NFT MODELS) ===")]
        [Tooltip("Folder path for model prefabs (Resources folder)")]
        public string modelResourcePath = "PlayerModels/";

        [Tooltip("Default model token ID (for non-NFT players)")]
        public string defaultModelId = "000";

        [Tooltip("Total number of NFT models")]
        public int totalNFTModels = 250;

        [Header("=== LEGACY SKIN SUPPORT ===")]
        [Tooltip("Player mesh renderer(s) for material-only skins")]
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

        [Tooltip("Current model token ID")]
        public string currentModelTokenId = "000";

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
        private List<string> ownedNFTTokenIds = new List<string>(); // Token IDs from NFT check
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
            // Create model parent if needed
            if (modelParent == null)
            {
                GameObject parentObj = new GameObject("ModelParent");
                parentObj.transform.SetParent(transform);
                parentObj.transform.localPosition = Vector3.zero;
                parentObj.transform.localRotation = Quaternion.identity;
                modelParent = parentObj.transform;
            }

            // Start NFT check if enabled
            if (enableNFTSkins)
            {
                CheckNFTSkins();
            }

            // Load saved model (waits for NFT check in WebGL)
#if !UNITY_WEBGL || UNITY_EDITOR
            LoadSavedModel();
#endif

            // Load saved skin
            LoadSavedSkin();

            // Apply default if none selected
            if (currentSkin == null && defaultSkin != null)
            {
                ApplySkin(defaultSkin);
            }
        }

        #region Model Swapping (250 NFT Models)

        /// <summary>
        /// Swap player model by NFT token ID (e.g., "001", "042", "250")
        /// All models have identical animation names!
        /// </summary>
        public void SwapModelByTokenId(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                tokenId = defaultModelId;
            }

            // Load model prefab from Resources
            string path = modelResourcePath + tokenId;
            GameObject modelPrefab = Resources.Load<GameObject>(path);

            if (modelPrefab == null)
            {
                Debug.LogWarning($"[SkinManager] Model not found: {path}, using default");
                modelPrefab = Resources.Load<GameObject>(modelResourcePath + defaultModelId);

                if (modelPrefab == null)
                {
                    Debug.LogError("[SkinManager] Default model not found!");
                    return;
                }
            }

            SwapModel(modelPrefab, tokenId);
        }

        /// <summary>
        /// Swap player model with a prefab directly
        /// </summary>
        public void SwapModel(GameObject modelPrefab, string tokenId = "")
        {
            if (modelPrefab == null) return;

            // Ensure parent exists
            if (modelParent == null)
            {
                modelParent = transform;
            }

            // Destroy current model
            if (currentModelInstance != null)
            {
                Destroy(currentModelInstance);
            }

            // Spawn new model
            currentModelInstance = Instantiate(modelPrefab, modelParent);
            currentModelInstance.transform.localPosition = Vector3.zero;
            currentModelInstance.transform.localRotation = Quaternion.identity;
            currentModelInstance.transform.localScale = Vector3.one;
            currentModelInstance.name = $"PlayerModel_{tokenId}";

            // Get animation components
            currentAnimator = currentModelInstance.GetComponent<Animator>();
            if (currentAnimator == null)
            {
                currentAnimator = currentModelInstance.GetComponentInChildren<Animator>();
            }

            currentSimpleAnimator = currentModelInstance.GetComponent<Animation.SimpleAnimationPlayer>();
            if (currentSimpleAnimator == null)
            {
                currentSimpleAnimator = currentModelInstance.GetComponentInChildren<Animation.SimpleAnimationPlayer>();
            }

            // Update renderers list
            playerRenderers.Clear();
            playerRenderers.AddRange(currentModelInstance.GetComponentsInChildren<Renderer>());

            skinnedMeshRenderer = currentModelInstance.GetComponentInChildren<SkinnedMeshRenderer>();

            // Store token ID
            currentModelTokenId = tokenId;

            // Save selection
            PlayerPrefs.SetString("CurrentModelTokenId", tokenId);
            PlayerPrefs.Save();

            // Notify network
            SyncModelToNetwork(tokenId);

            Debug.Log($"[SkinManager] Swapped to model: {tokenId}");
        }

        /// <summary>
        /// Check if player owns NFT model by token ID
        /// </summary>
        public bool OwnsNFTModel(string tokenId)
        {
            // Check if any NFT skin matches this token ID
            foreach (var skin in nftOwnedSkins)
            {
                if (skin.nftTokenId.ToString().PadLeft(3, '0') == tokenId)
                {
                    return true;
                }
            }

            // Also check if token ID is in our owned list
            return ownedNFTTokenIds.Contains(tokenId);
        }

        /// <summary>
        /// Get list of owned NFT model token IDs
        /// </summary>
        public List<string> GetOwnedModelIds()
        {
            List<string> owned = new List<string>();

            // Always include default
            owned.Add(defaultModelId);

            // Add NFT owned
            owned.AddRange(ownedNFTTokenIds);

            return owned;
        }

        /// <summary>
        /// Load saved model on start
        /// </summary>
        private void LoadSavedModel()
        {
            string savedTokenId = PlayerPrefs.GetString("CurrentModelTokenId", defaultModelId);

            // Only apply if owned
            if (savedTokenId == defaultModelId || OwnsNFTModel(savedTokenId))
            {
                SwapModelByTokenId(savedTokenId);
            }
            else
            {
                SwapModelByTokenId(defaultModelId);
            }
        }

        /// <summary>
        /// Sync model selection across network
        /// </summary>
        private void SyncModelToNetwork(string tokenId)
        {
#if UNITY_NETCODE
            // If we have NetworkedPlayer, sync the model
            var networkedPlayer = GetComponent<Networking.NetworkedPlayer>();
            if (networkedPlayer != null)
            {
                networkedPlayer.SetModel(tokenId);
            }
#endif
        }

        /// <summary>
        /// Called from network to apply model (for remote players)
        /// </summary>
        public void ApplyNetworkModel(string tokenId)
        {
            SwapModelByTokenId(tokenId);
        }

        #endregion

        #region Animation Helpers

        /// <summary>
        /// Play animation on current model (works with both Animator and SimpleAnimationPlayer)
        /// </summary>
        public void PlayAnimation(string animationName)
        {
            if (currentSimpleAnimator != null)
            {
                currentSimpleAnimator.Play(animationName);
            }
            else if (currentAnimator != null)
            {
                currentAnimator.Play(animationName);
            }
        }

        /// <summary>
        /// Get current animation name
        /// </summary>
        public string GetCurrentAnimationName()
        {
            if (currentSimpleAnimator != null)
            {
                return currentSimpleAnimator.GetCurrentAnimationName();
            }

            if (currentAnimator != null)
            {
                var clipInfo = currentAnimator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    return clipInfo[0].clip.name;
                }
            }

            return "";
        }

        #endregion

        #region Skin Management (Material-based)

        /// <summary>
        /// Apply a skin (material/mesh change, not full model swap).
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

            // If it's an NFT skin with a token ID, swap the whole model
            if (skin.isNFTSkin && skin.nftTokenId >= 0)
            {
                string tokenId = skin.nftTokenId.ToString().PadLeft(3, '0');
                SwapModelByTokenId(tokenId);
                return;
            }

            // Otherwise, just apply material
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
        /// Expected format: JSON array of token IDs owned (e.g., [1, 42, 150, 250])
        /// Models are named by token ID: "001", "042", "150", "250"
        /// </summary>
        public void OnNFTOwnershipReceived(string jsonData)
        {
            isNFTCheckComplete = true;
            ownedNFTTokenIds.Clear();

            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.Log("[SkinManager] No NFTs owned - using default model");
                LoadSavedModel();
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
                        // Store as padded string (001, 042, 250)
                        string paddedId = tokenId.ToString().PadLeft(3, '0');
                        ownedNFTTokenIds.Add(paddedId);

                        Debug.Log($"[SkinManager] NFT model unlocked: {paddedId}");

                        // Also find matching SkinData if exists
                        SkinData matchingSkin = allSkins.Find(s => s.nftTokenId == tokenId);
                        if (matchingSkin != null)
                        {
                            nftOwnedSkins.Add(matchingSkin);
                        }
                    }

                    Debug.Log($"[SkinManager] {ownedNFTTokenIds.Count} NFT models available!");
                    onNFTSkinsLoaded?.Invoke(nftOwnedSkins);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SkinManager] Error parsing NFT data: {e.Message}");
            }

            // Now load the saved model (after NFT check)
            LoadSavedModel();
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
