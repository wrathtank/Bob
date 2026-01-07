using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using BobsPetroleum.Core;
using BobsPetroleum.UI;
using BobsPetroleum.Animation;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Player health system with death, respawn, and damage handling.
    /// </summary>
    public class PlayerHealth : NetworkBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health")]
        public float maxHealth = 100f;

        [Tooltip("Starting health (set from GameManager if 0)")]
        public float startingHealth = 0f;

        [Tooltip("Health regeneration per second (0 = no regen)")]
        public float healthRegen = 0f;

        [Tooltip("Delay before regen starts after damage")]
        public float regenDelay = 3f;

        [Header("Death Settings")]
        [Tooltip("Respawn delay after death")]
        public float respawnDelay = 3f;

        [Tooltip("Clear inventory on death")]
        public bool clearInventoryOnDeath = true;

        [Header("Invincibility")]
        [Tooltip("Invincibility duration after respawn")]
        public float respawnInvincibilityDuration = 2f;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("Events")]
        public UnityEvent<float> onHealthChanged;
        public UnityEvent<float> onDamaged;
        public UnityEvent onDeath;
        public UnityEvent onRespawn;

        private NetworkVariable<float> networkHealth = new NetworkVariable<float>();
        private float lastDamageTime;
        private bool isDead = false;
        private bool isInvincible = false;

        public float CurrentHealth => IsServer ? networkHealth.Value : networkHealth.Value;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                float health = startingHealth > 0 ? startingHealth :
                    (GameManager.Instance != null ? GameManager.Instance.startingHealth : maxHealth);
                networkHealth.Value = health;
            }

            networkHealth.OnValueChanged += OnHealthValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            networkHealth.OnValueChanged -= OnHealthValueChanged;
        }

        private void Update()
        {
            if (!IsServer || isDead) return;

            // Health regeneration
            if (healthRegen > 0 && Time.time > lastDamageTime + regenDelay)
            {
                if (networkHealth.Value < maxHealth)
                {
                    networkHealth.Value = Mathf.Min(networkHealth.Value + healthRegen * Time.deltaTime, maxHealth);
                }
            }
        }

        private void OnHealthValueChanged(float oldValue, float newValue)
        {
            onHealthChanged?.Invoke(newValue);

            if (newValue < oldValue)
            {
                onDamaged?.Invoke(oldValue - newValue);
            }
        }

        /// <summary>
        /// Take damage. Only call on server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(float damage)
        {
            TakeDamage(damage);
        }

        /// <summary>
        /// Take damage (server-side).
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsServer || isDead || isInvincible) return;

            lastDamageTime = Time.time;
            networkHealth.Value = Mathf.Max(networkHealth.Value - damage, 0f);

            TakeDamageClientRpc(damage);

            if (networkHealth.Value <= 0)
            {
                Die();
            }
        }

        [ClientRpc]
        private void TakeDamageClientRpc(float damage)
        {
            animationHandler?.TriggerAnimation("Hurt");
        }

        /// <summary>
        /// Heal the player. Only call on server.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void HealServerRpc(float amount)
        {
            Heal(amount);
        }

        /// <summary>
        /// Heal the player (server-side).
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsServer || isDead) return;

            networkHealth.Value = Mathf.Min(networkHealth.Value + amount, maxHealth);
        }

        /// <summary>
        /// Set health to a specific value.
        /// </summary>
        public void SetHealth(float health)
        {
            if (!IsServer) return;

            networkHealth.Value = Mathf.Clamp(health, 0f, maxHealth);

            if (networkHealth.Value <= 0 && !isDead)
            {
                Die();
            }
        }

        private void Die()
        {
            isDead = true;
            onDeath?.Invoke();

            DieClientRpc();

            // Schedule respawn
            Invoke(nameof(Respawn), respawnDelay);
        }

        [ClientRpc]
        private void DieClientRpc()
        {
            animationHandler?.TriggerAnimation("Death");

            // Trigger fade in effect
            if (IsOwner)
            {
                FadeController.Instance?.FadeIn();
            }
        }

        private void Respawn()
        {
            if (!IsServer) return;

            isDead = false;

            // Reset health
            float health = GameManager.Instance != null ? GameManager.Instance.startingHealth : maxHealth;
            networkHealth.Value = health;

            // Clear inventory if configured
            if (clearInventoryOnDeath)
            {
                var inventory = GetComponent<PlayerInventory>();
                inventory?.ClearInventory();
            }

            // Get spawn position
            Vector3 spawnPos = Vector3.zero;
            if (GameManager.Instance != null && GameManager.Instance.playerSpawnPoint != null)
            {
                spawnPos = GameManager.Instance.playerSpawnPoint.position;
            }

            RespawnClientRpc(spawnPos);

            // Apply invincibility
            isInvincible = true;
            Invoke(nameof(DisableInvincibility), respawnInvincibilityDuration);

            onRespawn?.Invoke();
        }

        [ClientRpc]
        private void RespawnClientRpc(Vector3 spawnPosition)
        {
            var controller = GetComponent<PlayerController>();
            controller?.Teleport(spawnPosition);

            animationHandler?.TriggerAnimation("Respawn");

            // Trigger fade out effect
            if (IsOwner)
            {
                FadeController.Instance?.FadeOut();
            }
        }

        private void DisableInvincibility()
        {
            isInvincible = false;
        }

        /// <summary>
        /// Set temporary invincibility.
        /// </summary>
        public void SetInvincible(bool invincible, float duration = 0f)
        {
            isInvincible = invincible;

            if (invincible && duration > 0)
            {
                Invoke(nameof(DisableInvincibility), duration);
            }
        }

        /// <summary>
        /// Get health percentage (0-1).
        /// </summary>
        public float GetHealthPercentage()
        {
            return networkHealth.Value / maxHealth;
        }
    }
}
