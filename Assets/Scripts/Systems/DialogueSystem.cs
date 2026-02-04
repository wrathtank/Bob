using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace BobsPetroleum.Systems
{
    /// <summary>
    /// Dialogue system with automatic camera positioning, typing subtitles,
    /// and voice over support. Perfect for NPC conversations.
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        public static DialogueSystem Instance { get; private set; }

        [Header("UI Elements")]
        [Tooltip("Dialogue panel container")]
        public GameObject dialoguePanel;

        [Tooltip("Speaker name text")]
        public TMP_Text speakerNameText;

        [Tooltip("Dialogue text (types out)")]
        public TMP_Text dialogueText;

        [Tooltip("Continue prompt")]
        public GameObject continuePrompt;

        [Tooltip("Speaker portrait image")]
        public UnityEngine.UI.Image speakerPortrait;

        [Header("Typing Effect")]
        [Tooltip("Characters per second")]
        public float typingSpeed = 50f;

        [Tooltip("Pause on punctuation")]
        public bool pauseOnPunctuation = true;

        [Tooltip("Punctuation pause duration")]
        public float punctuationPause = 0.2f;

        [Tooltip("Typing sound (plays per character)")]
        public AudioClip typingSound;

        [Tooltip("Typing sound interval (characters)")]
        public int typingSoundInterval = 3;

        [Header("Camera")]
        [Tooltip("Auto-position camera for dialogue")]
        public bool autoPositionCamera = true;

        [Tooltip("Dialogue camera (separate from main)")]
        public Camera dialogueCamera;

        [Tooltip("Camera offset from speaker")]
        public Vector3 cameraOffset = new Vector3(1f, 1.5f, 2f);

        [Tooltip("Camera look offset (above speaker head)")]
        public Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

        [Tooltip("Camera transition speed")]
        public float cameraTransitionSpeed = 2f;

        [Header("Input")]
        [Tooltip("Continue dialogue key")]
        public KeyCode continueKey = KeyCode.Space;

        [Tooltip("Skip typing key")]
        public KeyCode skipTypingKey = KeyCode.Mouse0;

        [Header("Animation")]
        [Tooltip("Animation trigger for talking")]
        public string talkingTrigger = "Talk";

        [Tooltip("Animation bool for is talking")]
        public string talkingBool = "IsTalking";

        [Header("Events")]
        public UnityEvent onDialogueStart;
        public UnityEvent onDialogueEnd;
        public UnityEvent<DialogueLine> onLineStart;
        public UnityEvent<DialogueLine> onLineComplete;

        // State
        private DialogueData currentDialogue;
        private int currentLineIndex;
        private bool isTyping;
        private bool isWaitingForInput;
        private bool isInDialogue;
        private string fullText;
        private Coroutine typingCoroutine;
        private Camera mainCamera;
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;
        private Transform currentSpeaker;
        private AudioSource audioSource;
        private Player.PlayerController playerController;

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

            mainCamera = Camera.main;
        }

        private void Start()
        {
            // Hide dialogue panel
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            // Find player
            playerController = FindObjectOfType<Player.PlayerController>();
        }

        private void Update()
        {
            if (!isInDialogue) return;

            // Skip typing
            if (isTyping && Input.GetKeyDown(skipTypingKey))
            {
                SkipTyping();
            }

            // Continue to next line
            if (isWaitingForInput && Input.GetKeyDown(continueKey))
            {
                NextLine();
            }
        }

        #region Dialogue Control

        /// <summary>
        /// Start a dialogue sequence.
        /// </summary>
        public void StartDialogue(DialogueData dialogue, Transform speaker = null)
        {
            if (dialogue == null || dialogue.lines.Count == 0) return;
            if (isInDialogue) return;

            currentDialogue = dialogue;
            currentLineIndex = 0;
            isInDialogue = true;
            currentSpeaker = speaker;

            // Show UI
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            // Disable player controls
            if (playerController != null)
            {
                playerController.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Setup camera
            if (autoPositionCamera)
            {
                SetupDialogueCamera();
            }

            // Start first line
            ShowLine(currentDialogue.lines[0]);

            onDialogueStart?.Invoke();
        }

        /// <summary>
        /// Start dialogue with NPC.
        /// </summary>
        public void StartDialogueWithNPC(NPCDialogue npc)
        {
            if (npc == null || npc.dialogue == null) return;
            StartDialogue(npc.dialogue, npc.transform);
        }

        /// <summary>
        /// Skip to next line or end dialogue.
        /// </summary>
        public void NextLine()
        {
            currentLineIndex++;

            if (currentLineIndex >= currentDialogue.lines.Count)
            {
                EndDialogue();
            }
            else
            {
                ShowLine(currentDialogue.lines[currentLineIndex]);
            }
        }

        /// <summary>
        /// End the dialogue.
        /// </summary>
        public void EndDialogue()
        {
            if (!isInDialogue) return;

            isInDialogue = false;
            isTyping = false;
            isWaitingForInput = false;

            // Stop any playing voice
            if (audioSource != null)
            {
                audioSource.Stop();
            }

            // Stop typing
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            // Stop NPC talking animation
            if (currentSpeaker != null)
            {
                var animator = currentSpeaker.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool(talkingBool, false);
                }
            }

            // Hide UI
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            // Restore camera
            if (autoPositionCamera)
            {
                RestoreMainCamera();
            }

            // Re-enable player
            if (playerController != null)
            {
                playerController.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            onDialogueEnd?.Invoke();

            // Fire dialogue complete event if exists
            currentDialogue?.onDialogueComplete?.Invoke();
        }

        /// <summary>
        /// Skip current typing animation.
        /// </summary>
        public void SkipTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (dialogueText != null)
            {
                dialogueText.text = fullText;
            }

            isTyping = false;
            isWaitingForInput = true;
            ShowContinuePrompt(true);
        }

        #endregion

        #region Line Display

        private void ShowLine(DialogueLine line)
        {
            onLineStart?.Invoke(line);

            // Speaker name
            if (speakerNameText != null)
            {
                speakerNameText.text = line.speakerName;
            }

            // Portrait
            if (speakerPortrait != null)
            {
                if (line.portrait != null)
                {
                    speakerPortrait.sprite = line.portrait;
                    speakerPortrait.gameObject.SetActive(true);
                }
                else
                {
                    speakerPortrait.gameObject.SetActive(false);
                }
            }

            // Start typing
            fullText = line.text;
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText(line));

            // Play voice clip
            if (line.voiceClip != null && audioSource != null)
            {
                audioSource.clip = line.voiceClip;
                audioSource.Play();
            }

            // Trigger talking animation
            if (currentSpeaker != null)
            {
                var animator = currentSpeaker.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetTrigger(talkingTrigger);
                    animator.SetBool(talkingBool, true);
                }
            }

            // Position camera for this speaker
            if (autoPositionCamera && line.speakerTransform != null)
            {
                PositionCameraForSpeaker(line.speakerTransform);
            }

            ShowContinuePrompt(false);
        }

        private IEnumerator TypeText(DialogueLine line)
        {
            isTyping = true;
            isWaitingForInput = false;
            dialogueText.text = "";

            int charCount = 0;

            foreach (char c in fullText)
            {
                dialogueText.text += c;
                charCount++;

                // Play typing sound
                if (typingSound != null && charCount % typingSoundInterval == 0)
                {
                    audioSource.PlayOneShot(typingSound, 0.3f);
                }

                // Calculate delay
                float delay = 1f / typingSpeed;

                // Pause on punctuation
                if (pauseOnPunctuation && (c == '.' || c == '!' || c == '?' || c == ','))
                {
                    delay += punctuationPause;
                }

                yield return new WaitForSeconds(delay);
            }

            isTyping = false;
            isWaitingForInput = true;

            // Stop talking animation
            if (currentSpeaker != null)
            {
                var animator = currentSpeaker.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool(talkingBool, false);
                }
            }

            ShowContinuePrompt(true);
            onLineComplete?.Invoke(line);
        }

        private void ShowContinuePrompt(bool show)
        {
            if (continuePrompt != null)
            {
                continuePrompt.SetActive(show);
            }
        }

        #endregion

        #region Camera

        private void SetupDialogueCamera()
        {
            if (mainCamera == null) return;

            // Store original position
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;

            // Enable dialogue camera if separate
            if (dialogueCamera != null)
            {
                dialogueCamera.gameObject.SetActive(true);
                mainCamera.gameObject.SetActive(false);
            }

            // Initial position
            if (currentSpeaker != null)
            {
                PositionCameraForSpeaker(currentSpeaker);
            }
        }

        private void PositionCameraForSpeaker(Transform speaker)
        {
            if (speaker == null) return;

            Camera cam = dialogueCamera != null ? dialogueCamera : mainCamera;
            if (cam == null) return;

            // Calculate camera position
            Vector3 targetPosition = speaker.position + speaker.right * cameraOffset.x +
                                    Vector3.up * cameraOffset.y +
                                    speaker.forward * cameraOffset.z;

            // Look at speaker face
            Vector3 lookTarget = speaker.position + lookOffset;

            // Start transition
            StartCoroutine(TransitionCamera(cam.transform, targetPosition, lookTarget));
        }

        private IEnumerator TransitionCamera(Transform camTransform, Vector3 targetPos, Vector3 lookTarget)
        {
            float elapsed = 0f;
            Vector3 startPos = camTransform.position;
            Quaternion startRot = camTransform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPos);

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * cameraTransitionSpeed;
                float t = Mathf.SmoothStep(0f, 1f, elapsed);

                camTransform.position = Vector3.Lerp(startPos, targetPos, t);
                camTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            camTransform.position = targetPos;
            camTransform.rotation = targetRot;
        }

        private void RestoreMainCamera()
        {
            if (dialogueCamera != null)
            {
                dialogueCamera.gameObject.SetActive(false);
                mainCamera.gameObject.SetActive(true);
            }
            else if (mainCamera != null)
            {
                mainCamera.transform.position = originalCameraPosition;
                mainCamera.transform.rotation = originalCameraRotation;
            }
        }

        #endregion

        #region Properties

        public bool IsInDialogue => isInDialogue;
        public bool IsTyping => isTyping;
        public DialogueData CurrentDialogue => currentDialogue;

        #endregion
    }

    /// <summary>
    /// Dialogue data container.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Bob's Petroleum/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        public string dialogueName;
        public List<DialogueLine> lines = new List<DialogueLine>();

        [Header("Events")]
        public UnityEvent onDialogueComplete;
    }

    /// <summary>
    /// Single line of dialogue.
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        [Header("Content")]
        public string speakerName = "NPC";

        [TextArea(2, 5)]
        public string text = "Hello there!";

        [Header("Visual")]
        public Sprite portrait;
        public Transform speakerTransform;

        [Header("Audio")]
        public AudioClip voiceClip;

        [Header("Animation")]
        [Tooltip("Animation to play during this line")]
        public string animationName;
    }

    /// <summary>
    /// Component for NPCs that can have dialogue.
    /// </summary>
    public class NPCDialogue : MonoBehaviour
    {
        [Header("Dialogue")]
        public DialogueData dialogue;

        [Header("Interaction")]
        [Tooltip("Prompt shown to player")]
        public string interactionPrompt = "Press E to talk";

        [Tooltip("Can only talk once")]
        public bool oneTimeDialogue = false;

        [Header("Animation")]
        [Tooltip("Animator for talking animations")]
        public Animator animator;

        [Tooltip("Simple animation player for Meshy animations")]
        public Animation.SimpleAnimationPlayer simpleAnimator;

        [Tooltip("Idle animation name")]
        public string idleAnimation = "Idle";

        [Tooltip("Talking animation name")]
        public string talkingAnimation = "Talk";

        private bool hasSpoken = false;

        /// <summary>
        /// Start dialogue with this NPC.
        /// </summary>
        public void StartDialogue()
        {
            if (oneTimeDialogue && hasSpoken) return;

            // Use SimpleAnimationPlayer if available
            if (simpleAnimator != null)
            {
                simpleAnimator.Play(talkingAnimation);
            }

            DialogueSystem.Instance?.StartDialogueWithNPC(this);
            hasSpoken = true;
        }

        /// <summary>
        /// Called when dialogue ends.
        /// </summary>
        public void OnDialogueEnd()
        {
            // Return to idle
            if (simpleAnimator != null)
            {
                simpleAnimator.Play(idleAnimation);
            }
        }
    }
}
