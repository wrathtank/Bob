using UnityEngine;

namespace BobsPetroleum.Animation
{
    /// <summary>
    /// STANDARDIZED ANIMATION NAMES FOR ALL 250 NFT PLAYER MODELS
    ///
    /// ALL models MUST use these EXACT animation names!
    /// This ensures any model works with any player regardless of NFT.
    ///
    /// ARTIST INSTRUCTIONS:
    /// 1. Export animations with THESE EXACT NAMES
    /// 2. All 250 models must have ALL these animations
    /// 3. Animation timing can vary, but names must match
    /// </summary>
    public static class PlayerAnimationNames
    {
        // ===========================================
        // MOVEMENT - REQUIRED
        // ===========================================

        /// <summary>Standing still</summary>
        public const string Idle = "Idle";

        /// <summary>Slow movement</summary>
        public const string Walk = "Walk";

        /// <summary>Normal movement</summary>
        public const string Run = "Run";

        /// <summary>Fast movement (shift held)</summary>
        public const string Sprint = "Sprint";

        /// <summary>Moving backwards</summary>
        public const string WalkBack = "WalkBack";

        /// <summary>Strafing left</summary>
        public const string StrafeLeft = "StrafeLeft";

        /// <summary>Strafing right</summary>
        public const string StrafeRight = "StrafeRight";

        // ===========================================
        // JUMPING/FALLING - REQUIRED
        // ===========================================

        /// <summary>Jump start</summary>
        public const string Jump = "Jump";

        /// <summary>Falling through air</summary>
        public const string Fall = "Fall";

        /// <summary>Landing on ground</summary>
        public const string Land = "Land";

        // ===========================================
        // CROUCHING - OPTIONAL
        // ===========================================

        /// <summary>Crouching idle</summary>
        public const string CrouchIdle = "CrouchIdle";

        /// <summary>Crouching walk</summary>
        public const string CrouchWalk = "CrouchWalk";

        // ===========================================
        // COMBAT - REQUIRED
        // ===========================================

        /// <summary>Generic attack</summary>
        public const string Attack = "Attack";

        /// <summary>Punch attack</summary>
        public const string Punch = "Punch";

        /// <summary>Kick attack</summary>
        public const string Kick = "Kick";

        /// <summary>Taking damage</summary>
        public const string Hit = "Hit";

        /// <summary>Death animation</summary>
        public const string Die = "Die";

        /// <summary>Getting back up</summary>
        public const string GetUp = "GetUp";

        // ===========================================
        // WEAPONS - REQUIRED
        // ===========================================

        /// <summary>Holding pistol idle</summary>
        public const string PistolIdle = "PistolIdle";

        /// <summary>Firing pistol</summary>
        public const string PistolFire = "PistolFire";

        /// <summary>Holding shotgun idle</summary>
        public const string ShotgunIdle = "ShotgunIdle";

        /// <summary>Firing shotgun</summary>
        public const string ShotgunFire = "ShotgunFire";

        /// <summary>Holding flamethrower</summary>
        public const string FlamethrowerIdle = "FlamethrowerIdle";

        /// <summary>Using flamethrower</summary>
        public const string FlamethrowerFire = "FlamethrowerFire";

        // ===========================================
        // INTERACTION - REQUIRED
        // ===========================================

        /// <summary>Generic interact (press E)</summary>
        public const string Interact = "Interact";

        /// <summary>Picking up item</summary>
        public const string PickUp = "PickUp";

        /// <summary>Using item</summary>
        public const string UseItem = "UseItem";

        /// <summary>Throwing (net for pets)</summary>
        public const string Throw = "Throw";

        /// <summary>Eating/drinking</summary>
        public const string Consume = "Consume";

        /// <summary>Smoking cigar</summary>
        public const string Smoke = "Smoke";

        // ===========================================
        // WORK/GAS STATION - REQUIRED
        // ===========================================

        /// <summary>Operating cash register</summary>
        public const string CashRegister = "CashRegister";

        /// <summary>Pumping gas</summary>
        public const string PumpGas = "PumpGas";

        /// <summary>Carrying item</summary>
        public const string Carry = "Carry";

        /// <summary>Handing item to NPC</summary>
        public const string GiveItem = "GiveItem";

        // ===========================================
        // EMOTES - OPTIONAL
        // ===========================================

        /// <summary>Wave hello</summary>
        public const string Wave = "Wave";

        /// <summary>Dance</summary>
        public const string Dance = "Dance";

