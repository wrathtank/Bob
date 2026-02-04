using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// Handles spawning 1-4 Bob clones from tubes at game start.
    /// Plays intro sequence where Bob explains the mission.
    ///
    /// SETUP:
    /// 1. Add this to a manager object
    /// 2. Assign 4 spawn tubes (prefab with door animation)
    /// 3. Assign player prefab
    /// 4. Assign Bob character reference
    /// 5. Done! Players spawn from tubes when joining.
    /// </summary>
    public class CloneSpawnSystem : MonoBehaviour
    {
        public static CloneSpawnSystem Instance { get; private set; }

        [Header("=== SPAWN TUBES ===")]
        [Tooltip("4 tube spawn points - players spawn from these")]
        public SpawnTube[] spawnTubes = new SpawnTube[4];

        [Tooltip("Player prefab to spawn")]
        public GameObject playerPrefab;

        [Tooltip("Time for tube door to open")]
        public float tubeDoorOpenTime = 1.5f;

        [Tooltip("Spawn effect prefab (smoke/particles)")]
        public GameObject spawnEffectPrefab;

        [Header("=== BOB INTRO ===")]
        [Tooltip("Reference to injured Bob")]
        public BobCharacter injuredBob;

        [Tooltip("Play intro sequence on first spawn")]
        public bool playIntroOnFirstSpawn = true;

        [Tooltip("Camera for intro cutscene")]
        public Camera introCutsceneCamera;

        [Tooltip("Intro dialogue lines from Bob")]
        public string[] introDialogue = new string[]
        {
            "*cough* ...you made it...",
            "I'm Bob... and I'm dying...",
            "You're my clones... my only hope...",
            "Run my gas station... earn money...",
            "Buy hamburgers... feed me...",
            "Revive me... and we all survive...",
            "*groans* ...now go... hurry..."
        };

        [Tooltip("Time between dialogue lines")]
        public float dialogueDelay = 2.5f;

        [Header("=== GAME MODE ===")]
        [Tooltip("Current game mode")]
        public GameModeType gameMode = GameModeType.Forever;

        [Tooltip("Starting money for new games")]
        public int startingMoney = 50;

        [Tooltip("Starting hamburgers (gives players a head start)")]
        public int startingHamburgers = 1;

        [Header("=== AUDIO ===")]
        [Tooltip("Tube hiss sound when opening")]
        public AudioClip tubeOpenSound;

        [Tooltip("Clone emerge sound")]
        public AudioClip cloneSpawnSound;

        [Tooltip("Ambient lab music during intro")]
        public AudioClip introMusic;

        [Header("=== EVENTS ===")]
        public UnityEvent onIntroStarted;
        public UnityEvent onIntroComplete;
        public UnityEvent<int> onPlayerSpawned; // player index
        public UnityEvent onAllPlayersSpawned;

        public enum GameModeType
        {
            Forever,
            SevenNightRun
        }

        // Runtime
        private List<GameObject> spawnedPlayers = new List<GameObject>();
        private bool introPlayed = false;
        private int nextTubeIndex = 0;
        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            // Auto-create tube spawn points if not assigned
            EnsureSpawnTubesExist();

            // Auto-find Bob if not assigned
            if (injuredBob == null)
            {
                injuredBob = FindObjectOfType<BobCharacter>();
            }
        }

        #region Public API

        /// <summary>
        /// Spawn a player (call when player joins)
        /// </summary>
        public GameObject SpawnPlayer(int playerIndex = -1)
        {
            if (playerIndex < 0)
            {
                playerIndex = nextTubeIndex;
            }

            if (playerIndex >= spawnTubes.Length)
            {
                Debug.LogWarning("[CloneSpawn] All tubes occupied!");
                return null;
            }

            // Check if first player - play intro
            if (spawnedPlayers.Count == 0 && playIntroOnFirstSpawn && !introPlayed)
            {
                StartCoroutine(PlayIntroThenSpawn(playerIndex));
                return null; // Will spawn after intro
            }

            return SpawnPlayerAtTube(playerIndex);
        }

        /// <summary>
        /// Spawn player immediately at tube (skip intro)
        /// </summary>
        public GameObject SpawnPlayerAtTube(int tubeIndex)
        {
            if (tubeIndex >= spawnTubes.Length || spawnTubes[tubeIndex] == null)
            {
                Debug.LogError($"[CloneSpawn] Invalid tube index: {tubeIndex}");
                return null;
            }

            var tube = spawnTubes[tubeIndex];

            // Open tube door
            StartCoroutine(OpenTubeAndSpawn(tube, tubeIndex));

            return null; // Actual player created in coroutine
        }

        /// <summary>
        /// Start a new game (Forever mode)
        /// </summary>
        public void StartForeverMode()
        {
            gameMode = GameModeType.Forever;
            InitializeNewGame();
        }

        /// <summary>
        /// Start a 7 Night Run
        /// </summary>
        public void StartSevenNightRun()
        {
            gameMode = GameModeType.SevenNightRun;
            InitializeNewGame();

            // Also notify save system
            var saveSystem = Networking.SupabaseSaveSystem.Instance;
            if (saveSystem != null)
            {
                saveSystem.StartNightRun();
            }
        }

        /// <summary>
        /// Load existing game
        /// </summary>
        public void LoadGame()
        {
            var saveSystem = Networking.SupabaseSaveSystem.Instance;
            if (saveSystem != null)
            {
                saveSystem.LoadGame();
            }
        }

        #endregion

        #region Spawning

        private IEnumerator OpenTubeAndSpawn(SpawnTube tube, int tubeIndex)
        {
            // Play tube open sound
            if (tubeOpenSound != null)
            {
                audioSource.PlayOneShot(tubeOpenSound);
            }

            // Animate tube door
            if (tube.doorAnimator != null)
            {
                tube.doorAnimator.SetTrigger("Open");
            }
            else if (tube.doorTransform != null)
            {
                // Simple rotation animation
                yield return StartCoroutine(AnimateDoorOpen(tube.doorTransform));
            }

            yield return new WaitForSeconds(tubeDoorOpenTime * 0.5f);

            // Spawn effect
            if (spawnEffectPrefab != null && tube.spawnPoint != null)
            {
                Instantiate(spawnEffectPrefab, tube.spawnPoint.position, Quaternion.identity);
            }

            // Play spawn sound
            if (cloneSpawnSound != null)
            {
                audioSource.PlayOneShot(cloneSpawnSound);
            }

            // Create player
            GameObject player = null;
            if (playerPrefab != null && tube.spawnPoint != null)
            {
                player = Instantiate(playerPrefab, tube.spawnPoint.position, tube.spawnPoint.rotation);
                player.name = $"BobClone_{tubeIndex + 1}";

                // Give starting resources
                var inventory = player.GetComponent<Player.PlayerInventory>();
                if (inventory != null)
                {
                    inventory.AddMoney(startingMoney);
                    for (int i = 0; i < startingHamburgers; i++)
                    {
                        inventory.AddItem("hamburger", 1);
                    }
                }

                spawnedPlayers.Add(player);
                tube.isOccupied = true;
                nextTubeIndex = tubeIndex + 1;
            }

            yield return new WaitForSeconds(tubeDoorOpenTime * 0.5f);

            // Close tube door
            if (tube.doorAnimator != null)
            {
                tube.doorAnimator.SetTrigger("Close");
            }

            onPlayerSpawned?.Invoke(tubeIndex);

            // Check if this is the last player (for single player or when all join)
            if (spawnedPlayers.Count >= 1)
            {
                // In single player, this is enough
                // In multiplayer, NetworkGameManager handles additional spawns
                onAllPlayersSpawned?.Invoke();
            }

            Debug.Log($"[CloneSpawn] Player {tubeIndex + 1} spawned from tube!");
        }

        private IEnumerator AnimateDoorOpen(Transform door)
        {
            Quaternion startRot = door.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(0, -90, 0);
            float elapsed = 0f;

            while (elapsed < tubeDoorOpenTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / tubeDoorOpenTime;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out
                door.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
        }

        #endregion

        #region Intro Sequence

        private IEnumerator PlayIntroThenSpawn(int playerIndex)
        {
            introPlayed = true;
            onIntroStarted?.Invoke();

            // Disable player controls during intro
            // Switch to cutscene camera
            if (introCutsceneCamera != null)
            {
                introCutsceneCamera.gameObject.SetActive(true);
            }

            // Play intro music
            if (introMusic != null)
            {
                audioSource.clip = introMusic;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Show Bob speaking
            var dialogueSystem = FindObjectOfType<Systems.DialogueSystem>();

            foreach (string line in introDialogue)
            {
                if (dialogueSystem != null)
                {
                    dialogueSystem.ShowDialogue(line, injuredBob?.gameObject);
                }
                else if (injuredBob != null)
                {
                    // Fallback to Bob's speech bubble
                    // Use reflection or direct call if method exists
                    injuredBob.SendMessage("ShowSpeech", line, SendMessageOptions.DontRequireReceiver);
                }

                yield return new WaitForSeconds(dialogueDelay);
            }

            // Fade dialogue
            if (dialogueSystem != null)
            {
                dialogueSystem.HideDialogue();
            }

            yield return new WaitForSeconds(1f);

            // Stop intro music
            if (introMusic != null)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }

            // Switch back to player camera
            if (introCutsceneCamera != null)
            {
                introCutsceneCamera.gameObject.SetActive(false);
            }

            onIntroComplete?.Invoke();

            // Now spawn the player
            yield return StartCoroutine(OpenTubeAndSpawn(spawnTubes[playerIndex], playerIndex));

            Debug.Log("[CloneSpawn] Intro complete, player spawned!");
        }

        /// <summary>
        /// Skip intro (for returning players or loading saves)
        /// </summary>
        public void SkipIntro()
        {
            introPlayed = true;

            if (introCutsceneCamera != null)
            {
                introCutsceneCamera.gameObject.SetActive(false);
            }

            audioSource.Stop();
        }

        #endregion

        #region Game Initialization

        private void InitializeNewGame()
        {
            // Reset game state
            if (injuredBob != null)
            {
                injuredBob.currentHealth = injuredBob.maxHealth * 0.5f; // Start at 50%
                injuredBob.isAlive = true;
                injuredBob.isRevived = false;
            }

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.totalMoneyEarned = 0;
                gm.currentDay = 1;
            }

            // Reset intro
            introPlayed = false;
            spawnedPlayers.Clear();
            nextTubeIndex = 0;

            // Reset tubes
            foreach (var tube in spawnTubes)
            {
                if (tube != null)
                {
                    tube.isOccupied = false;
                }
            }

            Debug.Log($"[CloneSpawn] New {gameMode} game initialized!");
        }

        #endregion

        #region Setup Helpers

        private void EnsureSpawnTubesExist()
        {
            // Create default tube positions if none assigned
            for (int i = 0; i < spawnTubes.Length; i++)
            {
                if (spawnTubes[i] == null)
                {
                    spawnTubes[i] = new SpawnTube();
                }

                if (spawnTubes[i].spawnPoint == null)
                {
                    GameObject tubeObj = new GameObject($"SpawnTube_{i + 1}");
                    tubeObj.transform.SetParent(transform);

                    // Arrange in a row
                    float spacing = 2f;
                    float startX = -((spawnTubes.Length - 1) * spacing) / 2f;
                    tubeObj.transform.localPosition = new Vector3(startX + i * spacing, 0, 0);

                    spawnTubes[i].spawnPoint = tubeObj.transform;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw spawn tubes
            Gizmos.color = Color.cyan;
            for (int i = 0; i < spawnTubes.Length; i++)
            {
                if (spawnTubes[i]?.spawnPoint != null)
                {
                    var pos = spawnTubes[i].spawnPoint.position;
                    Gizmos.DrawWireCube(pos, new Vector3(1, 2, 1));
                    Gizmos.DrawLine(pos, pos + spawnTubes[i].spawnPoint.forward * 1.5f);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(pos + Vector3.up * 2.5f, $"Tube {i + 1}");
#endif
                }
            }

            // Draw line to Bob
            if (injuredBob != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, injuredBob.transform.position);
            }
        }

        #endregion

        #region Properties

        public int SpawnedPlayerCount => spawnedPlayers.Count;
        public int AvailableTubes => spawnTubes.Length - nextTubeIndex;
        public bool IntroPlayed => introPlayed;

        #endregion
    }

    /// <summary>
    /// Represents a single spawn tube
    /// </summary>
    [System.Serializable]
    public class SpawnTube
    {
        [Tooltip("Where player appears")]
        public Transform spawnPoint;

        [Tooltip("Door transform to animate")]
        public Transform doorTransform;

        [Tooltip("Door animator (optional)")]
        public Animator doorAnimator;

        [Tooltip("Tube visual mesh")]
        public GameObject tubeMesh;

        [Tooltip("Is a player using this tube")]
        public bool isOccupied = false;
    }
}
