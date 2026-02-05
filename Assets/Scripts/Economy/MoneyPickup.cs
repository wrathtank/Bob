using UnityEngine;

namespace BobsPetroleum.Economy
{
    /// <summary>
    /// MONEY PICKUP - Physical money in the world!
    /// Coins and bills that can be picked up.
    ///
    /// SETUP:
    /// 1. Create money model (coin/bill)
    /// 2. Add MoneyPickup component
    /// 3. Set value
    /// 4. Done! Auto-pickup when player touches.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MoneyPickup : MonoBehaviour
    {
        [Header("=== VALUE ===")]
        [Tooltip("Money value")]
        public int value = 1;

        [Tooltip("Money type for visuals")]
        public MoneyType type = MoneyType.Coin;

        public enum MoneyType
        {
            Coin,       // $1
            Bill_5,     // $5
            Bill_10,    // $10
            Bill_20,    // $20
            Bill_50,    // $50
            Bill_100    // $100
        }

        [Header("=== BEHAVIOR ===")]
        [Tooltip("Auto-pickup on touch")]
        public bool autoPickup = true;

        [Tooltip("Magnetic pull towards player")]
        public bool magneticPull = true;

        [Tooltip("Pull radius")]
        public float magnetRadius = 3f;

        [Tooltip("Pull speed")]
        public float magnetSpeed = 5f;

        [Tooltip("Spin while idle")]
        public bool spin = true;

        [Tooltip("Spin speed")]
        public float spinSpeed = 90f;

        [Tooltip("Bob up and down")]
        public bool bob = true;

        [Tooltip("Bob height")]
        public float bobHeight = 0.2f;

        [Tooltip("Bob speed")]
        public float bobSpeed = 2f;

        [Header("=== AUDIO ===")]
        public AudioClip pickupSound;

        [Header("=== EFFECTS ===")]
        [Tooltip("Particle effect on pickup")]
        public GameObject pickupEffect;

        // Internal
        private Vector3 startPosition;
        private float bobOffset;
        private Transform playerTarget;
        private bool isBeingPulled = false;

        private void Start()
        {
            startPosition = transform.position;
            bobOffset = Random.Range(0f, Mathf.PI * 2); // Random phase

            // Ensure trigger
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void Update()
        {
            // Spin
            if (spin && !isBeingPulled)
            {
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
            }

            // Bob
            if (bob && !isBeingPulled)
            {
                float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }

            // Magnetic pull
            if (magneticPull && !isBeingPulled)
            {
                CheckMagneticPull();
            }

            // Move towards player if being pulled
            if (isBeingPulled && playerTarget != null)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTarget.position + Vector3.up,
                    magnetSpeed * Time.deltaTime
                );
            }
        }

        private void CheckMagneticPull()
        {
            var player = Player.PlayerController.Instance;
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= magnetRadius)
            {
                isBeingPulled = true;
                playerTarget = player.transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoPickup) return;

            var player = other.GetComponent<Player.PlayerController>();
            if (player != null)
            {
                Pickup(player);
            }
        }

        /// <summary>
        /// Pickup this money.
        /// </summary>
        public void Pickup(Player.PlayerController player)
        {
            var inventory = player.GetComponent<Player.PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddMoney(value);
            }

            // Sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Effect
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Set money value based on type.
        /// </summary>
        public void SetType(MoneyType moneyType)
        {
            type = moneyType;
            value = GetValueForType(moneyType);
        }

        public static int GetValueForType(MoneyType type)
        {
            switch (type)
            {
                case MoneyType.Coin: return 1;
                case MoneyType.Bill_5: return 5;
                case MoneyType.Bill_10: return 10;
                case MoneyType.Bill_20: return 20;
                case MoneyType.Bill_50: return 50;
                case MoneyType.Bill_100: return 100;
                default: return 1;
            }
        }

        #region Spawning Helpers

        /// <summary>
        /// Spawn money at position.
        /// </summary>
        public static MoneyPickup Spawn(Vector3 position, int amount, GameObject prefab = null)
        {
            GameObject obj;
            if (prefab != null)
            {
                obj = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                // Create simple placeholder
                obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obj.transform.position = position;
                obj.transform.localScale = new Vector3(0.3f, 0.05f, 0.3f);
                obj.GetComponent<Collider>().isTrigger = true;
            }

            var pickup = obj.GetComponent<MoneyPickup>();
            if (pickup == null)
            {
                pickup = obj.AddComponent<MoneyPickup>();
            }

            pickup.value = amount;
            pickup.type = GetTypeForValue(amount);

            return pickup;
        }

        /// <summary>
        /// Spawn multiple money pickups with scatter.
        /// </summary>
        public static void SpawnScattered(Vector3 position, int totalAmount, float radius = 1f, GameObject prefab = null)
        {
            // Break into reasonable denominations
            int remaining = totalAmount;
            int[] denominations = { 100, 50, 20, 10, 5, 1 };

            foreach (int denom in denominations)
            {
                while (remaining >= denom && remaining > 0)
                {
                    Vector3 offset = Random.insideUnitSphere * radius;
                    offset.y = 0.5f;
                    Spawn(position + offset, denom, prefab);
                    remaining -= denom;
                }
            }
        }

        private static MoneyType GetTypeForValue(int value)
        {
            if (value >= 100) return MoneyType.Bill_100;
            if (value >= 50) return MoneyType.Bill_50;
            if (value >= 20) return MoneyType.Bill_20;
            if (value >= 10) return MoneyType.Bill_10;
            if (value >= 5) return MoneyType.Bill_5;
            return MoneyType.Coin;
        }

        #endregion
    }
}
