using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BobsPetroleum.Core;
using BobsPetroleum.Player;
using BobsPetroleum.Systems;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Centralized HUD manager displaying health, stamina, money, objectives, and interactions.
    /// Essential for player feedback in an S-tier game.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("Health Display")]
        [Tooltip("Health bar fill image")]
        public Image healthBarFill;

        [Tooltip("Health text display")]
        public TMP_Text healthText;

        [Tooltip("Health bar animator for damage flash")]
        public Animator healthBarAnimator;

        [Tooltip("Low health warning threshold")]
        [Range(0f, 1f)]
        public float lowHealthThreshold = 0.3f;

        [Tooltip("Low health warning image/overlay")]
        public Image lowHealthOverlay;

        [Header("Stamina Display")]
        [Tooltip("Stamina bar fill image")]
        public Image staminaBarFill;

        [Tooltip("Stamina bar container (hides when full)")]
        public GameObject staminaBarContainer;

        [Tooltip("Hide stamina bar when full")]
        public bool hideWhenFull = true;

        [Tooltip("Time before hiding full stamina bar")]
        public float hideDelay = 2f;

        [Header("Money Display")]
        [Tooltip("Money text display")]
        public TMP_Text moneyText;

        [Tooltip("Money gained popup text")]
        public TMP_Text moneyGainedText;

        [Tooltip("Money format string")]
        public string moneyFormat = "${0}";

        [Header("Day/Time Display")]
        [Tooltip("Current day text")]
        public TMP_Text dayText;

        [Tooltip("Current time text")]
        public TMP_Text timeText;

        [Tooltip("Day format string")]
        public string dayFormat = "Day {0}";

        [Header("Interaction Prompt")]
        [Tooltip("Interaction prompt text")]
        public TMP_Text interactionPromptText;

        [Tooltip("Interaction prompt container")]
        public GameObject interactionPromptContainer;

        [Header("Crosshair")]
        [Tooltip("Crosshair image")]
        public Image crosshair;

        [Tooltip("Crosshair color when hovering interactable")]
        public Color interactableCrosshairColor = Color.green;

        [Tooltip("Default crosshair color")]
        public Color defaultCrosshairColor = Color.white;

        [Header("Objectives")]
        [Tooltip("Current objective text")]
        public TMP_Text objectiveText;

        [Tooltip("Objective container")]
        public GameObject objectiveContainer;

        [Header("Notification")]
        [Tooltip("Notification text for popups")]
        public TMP_Text notificationText;

        [Tooltip("Notification animator")]
        public Animator notificationAnimator;

        [Header("Compass")]
        [Tooltip("Compass image (rotates with player)")]
        public RectTransform compassBar;

        [Tooltip("Compass marker prefab")]
        public GameObject compassMarkerPrefab;

        [Header("Minimap")]
        [Tooltip("Minimap camera")]
        public Camera minimapCamera;

        [Tooltip("Minimap player icon")]
        public RectTransform minimapPlayerIcon;

        [Header("Damage Indicators")]
        [Tooltip("Damage vignette overlay")]
        public Image damageVignette;

        [Tooltip("Damage indicator prefab (directional)")]
        public GameObject damageIndicatorPrefab;

        [Tooltip("Damage indicator container")]
        public Transform damageIndicatorContainer;

        [Header("Fade")]
        [Tooltip("Full screen fade image")]
        public Image fadeImage;

        // Private state
        private PlayerController player;
        private PlayerHealth playerHealth;
        private PlayerInventory playerInventory;
        private float staminaHideTimer;
        private float lastDisplayedMoney;
        private Color staminaBarDefaultColor;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (staminaBarFill != null)
            {
                staminaBarDefaultColor = staminaBarFill.color;
            }
        }

        private void Start()
        {
            // Hide optional elements
            HideInteractionPrompt();
            if (lowHealthOverlay != null) lowHealthOverlay.gameObject.SetActive(false);
            if (damageVignette != null) damageVignette.color = new Color(1, 0, 0, 0);
        }

        private void Update()
        {
            UpdateHealthDisplay();
            UpdateStaminaDisplay();
            UpdateMoneyDisplay();
            UpdateDayTimeDisplay();
            UpdateCompass();
            UpdateLowHealthEffect();
        }

        #region Player Registration

        /// <summary>
        /// Register the local player for HUD tracking.
        /// </summary>
        public void RegisterPlayer(PlayerController playerController)
        {
            player = playerController;
            playerHealth = player.GetComponent<PlayerHealth>();
            playerInventory = player.GetComponent<PlayerInventory>();

            // Subscribe to events
            if (playerHealth != null)
            {
                playerHealth.onDamaged.AddListener(OnPlayerDamaged);
                playerHealth.onHealed.AddListener(OnPlayerHealed);
            }

            if (player != null)
            {
                player.onStaminaChanged.AddListener(OnStaminaChanged);
            }
        }

        #endregion

        #region Health Display

        private void UpdateHealthDisplay()
        {
            if (playerHealth == null) return;

            float healthPercent = playerHealth.CurrentHealth / playerHealth.maxHealth;

            // Update bar
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = healthPercent;

                // Color based on health
                if (healthPercent <= lowHealthThreshold)
                {
                    healthBarFill.color = Color.red;
                }
                else if (healthPercent <= 0.5f)
                {
                    healthBarFill.color = Color.yellow;
                }
                else
                {
                    healthBarFill.color = Color.green;
                }
            }

            // Update text
            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.maxHealth)}";
            }
        }

        private void UpdateLowHealthEffect()
        {
            if (playerHealth == null || lowHealthOverlay == null) return;

            float healthPercent = playerHealth.CurrentHealth / playerHealth.maxHealth;

            if (healthPercent <= lowHealthThreshold && !playerHealth.IsDead)
            {
                lowHealthOverlay.gameObject.SetActive(true);

                // Pulse effect
                float pulse = Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f;
                float alpha = Mathf.Lerp(0.1f, 0.4f, pulse) * (1f - (healthPercent / lowHealthThreshold));
                lowHealthOverlay.color = new Color(1f, 0f, 0f, alpha);
            }
            else
            {
                lowHealthOverlay.gameObject.SetActive(false);
            }
        }

        private void OnPlayerDamaged(float damage)
        {
            // Flash health bar
            if (healthBarAnimator != null)
            {
                healthBarAnimator.SetTrigger("Flash");
            }

            // Show damage vignette
            ShowDamageVignette();

            // Play hurt sound through AudioManager
            AudioManager.Instance?.PlaySFX2D(null); // Would use a hurt sound
        }

        private void OnPlayerHealed(float amount)
        {
            // Optional heal flash effect
        }

        private void ShowDamageVignette()
        {
            if (damageVignette != null)
            {
                StopCoroutine(nameof(FadeDamageVignette));
                damageVignette.color = new Color(1, 0, 0, 0.5f);
                StartCoroutine(nameof(FadeDamageVignette));
            }
        }

        private System.Collections.IEnumerator FadeDamageVignette()
        {
            float duration = 0.5f;
            float timer = 0f;
            Color startColor = damageVignette.color;
            Color endColor = new Color(1, 0, 0, 0);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                damageVignette.color = Color.Lerp(startColor, endColor, timer / duration);
                yield return null;
            }

            damageVignette.color = endColor;
        }

        #endregion

        #region Stamina Display

        private void OnStaminaChanged(float staminaPercent)
        {
            staminaHideTimer = hideDelay;
        }

        private void UpdateStaminaDisplay()
        {
            if (player == null || staminaBarFill == null) return;

            float staminaPercent = player.StaminaPercentage;

            // Update bar fill
            staminaBarFill.fillAmount = staminaPercent;

            // Color based on stamina
            if (staminaPercent <= 0.2f)
            {
                staminaBarFill.color = Color.red;
            }
            else if (staminaPercent <= 0.5f)
            {
                staminaBarFill.color = Color.yellow;
            }
            else
            {
                staminaBarFill.color = staminaBarDefaultColor;
            }

            // Show/hide based on fullness
            if (hideWhenFull && staminaBarContainer != null)
            {
                if (staminaPercent >= 1f)
                {
                    staminaHideTimer -= Time.deltaTime;
                    if (staminaHideTimer <= 0f)
                    {
                        staminaBarContainer.SetActive(false);
                    }
                }
                else
                {
                    staminaBarContainer.SetActive(true);
                    staminaHideTimer = hideDelay;
                }
            }
        }

        #endregion

        #region Money Display

        private void UpdateMoneyDisplay()
        {
            if (playerInventory == null || moneyText == null) return;

            int currentMoney = playerInventory.Money;

            // Animate money change
            if (currentMoney != lastDisplayedMoney)
            {
                int diff = currentMoney - (int)lastDisplayedMoney;
                if (diff > 0)
                {
                    ShowMoneyGained(diff);
                }
                lastDisplayedMoney = currentMoney;
            }

            moneyText.text = string.Format(moneyFormat, currentMoney);
        }

        private void ShowMoneyGained(int amount)
        {
            if (moneyGainedText != null)
            {
                moneyGainedText.text = $"+${amount}";
                moneyGainedText.gameObject.SetActive(true);

                // Animate and hide
                StopCoroutine(nameof(HideMoneyGainedCoroutine));
                StartCoroutine(nameof(HideMoneyGainedCoroutine));
            }
        }

        private System.Collections.IEnumerator HideMoneyGainedCoroutine()
        {
            yield return new WaitForSeconds(1.5f);
            if (moneyGainedText != null)
            {
                moneyGainedText.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Day/Time Display

        private void UpdateDayTimeDisplay()
        {
            // Day
            if (dayText != null && GameManager.Instance != null)
            {
                dayText.text = string.Format(dayFormat, GameManager.Instance.currentDay);
            }

            // Time
            if (timeText != null && DayNightCycle.Instance != null)
            {
                timeText.text = DayNightCycle.Instance.GetTimeString();
            }
        }

        #endregion

        #region Interaction Prompt

        /// <summary>
        /// Show interaction prompt with text.
        /// </summary>
        public void ShowInteractionPrompt(string prompt)
        {
            if (interactionPromptContainer != null)
            {
                interactionPromptContainer.SetActive(true);
            }

            if (interactionPromptText != null)
            {
                interactionPromptText.text = prompt;
            }

            // Change crosshair color
            if (crosshair != null)
            {
                crosshair.color = interactableCrosshairColor;
            }
        }

        /// <summary>
        /// Hide interaction prompt.
        /// </summary>
        public void HideInteractionPrompt()
        {
            if (interactionPromptContainer != null)
            {
                interactionPromptContainer.SetActive(false);
            }

            // Reset crosshair color
            if (crosshair != null)
            {
                crosshair.color = defaultCrosshairColor;
            }
        }

        #endregion

        #region Objectives

        /// <summary>
        /// Set current objective text.
        /// </summary>
        public void SetObjective(string objective)
        {
            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }

            if (objectiveContainer != null)
            {
                objectiveContainer.SetActive(!string.IsNullOrEmpty(objective));
            }
        }

        /// <summary>
        /// Clear current objective.
        /// </summary>
        public void ClearObjective()
        {
            SetObjective("");
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Show a notification popup.
        /// </summary>
        public void ShowNotification(string message, float duration = 3f)
        {
            if (notificationText != null)
            {
                notificationText.text = message;

                if (notificationAnimator != null)
                {
                    notificationAnimator.SetTrigger("Show");
                }

                CancelInvoke(nameof(HideNotification));
                Invoke(nameof(HideNotification), duration);
            }
        }

        private void HideNotification()
        {
            if (notificationAnimator != null)
            {
                notificationAnimator.SetTrigger("Hide");
            }
        }

        #endregion

        #region Compass

        private void UpdateCompass()
        {
            if (compassBar == null || player == null) return;

            // Rotate compass based on player facing direction
            float rotation = player.transform.eulerAngles.y;
            compassBar.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        #endregion

        #region Screen Effects

        /// <summary>
        /// Fade screen to black.
        /// </summary>
        public void FadeToBlack(float duration = 1f)
        {
            if (fadeImage != null)
            {
                StartCoroutine(FadeCoroutine(new Color(0, 0, 0, 0), Color.black, duration));
            }
        }

        /// <summary>
        /// Fade screen from black.
        /// </summary>
        public void FadeFromBlack(float duration = 1f)
        {
            if (fadeImage != null)
            {
                StartCoroutine(FadeCoroutine(Color.black, new Color(0, 0, 0, 0), duration));
            }
        }

        private System.Collections.IEnumerator FadeCoroutine(Color from, Color to, float duration)
        {
            fadeImage.gameObject.SetActive(true);
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                fadeImage.color = Color.Lerp(from, to, timer / duration);
                yield return null;
            }

            fadeImage.color = to;

            if (to.a <= 0)
            {
                fadeImage.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Visibility Control

        /// <summary>
        /// Show/hide entire HUD.
        /// </summary>
        public void SetHUDVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// Show/hide crosshair.
        /// </summary>
        public void SetCrosshairVisible(bool visible)
        {
            if (crosshair != null)
            {
                crosshair.gameObject.SetActive(visible);
            }
        }

        #endregion
    }
}
