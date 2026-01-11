using UnityEngine;
using System.Runtime.InteropServices;

namespace BobsPetroleum.Utilities
{
    /// <summary>
    /// WebGL-specific optimizations and utilities.
    /// Handles browser integration, memory management, and platform-specific features.
    /// </summary>
    public class WebGLOptimizations : MonoBehaviour
    {
        public static WebGLOptimizations Instance { get; private set; }

        [Header("Memory Management")]
        [Tooltip("Target framerate for WebGL (lower = better performance)")]
        public int targetFrameRate = 60;

        [Tooltip("Enable aggressive garbage collection")]
        public bool aggressiveGC = true;

        [Tooltip("GC interval in seconds")]
        public float gcInterval = 60f;

        [Header("Quality Settings")]
        [Tooltip("Auto-detect and adjust quality")]
        public bool autoQuality = true;

        [Tooltip("Default quality level for WebGL")]
        public int defaultWebGLQuality = 2;

        [Header("Audio Optimization")]
        [Tooltip("Limit simultaneous audio sources")]
        public int maxAudioSources = 16;

        [Tooltip("Use compressed audio")]
        public bool useCompressedAudio = true;

        [Header("Texture Optimization")]
        [Tooltip("Max texture size for WebGL")]
        public int maxTextureSize = 1024;

        [Header("Browser Integration")]
        [Tooltip("Show loading progress")]
        public bool showLoadingProgress = true;

        [Tooltip("Enable fullscreen button")]
        public bool enableFullscreen = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        // JavaScript interop
        [DllImport("__Internal")]
        private static extern void JS_ShowMessage(string message);

        [DllImport("__Internal")]
        private static extern void JS_CopyToClipboard(string text);

        [DllImport("__Internal")]
        private static extern bool JS_IsMobile();

        [DllImport("__Internal")]
        private static extern void JS_RequestFullscreen();

        [DllImport("__Internal")]
        private static extern void JS_OpenURL(string url);

        [DllImport("__Internal")]
        private static extern string JS_GetBrowserInfo();

        [DllImport("__Internal")]
        private static extern void JS_SaveToLocalStorage(string key, string value);

        [DllImport("__Internal")]
        private static extern string JS_LoadFromLocalStorage(string key);

        [DllImport("__Internal")]
        private static extern void JS_ShareGame(string title, string text, string url);
#endif

        private float gcTimer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            ApplyWebGLSettings();
        }

        private void Start()
        {
            if (autoQuality)
            {
                DetectAndSetQuality();
            }
        }

        private void Update()
        {
            // Periodic garbage collection
            if (aggressiveGC)
            {
                gcTimer += Time.deltaTime;
                if (gcTimer >= gcInterval)
                {
                    gcTimer = 0f;
                    System.GC.Collect();
                }
            }
        }

        #region WebGL Settings

        private void ApplyWebGLSettings()
        {
#if UNITY_WEBGL
            // Set target framerate
            Application.targetFrameRate = targetFrameRate;

            // Disable VSync for WebGL (handled by browser)
            QualitySettings.vSyncCount = 0;

            // Reduce texture quality for mobile browsers
            if (IsMobileBrowser())
            {
                QualitySettings.globalTextureMipmapLimit = 1;
            }

            Debug.Log("WebGL optimizations applied");
#endif
        }

        private void DetectAndSetQuality()
        {
#if UNITY_WEBGL
            // Check if mobile browser
            if (IsMobileBrowser())
            {
                QualitySettings.SetQualityLevel(0); // Lowest
                Application.targetFrameRate = 30;
                Debug.Log("Mobile detected - using low quality");
            }
            else
            {
                QualitySettings.SetQualityLevel(defaultWebGLQuality);
                Debug.Log($"Desktop detected - using quality level {defaultWebGLQuality}");
            }
#endif
        }

        #endregion

        #region Browser Integration

        /// <summary>
        /// Check if running in mobile browser.
        /// </summary>
        public bool IsMobileBrowser()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return JS_IsMobile();
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Show browser alert/message.
        /// </summary>
        public void ShowBrowserMessage(string message)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                JS_ShowMessage(message);
            }
            catch { }
#else
            Debug.Log($"Browser message: {message}");
#endif
        }

        /// <summary>
        /// Copy text to clipboard (works in WebGL).
        /// </summary>
        public void CopyToClipboard(string text)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                JS_CopyToClipboard(text);
            }
            catch { }
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        /// <summary>
        /// Request fullscreen mode.
        /// </summary>
        public void RequestFullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                JS_RequestFullscreen();
            }
            catch { }
#else
            Screen.fullScreen = !Screen.fullScreen;
