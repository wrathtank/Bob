using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace BobsPetroleum.Animation
{
    /// <summary>
    /// Automatically creates a simple animator controller at runtime or in editor.
    /// Eliminates the need to manually set up state machines for simple animation playback.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RuntimeAnimatorSetup : MonoBehaviour
    {
        [Header("Setup Options")]
        [Tooltip("Create controller on Awake if none exists")]
        public bool autoSetup = true;

        [Tooltip("Default clip to use (optional)")]
        public AnimationClip defaultClip;

        [Header("Generated Controller")]
        [Tooltip("The runtime-generated controller (read-only)")]
        public RuntimeAnimatorController generatedController;

        private Animator animator;

        private void Awake()
        {
            animator = GetComponent<Animator>();

            if (autoSetup && animator.runtimeAnimatorController == null)
            {
                SetupRuntimeController();
            }
        }

        /// <summary>
        /// Create a minimal animator controller at runtime.
        /// </summary>
        public void SetupRuntimeController()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // Create override controller from a minimal base
            // Note: In builds, we need a pre-made controller asset to override
            // This is mainly for editor workflow

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                CreateEditorController();
                return;
            }
#endif

            // Runtime: Need a base controller to override
            // If none exists, we can still use Play() with state names
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"RuntimeAnimatorSetup on {gameObject.name}: No base controller. " +
                    "For runtime setup, assign a simple base controller or use Legacy Animation component.");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Create a simple controller in the editor (not at runtime).
        /// </summary>
        [ContextMenu("Create Simple Controller")]
        public void CreateEditorController()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            // Create controller asset
            string path = $"Assets/GeneratedAnimators/{gameObject.name}_Controller.controller";

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/GeneratedAnimators"))
            {
                AssetDatabase.CreateFolder("Assets", "GeneratedAnimators");
            }

            // Create the controller
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // Get the root state machine
            var rootStateMachine = controller.layers[0].stateMachine;

            // Add default state
            AnimatorState defaultState;
            if (defaultClip != null)
            {
                defaultState = rootStateMachine.AddState("Default");
                defaultState.motion = defaultClip;
            }
            else
            {
                defaultState = rootStateMachine.AddState("Idle");
            }

            // Make it the default state
            rootStateMachine.defaultState = defaultState;

            // Add a generic "Action" state for playing one-shots
            var actionState = rootStateMachine.AddState("Action");

            // Add transition from Action back to Default (on exit)
            var returnTransition = actionState.AddTransition(defaultState);
            returnTransition.hasExitTime = true;
            returnTransition.exitTime = 1f;
            returnTransition.duration = 0.1f;

            // Add trigger parameter
            controller.AddParameter("PlayAction", AnimatorControllerParameterType.Trigger);

            // Add transition from Default to Action on trigger
            var toActionTransition = defaultState.AddTransition(actionState);
            toActionTransition.AddCondition(AnimatorConditionMode.If, 0, "PlayAction");
            toActionTransition.duration = 0.1f;
            toActionTransition.hasExitTime = false;

            // Assign to animator
            animator.runtimeAnimatorController = controller;
            generatedController = controller;

            // Save
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(gameObject);

            Debug.Log($"Created simple animator controller at {path}");
        }

        /// <summary>
        /// Add a state for each assigned clip in SimpleAnimationPlayer.
        /// </summary>
        [ContextMenu("Create Controller From Clips")]
        public void CreateControllerFromClips()
        {
            var simplePlayer = GetComponent<SimpleAnimationPlayer>();
            if (simplePlayer == null || simplePlayer.animationClips.Count == 0)
            {
                Debug.LogWarning("No SimpleAnimationPlayer with clips found on this object.");
                return;
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            string path = $"Assets/GeneratedAnimators/{gameObject.name}_Controller.controller";

            if (!AssetDatabase.IsValidFolder("Assets/GeneratedAnimators"))
            {
                AssetDatabase.CreateFolder("Assets", "GeneratedAnimators");
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var rootStateMachine = controller.layers[0].stateMachine;

            AnimatorState firstState = null;

            // Add state for each clip
            foreach (var entry in simplePlayer.animationClips)
            {
                if (entry.clip == null) continue;

                var state = rootStateMachine.AddState(entry.animationName);
                state.motion = entry.clip;

                if (firstState == null)
                {
                    firstState = state;
                    rootStateMachine.defaultState = state;
                }
            }

            animator.runtimeAnimatorController = controller;
            generatedController = controller;

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(gameObject);

            Debug.Log($"Created controller with {simplePlayer.animationClips.Count} states at {path}");
        }
#endif
    }
}