        /// <summary>Thumbs up</summary>
        public const string ThumbsUp = "ThumbsUp";

        /// <summary>Shrug</summary>
        public const string Shrug = "Shrug";

        /// <summary>Celebrate</summary>
        public const string Celebrate = "Celebrate";

        // ===========================================
        // SPECIAL - REQUIRED
        // ===========================================

        /// <summary>Spawning from tube</summary>
        public const string SpawnFromTube = "SpawnFromTube";

        /// <summary>Respawning after death</summary>
        public const string Respawn = "Respawn";

        /// <summary>Victory pose</summary>
        public const string Victory = "Victory";

        // ===========================================
        // SWIMMING - OPTIONAL
        // ===========================================

        /// <summary>Swimming idle</summary>
        public const string SwimIdle = "SwimIdle";

        /// <summary>Swimming forward</summary>
        public const string Swim = "Swim";

        /// <summary>Drowning</summary>
        public const string Drown = "Drown";

        // ===========================================
        // SITTING - OPTIONAL
        // ===========================================

        /// <summary>Sit down</summary>
        public const string SitDown = "SitDown";

        /// <summary>Sitting idle</summary>
        public const string SitIdle = "SitIdle";

        /// <summary>Stand up from sitting</summary>
        public const string StandUp = "StandUp";

        // ===========================================
        // HELPER METHODS
        // ===========================================

        /// <summary>
        /// Get all required animation names (models MUST have these)
        /// </summary>
        public static string[] GetRequiredAnimations()
        {
            return new string[]
            {
                // Movement
                Idle, Walk, Run, Sprint,
                // Jumping
                Jump, Fall, Land,
                // Combat
                Attack, Hit, Die,
                // Weapons
                PistolIdle, PistolFire,
                ShotgunIdle, ShotgunFire,
                FlamethrowerIdle, FlamethrowerFire,
                // Interaction
                Interact, PickUp, Throw, Consume,
                // Work
                CashRegister, PumpGas, Carry,
                // Special
                SpawnFromTube, Respawn
            };
        }

        /// <summary>
        /// Get all optional animation names (nice to have)
        /// </summary>
        public static string[] GetOptionalAnimations()
        {
            return new string[]
            {
                WalkBack, StrafeLeft, StrafeRight,
                CrouchIdle, CrouchWalk,
                Punch, Kick, GetUp,
                UseItem, Smoke, GiveItem,
                Wave, Dance, ThumbsUp, Shrug, Celebrate,
                Victory,
                SwimIdle, Swim, Drown,
                SitDown, SitIdle, StandUp
            };
        }

        /// <summary>
        /// Check if an animator has all required animations
        /// </summary>
        public static bool ValidateAnimator(Animator animator)
        {
            if (animator == null) return false;

            var requiredAnims = GetRequiredAnimations();
            foreach (string animName in requiredAnims)
            {
                // Check if animation exists in any layer
                bool found = false;
                for (int layer = 0; layer < animator.layerCount; layer++)
                {
                    if (animator.HasState(layer, Animator.StringToHash(animName)))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.LogWarning($"[AnimationValidation] Missing required animation: {animName}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if SimpleAnimationPlayer has all required animations
        /// </summary>
        public static bool ValidateSimpleAnimator(SimpleAnimationPlayer animator)
        {
            if (animator == null) return false;

            var requiredAnims = GetRequiredAnimations();
            foreach (string animName in requiredAnims)
            {
                if (!animator.HasAnimation(animName))
                {
                    Debug.LogWarning($"[AnimationValidation] Missing required animation: {animName}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Extension methods for easy animation playback using standardized names
    /// </summary>
    public static class PlayerAnimationExtensions
    {
        public static void PlayIdle(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Idle);
        public static void PlayWalk(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Walk);
        public static void PlayRun(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Run);
        public static void PlaySprint(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Sprint);
        public static void PlayJump(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Jump);
        public static void PlayFall(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Fall);
        public static void PlayLand(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Land);
        public static void PlayAttack(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Attack);
        public static void PlayHit(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Hit);
        public static void PlayDie(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Die);
        public static void PlayInteract(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Interact);
        public static void PlayThrow(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Throw);
        public static void PlayConsume(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Consume);
        public static void PlaySpawnFromTube(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.SpawnFromTube);
        public static void PlayRespawn(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Respawn);
        public static void PlayVictory(this SimpleAnimationPlayer anim) => anim?.Play(PlayerAnimationNames.Victory);
    }
}
