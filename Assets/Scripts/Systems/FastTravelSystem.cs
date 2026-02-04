using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Fast travel system using subway stations.
    /// Find the pipe to unlock, then travel between stations!
    /// </summary>
    public class FastTravelSystem : MonoBehaviour
    {
        public static FastTravelSystem Instance { get; private set; }

        [Header("Unlock Requirement")]
        [Tooltip("Item ID required to unlock fast travel")]
        public string unlockItemId = "subway_pipe";

        [Tooltip("Is fast travel unlocked?")]
        public bool isUnlocked = false;

        [Header("Fast Travel Stations")]
        [Tooltip("All subway stations in the world")]
        public List<SubwayStation> stations = new List<SubwayStation>();

        [Header("UI")]
        [Tooltip("Fast travel panel")]
        public GameObject fastTravelPanel;

        [Tooltip("Destination list container")]
        public Transform destinationListContainer;

        [Tooltip("Destination button prefab")]
        public GameObject destinationButtonPrefab;

        [Tooltip("Current station text")]
        public TMP_Text currentStationText;

        [Tooltip("Status text")]
        public TMP_Text statusText;

        [Header("Transition")]
        [Tooltip("Fade image for transition")]
        public Image fadeImage;

        [Tooltip("Fade duration")]
        public float fadeDuration = 1f;

        [Tooltip("Loading text")]
        public TMP_Text loadingText;

        [Header("Audio")]
        public AudioClip stationAmbience;
        public AudioClip travelSound;
        public AudioClip arriveSound;
        public AudioClip unlockSound;

        [Header("Events")]
        public UnityEvent onFastTravelUnlocked;
        public UnityEvent<SubwayStation, SubwayStation> onFastTravel;

        // State
        private SubwayStation currentStation;
        private bool isPanelOpen = false;
        private bool isTraveling = false;
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
            // Load unlock state
            isUnlocked = PlayerPrefs.GetInt("FastTravelUnlocked", 0) == 1;

            // Hide panel
            if (fastTravelPanel != null)
            {
                fastTravelPanel.SetActive(false);
            }

            // Register all stations
            AutoFindStations();
        }

        #region Unlock System

        /// <summary>
        /// Try to unlock fast travel with the pipe.
        /// </summary>
        public bool TryUnlock()
        {
            if (isUnlocked) return true;

            // Check for item
            var consumables = Items.ConsumableSystem.Instance;
            var playerInventory = FindObjectOfType<Player.PlayerInventory>();

            bool hasItem = false;

            if (consumables != null && consumables.HasItem(unlockItemId))
            {
                consumables.RemoveItem(unlockItemId, 1);
                hasItem = true;
            }
            else if (playerInventory != null && playerInventory.HasItem(unlockItemId))
            {
                playerInventory.RemoveItem(unlockItemId, 1);
                hasItem = true;
            }

            if (hasItem)
            {
                Unlock();
                return true;
            }

            UI.HUDManager.Instance?.ShowNotification("You need to find the Subway Pipe first!");
            return false;
        }

        /// <summary>
        /// Unlock fast travel.
        /// </summary>
        public void Unlock()
        {
            isUnlocked = true;
            PlayerPrefs.SetInt("FastTravelUnlocked", 1);
            PlayerPrefs.Save();

            PlaySound(unlockSound);
            UI.HUDManager.Instance?.ShowNotification("Fast travel unlocked! You can now use the subway.");

            onFastTravelUnlocked?.Invoke();
        }

        #endregion

        #region Station Management

        private void AutoFindStations()
        {
            // Find all stations in scene
            var allStations = FindObjectsOfType<SubwayStation>();
            foreach (var station in allStations)
            {
                if (!stations.Contains(station))
                {
                    stations.Add(station);
                }
            }
        }

        /// <summary>
        /// Register a station.
        /// </summary>
        public void RegisterStation(SubwayStation station)
        {
            if (!stations.Contains(station))
            {
                stations.Add(station);
            }
        }

        /// <summary>
        /// Get station by name.
        /// </summary>
        public SubwayStation GetStation(string stationName)
        {
            foreach (var station in stations)
            {
                if (station.stationName == stationName)
                {
                    return station;
                }
            }
            return null;
        }

        #endregion

        #region Fast Travel UI

        /// <summary>
        /// Open fast travel menu at a station.
        /// </summary>
        public void OpenFastTravelMenu(SubwayStation fromStation)
        {
            if (!isUnlocked)
            {
                UI.HUDManager.Instance?.ShowNotification("Fast travel not unlocked! Find the Subway Pipe.");
                return;
            }

            if (isTraveling) return;

            currentStation = fromStation;
            isPanelOpen = true;

            if (fastTravelPanel != null)
            {
                fastTravelPanel.SetActive(true);
            }

            // Disable player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Show current station
            if (currentStationText != null)
            {
                currentStationText.text = $"Current Station: {fromStation.stationName}";
            }

            if (statusText != null)
            {
                statusText.text = "Select a destination";
            }

            PopulateDestinations();
        }

        /// <summary>
        /// Close fast travel menu.
        /// </summary>
        public void CloseFastTravelMenu()
        {
            isPanelOpen = false;

            if (fastTravelPanel != null)
            {
                fastTravelPanel.SetActive(false);
            }

            // Enable player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null)
            {
                player.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void PopulateDestinations()
        {
            if (destinationListContainer == null) return;

            // Clear existing
            foreach (Transform child in destinationListContainer)
            {
                Destroy(child.gameObject);
            }

            // Add destinations
            foreach (var station in stations)
            {
                // Skip current station
                if (station == currentStation) continue;

                // Skip locked stations
                if (!station.isDiscovered) continue;

                GameObject btn;

                if (destinationButtonPrefab != null)
                {
                    btn = Instantiate(destinationButtonPrefab, destinationListContainer);
                }
                else
                {
                    btn = new GameObject(station.stationName);
                    btn.transform.SetParent(destinationListContainer);
                    btn.AddComponent<RectTransform>();
                    btn.AddComponent<Button>();

                    var text = new GameObject("Text").AddComponent<TMP_Text>();
                    text.transform.SetParent(btn.transform);
                    text.text = station.stationName;
                }

                var buttonText = btn.GetComponentInChildren<TMP_Text>();
                if (buttonText != null)
                {
                    buttonText.text = station.stationName;
                }

                var button = btn.GetComponent<Button>();
                if (button != null)
                {
                    SubwayStation dest = station;
                    button.onClick.AddListener(() => TravelTo(dest));
                }
            }
        }

        #endregion

        #region Travel

        /// <summary>
        /// Travel to a station.
        /// </summary>
        public void TravelTo(SubwayStation destination)
        {
            if (destination == null) return;
            if (destination == currentStation) return;
            if (isTraveling) return;

            StartCoroutine(TravelSequence(destination));
        }

        private System.Collections.IEnumerator TravelSequence(SubwayStation destination)
        {
            isTraveling = true;

            // Close menu
            if (fastTravelPanel != null)
            {
                fastTravelPanel.SetActive(false);
            }

            // Play travel sound
            PlaySound(travelSound);

            // Fade out
            yield return StartCoroutine(Fade(0f, 1f));

            // Show loading
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = $"Traveling to {destination.stationName}...";
            }

            // Wait a moment
            yield return new WaitForSeconds(1.5f);

            // Move player
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null && destination.playerSpawnPoint != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = destination.playerSpawnPoint.position;
                player.transform.rotation = destination.playerSpawnPoint.rotation;

                if (cc != null) cc.enabled = true;
            }

            // Hide loading
            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(false);
            }

            // Fade in
            yield return StartCoroutine(Fade(1f, 0f));

            // Play arrive sound
            PlaySound(arriveSound);

            // Re-enable player
            if (player != null)
            {
                player.enabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            isTraveling = false;
            isPanelOpen = false;

            onFastTravel?.Invoke(currentStation, destination);
            currentStation = destination;

            UI.HUDManager.Instance?.ShowNotification($"Arrived at {destination.stationName}");
        }

        private System.Collections.IEnumerator Fade(float from, float to)
        {
            if (fadeImage == null) yield break;

            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
                fadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            fadeImage.color = new Color(0, 0, 0, to);

            if (to <= 0)
            {
                fadeImage.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Audio

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Properties

        public bool IsUnlocked => isUnlocked;
        public bool IsTraveling => isTraveling;
        public SubwayStation CurrentStation => currentStation;

        #endregion
    }

    /// <summary>
    /// Subway station component - place on your subway entrance prefab.
    /// </summary>
    public class SubwayStation : MonoBehaviour
    {
        [Header("Station Info")]
        [Tooltip("Name of this station")]
        public string stationName = "Central Station";

        [Tooltip("Has player discovered this station?")]
        public bool isDiscovered = false;

        [Tooltip("Auto-discover when player enters area")]
        public bool autoDiscover = true;

        [Header("Spawn Point")]
        [Tooltip("Where player spawns when traveling here")]
        public Transform playerSpawnPoint;

        [Header("NPC")]
        [Tooltip("The subway attendant NPC")]
        public SubwayAttendant attendant;

        [Header("Interaction")]
        [Tooltip("Interaction range")]
        public float interactionRange = 3f;

        [Tooltip("Discover range (auto-discover)")]
        public float discoverRange = 10f;

        private void Start()
        {
            // Register with system
            FastTravelSystem.Instance?.RegisterStation(this);

            // Create spawn point if missing
            if (playerSpawnPoint == null)
            {
                GameObject spawn = new GameObject("PlayerSpawnPoint");
                spawn.transform.SetParent(transform);
                spawn.transform.localPosition = new Vector3(0, 0, 2f);
                playerSpawnPoint = spawn.transform;
            }

            // Load discovered state
            isDiscovered = PlayerPrefs.GetInt($"Station_{stationName}_Discovered", 0) == 1;
        }

        private void Update()
        {
            var player = FindObjectOfType<Player.PlayerController>();
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);

            // Auto-discover
            if (autoDiscover && !isDiscovered && distance <= discoverRange)
            {
                Discover();
            }

            // Interaction prompt
            if (distance <= interactionRange)
            {
                if (FastTravelSystem.Instance?.IsUnlocked == true)
                {
                    UI.HUDManager.Instance?.ShowInteractionPrompt($"Press E to enter {stationName}");
                }
                else
                {
                    UI.HUDManager.Instance?.ShowInteractionPrompt("Press E to talk to Subway Attendant");
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    Interact();
                }
            }
        }

        /// <summary>
        /// Discover this station.
        /// </summary>
        public void Discover()
        {
            if (isDiscovered) return;

            isDiscovered = true;
            PlayerPrefs.SetInt($"Station_{stationName}_Discovered", 1);
            PlayerPrefs.Save();

            UI.HUDManager.Instance?.ShowNotification($"Discovered: {stationName}!");
        }

        /// <summary>
        /// Interact with this station.
        /// </summary>
        public void Interact()
        {
            if (!isDiscovered)
            {
                Discover();
            }

            if (FastTravelSystem.Instance?.IsUnlocked == true)
            {
                FastTravelSystem.Instance.OpenFastTravelMenu(this);
            }
            else
            {
                // Talk to attendant about unlocking
                if (attendant != null)
                {
                    attendant.StartDialogue();
                }
                else
                {
                    // No attendant, try to unlock directly
                    FastTravelSystem.Instance?.TryUnlock();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            // Draw discover range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, discoverRange);

            // Draw spawn point
            if (playerSpawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(playerSpawnPoint.position, 0.5f);
                Gizmos.DrawLine(playerSpawnPoint.position, playerSpawnPoint.position + playerSpawnPoint.forward);
            }
        }
    }

    /// <summary>
    /// Subway attendant NPC - asks for pipe to unlock fast travel.
    /// </summary>
    public class SubwayAttendant : MonoBehaviour
    {
        [Header("Dialogue")]
        [Tooltip("Dialogue when not unlocked")]
        public DialogueData lockedDialogue;

        [Tooltip("Dialogue when unlocked")]
        public DialogueData unlockedDialogue;

        [Header("Custom Dialogue (if no DialogueData)")]
        [Tooltip("What attendant says when locked")]
        [TextArea(2, 4)]
        public string lockedMessage = "Hey there! The subway's busted - some punk stole the main pipe! " +
            "Find it for me and I'll let you use the fast travel system. I think I saw it near the old warehouse...";

        [Tooltip("What attendant says after giving pipe")]
        [TextArea(2, 4)]
        public string thankYouMessage = "You found it! Amazing! The subway is now operational. " +
            "You can fast travel between any stations you've discovered!";

        [Tooltip("What attendant says when already unlocked")]
        [TextArea(2, 4)]
        public string unlockedMessage = "Welcome back! Where would you like to go today?";

        [Header("Animation")]
        public Animation.SimpleAnimationPlayer animPlayer;
        public string idleAnimation = "Idle";
        public string talkAnimation = "Talk";

        private bool isTalking = false;

        public void StartDialogue()
        {
            if (isTalking) return;

            if (FastTravelSystem.Instance?.IsUnlocked == true)
            {
                // Already unlocked
                ShowMessage(unlockedMessage);
                FastTravelSystem.Instance.OpenFastTravelMenu(GetComponentInParent<SubwayStation>());
            }
            else
            {
                // Check if player has pipe
                bool hasPipe = Items.ConsumableSystem.Instance?.HasItem("subway_pipe") == true ||
                              FindObjectOfType<Player.PlayerInventory>()?.HasItem("subway_pipe") == true;

                if (hasPipe)
                {
                    // Unlock!
                    FastTravelSystem.Instance?.TryUnlock();
                    ShowMessage(thankYouMessage);
                }
                else
                {
                    // Tell them to find it
                    ShowMessage(lockedMessage);
                }
            }
        }

        private void ShowMessage(string message)
        {
            isTalking = true;

            // Play talk animation
            if (animPlayer != null)
            {
                animPlayer.Play(talkAnimation);
            }

            // Use dialogue system if available
            if (DialogueSystem.Instance != null)
            {
                // Create quick dialogue
                DialogueData quickDialogue = ScriptableObject.CreateInstance<DialogueData>();
                quickDialogue.lines.Add(new DialogueLine
                {
                    speakerName = "Subway Attendant",
                    text = message
                });

                DialogueSystem.Instance.StartDialogue(quickDialogue, transform);
            }
            else
            {
                // Fallback: just show notification
                UI.HUDManager.Instance?.ShowNotification(message);
            }

            // Return to idle after delay
            Invoke(nameof(StopTalking), 3f);
        }

        private void StopTalking()
        {
            isTalking = false;

            if (animPlayer != null)
            {
                animPlayer.Play(idleAnimation);
            }
        }
    }
}
