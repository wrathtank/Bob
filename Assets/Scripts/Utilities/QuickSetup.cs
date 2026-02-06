using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// Quick setup utilities for game objects.
    /// Add this component and use context menu to auto-configure objects.
    /// </summary>
    public class QuickSetup : MonoBehaviour
    {
        public enum SetupType
        {
            None,
            Zombie,
            Customer,
            WildAnimal,
            GasPump,
            Interactable,
            Player
        }

        [Header("Setup Type")]
        public SetupType setupType = SetupType.None;

        [Header("Auto Setup")]
        [Tooltip("Run setup on Awake")]
        public bool setupOnAwake = true;

        private void Awake()
        {
            if (setupOnAwake && setupType != SetupType.None)
            {
                RunSetup();
            }
        }

        /// <summary>
        /// Run auto-setup based on selected type.
        /// </summary>
        [ContextMenu("Run Setup")]
        public void RunSetup()
        {
            switch (setupType)
            {
                case SetupType.Zombie:
                    SetupAsZombie();
                    break;
                case SetupType.Customer:
                    SetupAsCustomer();
                    break;
                case SetupType.WildAnimal:
                    SetupAsWildAnimal();
                    break;
                case SetupType.GasPump:
                    SetupAsGasPump();
                    break;
                case SetupType.Interactable:
                    SetupAsInteractable();
                    break;
                case SetupType.Player:
                    SetupAsPlayer();
                    break;
            }
        }

        #region Setup Methods

        [ContextMenu("Setup As Zombie")]
        public void SetupAsZombie()
        {
            // Add NavMeshAgent if missing
            var agent = GetOrAddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 2f;
            agent.avoidancePriority = 50;

            // Add collider if missing
            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<CapsuleCollider>();
                col.height = 2f;
                col.radius = 0.5f;
                col.center = new Vector3(0, 1f, 0);
            }

            // Add ZombieAI
            GetOrAddComponent<NPC.ZombieAI>();

            // Add Animator and SimpleAnimationPlayer
            GetOrAddComponent<Animator>();
            GetOrAddComponent<Animation.SimpleAnimationPlayer>();

            // Set layer
            SetLayerIfExists("Enemy");

            Debug.Log($"[QuickSetup] {gameObject.name} setup as Zombie");
        }

        [ContextMenu("Setup As Customer")]
        public void SetupAsCustomer()
        {
            var agent = GetOrAddComponent<NavMeshAgent>();
            agent.speed = 2f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 1f;

            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<CapsuleCollider>();
                col.height = 1.8f;
                col.radius = 0.4f;
                col.center = new Vector3(0, 0.9f, 0);
            }

            GetOrAddComponent<AI.CustomerAI>();
            GetOrAddComponent<Animator>();
            GetOrAddComponent<Animation.SimpleAnimationPlayer>();

            SetLayerIfExists("NPC");

            Debug.Log($"[QuickSetup] {gameObject.name} setup as Customer");
        }

        [ContextMenu("Setup As Wild Animal")]
        public void SetupAsWildAnimal()
        {
            var agent = GetOrAddComponent<NavMeshAgent>();
            agent.speed = 4f;
            agent.angularSpeed = 180f;
            agent.acceleration = 10f;
            agent.stoppingDistance = 0.5f;

            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<CapsuleCollider>();
                col.height = 1f;
                col.radius = 0.3f;
                col.center = new Vector3(0, 0.5f, 0);
            }

            GetOrAddComponent<NPC.WanderingAnimalAI>();
            GetOrAddComponent<Animator>();
            GetOrAddComponent<Animation.SimpleAnimationPlayer>();

            Debug.Log($"[QuickSetup] {gameObject.name} setup as Wild Animal");
        }

        [ContextMenu("Setup As Gas Pump")]
        public void SetupAsGasPump()
        {
            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(1f, 2f, 0.5f);
                col.center = new Vector3(0, 1f, 0);
            }

            GetOrAddComponent<Economy.GasPump>();

            // Create interaction point if missing
            Transform interactPoint = transform.Find("InteractPoint");
            if (interactPoint == null)
            {
                GameObject point = new GameObject("InteractPoint");
                point.transform.SetParent(transform);
                point.transform.localPosition = new Vector3(1f, 0, 0);
            }

            SetLayerIfExists("Interactable");

            Debug.Log($"[QuickSetup] {gameObject.name} setup as Gas Pump");
        }

        [ContextMenu("Setup As Interactable")]
        public void SetupAsInteractable()
        {
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            SetLayerIfExists("Interactable");

            Debug.Log($"[QuickSetup] {gameObject.name} setup as Interactable");
        }

        [ContextMenu("Setup As Player")]
        public void SetupAsPlayer()
        {
            gameObject.tag = "Player";

            // Add CharacterController
            var cc = GetOrAddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0, 0.9f, 0);

            // Add player components
            GetOrAddComponent<Player.PlayerController>();
            GetOrAddComponent<Player.PlayerHealth>();
            GetOrAddComponent<Player.PlayerInventory>();

            // Find or create camera
            Camera cam = GetComponentInChildren<Camera>();
            if (cam == null)
            {
                GameObject camHolder = new GameObject("CameraHolder");
                camHolder.transform.SetParent(transform);
                camHolder.transform.localPosition = new Vector3(0, 1.6f, 0);
                cam = camHolder.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camHolder.AddComponent<AudioListener>();
            }

            // Find or create flashlight
            Player.Flashlight flashlight = GetComponentInChildren<Player.Flashlight>();
            if (flashlight == null)
            {
                GameObject lightObj = new GameObject("Flashlight");
                lightObj.transform.SetParent(cam.transform);
                lightObj.transform.localPosition = Vector3.zero;
                lightObj.AddComponent<Player.Flashlight>();
            }

            SetLayerIfExists("Player");

            Debug.Log($"[QuickSetup] {gameObject.name} setup as Player");
        }

        #endregion

        #region Utilities

        private T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        private void SetLayerIfExists(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1)
            {
                gameObject.layer = layer;
            }
        }

        #endregion

#if UNITY_EDITOR
        /// <summary>
        /// Editor shortcut to setup selected objects.
        /// </summary>
        [MenuItem("GameObject/Bob's Petroleum/Quick Setup Selected", false, 0)]
        public static void QuickSetupSelected()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var setup = obj.GetComponent<QuickSetup>();
                if (setup == null)
                {
                    setup = obj.AddComponent<QuickSetup>();
                }
                setup.RunSetup();
            }
        }
#endif
    }
}
