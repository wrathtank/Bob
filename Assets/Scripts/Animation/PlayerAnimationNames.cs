using UnityEngine;
using System.Collections.Generic;

namespace BobsPetroleum.Animation
{
    /// <summary>
    /// YOUR ANIMATION NAMES - CONFIGURE IN ONE PLACE!
    /// Create one of these: Assets > Create > Bob's Petroleum > Animation Config
    ///
    /// All 250 NFT models use whatever names YOU put here.
    /// Missing an animation? No problem - it falls back to Idle/Walk.
    ///
    /// GOOFY STYLE:
    /// - No gun animations needed! Walk pose = holding guns (Frankenstein arms)
    /// - Attack animation doubles for shooting
    /// - Keep it simple and cartoony!
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "Bob's Petroleum/Animation Config")]
    public class AnimationConfig : ScriptableObject
    {
        public static AnimationConfig Instance { get; private set; }

        [Header("=== YOUR ANIMATION NAMES ===")]
        [Tooltip("Fill in the animation names YOUR models use")]

        [Header("Movement (Required)")]
        public string idle = "Idle";
        public string walk = "Walk";
        public string run = "Run";

        [Header("Actions (Optional - falls back to Idle/Walk)")]
        public string jump = "";
        public string fall = "";
        public string attack = "";
        public string die = "";
        public string interact = "";

        [Header("Emotes (Optional)")]
        public string wave = "";
        public string dance = "";
        public string victory = "";

        [Header("Special (Optional)")]
        public string spawnFromTube = "";
        public string consume = "";
        public string throwNet = "";

        [Header("=== FALLBACK SETTINGS ===")]
        [Tooltip("Animation to use when requested one is missing")]
        public string fallbackAnimation = "Idle";

        [Tooltip("Show debug messages for missing animations")]
        public bool debugMode = false;

        // Lookup table built at runtime
        private Dictionary<string, string> lookupTable;

        private void OnEnable()
        {
            Instance = this;
            BuildLookupTable();
        }

        private void BuildLookupTable()
        {
            lookupTable = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            // Map standard names to YOUR names
            // If your name is empty, it falls back

            // Movement - always required
            AddMapping("Idle", idle);
            AddMapping("Walk", walk);
            AddMapping("Run", run);
            AddMapping("Sprint", run); // Sprint = Run

            // Jumping - optional
            AddMapping("Jump", jump, idle);
            AddMapping("Fall", fall, idle);
            AddMapping("Land", idle); // Always idle

            // Combat - optional, falls back to idle or attack
            AddMapping("Attack", attack, idle);
            AddMapping("Punch", attack, idle);
            AddMapping("Kick", attack, idle);
            AddMapping("Hit", idle); // Hit = just idle (goofy)
            AddMapping("Die", die, idle);
            AddMapping("Death", die, idle);

            // Interaction - optional
            AddMapping("Interact", interact, idle);
            AddMapping("PickUp", interact, idle);
            AddMapping("Use", interact, idle);

            // Items
            AddMapping("Throw", throwNet, attack, idle);
            AddMapping("Consume", consume, idle);
            AddMapping("Eat", consume, idle);
            AddMapping("Drink", consume, idle);
            AddMapping("Smoke", consume, idle);

            // GOOFY GUN HANDLING - No gun anims needed!
            // Just use Walk (Frankenstein arms out) and Attack for shooting
            AddMapping("PistolIdle", walk);
            AddMapping("PistolFire", attack, idle);
            AddMapping("PistolReload", idle);
            AddMapping("ShotgunIdle", walk);
            AddMapping("ShotgunFire", attack, idle);
            AddMapping("ShotgunReload", idle);
            AddMapping("FlamethrowerIdle", walk);
            AddMapping("FlamethrowerFire", walk); // Keep walking while flaming!

            // Work - use interact or walk
            AddMapping("CashRegister", interact, idle);
            AddMapping("PumpGas", interact, idle);
            AddMapping("Carry", walk);
            AddMapping("GiveItem", interact, idle);

            // Special
            AddMapping("SpawnFromTube", spawnFromTube, idle);
            AddMapping("Spawn", spawnFromTube, idle);
            AddMapping("Respawn", spawnFromTube, idle);
            AddMapping("Victory", victory, idle);
            AddMapping("Celebrate", victory, idle);

            // Emotes
            AddMapping("Wave", wave, idle);
            AddMapping("Dance", dance, idle);
            AddMapping("ThumbsUp", wave, idle);

            // Water - just use movement
            AddMapping("SwimIdle", idle);
            AddMapping("Swim", walk);
            AddMapping("Drown", die, idle);
        }

        private void AddMapping(string standardName, string yourName, params string[] fallbacks)
        {
            // Use your name if set
            if (!string.IsNullOrEmpty(yourName))
            {
                lookupTable[standardName] = yourName;
                return;
            }

            // Try fallbacks in order
            foreach (string fb in fallbacks)
            {
                if (!string.IsNullOrEmpty(fb))
                {
                    lookupTable[standardName] = fb;
                    return;
                }
            }

            // Ultimate fallback
            lookupTable[standardName] = fallbackAnimation;
        }

        /// <summary>
        /// Get YOUR animation name for a standard action.
        /// Example: GetAnimation("PistolFire") might return "Attack" or "Idle"
        /// </summary>
        public string Get(string standardName)
        {
            if (lookupTable == null) BuildLookupTable();

            if (lookupTable.TryGetValue(standardName, out string yourAnim))
            {
                return yourAnim;
            }

            if (debugMode)
            {
                Debug.Log($"[AnimConfig] Unknown animation '{standardName}', using fallback");
            }

            return fallbackAnimation;
        }

        /// <summary>
        /// Quick access - same as Get()
        /// </summary>
        public string this[string standardName] => Get(standardName);

        /// <summary>
        /// Check if you've configured a specific animation
        /// </summary>
        public bool Has(string standardName)
        {
            if (lookupTable == null) BuildLookupTable();
            return lookupTable.ContainsKey(standardName);
        }

        /// <summary>
        /// Rebuild lookup table (call if you change values at runtime)
        /// </summary>
        public void Refresh()
        {
            BuildLookupTable();
        }
    }

    /// <summary>
    /// Static helper for quick animation access anywhere in code.
    /// Uses the AnimationConfig singleton.
    ///
    /// Usage:
    ///   string anim = Anim.Get("Attack");
    ///   animator.Play(Anim.Walk);
    /// </summary>
    public static class Anim
    {
        private static AnimationConfig Config => AnimationConfig.Instance;

        /// <summary>
        /// Get your animation name for a standard action
        /// </summary>
        public static string Get(string standardName)
        {
            if (Config != null)
            {
                return Config.Get(standardName);
            }

            // No config - just return the standard name
            return standardName;
        }

        // Quick accessors for common animations
        public static string Idle => Get("Idle");
        public static string Walk => Get("Walk");
        public static string Run => Get("Run");
        public static string Jump => Get("Jump");
        public static string Fall => Get("Fall");
        public static string Attack => Get("Attack");
        public static string Die => Get("Die");
        public static string Interact => Get("Interact");
        public static string Throw => Get("Throw");
        public static string Victory => Get("Victory");
        public static string Wave => Get("Wave");
        public static string Dance => Get("Dance");

        // Gun animations (map to walk/attack)
        public static string PistolIdle => Get("PistolIdle");
        public static string PistolFire => Get("PistolFire");
        public static string ShotgunIdle => Get("ShotgunIdle");
        public static string ShotgunFire => Get("ShotgunFire");
        public static string FlamethrowerIdle => Get("FlamethrowerIdle");
        public static string FlamethrowerFire => Get("FlamethrowerFire");
    }
}