#endif
        }

        /// <summary>
        /// Open URL in new tab.
        /// </summary>
        public void OpenURL(string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                JS_OpenURL(url);
            }
            catch
            {
                Application.OpenURL(url);
            }
#else
            Application.OpenURL(url);
#endif
        }

        /// <summary>
        /// Share game (if supported).
        /// </summary>
        public void ShareGame(string title, string text, string url)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                JS_ShareGame(title, text, url);
            }
            catch { }
#endif
        }

        #endregion

        #region Local Storage (WebGL Save Fallback)

        /// <summary>
        /// Save data to local storage (alternative to PlayerPrefs for large data).
        /// </summary>
        public void SaveToLocalStorage(string key, string value)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                JS_SaveToLocalStorage(key, value);
            }
            catch
            {
                PlayerPrefs.SetString(key, value);
            }
#else
            PlayerPrefs.SetString(key, value);
#endif
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load data from local storage.
        /// </summary>
        public string LoadFromLocalStorage(string key)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string value = JS_LoadFromLocalStorage(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch { }
#endif
            return PlayerPrefs.GetString(key, "");
        }

        #endregion

        #region Memory Optimization

        /// <summary>
        /// Force garbage collection.
        /// </summary>
        public void ForceGarbageCollection()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        /// <summary>
        /// Unload unused assets.
        /// </summary>
        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }

        #endregion

        #region Audio Optimization

        /// <summary>
        /// Limit active audio sources for WebGL.
        /// </summary>
        public void OptimizeAudioSources()
        {
            AudioSource[] sources = FindObjectsOfType<AudioSource>();

            if (sources.Length > maxAudioSources)
            {
                // Sort by distance from listener
                AudioListener listener = FindObjectOfType<AudioListener>();
                if (listener == null) return;

                System.Array.Sort(sources, (a, b) =>
                {
                    float distA = Vector3.Distance(a.transform.position, listener.transform.position);
                    float distB = Vector3.Distance(b.transform.position, listener.transform.position);
                    return distA.CompareTo(distB);
                });

                // Disable furthest sources
                for (int i = maxAudioSources; i < sources.Length; i++)
                {
                    if (!sources[i].isPlaying)
                    {
                        sources[i].enabled = false;
                    }
                }
            }
        }

        #endregion

        #region Performance Helpers

        /// <summary>
        /// Check if performance is struggling.
        /// </summary>
        public bool IsPerformanceStrugging()
        {
            // If FPS is consistently low
            return 1f / Time.deltaTime < 25f;
        }

        /// <summary>
        /// Reduce quality if performance is bad.
        /// </summary>
        public void AdaptiveQuality()
        {
            if (IsPerformanceStrugging())
            {
                int currentLevel = QualitySettings.GetQualityLevel();
                if (currentLevel > 0)
                {
                    QualitySettings.SetQualityLevel(currentLevel - 1);
                    Debug.Log($"Quality reduced to {currentLevel - 1} due to performance");
                }
            }
        }

        #endregion

        #region Object Pooling Helper

        /// <summary>
        /// Get or create object pool for WebGL efficiency.
        /// </summary>
        public ObjectPool GetOrCreatePool(string poolId, GameObject prefab, int initialSize = 10)
        {
            var existingPool = FindObjectOfType<ObjectPool>();
            if (existingPool != null && existingPool.poolId == poolId)
            {
                return existingPool;
            }

            // Create new pool
            GameObject poolObj = new GameObject($"Pool_{poolId}");
            ObjectPool pool = poolObj.AddComponent<ObjectPool>();
            pool.poolId = poolId;
            pool.prefab = prefab;
            pool.initialPoolSize = initialSize;

            return pool;
        }

        #endregion
    }

    /// <summary>
    /// WebGL-specific loading screen.
    /// </summary>
    public class WebGLLoadingScreen : MonoBehaviour
    {
        [Header("UI")]
        public UnityEngine.UI.Slider progressBar;
        public TMPro.TMP_Text progressText;
        public TMPro.TMP_Text statusText;
        public GameObject loadingPanel;

        private void Start()
        {
#if UNITY_WEBGL
            // In WebGL, the browser handles initial loading
            // This is for scene loading
            StartCoroutine(LoadingSequence());
#else
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
#endif
        }

        private System.Collections.IEnumerator LoadingSequence()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            // Simulate loading progress
            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.deltaTime * 0.5f;

                if (progressBar != null)
                {
                    progressBar.value = progress;
                }

                if (progressText != null)
                {
                    progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
                }

                yield return null;
            }

            // Hide loading screen
            yield return new WaitForSeconds(0.5f);

            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        public void SetStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }
    }
}
