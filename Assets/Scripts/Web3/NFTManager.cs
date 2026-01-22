using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using BobsPetroleum.Player;
using BobsPetroleum.Systems;

namespace BobsPetroleum.Web3
{
    /// <summary>
    /// Manages NFT-based weapons, boosts, and skins.
    /// Configure NFT items in the inspector.
    /// </summary>
    public class NFTManager : MonoBehaviour
    {
        public static NFTManager Instance { get; private set; }

        [Header("NFT Weapons (ERC-1155)")]
        [Tooltip("Special weapons tied to NFTs")]
        public List<NFTWeapon> nftWeapons = new List<NFTWeapon>();

        [Header("NFT Boosts (ERC-1155)")]
        [Tooltip("Special boosts tied to NFTs")]
        public List<NFTBoost> nftBoosts = new List<NFTBoost>();

        [Header("NFT Skins (ERC-721)")]
        [Tooltip("1:1 skins tied to NFTs")]
        public List<NFTSkin> nftSkins = new List<NFTSkin>();

        [Header("Events")]
        public UnityEvent<NFTWeapon> onNFTWeaponEquipped;
        public UnityEvent<NFTBoost> onNFTBoostApplied;
        public UnityEvent<NFTSkin> onNFTSkinApplied;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Listen for wallet connection
            if (WalletConnector.Instance != null)
            {
                WalletConnector.Instance.onNFTsLoaded.AddListener(OnNFTsLoaded);
            }
        }

        private void OnNFTsLoaded(List<OwnedNFT> nfts)
        {
            // Update available items based on owned NFTs
            Debug.Log($"Loaded {nfts.Count} NFTs");
        }

        #region Weapons

        /// <summary>
        /// Get NFT weapons the player owns.
        /// </summary>
        public List<NFTWeapon> GetOwnedWeapons()
        {
            var owned = new List<NFTWeapon>();

            if (WalletConnector.Instance == null || !WalletConnector.Instance.isConnected)
                return owned;

            foreach (var weapon in nftWeapons)
            {
                if (WalletConnector.Instance.OwnsNFT(weapon.contractAddress, weapon.tokenId))
                {
                    owned.Add(weapon);
                }
            }

            return owned;
        }

        /// <summary>
        /// Try to equip an NFT weapon for a player.
        /// </summary>
        public bool TryEquipNFTWeapon(string tokenId, PlayerController player)
        {
            var weapon = nftWeapons.Find(w => w.tokenId == tokenId);
            if (weapon == null) return false;

            if (!WalletConnector.Instance.OwnsNFT(weapon.contractAddress, weapon.tokenId))
            {
                Debug.Log("Player doesn't own this NFT weapon!");
                return false;
            }

            // Spawn weapon
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return false;

            var weaponObj = Instantiate(weapon.weaponPrefab, player.transform);
            weaponObj.SetActive(false);

            var meleeWeapon = weaponObj.GetComponent<MeleeWeapon>();
            if (meleeWeapon != null)
            {
                meleeWeapon.SetOwner(player);
            }

            bool added = inventory.AddWeapon(new WeaponItem
            {
                weaponId = weapon.tokenId,
                weaponName = weapon.weaponName,
                weaponObject = weaponObj,
                damage = weapon.damage,
                attackSpeed = weapon.attackSpeed,
                range = weapon.range,
                isNFTWeapon = true,
                nftTokenId = weapon.tokenId
            });

            if (added)
            {
                onNFTWeaponEquipped?.Invoke(weapon);
            }

            return added;
        }

        #endregion

        #region Boosts

        /// <summary>
        /// Get NFT boosts the player owns.
        /// </summary>
        public List<NFTBoost> GetOwnedBoosts()
        {
            var owned = new List<NFTBoost>();

            if (WalletConnector.Instance == null || !WalletConnector.Instance.isConnected)
                return owned;

            foreach (var boost in nftBoosts)
            {
                if (WalletConnector.Instance.OwnsNFT(boost.contractAddress, boost.tokenId))
                {
                    owned.Add(boost);
                }
            }

            return owned;
        }

        /// <summary>
        /// Apply an NFT boost to a player.
        /// </summary>
        public bool ApplyNFTBoost(string tokenId, PlayerController player)
        {
            var boost = nftBoosts.Find(b => b.tokenId == tokenId);
            if (boost == null) return false;

            if (!WalletConnector.Instance.OwnsNFT(boost.contractAddress, boost.tokenId))
            {
                return false;
            }

            // Apply effects
            switch (boost.boostType)
            {
                case BoostType.SpeedMultiplier:
                    player.ApplyCigarEffect(boost.value, 1f, boost.duration);
                    break;

                case BoostType.JumpMultiplier:
                    player.ApplyCigarEffect(1f, boost.value, boost.duration);
                    break;

                case BoostType.DamageMultiplier:
                    // Would need to add damage multiplier to PlayerCombat
                    break;

                case BoostType.StartingMoney:
                    var inventory = player.GetComponent<PlayerInventory>();
                    inventory?.AddMoney((int)boost.value);
                    break;

                case BoostType.HealthBoost:
                    var health = player.GetComponent<PlayerHealth>();
                    health?.Heal(boost.value);
                    break;
            }

            onNFTBoostApplied?.Invoke(boost);
            return true;
        }

