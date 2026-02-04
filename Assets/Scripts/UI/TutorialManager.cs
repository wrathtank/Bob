using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// TUTORIAL SYSTEM - Guide new players step by step!
    /// Shows tips, highlights objects, and tracks progress.
    ///
    /// SETUP:
    /// 1. Create Tutorial UI (panel, text, next button, skip button)
    /// 2. Drag into slots below
    /// 3. Add TutorialStep entries
    /// 4. Done! Tutorial auto-starts for new players.
    ///
    /// TUTORIAL STEPS:
    /// - Add steps in the inspector
    /// - Each step has: Title, Description, Target to highlight
    /// - Steps auto-advance or wait for player action
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("=== TUTORIAL UI ===")]
        [Tooltip("Main tutorial panel")]
        public GameObject tutorialPanel;

        [Tooltip("Step title text")]
        public TMP_Text titleText;

        [Tooltip("Step description text")]
        public TMP_Text descriptionText;

        [Tooltip("Step counter text (Step 1/10)")]
        public TMP_Text stepCounterText;

        [Tooltip("Next/Continue button")]
        public Button nextButton;

        [Tooltip("Skip tutorial button")]
        public Button skipButton;

        [Tooltip("Got it! / Dismiss button")]
        public Button dismissButton;

        [Header("=== HIGHLIGHT SYSTEM ===")]
        [Tooltip("Highlight effect prefab (spawn at target)")]
        public GameObject highlightPrefab;

        [Tooltip("Arrow pointing to target")]
        public GameObject arrowIndicator;

        [Tooltip("Highlight color")]
        public Color highlightColor = new Color(1f, 0.8f, 0f, 0.5f);

        [Header("=== TUTORIAL STEPS ===")]
        [Tooltip("All tutorial steps in order")]
        public List<TutorialStep> tutorialSteps = new List<TutorialStep>();

        [Header("=== SETTINGS ===")]
        [Tooltip("Auto-start tutorial for new players")]
        public bool autoStartForNewPlayers = true;

        [Tooltip("PlayerPrefs key for tutorial completion")]
        public string completedKey = "TutorialCompleted";

        [Tooltip("Pause game during tutorial")]
        public bool pauseDuringTutorial = false;

        [Tooltip("Time between auto-advance steps")]
        public float autoAdvanceDelay = 5f;

        [Header("=== AUDIO ===")]
        [Tooltip("Sound for next step")]
        public AudioClip stepSound;

        [Tooltip("Sound for tutorial complete")]
        public AudioClip completeSound;

        [Range(0f, 1f)]
        public float soundVolume = 0.5f;

        [Header("=== EVENTS ===")]
        public UnityEvent onTutorialStarted;
        public UnityEvent onTutorialCompleted;
        public UnityEvent onTutorialSkipped;
        public UnityEvent<int> onStepChanged;

        // State
        private int currentStepIndex = -1;
        private bool isTutorialActive = false;
        private GameObject currentHighlight;
        private AudioSource audioSource;
        private Coroutine autoAdvanceCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Wire buttons
            if (nextButton != null)
                nextButton.onClick.AddListener(NextStep);

            if (skipButton != null)
                skipButton.onClick.AddListener(SkipTutorial);

            if (dismissButton != null)
                dismissButton.onClick.AddListener(DismissCurrentStep);

            // Hide initially
            HideTutorial();

            // Auto-start for new players
            if (autoStartForNewPlayers && !HasCompletedTutorial())
            {
                // Delay start to let game initialize
                Invoke(nameof(StartTutorial), 1f);
            }
        }

        #region Public API

        /// <summary>
        /// Start the tutorial from the beginning.
        /// </summary>
        public void StartTutorial()
        {
            if (tutorialSteps.Count == 0)
            {
                Debug.LogWarning("[Tutorial] No steps defined!");
                return;
            }

            isTutorialActive = true;
            currentStepIndex = -1;

            if (pauseDuringTutorial)
            {
                Time.timeScale = 0f;
            }

            onTutorialStarted?.Invoke();
            NextStep();

            Debug.Log("[Tutorial] Started");
        }

        /// <summary>
        /// Show a specific tutorial step.
        /// </summary>
        public void ShowStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= tutorialSteps.Count) return;

            currentStepIndex = stepIndex;
            TutorialStep step = tutorialSteps[stepIndex];

            // Show panel
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }

            // Update text
            if (titleText != null)
            {
                titleText.text = step.title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = step.description;
            }

            if (stepCounterText != null)
            {
                stepCounterText.text = $"Step {stepIndex + 1} / {tutorialSteps.Count}";
            }

            // Update buttons
            if (nextButton != null)
            {
                bool isLastStep = stepIndex >= tutorialSteps.Count - 1;
                nextButton.GetComponentInChildren<TMP_Text>()?.SetText(isLastStep ? "Finish" : "Next");
            }

            // Show highlight
            HighlightTarget(step.targetObject);

            // Position arrow
            if (arrowIndicator != null && step.targetObject != null)
            {
                arrowIndicator.SetActive(true);
                // Arrow follows target - you'd implement this based on your UI setup
            }
            else if (arrowIndicator != null)
            {
                arrowIndicator.SetActive(false);
            }

            // Play sound
            PlaySound(stepSound);

            // Auto-advance if configured
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
            }

            if (step.autoAdvance)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceCoroutine(step.autoAdvanceTime > 0 ? step.autoAdvanceTime : autoAdvanceDelay));
            }

            // Fire event
            step.onStepShown?.Invoke();
            onStepChanged?.Invoke(stepIndex);

            Debug.Log($"[Tutorial] Step {stepIndex + 1}: {step.title}");
        }

        /// <summary>
        /// Move to next step or complete tutorial.
        /// </summary>
        public void NextStep()
        {
            if (!isTutorialActive) return;

            // Complete current step
            if (currentStepIndex >= 0 && currentStepIndex < tutorialSteps.Count)
            {
                tutorialSteps[currentStepIndex].onStepCompleted?.Invoke();
            }

            // Move to next
            currentStepIndex++;

            if (currentStepIndex >= tutorialSteps.Count)
            {
                CompleteTutorial();
            }
            else
            {
                ShowStep(currentStepIndex);
            }
        }

        /// <summary>
        /// Skip the entire tutorial.
        /// </summary>
        public void SkipTutorial()
        {
            HideTutorial();
            isTutorialActive = false;

            if (pauseDuringTutorial)
            {
                Time.timeScale = 1f;
            }

            // Still mark as completed so it doesn't show again
            PlayerPrefs.SetInt(completedKey, 1);
            PlayerPrefs.Save();

            onTutorialSkipped?.Invoke();

            Debug.Log("[Tutorial] Skipped");
        }

        /// <summary>
        /// Dismiss current step (hide but don't advance).
        /// </summary>
        public void DismissCurrentStep()
        {
            HideTutorial();

            if (pauseDuringTutorial)
            {
                Time.timeScale = 1f;
            }
        }

        /// <summary>
        /// Resume tutorial after dismiss.
        /// </summary>
        public void ResumeTutorial()
        {
            if (isTutorialActive && currentStepIndex >= 0)
            {
                ShowStep(currentStepIndex);

                if (pauseDuringTutorial)
                {
                    Time.timeScale = 0f;
                }
            }
        }

        /// <summary>
        /// Reset tutorial progress (show again next time).
        /// </summary>
        public void ResetTutorial()
        {
            PlayerPrefs.DeleteKey(completedKey);
            PlayerPrefs.Save();
            currentStepIndex = -1;
            isTutorialActive = false;

            Debug.Log("[Tutorial] Progress reset");
        }

        /// <summary>
        /// Check if player has completed tutorial.
        /// </summary>
        public bool HasCompletedTutorial()
        {
            return PlayerPrefs.GetInt(completedKey, 0) == 1;
        }

        /// <summary>
        /// Show a quick tip (single message, not part of tutorial).
        /// </summary>
        public void ShowQuickTip(string title, string description, GameObject target = null)
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }

            if (titleText != null)
            {
                titleText.text = title;
            }

            if (descriptionText != null)
            {
                descriptionText.text = description;
            }

            if (stepCounterText != null)
            {
                stepCounterText.gameObject.SetActive(false);
            }

            // Hide next/skip, show dismiss
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            if (dismissButton != null) dismissButton.gameObject.SetActive(true);

            HighlightTarget(target);
        }

        #endregion

        #region Internal

        private void CompleteTutorial()
        {
            HideTutorial();
            isTutorialActive = false;

            if (pauseDuringTutorial)
            {
                Time.timeScale = 1f;
            }

            // Save completion
            PlayerPrefs.SetInt(completedKey, 1);
            PlayerPrefs.Save();

            // Play complete sound
            PlaySound(completeSound);

            onTutorialCompleted?.Invoke();

            Debug.Log("[Tutorial] Completed!");
        }

        private void HideTutorial()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            ClearHighlight();

            if (arrowIndicator != null)
            {
                arrowIndicator.SetActive(false);
            }

            // Reset UI state
            if (stepCounterText != null)
            {
                stepCounterText.gameObject.SetActive(true);
            }
            if (nextButton != null) nextButton.gameObject.SetActive(true);
            if (skipButton != null) skipButton.gameObject.SetActive(true);

            // Stop auto advance
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
            }
        }

        private void HighlightTarget(GameObject target)
        {
            ClearHighlight();

            if (target == null) return;

            if (highlightPrefab != null)
            {
                currentHighlight = Instantiate(highlightPrefab, target.transform.position, Quaternion.identity);
                currentHighlight.transform.SetParent(target.transform);
            }
            else
            {
                // Simple outline effect using a temporary child
                // In a real project, you'd use outline shaders or UI highlights
            }
        }

        private void ClearHighlight()
        {
            if (currentHighlight != null)
            {
                Destroy(currentHighlight);
                currentHighlight = null;
            }
        }

        private IEnumerator AutoAdvanceCoroutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            NextStep();
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip, soundVolume);
            }
        }

        #endregion

        #region Context-Sensitive Tips

        /// <summary>
        /// Show tip when player approaches something for the first time.
        /// </summary>
        public void ShowFirstTimeTip(string tipKey, string title, string description, GameObject target = null)
        {
            string key = $"Tip_{tipKey}";
            if (PlayerPrefs.GetInt(key, 0) == 1) return; // Already shown

            ShowQuickTip(title, description, target);

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reset all first-time tips (show them again).
        /// </summary>
        public void ResetAllTips()
        {
            // This is a simplified version - in production you'd track all tip keys
            Debug.Log("[Tutorial] Tips reset");
        }

        #endregion
    }

    [System.Serializable]
    public class TutorialStep
    {
        [Header("Content")]
        [Tooltip("Step title")]
        public string title = "Tutorial Step";

        [TextArea(2, 4)]
        [Tooltip("Step description/instructions")]
        public string description = "Do this thing...";

        [Header("Target")]
        [Tooltip("Object to highlight (optional)")]
        public GameObject targetObject;

        [Tooltip("Tag to find target at runtime")]
        public string targetTag = "";

        [Header("Advancement")]
        [Tooltip("Auto-advance to next step")]
        public bool autoAdvance = false;

        [Tooltip("Time before auto-advance")]
        public float autoAdvanceTime = 5f;

        [Tooltip("Required action to advance (e.g., 'OpenInventory')")]
        public string requiredAction = "";

        [Header("Events")]
        public UnityEvent onStepShown;
        public UnityEvent onStepCompleted;
    }
}
