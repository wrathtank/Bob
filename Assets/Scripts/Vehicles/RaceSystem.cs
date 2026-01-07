using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;
using BobsPetroleum.Player;

namespace BobsPetroleum.Vehicles
{
    /// <summary>
    /// Race minigame system. Trigger zone starts a race.
    /// </summary>
    public class RaceSystem : MonoBehaviour
    {
        public static RaceSystem Instance { get; private set; }

        [Header("Race Settings")]
        [Tooltip("Number of laps")]
        public int totalLaps = 3;

        [Tooltip("Countdown duration")]
        public float countdownDuration = 3f;

        [Header("Track")]
        [Tooltip("Checkpoint transforms in order")]
        public Transform[] checkpoints;

        [Tooltip("Start/Finish line")]
        public Transform startLine;

        [Tooltip("Starting positions for racers")]
        public Transform[] startingPositions;

        [Header("Rewards")]
        [Tooltip("Money reward for 1st place")]
        public int firstPlaceReward = 500;

        [Tooltip("Money reward for 2nd place")]
        public int secondPlaceReward = 200;

        [Tooltip("Money reward for 3rd place")]
        public int thirdPlaceReward = 100;

        [Header("UI")]
        [Tooltip("Race UI prefab")]
        public GameObject raceUIPrefab;

        [Header("Audio")]
        public AudioClip countdownSound;
        public AudioClip startSound;
        public AudioClip checkpointSound;
        public AudioClip finishSound;

        [Header("Events")]
        public UnityEvent onRaceStart;
        public UnityEvent<RaceParticipant> onLapComplete;
        public UnityEvent<RaceParticipant> onRaceFinish;
        public UnityEvent onRaceEnd;

        // Race state
        private bool isRaceActive = false;
        private bool isCountingDown = false;
        private float countdownTimer = 0f;
        private List<RaceParticipant> participants = new List<RaceParticipant>();
        private List<RaceParticipant> finishOrder = new List<RaceParticipant>();
        private GameObject currentRaceUI;
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

        /// <summary>
        /// Register a participant for the race.
        /// </summary>
        public bool RegisterParticipant(CarController car, PlayerController driver)
        {
            if (isRaceActive || participants.Count >= startingPositions.Length)
            {
                return false;
            }

            var participant = new RaceParticipant
            {
                car = car,
                driver = driver,
                currentCheckpoint = 0,
                currentLap = 0,
                finishTime = 0f,
                hasFinished = false
            };

            participants.Add(participant);

            // Move to starting position
            int index = participants.Count - 1;
            car.transform.position = startingPositions[index].position;
            car.transform.rotation = startingPositions[index].rotation;

            return true;
        }

        /// <summary>
        /// Start the race countdown.
        /// </summary>
        public void StartCountdown()
        {
            if (isRaceActive || participants.Count == 0)
            {
                return;
            }

            isCountingDown = true;
            countdownTimer = countdownDuration;

            // Show race UI
            if (raceUIPrefab != null)
            {
                currentRaceUI = Instantiate(raceUIPrefab);
            }

            // Disable car controls during countdown
            foreach (var participant in participants)
            {
                // Freeze cars
                var rb = participant.car.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
            }
        }

        private void Update()
        {
            if (isCountingDown)
            {
                countdownTimer -= Time.deltaTime;

                if (countdownSound != null && Mathf.Floor(countdownTimer + Time.deltaTime) > Mathf.Floor(countdownTimer))
                {
                    audioSource.PlayOneShot(countdownSound);
                }

                if (countdownTimer <= 0)
                {
                    StartRace();
                }
            }

            if (isRaceActive)
            {
                UpdateRace();
            }
        }

        private void StartRace()
        {
            isCountingDown = false;
            isRaceActive = true;

            // Enable car controls
            foreach (var participant in participants)
            {
                var rb = participant.car.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }
                participant.startTime = Time.time;
            }

            if (startSound != null)
            {
                audioSource.PlayOneShot(startSound);
            }

            onRaceStart?.Invoke();
        }

        private void UpdateRace()
        {
            // Check if all participants have finished
            bool allFinished = participants.All(p => p.hasFinished);
            if (allFinished)
            {
                EndRace();
            }
        }

