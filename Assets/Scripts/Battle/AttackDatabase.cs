using UnityEngine;
using System.Collections.Generic;

namespace BobsPetroleum.Battle
{
    /// <summary>
    /// Database of all available attacks. Used for random attack assignment.
    /// </summary>
    public class AttackDatabase : MonoBehaviour
    {
        public static AttackDatabase Instance { get; private set; }

        [Header("All Available Attacks")]
        [Tooltip("List of all attacks that can be assigned to animals")]
        public List<AnimalAttack> allAttacks = new List<AnimalAttack>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;

                // Initialize default attacks if empty
                if (allAttacks.Count == 0)
                {
                    InitializeDefaultAttacks();
                }
            }
        }

        private void InitializeDefaultAttacks()
        {
            allAttacks = new List<AnimalAttack>
            {
                // Normal attacks
                new AnimalAttack { attackName = "Tackle", damage = 10f, accuracy = 0.95f, attackType = AttackType.Normal },
                new AnimalAttack { attackName = "Scratch", damage = 12f, accuracy = 0.9f, attackType = AttackType.Normal },
                new AnimalAttack { attackName = "Bite", damage = 15f, accuracy = 0.85f, attackType = AttackType.Normal },
                new AnimalAttack { attackName = "Headbutt", damage = 18f, accuracy = 0.8f, attackType = AttackType.Normal },
                new AnimalAttack { attackName = "Slam", damage = 20f, accuracy = 0.75f, attackType = AttackType.Normal },

                // Fire attacks
                new AnimalAttack { attackName = "Ember", damage = 12f, accuracy = 0.9f, attackType = AttackType.Fire },
                new AnimalAttack { attackName = "Fire Breath", damage = 18f, accuracy = 0.8f, attackType = AttackType.Fire },
                new AnimalAttack { attackName = "Flame Burst", damage = 25f, accuracy = 0.7f, attackType = AttackType.Fire },

                // Water attacks
                new AnimalAttack { attackName = "Water Gun", damage = 12f, accuracy = 0.9f, attackType = AttackType.Water },
                new AnimalAttack { attackName = "Bubble", damage = 10f, accuracy = 0.95f, attackType = AttackType.Water },
                new AnimalAttack { attackName = "Hydro Pump", damage = 25f, accuracy = 0.65f, attackType = AttackType.Water },

                // Electric attacks
                new AnimalAttack { attackName = "Spark", damage = 12f, accuracy = 0.9f, attackType = AttackType.Electric },
                new AnimalAttack { attackName = "Thunder Shock", damage = 15f, accuracy = 0.85f, attackType = AttackType.Electric },
                new AnimalAttack { attackName = "Thunderbolt", damage = 22f, accuracy = 0.75f, attackType = AttackType.Electric },

                // Grass attacks
                new AnimalAttack { attackName = "Vine Whip", damage = 12f, accuracy = 0.9f, attackType = AttackType.Grass },
                new AnimalAttack { attackName = "Razor Leaf", damage = 15f, accuracy = 0.85f, attackType = AttackType.Grass },
                new AnimalAttack { attackName = "Solar Beam", damage = 28f, accuracy = 0.6f, attackType = AttackType.Grass },

                // Poison attacks
                new AnimalAttack { attackName = "Poison Sting", damage = 10f, accuracy = 0.95f, attackType = AttackType.Poison },
                new AnimalAttack { attackName = "Acid", damage = 15f, accuracy = 0.85f, attackType = AttackType.Poison },
                new AnimalAttack { attackName = "Sludge Bomb", damage = 22f, accuracy = 0.75f, attackType = AttackType.Poison }
            };
        }

        /// <summary>
        /// Get random attacks for an animal.
        /// </summary>
        public List<AnimalAttack> GetRandomAttacks(int count)
        {
            var result = new List<AnimalAttack>();
            var available = new List<AnimalAttack>(allAttacks);

            count = Mathf.Min(count, available.Count);

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, available.Count);
                result.Add(CloneAttack(available[index]));
                available.RemoveAt(index);
            }

            return result;
        }

        /// <summary>
        /// Get random attacks of a specific type.
        /// </summary>
        public List<AnimalAttack> GetRandomAttacks(int count, AttackType type)
        {
            var result = new List<AnimalAttack>();
            var available = allAttacks.FindAll(a => a.attackType == type);

            count = Mathf.Min(count, available.Count);

            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, available.Count);
                result.Add(CloneAttack(available[index]));
                available.RemoveAt(index);
            }

            return result;
        }

        /// <summary>
        /// Get an attack by name.
        /// </summary>
        public AnimalAttack GetAttack(string name)
        {
            var attack = allAttacks.Find(a => a.attackName == name);
            return attack != null ? CloneAttack(attack) : null;
        }

        private AnimalAttack CloneAttack(AnimalAttack original)
        {
            return new AnimalAttack
            {
                attackName = original.attackName,
                damage = original.damage,
                accuracy = original.accuracy,
                attackType = original.attackType,
                animationTrigger = original.animationTrigger,
                attackSound = original.attackSound,
                effectPrefab = original.effectPrefab
            };
        }
    }
}
