using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BobsPetroleum.Battle
{
    /// <summary>
    /// Pet animation controller for quadrupeds (rats, dogs, cats, demonic versions).
    /// All animation names configurable in inspector - just type the name!
    /// Works with SimpleAnimationPlayer or Animator.
    /// </summary>
    public class PetAnimationController : MonoBehaviour
    {
        [Header("Animation System")]
        [Tooltip("Use SimpleAnimationPlayer (recommended for Meshy)")]
        public Animation.SimpleAnimationPlayer simpleAnimator;

        [Tooltip("Or use standard Animator")]
        public Animator animator;

        [Header("Pet Type")]
        public PetAnimalType animalType = PetAnimalType.Dog;

        [Tooltip("Is this a demonic version?")]
        public bool isDemonic = false;

        [Header("=== MOVEMENT ANIMATIONS ===")]
        [Tooltip("Idle animation name")]
        public string idleAnimation = "Idle";

        [Tooltip("Walk animation name")]
        public string walkAnimation = "Walk";

        [Tooltip("Run animation name")]
        public string runAnimation = "Run";

        [Tooltip("Jump animation name")]
        public string jumpAnimation = "Jump";

        [Header("=== COMBAT ANIMATIONS ===")]
        [Tooltip("Basic attack animation")]
        public string attackAnimation = "Attack";

        [Tooltip("Heavy/special attack")]
        public string heavyAttackAnimation = "HeavyAttack";

        [Tooltip("Bite attack")]
        public string biteAnimation = "Bite";

        [Tooltip("Claw/scratch attack")]
        public string clawAnimation = "Claw";

        [Tooltip("Tail whip attack")]
        public string tailWhipAnimation = "TailWhip";

        [Tooltip("Pounce/lunge attack")]
        public string pounceAnimation = "Pounce";

        [Header("=== DEMONIC SPECIAL ATTACKS ===")]
        [Tooltip("Demonic fire breath")]
        public string fireBreathAnimation = "FireBreath";

        [Tooltip("Demonic dark pulse")]
        public string darkPulseAnimation = "DarkPulse";

        [Tooltip("Demonic possession attack")]
        public string possessionAnimation = "Possession";

        [Tooltip("Demonic transformation")]
        public string transformAnimation = "Transform";

        [Header("=== REACTION ANIMATIONS ===")]
        [Tooltip("Take damage/hurt")]
        public string hurtAnimation = "Hurt";

        [Tooltip("Death animation")]
        public string deathAnimation = "Death";

        [Tooltip("Faint/KO animation")]
        public string faintAnimation = "Faint";

        [Tooltip("Dodge animation")]
        public string dodgeAnimation = "Dodge";

        [Header("=== EMOTION ANIMATIONS ===")]
        [Tooltip("Happy/victory")]
        public string happyAnimation = "Happy";

        [Tooltip("Angry/aggressive")]
        public string angryAnimation = "Angry";

        [Tooltip("Scared animation")]
        public string scaredAnimation = "Scared";

        [Tooltip("Sleep animation")]
        public string sleepAnimation = "Sleep";

        [Tooltip("Eat animation")]
        public string eatAnimation = "Eat";

        [Header("=== SPECIAL ANIMATIONS ===")]
        [Tooltip("Being captured by net")]
        public string capturedAnimation = "Captured";

        [Tooltip("Entering battle")]
        public string enterBattleAnimation = "EnterBattle";

        [Tooltip("Victory pose")]
        public string victoryAnimation = "Victory";

        [Tooltip("Level up")]
        public string levelUpAnimation = "LevelUp";

        [Header("=== MOVE-SPECIFIC ANIMATIONS ===")]
        [Tooltip("Custom move animations - add your own!")]
        public List<MoveAnimation> customMoveAnimations = new List<MoveAnimation>();

        [Header("Animation Settings")]
        [Tooltip("Default crossfade duration")]
        public float crossfadeDuration = 0.2f;

        [Tooltip("Return to idle after attack")]
        public bool returnToIdleAfterAttack = true;

        [Tooltip("Attack animation duration")]
        public float attackDuration = 0.5f;

        [Header("Events")]
        public UnityEvent<string> onAnimationStarted;
        public UnityEvent<string> onAnimationComplete;
        public UnityEvent onAttackHit; // Trigger at impact frame

        // State
        private string currentAnimation;
        private bool isPlayingOneShot = false;

        private void Awake()
        {
            // Auto-find animation components
            if (simpleAnimator == null)
            {
                simpleAnimator = GetComponent<Animation.SimpleAnimationPlayer>();
            }

            if (animator == null && simpleAnimator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        private void Start()
        {
            // Start in idle
            PlayIdle();
        }

        #region Movement Animations

        public void PlayIdle()
        {
            PlayAnimation(idleAnimation, true);
        }

        public void PlayWalk()
        {
            PlayAnimation(walkAnimation, true);
        }

        public void PlayRun()
        {
            PlayAnimation(runAnimation, true);
        }

        public void PlayJump()
        {
            PlayAnimationOnce(jumpAnimation);
        }

        #endregion

        #region Combat Animations

        public void PlayAttack()
        {
            PlayAnimationOnce(attackAnimation, returnToIdleAfterAttack);
        }

        public void PlayHeavyAttack()
        {
            PlayAnimationOnce(heavyAttackAnimation, returnToIdleAfterAttack);
        }

        public void PlayBite()
        {
            PlayAnimationOnce(biteAnimation, returnToIdleAfterAttack);
        }

        public void PlayClaw()
        {
            PlayAnimationOnce(clawAnimation, returnToIdleAfterAttack);
        }

        public void PlayTailWhip()
        {
            PlayAnimationOnce(tailWhipAnimation, returnToIdleAfterAttack);
        }

        public void PlayPounce()
        {
            PlayAnimationOnce(pounceAnimation, returnToIdleAfterAttack);
        }

        // Demonic specials
        public void PlayFireBreath()
        {
            if (!isDemonic) return;
            PlayAnimationOnce(fireBreathAnimation, returnToIdleAfterAttack);
        }

        public void PlayDarkPulse()
        {
            if (!isDemonic) return;
            PlayAnimationOnce(darkPulseAnimation, returnToIdleAfterAttack);
        }

        public void PlayPossession()
        {
            if (!isDemonic) return;
            PlayAnimationOnce(possessionAnimation, returnToIdleAfterAttack);
        }

        public void PlayTransform()
        {
            PlayAnimationOnce(transformAnimation, false);
        }

        /// <summary>
        /// Play a move by name (looks up in custom moves first).
        /// </summary>
        public void PlayMove(string moveName)
        {
            // Check custom moves
            foreach (var move in customMoveAnimations)
            {
                if (move.moveName.Equals(moveName, System.StringComparison.OrdinalIgnoreCase))
                {
                    PlayAnimationOnce(move.animationName, returnToIdleAfterAttack);
                    return;
                }
            }

            // Try standard attacks
            switch (moveName.ToLower())
            {
                case "attack":
                case "tackle":
                    PlayAttack();
                    break;
                case "bite":
                    PlayBite();
                    break;
                case "claw":
                case "scratch":
                    PlayClaw();
                    break;
                case "tailwhip":
                case "tail":
                    PlayTailWhip();
                    break;
                case "pounce":
                case "lunge":
                    PlayPounce();
                    break;
                case "firebreath":
                case "fire":
                    PlayFireBreath();
                    break;
                case "darkpulse":
                case "dark":
                    PlayDarkPulse();
                    break;
                default:
                    PlayAttack(); // Default
                    break;
            }
        }

        #endregion

        #region Reaction Animations

        public void PlayHurt()
        {
            PlayAnimationOnce(hurtAnimation, true);
        }

        public void PlayDeath()
        {
            PlayAnimation(deathAnimation, false);
        }

        public void PlayFaint()
        {
            PlayAnimation(faintAnimation, false);
        }

        public void PlayDodge()
        {
            PlayAnimationOnce(dodgeAnimation, true);
        }

        #endregion

        #region Emotion Animations

        public void PlayHappy()
        {
            PlayAnimationOnce(happyAnimation, true);
        }

        public void PlayAngry()
        {
            PlayAnimation(angryAnimation, true);
        }

        public void PlayScared()
        {
            PlayAnimationOnce(scaredAnimation, true);
        }

        public void PlaySleep()
        {
            PlayAnimation(sleepAnimation, true);
        }

        public void PlayEat()
        {
            PlayAnimationOnce(eatAnimation, true);
        }

        #endregion

        #region Special Animations

        public void PlayCaptured()
        {
            PlayAnimation(capturedAnimation, false);
        }

        public void PlayEnterBattle()
        {
            PlayAnimationOnce(enterBattleAnimation, true);
        }

        public void PlayVictory()
        {
            PlayAnimation(victoryAnimation, true);
        }

        public void PlayLevelUp()
        {
            PlayAnimationOnce(levelUpAnimation, true);
        }

        #endregion

        #region Core Playback

        /// <summary>
        /// Play animation by name.
        /// </summary>
        public void PlayAnimation(string animName, bool loop = false)
        {
            if (string.IsNullOrEmpty(animName)) return;

            currentAnimation = animName;

            if (simpleAnimator != null)
            {
                if (loop)
                {
                    simpleAnimator.PlayLooped(animName, crossfadeDuration);
                }
                else
                {
                    simpleAnimator.Play(animName, crossfadeDuration);
                }
            }
            else if (animator != null)
            {
                animator.CrossFade(animName, crossfadeDuration);
            }

            onAnimationStarted?.Invoke(animName);
        }

        /// <summary>
        /// Play animation once then return to idle.
        /// </summary>
        public void PlayAnimationOnce(string animName, bool returnToIdle = true)
        {
            if (string.IsNullOrEmpty(animName)) return;
            if (isPlayingOneShot) return;

            StartCoroutine(PlayOneShotCoroutine(animName, returnToIdle));
        }

        private System.Collections.IEnumerator PlayOneShotCoroutine(string animName, bool returnToIdle)
        {
            isPlayingOneShot = true;

            PlayAnimation(animName, false);

            // Wait for animation
            yield return new WaitForSeconds(attackDuration);

            onAnimationComplete?.Invoke(animName);

            if (returnToIdle)
            {
                PlayIdle();
            }

            isPlayingOneShot = false;
        }

        /// <summary>
        /// Trigger attack hit event (call from animation event or timer).
        /// </summary>
        public void TriggerAttackHit()
        {
            onAttackHit?.Invoke();
        }

        #endregion

        #region Properties

        public string CurrentAnimation => currentAnimation;
        public bool IsPlayingOneShot => isPlayingOneShot;

        #endregion
    }

    [System.Serializable]
    public class MoveAnimation
    {
        [Tooltip("Move name to match")]
        public string moveName;

        [Tooltip("Animation to play")]
        public string animationName;

        [Tooltip("Duration override (0 = use default)")]
        public float duration = 0f;

        [Tooltip("Particle effect for this move")]
        public ParticleSystem effect;

        [Tooltip("Sound for this move")]
        public AudioClip sound;
    }

    public enum PetAnimalType
    {
        Rat,
        Dog,
        Cat,
        DemonicRat,
        DemonicDog,
        DemonicCat
    }
}