        #endregion

        #region Skins

        /// <summary>
        /// Get NFT skins the player owns.
        /// </summary>
        public List<NFTSkin> GetOwnedSkins()
        {
            var owned = new List<NFTSkin>();

            if (WalletConnector.Instance == null || !WalletConnector.Instance.isConnected)
                return owned;

            foreach (var skin in nftSkins)
            {
                if (WalletConnector.Instance.OwnsNFT(skin.contractAddress, skin.tokenId))
                {
                    owned.Add(skin);
                }
            }

            return owned;
        }

        /// <summary>
        /// Apply an NFT skin to a player.
        /// </summary>
        public bool ApplyNFTSkin(string tokenId, PlayerController player)
        {
            var skin = nftSkins.Find(s => s.tokenId == tokenId);
            if (skin == null) return false;

            if (!WalletConnector.Instance.OwnsNFT(skin.contractAddress, skin.tokenId))
            {
                return false;
            }

            // Apply skin
            var skinApplier = player.GetComponent<PlayerSkinApplier>();
            if (skinApplier != null)
            {
                skinApplier.ApplySkin(skin);
            }

            onNFTSkinApplied?.Invoke(skin);
            return true;
        }

        #endregion

        /// <summary>
        /// Load pre-run NFT items for a player starting a game.
        /// </summary>
        public void LoadPreRunNFTItems(PlayerController player, List<string> selectedTokenIds)
        {
            foreach (var tokenId in selectedTokenIds)
            {
                // Try weapons
                if (nftWeapons.Exists(w => w.tokenId == tokenId))
                {
                    TryEquipNFTWeapon(tokenId, player);
                    continue;
                }

                // Try boosts
                if (nftBoosts.Exists(b => b.tokenId == tokenId))
                {
                    ApplyNFTBoost(tokenId, player);
                    continue;
                }

                // Try skins
                if (nftSkins.Exists(s => s.tokenId == tokenId))
                {
                    ApplyNFTSkin(tokenId, player);
                }
            }
        }
    }

    [System.Serializable]
    public class NFTWeapon
    {
        public string tokenId;
        public string contractAddress;
        public string weaponName;
        public string description;
        public Sprite icon;
        public GameObject weaponPrefab;

        [Header("Stats")]
        public float damage = 25f;
        public float attackSpeed = 1.2f;
        public float range = 2.5f;

        [Header("Special")]
        public DamageType damageType = DamageType.Physical;
        public GameObject specialEffect;
    }

    [System.Serializable]
    public class NFTBoost
    {
        public string tokenId;
        public string contractAddress;
        public string boostName;
        public string description;
        public Sprite icon;

        [Header("Effect")]
        public BoostType boostType;
        public float value;
        public float duration;
    }

    public enum BoostType
    {
        SpeedMultiplier,
        JumpMultiplier,
        DamageMultiplier,
        StartingMoney,
        HealthBoost
    }

    [System.Serializable]
    public class NFTSkin
    {
        public string tokenId;
        public string contractAddress;
        public string skinName;
        public string description;
        public Sprite icon;

        [Header("Visuals")]
        public GameObject playerModelPrefab;
        public Material[] materials;
        public RuntimeAnimatorController animatorOverride;
    }

    /// <summary>
    /// Applies skins to the player model.
    /// </summary>
    public class PlayerSkinApplier : MonoBehaviour
    {
        [Header("References")]
        public SkinnedMeshRenderer[] meshRenderers;
        public Animator animator;

        private NFTSkin currentSkin;

        /// <summary>
        /// Apply an NFT skin.
        /// </summary>
        public void ApplySkin(NFTSkin skin)
        {
            currentSkin = skin;

            // Apply materials
            if (skin.materials != null && skin.materials.Length > 0)
            {
                foreach (var renderer in meshRenderers)
                {
                    renderer.materials = skin.materials;
                }
            }

            // Apply animator override
            if (skin.animatorOverride != null && animator != null)
            {
                animator.runtimeAnimatorController = skin.animatorOverride;
            }
        }

        /// <summary>
        /// Reset to default skin.
        /// </summary>
        public void ResetSkin()
        {
            currentSkin = null;
            // Reset to default materials/animator
        }

        public NFTSkin GetCurrentSkin()
        {
            return currentSkin;
        }
    }
}
