using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using BobsPetroleum.Animation;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Handles melee combat for the player.
    /// </summary>
    public class PlayerCombat : NetworkBehaviour
    {
        [Header("Combat Settings")]
        [Tooltip("Attack input key")]
        public KeyCode attackKey = KeyCode.Mouse0;

        [Tooltip("Base attack damage (without weapon)")]
        public float baseDamage = 5f;

        [Tooltip("Base attack range")]
        public float baseRange = 1.5f;

        [Tooltip("Base attack speed (attacks per second)")]
        public float baseAttackSpeed = 1f;

        [Tooltip("Attack angle (degrees)")]
        public float attackAngle = 90f;

        [Header("Layers")]
        [Tooltip("Layers that can be damaged")]
        public LayerMask damageableLayers;

        [Header("Attack Point")]
        [Tooltip("Transform for attack origin (if null, uses camera)")]
        public Transform attackPoint;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("Audio")]
        public AudioClip swingSound;
        public AudioClip hitSound;

        [Header("Events")]
        public UnityEvent onAttackStart;
        public UnityEvent onAttackHit;
        public UnityEvent onAttackMiss;

        private PlayerInventory inventory;
        private PlayerController controller;
        private AudioSource audioSource;
        private float lastAttackTime = -999f;
        private bool isAttacking = false;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            controller = GetComponent<PlayerController>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Update()
        {
            if (!IsOwner || isAttacking) return;

            if (Input.GetKeyDown(attackKey))
            {
                TryAttack();
            }
        }

        private void TryAttack()
        {
            float attackSpeed = GetAttackSpeed();
            float attackCooldown = 1f / attackSpeed;

            if (Time.time < lastAttackTime + attackCooldown)
            {
                return;
            }

            lastAttackTime = Time.time;
            isAttacking = true;

            // Trigger animation
            animationHandler?.TriggerAnimation("Attack");

            // Play swing sound
            if (swingSound != null)
            {
                audioSource.PlayOneShot(swingSound);
            }

            onAttackStart?.Invoke();

            // Perform attack after animation delay
            float attackDelay = 0.2f; // Adjust based on animation
            Invoke(nameof(PerformAttack), attackDelay);
        }

        private void PerformAttack()
        {
            AttackServerRpc(GetAttackOrigin(), GetAttackDirection(), GetDamage(), GetRange());
            isAttacking = false;
        }

        [ServerRpc]
        private void AttackServerRpc(Vector3 origin, Vector3 direction, float damage, float range)
        {
            bool hitSomething = false;

            // Sphere cast for melee
            Collider[] hits = Physics.OverlapSphere(origin + direction * (range / 2f), range / 2f, damageableLayers);

            foreach (var hit in hits)
            {
                // Skip self
                if (hit.transform.root == transform) continue;

                // Check if within attack angle
                Vector3 toTarget = (hit.transform.position - origin).normalized;
                float angle = Vector3.Angle(direction, toTarget);

                if (angle <= attackAngle / 2f)
                {
                    // Apply damage
                    var health = hit.GetComponent<PlayerHealth>();
                    if (health != null)
                    {
                        health.TakeDamage(damage);
                        hitSomething = true;
                    }

                    var npcHealth = hit.GetComponent<NPC.NPCHealth>();
                    if (npcHealth != null)
                    {
                        npcHealth.TakeDamage(damage);
                        hitSomething = true;
                    }
                }
            }

            AttackResultClientRpc(hitSomething);
        }

        [ClientRpc]
        private void AttackResultClientRpc(bool hit)
        {
            if (hit)
            {
                if (hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }
                onAttackHit?.Invoke();
            }
            else
            {
                onAttackMiss?.Invoke();
            }
        }

        private Vector3 GetAttackOrigin()
        {
            if (attackPoint != null)
            {
                return attackPoint.position;
            }

            var cam = controller?.playerCamera;
            if (cam != null)
            {
                return cam.transform.position;
            }

            return transform.position + Vector3.up * 1.5f;
        }

        private Vector3 GetAttackDirection()
        {
            var cam = controller?.playerCamera;
            if (cam != null)
            {
                return cam.transform.forward;
            }

            return transform.forward;
        }

        private float GetDamage()
        {
            var weapon = inventory?.GetEquippedWeapon();
            return weapon != null ? weapon.damage : baseDamage;
        }

        private float GetRange()
        {
            var weapon = inventory?.GetEquippedWeapon();
            return weapon != null ? weapon.range : baseRange;
        }

        private float GetAttackSpeed()
        {
            var weapon = inventory?.GetEquippedWeapon();
            return weapon != null ? weapon.attackSpeed : baseAttackSpeed;
        }

        /// <summary>
        /// Check if player can attack.
        /// </summary>
        public bool CanAttack()
        {
            float attackSpeed = GetAttackSpeed();
            float attackCooldown = 1f / attackSpeed;
            return !isAttacking && Time.time >= lastAttackTime + attackCooldown;
        }
    }
}
