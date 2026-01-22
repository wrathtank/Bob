using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Attach to a UI Image to control fade in/out transitions.
    /// Use for opening sequences, death screens, and scene transitions.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class FadeController : MonoBehaviour
    {
        public static FadeController Instance { get; private set; }

        [Header("Fade Settings")]
        [Tooltip("Duration of fade in seconds")]
        public float fadeDuration = 1f;

        [Tooltip("Curve for fade animation (optional)")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Tooltip("Start faded in (100% opacity) on awake")]
        public bool startFadedIn = true;

        [Tooltip("Automatically fade out on start")]
        public bool autoFadeOutOnStart = true;

        [Tooltip("Delay before auto fade out")]
        public float autoFadeDelay = 0.5f;

        [Header("Colors")]
        [Tooltip("Color when fully faded in (default black)")]
        public Color fadeInColor = Color.black;

        [Tooltip("Color when fully faded out (default transparent black)")]
        public Color fadeOutColor = new Color(0, 0, 0, 0);

        [Header("Events")]
        public UnityEvent onFadeInStart;
        public UnityEvent onFadeInComplete;
        public UnityEvent onFadeOutStart;
        public UnityEvent onFadeOutComplete;

        private Image fadeImage;
        private Coroutine currentFade;
        private bool isFading = false;

        private void Awake()
        {
            // Singleton pattern - but allows multiple instances if needed
            if (Instance == null)
            {
                Instance = this;
            }

            fadeImage = GetComponent<Image>();

            if (startFadedIn)
            {
                SetOpacity(1f);
            }
            else
            {
                SetOpacity(0f);
            }
        }

        private void Start()
        {
            if (autoFadeOutOnStart && startFadedIn)
            {
                StartCoroutine(AutoFadeOutCoroutine());
            }
        }

        private IEnumerator AutoFadeOutCoroutine()
        {
            yield return new WaitForSeconds(autoFadeDelay);
            FadeOut();
        }

        /// <summary>
        /// Fade from transparent to opaque (0% to 100% opacity).
        /// Use for death screens, scene transitions, etc.
        /// </summary>
        public void FadeIn()
        {
            FadeIn(fadeDuration);
        }

        /// <summary>
        /// Fade from transparent to opaque with custom duration.
        /// </summary>
        public void FadeIn(float duration)
        {
            if (currentFade != null)
                StopCoroutine(currentFade);

            currentFade = StartCoroutine(FadeCoroutine(0f, 1f, duration, true));
        }

        /// <summary>
        /// Fade from opaque to transparent (100% to 0% opacity).
        /// Use for opening sequences, respawn, etc.
        /// </summary>
        public void FadeOut()
        {
            FadeOut(fadeDuration);
        }

        /// <summary>
        /// Fade from opaque to transparent with custom duration.
        /// </summary>
        public void FadeOut(float duration)
        {
            if (currentFade != null)
                StopCoroutine(currentFade);

            currentFade = StartCoroutine(FadeCoroutine(1f, 0f, duration, false));
        }

        /// <summary>
        /// Fade to a specific opacity value (0-1).
        /// </summary>
        public void FadeTo(float targetOpacity)
        {
            FadeTo(targetOpacity, fadeDuration);
        }

        /// <summary>
        /// Fade to a specific opacity value with custom duration.
        /// </summary>
        public void FadeTo(float targetOpacity, float duration)
        {
            if (currentFade != null)
                StopCoroutine(currentFade);

            float currentOpacity = fadeImage.color.a;
            bool isFadingIn = targetOpacity > currentOpacity;

            currentFade = StartCoroutine(FadeCoroutine(currentOpacity, targetOpacity, duration, isFadingIn));
        }

        /// <summary>
        /// Immediately set opacity without animation.
        /// </summary>
        public void SetOpacity(float opacity)
        {
            Color color = Color.Lerp(fadeOutColor, fadeInColor, opacity);
            fadeImage.color = color;
        }

        /// <summary>
        /// Perform a full fade cycle: fade in, wait, then fade out.
        /// Useful for transition effects.
        /// </summary>
        public void FadeInThenOut(float holdDuration = 0.5f)
        {
            StartCoroutine(FadeInThenOutCoroutine(holdDuration));
        }

        /// <summary>
        /// Perform a full fade cycle: fade out, wait, then fade in.
        /// Useful for spawn effects.
        /// </summary>
        public void FadeOutThenIn(float holdDuration = 0.5f)
        {
            StartCoroutine(FadeOutThenInCoroutine(holdDuration));
        }

        private IEnumerator FadeCoroutine(float startOpacity, float endOpacity, float duration, bool isFadingIn)
        {
            isFading = true;

            if (isFadingIn)
                onFadeInStart?.Invoke();
            else
                onFadeOutStart?.Invoke();

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curvedT = fadeCurve.Evaluate(t);
                float currentOpacity = Mathf.Lerp(startOpacity, endOpacity, curvedT);
                SetOpacity(currentOpacity);
                yield return null;
            }

            SetOpacity(endOpacity);
            isFading = false;

            if (isFadingIn)
                onFadeInComplete?.Invoke();
            else
                onFadeOutComplete?.Invoke();

            currentFade = null;
        }

        private IEnumerator FadeInThenOutCoroutine(float holdDuration)
        {
            yield return FadeCoroutine(0f, 1f, fadeDuration, true);
            yield return new WaitForSeconds(holdDuration);
            yield return FadeCoroutine(1f, 0f, fadeDuration, false);
        }

        private IEnumerator FadeOutThenInCoroutine(float holdDuration)
        {
            yield return FadeCoroutine(1f, 0f, fadeDuration, false);
            yield return new WaitForSeconds(holdDuration);
            yield return FadeCoroutine(0f, 1f, fadeDuration, true);
        }

        /// <summary>
        /// Check if currently fading.
        /// </summary>
        public bool IsFading()
        {
            return isFading;
        }

        /// <summary>
        /// Stop current fade immediately.
        /// </summary>
        public void StopFade()
        {
            if (currentFade != null)
            {
                StopCoroutine(currentFade);
                currentFade = null;
                isFading = false;
            }
        }

        /// <summary>
        /// Set the fade colors.
        /// </summary>
        public void SetColors(Color inColor, Color outColor)
        {
            fadeInColor = inColor;
            fadeOutColor = outColor;
        }
    }
}
