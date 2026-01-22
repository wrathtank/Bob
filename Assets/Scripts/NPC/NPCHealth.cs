using UnityEngine;
using UnityEngine.Events;
using BobsPetroleum.Animation;

namespace BobsPetroleum.NPC
{
    /// <summary>
    /// Health component for NPCs (zombies, customers, etc.)
    /// </summary>
    public class NPCHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health")]
        public float maxHealth = 100f;

        [Tooltip("Current health")]
        public float currentHealth;

        [Header("Animation")]
        public AnimationEventHandler animationHandler;

        [Header("On Death")]
        [Tooltip("Destroy on death")]
        public bool destroyOnDeath = true;

        [Tooltip("Delay before destroying")]
        public float destroyDelay = 2f;

        [Tooltip("Drop items on death")]
        public GameObject[] dropOnDeath;

        [Header("Events")]
        public UnityEvent<float> onDamaged;
        public UnityEvent onDeath;

        private bool isDead = false;

        public bool IsDead => isDead;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        /// <summary>
        /// Take damage.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            onDamaged?.Invoke(damage);
            animationHandler?.TriggerAnimation("Hurt");

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Heal the NPC.
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        }

        private void Die()
        {
            isDead = true;
            onDeath?.Invoke();
            animationHandler?.TriggerAnimation("Death");

            // Drop items
            foreach (var item in dropOnDeath)
            {
                if (item != null)
                {
                    Instantiate(item, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                }
            }

            // Disable AI
            var customerAI = GetComponent<CustomerAI>();
            if (customerAI != null) customerAI.enabled = false;

            var zombieAI = GetComponent<ZombieAI>();
            if (zombieAI != null) zombieAI.enabled = false;

            // Destroy
            if (destroyOnDeath)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        /// <summary>
        /// Reset health (for respawning NPCs).
        /// </summary>
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
        }

        /// <summary>
        /// Get health percentage.
        /// </summary>
        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }
    }
}
