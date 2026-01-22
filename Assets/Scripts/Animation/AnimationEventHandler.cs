using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BobsPetroleum.Animation
{
    /// <summary>
    /// Inspector-based animation trigger system.
    /// Set animation names in inspector and trigger them via code.
    /// No need to mess with animator transitions!
    /// </summary>
    public class AnimationEventHandler : MonoBehaviour
    {
        [Header("Animator")]
        [Tooltip("Animator component (auto-detected if not set)")]
        public Animator animator;

        [Header("Animation Mappings")]
        [Tooltip("Map event names to animation triggers/states")]
        public List<AnimationMapping> animations = new List<AnimationMapping>();

        [Header("Default Animations")]
        [Tooltip("Default idle animation name")]
        public string idleAnimation = "Idle";

        [Tooltip("Play idle on start")]
        public bool playIdleOnStart = true;

        [Header("Events")]
        public UnityEvent<string> onAnimationTriggered;
        public UnityEvent<string> onAnimationComplete;

        private string currentAnimation = "";
        private Dictionary<string, AnimationMapping> animationLookup;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // Build lookup dictionary
            animationLookup = new Dictionary<string, AnimationMapping>();
            foreach (var mapping in animations)
            {
                if (!string.IsNullOrEmpty(mapping.eventName))
                {
                    animationLookup[mapping.eventName] = mapping;
                }
            }
        }

        private void Start()
        {
            if (playIdleOnStart && !string.IsNullOrEmpty(idleAnimation))
            {
                SetAnimation(idleAnimation, true);
            }
        }

        /// <summary>
        /// Trigger a one-shot animation by event name.
        /// </summary>
        public void TriggerAnimation(string eventName)
        {
            if (animator == null) return;

            if (animationLookup.TryGetValue(eventName, out AnimationMapping mapping))
            {
                if (mapping.useTrigger)
                {
                    animator.SetTrigger(mapping.animationName);
                }
                else
                {
                    animator.Play(mapping.animationName, mapping.layer, 0f);
                }

                currentAnimation = eventName;
                onAnimationTriggered?.Invoke(eventName);

                // Invoke events
                mapping.onTrigger?.Invoke();

                // Play sound
                if (mapping.sound != null)
                {
                    AudioSource.PlayClipAtPoint(mapping.sound, transform.position);
                }
            }
            else
            {
                // Try direct trigger
                animator.SetTrigger(eventName);
                currentAnimation = eventName;
                onAnimationTriggered?.Invoke(eventName);
            }
        }

        /// <summary>
        /// Set a looping/state animation.
        /// </summary>
        public void SetAnimation(string eventName, bool value)
        {
            if (animator == null) return;

            if (animationLookup.TryGetValue(eventName, out AnimationMapping mapping))
            {
                if (mapping.useBool)
                {
                    animator.SetBool(mapping.animationName, value);
                }
                else if (value)
                {
                    animator.Play(mapping.animationName, mapping.layer);
                }

                if (value)
                {
                    currentAnimation = eventName;
                }
            }
            else
            {
                // Try direct bool
                animator.SetBool(eventName, value);
                if (value) currentAnimation = eventName;
            }
        }

        /// <summary>
        /// Set an animation parameter (float).
        /// </summary>
        public void SetAnimationFloat(string paramName, float value)
        {
            if (animator == null) return;
            animator.SetFloat(paramName, value);
        }

        /// <summary>
        /// Set an animation parameter (int).
        /// </summary>
        public void SetAnimationInt(string paramName, int value)
        {
            if (animator == null) return;
            animator.SetInteger(paramName, value);
        }

        /// <summary>
        /// Play animation directly by state name.
        /// </summary>
        public void PlayAnimation(string stateName, int layer = 0)
        {
            if (animator == null) return;
            animator.Play(stateName, layer);
            currentAnimation = stateName;
        }

        /// <summary>
        /// Crossfade to animation.
        /// </summary>
        public void CrossfadeAnimation(string stateName, float duration = 0.25f, int layer = 0)
        {
            if (animator == null) return;
            animator.CrossFade(stateName, duration, layer);
            currentAnimation = stateName;
        }

        /// <summary>
        /// Get current animation name.
        /// </summary>
        public string GetCurrentAnimation()
        {
            return currentAnimation;
        }

        /// <summary>
        /// Check if an animation is currently playing.
        /// </summary>
        public bool IsPlaying(string eventName)
        {
            return currentAnimation == eventName;
        }

        /// <summary>
        /// Add an animation mapping at runtime.
        /// </summary>
        public void AddMapping(string eventName, string animationName, bool useTrigger = true)
        {
            var mapping = new AnimationMapping
            {
                eventName = eventName,
                animationName = animationName,
                useTrigger = useTrigger
            };

            animations.Add(mapping);
            animationLookup[eventName] = mapping;
        }

        // Called by animation events
        public void OnAnimationComplete(string animName)
        {
            onAnimationComplete?.Invoke(animName);

            // Find mapping and invoke completion event
            if (animationLookup.TryGetValue(animName, out AnimationMapping mapping))
            {
                mapping.onComplete?.Invoke();
            }
        }
    }

    [System.Serializable]
    public class AnimationMapping
    {
        [Tooltip("Event name to use in code (e.g., 'Attack', 'Jump', 'Hurt')")]
        public string eventName;

        [Tooltip("Actual animation trigger/state name in Animator")]
        public string animationName;

        [Tooltip("Animation layer (usually 0)")]
        public int layer = 0;

        [Tooltip("Use SetTrigger instead of Play")]
        public bool useTrigger = true;

        [Tooltip("Use SetBool for state animations")]
        public bool useBool = false;

        [Tooltip("Sound to play with animation")]
        public AudioClip sound;

        [Tooltip("Event when animation is triggered")]
        public UnityEvent onTrigger;

        [Tooltip("Event when animation completes")]
        public UnityEvent onComplete;
    }
}
