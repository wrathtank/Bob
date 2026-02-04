using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace BobsPetroleum.Core
{
    /// <summary>
    /// SCENE LOADER with LOADING SCREENS!
    /// Shows progress bar, tips, and smooth transitions.
    ///
    /// SETUP:
    /// 1. Create a Loading Screen Canvas (set to DontDestroyOnLoad)
    /// 2. Add: Background image, Progress bar, Loading text, Tip text
    /// 3. Drag into the slots below
    /// 4. Call SceneLoader.Instance.LoadScene("YourScene")
    ///
    /// SCENE SETUP:
    /// - Scene 0 = Main Menu
    /// - Scene 1 = Game Scene
    /// - Add scenes in Build Settings!
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        [Header("=== LOADING SCREEN UI ===")]
        [Tooltip("The entire loading screen canvas/panel")]
        public GameObject loadingScreen;

        [Tooltip("Progress bar slider (0-1)")]
        public Slider progressBar;

        [Tooltip("Progress percentage text")]
        public TMP_Text progressText;

        [Tooltip("Loading... text (animated dots)")]
        public TMP_Text loadingText;

        [Tooltip("Random tip text")]
        public TMP_Text tipText;

        [Tooltip("Scene name being loaded")]
        public TMP_Text sceneNameText;

        [Header("=== BACKGROUND ===")]
        [Tooltip("Background image (optional - can swap per scene)")]
        public Image backgroundImage;

        [Tooltip("List of loading screen backgrounds")]
        public List<Sprite> loadingBackgrounds = new List<Sprite>();

        [Header("=== LOADING TIPS ===")]
        [Tooltip("Tips shown during loading")]
        [TextArea(2, 4)]
        public List<string> loadingTips = new List<string>()
        {
            "Feed Bob hamburgers to revive him!",
            "Run the gas station to earn money.",
            "You can set your respawn point at beds and couches.",
            "Press TAB to open your inventory.",
            "Buy weapons from the shop to defend yourself.",
            "Capture pets with the net - throw with right click!",
            "Fast travel unlocks after finding subway stations.",
            "Bob's health increases with each hamburger.",
            "Watch out for horror events at night!",
            "Play with friends - up to 4 players co-op!"
        };

        [Header("=== SETTINGS ===")]
        [Tooltip("Minimum time to show loading screen")]
        public float minLoadingTime = 1.5f;

        [Tooltip("Fade in/out duration")]
        public float fadeDuration = 0.5f;

        [Tooltip("Animate loading dots")]
        public bool animateLoadingDots = true;

        [Tooltip("Change tip every X seconds")]
        public float tipChangeInterval = 3f;

        [Header("=== AUDIO ===")]
        [Tooltip("Music during loading (optional)")]
        public AudioClip loadingMusic;

        [Tooltip("Sound when loading complete")]
        public AudioClip loadCompleteSound;

        [Header("=== EVENTS ===")]
        public UnityEvent onLoadingStarted;
        public UnityEvent onLoadingComplete;
        public UnityEvent<float> onProgressUpdated;

        // Internal
        private CanvasGroup canvasGroup;
        private AudioSource audioSource;
        private bool isLoading = false;
        private Coroutine dotAnimCoroutine;
        private Coroutine tipCoroutine;

        private void Awake()
        {
            // Singleton that persists
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                // Ensure loading screen also persists
                if (loadingScreen != null && loadingScreen.transform.parent == transform)
                {
                    // Already child of this, will persist
                }
                else if (loadingScreen != null)
                {
                    loadingScreen.transform.SetParent(transform);
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Setup canvas group for fading
            if (loadingScreen != null)
            {
                canvasGroup = loadingScreen.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = loadingScreen.AddComponent<CanvasGroup>();
                }
            }

            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            // Hide loading screen initially
            HideLoadingScreen();
        }

        #region Public API

        /// <summary>
        /// Load a scene by name with loading screen.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isLoading) return;
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// Load a scene by build index with loading screen.
        /// </summary>
        public void LoadScene(int sceneIndex)
        {
            if (isLoading) return;
            string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
            if (string.IsNullOrEmpty(sceneName))
            {
                sceneName = $"Scene {sceneIndex}";
            }
            StartCoroutine(LoadSceneAsyncByIndex(sceneIndex, sceneName));
        }

        /// <summary>
        /// Load main menu (scene 0).
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene(0);
        }

        /// <summary>
        /// Load game scene (scene 1).
        /// </summary>
        public void LoadGameScene()
        {
            LoadScene(1);
        }

        /// <summary>
        /// Reload current scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        #endregion

        #region Loading Coroutines

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;
            onLoadingStarted?.Invoke();

            // Show loading screen
            yield return StartCoroutine(ShowLoadingScreenCoroutine());

            // Update scene name
            if (sceneNameText != null)
            {
                sceneNameText.text = sceneName;
            }

            // Start loading
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            float startTime = Time.realtimeSinceStartup;

            // Wait for load (stops at 0.9)
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateProgress(progress);

                // Check if ready and minimum time passed
                if (asyncLoad.progress >= 0.9f)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed >= minLoadingTime)
                    {
                        asyncLoad.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            // Ensure we hit 100%
            UpdateProgress(1f);
            yield return new WaitForSecondsRealtime(0.2f);

            // Play complete sound
            if (loadCompleteSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(loadCompleteSound);
            }

            // Hide loading screen
            yield return StartCoroutine(HideLoadingScreenCoroutine());

            isLoading = false;
            onLoadingComplete?.Invoke();
        }

        private IEnumerator LoadSceneAsyncByIndex(int sceneIndex, string displayName)
        {
            isLoading = true;
            onLoadingStarted?.Invoke();

            // Show loading screen
            yield return StartCoroutine(ShowLoadingScreenCoroutine());

            // Update scene name
            if (sceneNameText != null)
            {
                sceneNameText.text = displayName;
            }

            // Start loading
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
            asyncLoad.allowSceneActivation = false;

            float startTime = Time.realtimeSinceStartup;

            // Wait for load
            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateProgress(progress);

                if (asyncLoad.progress >= 0.9f)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed >= minLoadingTime)
                    {
                        asyncLoad.allowSceneActivation = true;
                    }
                }

                yield return null;
            }

            UpdateProgress(1f);
            yield return new WaitForSecondsRealtime(0.2f);

            if (loadCompleteSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(loadCompleteSound);
            }

            yield return StartCoroutine(HideLoadingScreenCoroutine());

            isLoading = false;
            onLoadingComplete?.Invoke();
        }

        #endregion

        #region Loading Screen

        private IEnumerator ShowLoadingScreenCoroutine()
        {
            if (loadingScreen == null) yield break;

            // Pick random background
            if (backgroundImage != null && loadingBackgrounds.Count > 0)
            {
                backgroundImage.sprite = loadingBackgrounds[Random.Range(0, loadingBackgrounds.Count)];
            }

            // Show random tip
            ShowRandomTip();

            // Reset progress
            UpdateProgress(0f);

            // Play loading music
            if (loadingMusic != null && audioSource != null)
            {
                audioSource.clip = loadingMusic;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Start animations
            if (animateLoadingDots)
            {
                dotAnimCoroutine = StartCoroutine(AnimateLoadingDots());
            }
            tipCoroutine = StartCoroutine(CycleTips());

            // Fade in
            loadingScreen.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    canvasGroup.alpha = t / fadeDuration;
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }
        }

        private IEnumerator HideLoadingScreenCoroutine()
        {
            // Stop music
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Stop animations
            if (dotAnimCoroutine != null)
            {
                StopCoroutine(dotAnimCoroutine);
            }
            if (tipCoroutine != null)
            {
                StopCoroutine(tipCoroutine);
            }

            // Fade out
            if (canvasGroup != null)
            {
                float t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    canvasGroup.alpha = 1f - (t / fadeDuration);
                    yield return null;
                }
                canvasGroup.alpha = 0f;
            }

            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
        }

        private void HideLoadingScreen()
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        #endregion

        #region Progress & Display

        private void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }

            onProgressUpdated?.Invoke(progress);
        }

        private void ShowRandomTip()
        {
            if (tipText != null && loadingTips.Count > 0)
            {
                tipText.text = loadingTips[Random.Range(0, loadingTips.Count)];
            }
        }

        private IEnumerator AnimateLoadingDots()
        {
            string baseText = "Loading";
            int dots = 0;

            while (true)
            {
                if (loadingText != null)
                {
                    loadingText.text = baseText + new string('.', dots);
                }

                dots = (dots + 1) % 4;
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private IEnumerator CycleTips()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(tipChangeInterval);
                ShowRandomTip();
            }
        }

        #endregion

        #region Quick Load (No Loading Screen)

        /// <summary>
        /// Load scene instantly without loading screen.
        /// </summary>
        public void LoadSceneInstant(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Load scene instantly by index.
        /// </summary>
        public void LoadSceneInstant(int sceneIndex)
        {
            SceneManager.LoadScene(sceneIndex);
        }

        #endregion
    }
}
