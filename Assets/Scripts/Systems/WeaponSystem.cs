using UnityEngine;
using UnityEngine.Events;
using BobsPetroleum.Player;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Base class for melee weapons. Attach to weapon prefabs.
    /// Configure damage, speed, and range in inspector.
    /// </summary>
    public class MeleeWeapon : MonoBehaviour
    {
        [Header("Weapon Info")]
        [Tooltip("Weapon name")]
        public string weaponName = "Weapon";

        [Tooltip("Weapon ID for inventory")]
        public string weaponId = "weapon_01";

        [Header("Stats")]
        [Tooltip("Damage per hit")]
        public float damage = 20f;

        [Tooltip("Attack speed (attacks per second)")]
        public float attackSpeed = 1f;

        [Tooltip("Attack range")]
        public float range = 2f;

        [Tooltip("Attack angle (degrees)")]
        public float attackAngle = 90f;

        [Header("Damage Type")]
        [Tooltip("Type of damage")]
        public DamageType damageType = DamageType.Physical;

        [Tooltip("Knockback force")]
        public float knockbackForce = 5f;

        [Header("Effects")]
        [Tooltip("Particle effect on hit")]
        public ParticleSystem hitEffect;

        [Tooltip("Trail effect while swinging")]
        public TrailRenderer swingTrail;

        [Header("Audio")]
        public AudioClip swingSound;
        public AudioClip hitSound;

        [Header("Animation")]
        [Tooltip("Animation trigger name for attack")]
        public string attackAnimationTrigger = "Attack";

        [Header("NFT Settings")]
        [Tooltip("Is this an NFT weapon")]
        public bool isNFTWeapon = false;

        [Tooltip("Required NFT token ID")]
        public string requiredTokenId = "";

        [Tooltip("NFT contract address")]
        public string contractAddress = "";

        [Header("Events")]
        public UnityEvent onSwing;
        public UnityEvent<Collider> onHit;

        private AudioSource audioSource;
        private PlayerController owner;
        private float lastAttackTime = -999f;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Disable trail by default
            if (swingTrail != null)
            {
                swingTrail.enabled = false;
            }
        }

        /// <summary>
        /// Set the weapon owner.
        /// </summary>
        public void SetOwner(PlayerController player)
        {
            owner = player;
        }

        /// <summary>
        /// Perform an attack.
        /// </summary>
        public bool Attack(Vector3 origin, Vector3 direction, LayerMask targetLayers)
        {
            if (Time.time < lastAttackTime + (1f / attackSpeed))
            {
                return false;
            }

            lastAttackTime = Time.time;

            // Play swing sound
            if (swingSound != null)
            {
                audioSource.PlayOneShot(swingSound);
            }

            // Enable trail
            if (swingTrail != null)
            {
                swingTrail.enabled = true;
                Invoke(nameof(DisableTrail), 0.3f);
            }

            onSwing?.Invoke();

            // Perform hit detection
            Collider[] hits = Physics.OverlapSphere(origin + direction * (range / 2f), range / 2f, targetLayers);

            bool hitSomething = false;

            foreach (var hit in hits)
            {
                // Skip owner
                if (owner != null && hit.transform.root == owner.transform.root)
                    continue;

                // Check angle
                Vector3 toTarget = (hit.transform.position - origin).normalized;
                float angle = Vector3.Angle(direction, toTarget);

                if (angle <= attackAngle / 2f)
                {
                    ApplyDamage(hit);
                    hitSomething = true;
                }
            }

            return hitSomething;
        }

        private void ApplyDamage(Collider target)
        {
            // Play hit sound
            if (hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            // Play hit effect
            if (hitEffect != null)
            {
                var effect = Instantiate(hitEffect, target.transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, 2f);
            }

            // Apply damage to player
            var playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }

            // Apply damage to NPC
            var npcHealth = target.GetComponent<NPC.NPCHealth>();
            if (npcHealth != null)
            {
                npcHealth.TakeDamage(damage);
            }

            // Apply knockback
            var rb = target.GetComponent<Rigidbody>();
            if (rb != null && knockbackForce > 0)
            {
                Vector3 knockbackDir = (target.transform.position - transform.position).normalized;
                rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
            }

            onHit?.Invoke(target);
        }

        private void DisableTrail()
        {
            if (swingTrail != null)
            {
                swingTrail.enabled = false;
            }
        }

        /// <summary>
        /// Check if this weapon requires an NFT.
        /// </summary>
        public bool RequiresNFT()
        {
            return isNFTWeapon && !string.IsNullOrEmpty(requiredTokenId);
        }

        /// <summary>
        /// Get weapon data for inventory.
        /// </summary>
        public WeaponItem GetWeaponItem()
        {
            return new WeaponItem
            {
                weaponId = weaponId,
                weaponName = weaponName,
                weaponObject = gameObject,
                damage = damage,
                attackSpeed = attackSpeed,
                range = range,
                isNFTWeapon = isNFTWeapon,
                nftTokenId = requiredTokenId
            };
        }
    }

    public enum DamageType
    {
        Physical,
        Fire,
        Electric,
        Poison
    }

    /// <summary>
    /// Weapon pickup item. Player interacts to pick up.
    /// </summary>
    public class WeaponPickup : MonoBehaviour, IInteractable
    {
        [Header("Weapon")]
        [Tooltip("Weapon prefab to give")]
        public MeleeWeapon weaponPrefab;

        [Header("Interaction")]
        public string interactionPrompt = "Press E to Pick Up";

        [Header("Audio")]
        public AudioClip pickupSound;

        public void Interact(PlayerController player)
        {
            PickUp(player);
        }

        public string GetInteractionPrompt()
        {
            return $"{interactionPrompt} {weaponPrefab?.weaponName}";
        }

        private void PickUp(PlayerController player)
        {
            if (weaponPrefab == null) return;

            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory == null) return;

            // Check NFT requirement
            if (weaponPrefab.RequiresNFT())
            {
                // Check if player owns NFT
                var wallet = player.GetComponent<Web3.WalletConnector>();
                if (wallet == null || !wallet.OwnsNFT(weaponPrefab.contractAddress, weaponPrefab.requiredTokenId))
                {
                    Debug.Log("Player doesn't own required NFT!");
                    return;
                }
            }

            // Spawn weapon for player
            var weapon = Instantiate(weaponPrefab, player.transform);
            weapon.gameObject.SetActive(false);
            weapon.SetOwner(player);

            // Add to inventory
            bool added = inventory.AddWeapon(weapon.GetWeaponItem());

            if (added)
            {
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
            else
            {
                // Inventory full
                Destroy(weapon.gameObject);
            }
        }
    }
}