        /// <summary>
        /// Called when a car passes through a checkpoint.
        /// </summary>
        public void OnCheckpointReached(CarController car, int checkpointIndex)
        {
            if (!isRaceActive) return;

            var participant = participants.Find(p => p.car == car);
            if (participant == null) return;

            // Check if this is the expected checkpoint
            int expectedCheckpoint = participant.currentCheckpoint;

            if (checkpointIndex == expectedCheckpoint)
            {
                participant.currentCheckpoint++;

                if (checkpointSound != null)
                {
                    audioSource.PlayOneShot(checkpointSound);
                }

                // Check if completed a lap
                if (participant.currentCheckpoint >= checkpoints.Length)
                {
                    participant.currentCheckpoint = 0;
                    participant.currentLap++;

                    onLapComplete?.Invoke(participant);

                    // Check if finished
                    if (participant.currentLap >= totalLaps)
                    {
                        FinishParticipant(participant);
                    }
                }
            }
        }

        private void FinishParticipant(RaceParticipant participant)
        {
            participant.hasFinished = true;
            participant.finishTime = Time.time - participant.startTime;
            participant.finishPosition = finishOrder.Count + 1;
            finishOrder.Add(participant);

            if (finishSound != null)
            {
                audioSource.PlayOneShot(finishSound);
            }

            // Award prize
            int reward = 0;
            switch (participant.finishPosition)
            {
                case 1: reward = firstPlaceReward; break;
                case 2: reward = secondPlaceReward; break;
                case 3: reward = thirdPlaceReward; break;
            }

            if (reward > 0 && participant.driver != null)
            {
                var inventory = participant.driver.GetComponent<PlayerInventory>();
                inventory?.AddMoney(reward);
            }

            onRaceFinish?.Invoke(participant);
        }

        private void EndRace()
        {
            isRaceActive = false;

            // Clean up
            if (currentRaceUI != null)
            {
                Destroy(currentRaceUI);
            }

            onRaceEnd?.Invoke();

            // Reset for next race
            participants.Clear();
            finishOrder.Clear();
        }

        /// <summary>
        /// Cancel the race.
        /// </summary>
        public void CancelRace()
        {
            isRaceActive = false;
            isCountingDown = false;

            if (currentRaceUI != null)
            {
                Destroy(currentRaceUI);
            }

            participants.Clear();
            finishOrder.Clear();
        }

        /// <summary>
        /// Get current race standings.
        /// </summary>
        public List<RaceParticipant> GetStandings()
        {
            return participants.OrderByDescending(p => p.currentLap)
                              .ThenByDescending(p => p.currentCheckpoint)
                              .ToList();
        }

        public bool IsRaceActive => isRaceActive;
        public bool IsCountingDown => isCountingDown;
        public float CountdownTime => countdownTimer;
        public int ParticipantCount => participants.Count;
    }

    [System.Serializable]
    public class RaceParticipant
    {
        public CarController car;
        public PlayerController driver;
        public int currentCheckpoint;
        public int currentLap;
        public float startTime;
        public float finishTime;
        public int finishPosition;
        public bool hasFinished;
    }

    /// <summary>
    /// Race start zone. Enter with a car to join the race.
    /// </summary>
    public class RaceStartZone : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Minimum participants to start")]
        public int minParticipants = 1;

        [Tooltip("Auto-start delay after minimum reached")]
        public float autoStartDelay = 10f;

        [Header("UI")]
        public TMPro.TMP_Text statusText;

        private float autoStartTimer = 0f;
        private bool waitingToStart = false;

        private void OnTriggerEnter(Collider other)
        {
            var car = other.GetComponent<CarController>();
            if (car == null) return;

            var driver = car.GetDriver();
            if (driver == null) return;

            if (RaceSystem.Instance != null && !RaceSystem.Instance.IsRaceActive)
            {
                RaceSystem.Instance.RegisterParticipant(car, driver);
                CheckAutoStart();
            }
        }

        private void Update()
        {
            if (waitingToStart)
            {
                autoStartTimer -= Time.deltaTime;

                if (statusText != null)
                {
                    statusText.text = $"Race starting in {Mathf.CeilToInt(autoStartTimer)}...";
                }

                if (autoStartTimer <= 0)
                {
                    waitingToStart = false;
                    RaceSystem.Instance?.StartCountdown();
                }
            }
        }

        private void CheckAutoStart()
        {
            if (RaceSystem.Instance != null &&
                RaceSystem.Instance.ParticipantCount >= minParticipants &&
                !waitingToStart)
            {
                waitingToStart = true;
                autoStartTimer = autoStartDelay;
            }
        }
    }

    /// <summary>
    /// Race checkpoint trigger.
    /// </summary>
    public class RaceCheckpoint : MonoBehaviour
    {
        [Header("Checkpoint")]
        [Tooltip("Index of this checkpoint (0-based)")]
        public int checkpointIndex;

        private void OnTriggerEnter(Collider other)
        {
            var car = other.GetComponent<CarController>();
            if (car != null)
            {
                RaceSystem.Instance?.OnCheckpointReached(car, checkpointIndex);
            }
        }
    }
}
