using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// Runtime scene validation and auto-fix utility.
    /// Checks all systems are properly configured.
    /// </summary>
    public class SceneValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        [Tooltip("Run validation on Start")]
        public bool validateOnStart = true;

        [Tooltip("Auto-fix issues when possible")]
        public bool autoFix = true;

        [Tooltip("Log detailed results")]
        public bool verboseLogging = true;

        private List<string> errors = new List<string>();
        private List<string> warnings = new List<string>();
        private List<string> fixes = new List<string>();

        private void Start()
        {
            if (validateOnStart)
            {
                ValidateScene();
            }
        }

        /// <summary>
        /// Validate all scene systems.
        /// </summary>
        [ContextMenu("Validate Scene")]
        public void ValidateScene()
        {
            errors.Clear();
            warnings.Clear();
            fixes.Clear();

            Debug.Log("=== BOB'S PETROLEUM SCENE VALIDATION ===");

            ValidateManagers();
            ValidatePlayer();
            ValidateUI();
            ValidateNavigation();
            ValidateNPCs();
            ValidateLighting();

            PrintResults();
        }

        private void ValidateManagers()
        {
            // GameManager
            var gameManager = FindObjectOfType<Core.GameManager>();
            if (gameManager == null)
            {
                if (autoFix)
                {
                    var obj = new GameObject("GameManager");
                    obj.AddComponent<Core.GameManager>();
                    fixes.Add("Created GameManager");
                }
                else
                {
                    errors.Add("Missing GameManager");
                }
            }

            // AudioManager
            var audioManager = FindObjectOfType<Audio.AudioManager>();
            if (audioManager == null)
            {
                if (autoFix)
                {
                    var obj = new GameObject("AudioManager");
                    obj.AddComponent<Audio.AudioManager>();
                    fixes.Add("Created AudioManager");
                }
                else
                {
                    warnings.Add("Missing AudioManager - no game sounds");
                }
            }

            // DayNightCycle
            var dayNight = FindObjectOfType<Systems.DayNightCycle>();
            if (dayNight == null)
            {
                if (autoFix)
                {
                    var obj = new GameObject("DayNightCycle");
                    obj.AddComponent<Systems.DayNightCycle>();
                    fixes.Add("Created DayNightCycle");
                }
                else
                {
                    warnings.Add("Missing DayNightCycle");
                }
            }

            // HorrorEventsSystem
            var horror = FindObjectOfType<Systems.HorrorEventsSystem>();
            if (horror == null)
            {
                warnings.Add("Missing HorrorEventsSystem - no horror events");
            }

            // QuestSystem
            var quests = FindObjectOfType<Systems.QuestSystem>();
            if (quests == null)
            {
                warnings.Add("Missing QuestSystem - no quests");
            }
        }

        private void ValidatePlayer()
        {
            var player = FindObjectOfType<Player.PlayerController>();
            if (player == null)
            {
                errors.Add("No Player in scene!");
                return;
            }

            // Check CharacterController
            var cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                if (autoFix)
                {
                    cc = player.gameObject.AddComponent<CharacterController>();
                    cc.height = 1.8f;
                    cc.radius = 0.4f;
                    cc.center = new Vector3(0, 0.9f, 0);
                    fixes.Add("Added CharacterController to Player");
                }
                else
                {
                    errors.Add("Player missing CharacterController");
                }
            }

            // Check PlayerHealth
            var health = player.GetComponent<Player.PlayerHealth>();
            if (health == null)
            {
                if (autoFix)
                {
                    player.gameObject.AddComponent<Player.PlayerHealth>();
                    fixes.Add("Added PlayerHealth to Player");
                }
                else
                {
                    warnings.Add("Player missing PlayerHealth");
                }
            }

            // Check Camera
            var cam = player.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                warnings.Add("Player has no Camera child");
            }
            else if (!cam.CompareTag("MainCamera"))
            {
                if (autoFix)
                {
                    cam.tag = "MainCamera";
                    fixes.Add("Set Player camera tag to MainCamera");
                }
                else
                {
                    warnings.Add("Player camera not tagged MainCamera");
                }
            }

            // Check AudioListener
            var listener = player.GetComponentInChildren<AudioListener>();
            if (listener == null)
            {
                warnings.Add("No AudioListener on Player - add to camera");
            }

            // Check tag
            if (!player.CompareTag("Player"))
            {
                if (autoFix)
                {
                    player.tag = "Player";
                    fixes.Add("Set Player tag");
                }
                else
                {
                    warnings.Add("Player not tagged as 'Player'");
                }
            }
        }

        private void ValidateUI()
        {
            // Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                warnings.Add("No Canvas in scene - UI won't work");
            }

            // EventSystem
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                if (autoFix)
                {
                    var obj = new GameObject("EventSystem");
                    obj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    obj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    fixes.Add("Created EventSystem");
                }
                else
                {
                    warnings.Add("No EventSystem - UI interactions won't work");
                }
            }

            // HUDManager
            var hud = FindObjectOfType<UI.HUDManager>();
            if (hud == null)
            {
                warnings.Add("No HUDManager - no player HUD");
            }

            // PauseMenu
            var pause = FindObjectOfType<UI.PauseMenu>();
            if (pause == null)
            {
                warnings.Add("No PauseMenu - can't pause game");
            }
        }

        private void ValidateNavigation()
        {
            // Check for NavMesh
            NavMeshTriangulation navMesh = NavMesh.CalculateTriangulation();
            if (navMesh.vertices.Length == 0)
            {
                warnings.Add("No NavMesh baked - AI won't navigate. Bake NavMesh in Window > AI > Navigation");
            }

            // Check NPCs have agents
            var zombies = FindObjectsOfType<NPC.ZombieAI>();
            foreach (var zombie in zombies)
            {
                var agent = zombie.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    if (autoFix)
                    {
                        agent = zombie.gameObject.AddComponent<NavMeshAgent>();
                        agent.speed = 3.5f;
                        agent.stoppingDistance = 2f;
                        fixes.Add($"Added NavMeshAgent to {zombie.name}");
                    }
                    else
                    {
                        errors.Add($"Zombie '{zombie.name}' missing NavMeshAgent");
                    }
                }
            }

            var customers = FindObjectsOfType<AI.CustomerAI>();
            foreach (var customer in customers)
            {
                var agent = customer.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    if (autoFix)
                    {
                        agent = customer.gameObject.AddComponent<NavMeshAgent>();
                        agent.speed = 2f;
                        agent.stoppingDistance = 1f;
                        fixes.Add($"Added NavMeshAgent to {customer.name}");
                    }
                    else
                    {
                        errors.Add($"Customer '{customer.name}' missing NavMeshAgent");
                    }
                }
            }
        }

        private void ValidateNPCs()
        {
            // Check Animators
            var animationPlayers = FindObjectsOfType<Animation.SimpleAnimationPlayer>();
            foreach (var player in animationPlayers)
            {
                var animator = player.GetComponent<Animator>();
                if (animator == null)
                {
                    if (autoFix)
                    {
                        player.gameObject.AddComponent<Animator>();
                        fixes.Add($"Added Animator to {player.name}");
                    }
                    else
                    {
                        warnings.Add($"SimpleAnimationPlayer '{player.name}' missing Animator");
                    }
                }
                else if (animator.runtimeAnimatorController == null)
                {
                    warnings.Add($"Animator on '{player.name}' has no controller - use RuntimeAnimatorSetup");
                }
            }
        }

        private void ValidateLighting()
        {
            // Check for lights
            var lights = FindObjectsOfType<Light>();
            if (lights.Length == 0)
            {
                warnings.Add("No lights in scene!");
            }

            // Check directional light
            bool hasDirectional = false;
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectional = true;
                    break;
                }
            }

            if (!hasDirectional)
            {
                warnings.Add("No Directional Light - scene may be dark");
            }

            // Check DayNightCycle has sun reference
            var dayNight = FindObjectOfType<Systems.DayNightCycle>();
            if (dayNight != null && dayNight.sunLight == null)
            {
                // Try to auto-find
                foreach (var light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        if (autoFix)
                        {
                            dayNight.sunLight = light;
                            fixes.Add("Assigned sun light to DayNightCycle");
                        }
                        else
                        {
                            warnings.Add("DayNightCycle has no sun light assigned");
                        }
                        break;
                    }
                }
            }
        }

        private void PrintResults()
        {
            Debug.Log($"=== VALIDATION RESULTS ===");

            if (fixes.Count > 0)
            {
                Debug.Log($"<color=cyan>AUTO-FIXES ({fixes.Count}):</color>");
                foreach (var fix in fixes)
                {
                    Debug.Log($"  [FIXED] {fix}");
                }
            }

            if (errors.Count > 0)
            {
                Debug.LogError($"ERRORS ({errors.Count}):");
                foreach (var error in errors)
                {
                    Debug.LogError($"  [ERROR] {error}");
                }
            }

            if (warnings.Count > 0)
            {
                Debug.LogWarning($"WARNINGS ({warnings.Count}):");
                foreach (var warning in warnings)
                {
                    Debug.LogWarning($"  [WARN] {warning}");
                }
            }

            if (errors.Count == 0 && warnings.Count == 0)
            {
                Debug.Log("<color=green>All validations passed! Scene is ready.</color>");
            }
            else if (errors.Count == 0)
            {
                Debug.Log("<color=yellow>No errors, but review warnings above.</color>");
            }
            else
            {
                Debug.LogError($"Scene has {errors.Count} errors that must be fixed!");
            }
        }

        /// <summary>
        /// Static helper to run validation from anywhere.
        /// </summary>
        public static void RunValidation(bool autoFix = true)
        {
            var validator = FindObjectOfType<SceneValidator>();
            if (validator == null)
            {
                var obj = new GameObject("SceneValidator");
                validator = obj.AddComponent<SceneValidator>();
                validator.autoFix = autoFix;
            }
            validator.ValidateScene();
        }

#if UNITY_EDITOR
        [MenuItem("Window/Bob's Petroleum/Validate Scene")]
        public static void MenuValidateScene()
        {
            RunValidation(true);
        }
#endif
    }
}
