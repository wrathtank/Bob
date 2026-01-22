using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BobsPetroleum.Animation
{
    /// <summary>
    /// Simple animation player that plays clips directly by name.
    /// No state machines, no transitions - just assign clips in inspector and play by name.
    /// Perfect for Meshy/Mixamo animations.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class SimpleAnimationPlayer : MonoBehaviour
    {
        [Header("Animation Clips")]
        [Tooltip("Assign your Meshy/Mixamo animation clips here")]
        public List<AnimationClipEntry> animationClips = new List<AnimationClipEntry>();

        [Header("Settings")]
        [Tooltip("Default crossfade duration")]
        public float defaultCrossfadeDuration = 0.2f;

        [Tooltip("Default animation to play on start")]
        public string defaultAnimation = "Idle";

        [Tooltip("Play default animation on start")]
        public bool playOnStart = true;

        [Header("Root Motion")]
        [Tooltip("Apply root motion from animations")]
        public bool applyRootMotion = false;

        [Header("Events")]
        public UnityEvent<string> onAnimationStarted;
        public UnityEvent<string> onAnimationFinished;

        // Runtime
        private Animator animator;
        private AnimatorOverrideController overrideController;
        private Dictionary<string, AnimationClipEntry> clipLookup = new Dictionary<string, AnimationClipEntry>();
        private string currentAnimation = "";
        private string previousAnimation = "";
        private float currentAnimationLength = 0f;
        private float animationTimer = 0f;
        private bool isPlaying = false;

        // Placeholder clip name in base controller
        private const string PLACEHOLDER_STATE = "Placeholder";

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = applyRootMotion;

            // Build lookup
            foreach (var entry in animationClips)
            {
                if (entry.clip != null && !string.IsNullOrEmpty(entry.animationName))
                {
                    clipLookup[entry.animationName.ToLower()] = entry;
                }
            }

            // Setup override controller for direct clip playback
            SetupOverrideController();
        }

        private void Start()
        {
            if (playOnStart && !string.IsNullOrEmpty(defaultAnimation))
            {
                Play(defaultAnimation);
            }
        }

        private void Update()
        {
            // Track animation completion
            if (isPlaying && currentAnimationLength > 0)
            {
                animationTimer += Time.deltaTime;

                if (animationTimer >= currentAnimationLength)
                {
                    OnAnimationComplete();
                }
            }
        }

        private void SetupOverrideController()
        {
            // Create a simple runtime animator override controller
            // This allows us to swap clips without complex state machines

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"SimpleAnimationPlayer on {gameObject.name}: No Animator Controller assigned. " +
                    "Create a simple controller with one state for this to work, or use PlayDirect().");
                return;
            }

            overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;
        }

        #region Play Methods

        /// <summary>
        /// Play animation by name with crossfade.
        /// </summary>
        public void Play(string animationName, float crossfadeDuration = -1f)
        {
            if (animator == null) return;

            string lookupName = animationName.ToLower();

            if (clipLookup.TryGetValue(lookupName, out AnimationClipEntry entry))
            {
                PlayClip(entry, crossfadeDuration >= 0 ? crossfadeDuration : defaultCrossfadeDuration);
            }
            else
            {
                // Try to play directly as state name
                float duration = crossfadeDuration >= 0 ? crossfadeDuration : defaultCrossfadeDuration;
                animator.CrossFade(animationName, duration, 0);
                currentAnimation = animationName;
                onAnimationStarted?.Invoke(animationName);
            }
        }

        /// <summary>
        /// Play animation instantly (no crossfade).
        /// </summary>
        public void PlayInstant(string animationName)
        {
            Play(animationName, 0f);
        }

        /// <summary>
        /// Play animation by name, looping.
        /// </summary>
        public void PlayLooped(string animationName, float crossfadeDuration = -1f)
        {
            if (clipLookup.TryGetValue(animationName.ToLower(), out AnimationClipEntry entry))
            {
                // Ensure clip is set to loop
                if (entry.clip != null)
                {
                    entry.clip.wrapMode = WrapMode.Loop;
                }
                PlayClip(entry, crossfadeDuration >= 0 ? crossfadeDuration : defaultCrossfadeDuration, true);
            }
        }

        /// <summary>
        /// Play animation once then return to default.
        /// </summary>
        public void PlayOnce(string animationName, float crossfadeDuration = -1f)
        {
            if (clipLookup.TryGetValue(animationName.ToLower(), out AnimationClipEntry entry))
            {
                PlayClip(entry, crossfadeDuration >= 0 ? crossfadeDuration : defaultCrossfadeDuration, false);
            }
        }

        private void PlayClip(AnimationClipEntry entry, float crossfadeDuration, bool loop = false)
        {
            if (entry.clip == null || animator == null) return;

            previousAnimation = currentAnimation;
            currentAnimation = entry.animationName;
            currentAnimationLength = loop ? 0f : entry.clip.length; // 0 = don't track completion for loops
            animationTimer = 0f;
            isPlaying = true;

            // Use override controller to swap the clip
            if (overrideController != null)
            {
                // Find a state to override
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);

                if (overrides.Count > 0)
                {
                    // Override first clip
                    overrideController[overrides[0].Key] = entry.clip;
                    animator.CrossFade(overrides[0].Key.name, crossfadeDuration, 0, 0f);
                }
                else
                {
                    // Fallback to direct play
                    animator.Play(entry.clip.name, 0, 0f);
                }
            }
            else
            {
                // Direct crossfade to state name
                animator.CrossFade(entry.clip.name, crossfadeDuration, 0);
            }

            // Play sound if configured
            if (entry.sound != null)
            {
                AudioSource.PlayClipAtPoint(entry.sound, transform.position, entry.soundVolume);
            }

            // Fire event
            entry.onPlay?.Invoke();
            onAnimationStarted?.Invoke(entry.animationName);
        }

        /// <summary>
        /// Play animation clip directly (bypasses lookup).
        /// </summary>
        public void PlayDirect(AnimationClip clip, float crossfadeDuration = -1f)
        {
            if (clip == null || animator == null) return;

            float duration = crossfadeDuration >= 0 ? crossfadeDuration : defaultCrossfadeDuration;

            previousAnimation = currentAnimation;
            currentAnimation = clip.name;
            currentAnimationLength = clip.length;
            animationTimer = 0f;
            isPlaying = true;

            if (overrideController != null)
            {
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);

                if (overrides.Count > 0)
                {
                    overrideController[overrides[0].Key] = clip;
                    animator.CrossFade(overrides[0].Key.name, duration, 0, 0f);
                }
            }
            else
            {
                animator.CrossFade(clip.name, duration, 0);
            }

            onAnimationStarted?.Invoke(clip.name);
        }

        #endregion

        #region State Queries

        /// <summary>
        /// Get currently playing animation name.
        /// </summary>
        public string CurrentAnimation => currentAnimation;

        /// <summary>
        /// Check if specific animation is playing.
        /// </summary>
        public bool IsPlaying(string animationName)
        {
            return currentAnimation.Equals(animationName, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if any animation is actively playing.
        /// </summary>
        public bool IsAnimating => isPlaying;

        /// <summary>
        /// Get normalized time of current animation (0-1).
        /// </summary>
        public float NormalizedTime
        {
            get
            {
                if (currentAnimationLength <= 0) return 0f;
                return Mathf.Clamp01(animationTimer / currentAnimationLength);
            }
        }

        #endregion

        #region Speed Control

        /// <summary>
        /// Set animation playback speed.
        /// </summary>
        public void SetSpeed(float speed)
        {
            if (animator != null)
            {
                animator.speed = speed;
            }
        }

        /// <summary>
        /// Pause animation.
        /// </summary>
        public void Pause()
        {
            SetSpeed(0f);
        }

        /// <summary>
        /// Resume animation.
        /// </summary>
        public void Resume()
        {
            SetSpeed(1f);
        }

        #endregion

        #region Callbacks

        private void OnAnimationComplete()
        {
            isPlaying = false;

            var entry = GetEntry(currentAnimation);
            entry?.onComplete?.Invoke();
            onAnimationFinished?.Invoke(currentAnimation);

            // Return to default if configured
            if (entry != null && entry.returnToDefault && !string.IsNullOrEmpty(defaultAnimation))
            {
                Play(defaultAnimation);
            }
        }

        private AnimationClipEntry GetEntry(string animationName)
        {
            clipLookup.TryGetValue(animationName.ToLower(), out AnimationClipEntry entry);
            return entry;
        }

        #endregion

        #region Runtime Clip Management

        /// <summary>
        /// Add a clip at runtime.
        /// </summary>
        public void AddClip(string name, AnimationClip clip)
        {
            if (clip == null) return;

            var entry = new AnimationClipEntry
            {
                animationName = name,
                clip = clip
            };

            animationClips.Add(entry);
            clipLookup[name.ToLower()] = entry;
        }

        /// <summary>
        /// Remove a clip at runtime.
        /// </summary>
        public void RemoveClip(string name)
        {
            string key = name.ToLower();
            if (clipLookup.ContainsKey(key))
            {
                clipLookup.Remove(key);
                animationClips.RemoveAll(e => e.animationName.ToLower() == key);
            }
        }

        /// <summary>
        /// Check if animation exists.
        /// </summary>
        public bool HasAnimation(string name)
        {
            return clipLookup.ContainsKey(name.ToLower());
        }

        /// <summary>
        /// Get all available animation names.
        /// </summary>
        public List<string> GetAnimationNames()
        {
            List<string> names = new List<string>();
            foreach (var entry in animationClips)
            {
                names.Add(entry.animationName);
            }
            return names;
        }

        #endregion
    }

    [System.Serializable]
    public class AnimationClipEntry
    {
        [Tooltip("Name to use when calling Play() - e.g., 'Walk', 'Attack', 'Die'")]
        public string animationName;

        [Tooltip("The animation clip from Meshy/Mixamo")]
        public AnimationClip clip;

        [Header("Options")]
        [Tooltip("Return to default animation after this one completes")]
        public bool returnToDefault = false;

        [Header("Audio")]
        [Tooltip("Sound to play with this animation")]
        public AudioClip sound;

        [Range(0f, 1f)]
        public float soundVolume = 1f;

        [Header("Events")]
        public UnityEvent onPlay;
        public UnityEvent onComplete;
    }
}
