using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Controls intro video playback. Attach to a camera with a VideoPlayer.
    /// Disables/enables specified objects when video ends.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class IntroVideoController : MonoBehaviour
    {
        [Header("Video Settings")]
        [Tooltip("Video clip to play (or set URL on VideoPlayer)")]
        public VideoClip introVideo;

        [Tooltip("Allow skipping with any key")]
        public bool allowSkip = true;

        [Tooltip("Keys that can skip the video")]
        public KeyCode[] skipKeys = { KeyCode.Space, KeyCode.Escape, KeyCode.Return };

        [Tooltip("Delay before allowing skip (seconds)")]
        public float skipDelay = 1f;

        [Header("On Video End")]
        [Tooltip("GameObjects to DISABLE when video ends")]
        public GameObject[] objectsToDisable;

        [Tooltip("GameObjects to ENABLE when video ends")]
        public GameObject[] objectsToEnable;

        [Header("Events")]
        public UnityEvent onVideoStart;
        public UnityEvent onVideoEnd;
        public UnityEvent onVideoSkipped;

        private VideoPlayer videoPlayer;
        private bool videoEnded = false;
        private float playTime = 0f;
        private bool canSkip = false;

        private void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();

            if (introVideo != null)
            {
                videoPlayer.clip = introVideo;
            }

            videoPlayer.loopPointReached += OnVideoComplete;
        }

        private void Start()
        {
            PlayVideo();
        }

        private void Update()
        {
            if (videoEnded) return;

            playTime += Time.deltaTime;

            if (playTime >= skipDelay)
            {
                canSkip = true;
            }

            if (allowSkip && canSkip)
            {
                // Check for skip input
                foreach (var key in skipKeys)
                {
                    if (Input.GetKeyDown(key))
                    {
                        SkipVideo();
                        return;
                    }
                }

                // Also check for any mouse click
                if (Input.GetMouseButtonDown(0))
                {
                    SkipVideo();
                }
            }
        }

        private void OnDestroy()
        {
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoComplete;
            }
        }

        /// <summary>
        /// Start playing the intro video.
        /// </summary>
        public void PlayVideo()
        {
            videoEnded = false;
            playTime = 0f;
            canSkip = false;

            videoPlayer.Play();
            onVideoStart?.Invoke();
        }

        /// <summary>
        /// Skip the video immediately.
        /// </summary>
        public void SkipVideo()
        {
            if (videoEnded) return;

            videoPlayer.Stop();
            videoEnded = true;
            onVideoSkipped?.Invoke();
            HandleVideoEnd();
        }

        private void OnVideoComplete(VideoPlayer source)
        {
            if (videoEnded) return;

            videoEnded = true;
            onVideoEnd?.Invoke();
            HandleVideoEnd();
        }

        private void HandleVideoEnd()
        {
            // Disable specified objects (like the video player camera)
            foreach (var obj in objectsToDisable)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }

            // Enable specified objects (like the main menu)
            foreach (var obj in objectsToEnable)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Check if video has ended.
        /// </summary>
        public bool HasEnded => videoEnded;

        /// <summary>
        /// Check if video is currently playing.
        /// </summary>
        public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;
    }
}
