using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using BobsPetroleum.Core;
using BobsPetroleum.Player;
using BobsPetroleum.Systems;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// COMPLETE HUD MANAGER - Everything you need on screen!
    /// Displays health, money, Bob's status, objectives, and more.
    ///
    /// SETUP:
    /// 1. Create Canvas with all UI elements
    /// 2. Drag elements into the slots below
    /// 3. Done! HUD auto-updates everything.
    ///
    /// UI ELEMENTS NEEDED:
    /// - Health bar (Image with Fill)
    /// - Money text (TMP_Text)
    /// - Bob's health bar (Image with Fill)
    /// - Hamburger count (TMP_Text)
    /// - Objective text (TMP_Text)
    /// - Crosshair (Image)
    /// - Notification popup (TMP_Text + Animator)
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        [Header("=== PLAYER HEALTH ===")]
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

        [Header("=== BOB'S STATUS ===")]
        [Tooltip("Bob's health bar fill")]
        public Image bobHealthBarFill;

        [Tooltip("Bob's health text")]
        public TMP_Text bobHealthText;

        [Tooltip("Bob's status text (Dying/Recovering/Revived)")]
        public TMP_Text bobStatusText;

        [Tooltip("Container for Bob's status (hide when not relevant)")]
        public GameObject bobStatusContainer;

        [Header("=== HAMBURGER COUNT ===")]
        [Tooltip("Hamburger count text")]
        public TMP_Text hamburgerCountText;

        [Tooltip("Hamburger icon image")]
        public Image hamburgerIcon;

        [Tooltip("Hamburger container")]
        public GameObject hamburgerContainer;

        [Header("=== NIGHT DISPLAY (7 Night Runs) ===")]
        [Tooltip("Current night text")]
        public TMP_Text nightText;

        [Tooltip("Night progress bar")]
        public Image nightProgressFill;

        [Tooltip("Night container (hide in Forever Mode)")]
        public GameObject nightContainer;

        [Header("=== FPS COUNTER ===")]
        [Tooltip("FPS text display")]
        public TMP_Text fpsText;

        [Tooltip("FPS container")]
        public GameObject fpsContainer;

        [Tooltip("Update FPS every X seconds")]
        public float fpsUpdateInterval = 0.5f;

        [Header("=== ITEM PICKUP NOTIFICATIONS ===")]
        [Tooltip("Item pickup text")]
        public TMP_Text itemPickupText;

        [Tooltip("Item pickup icon")]
        public Image itemPickupIcon;

        [Tooltip("Item pickup animator")]
        public Animator itemPickupAnimator;

        [Header("=== QUICK MESSAGE ===")]
        [Tooltip("Quick message text (center screen)")]
        public TMP_Text quickMessageText;

        [Tooltip("Quick message animator")]
        public Animator quickMessageAnimator;

        [Header("=== WEAPON DISPLAY ===")]
        [Tooltip("Current weapon name")]
        public TMP_Text weaponNameText;

        [Tooltip("Ammo count text")]
        public TMP_Text ammoText;

        [Tooltip("Weapon icon")]
        public Image weaponIcon;

        [Header("=== MINIMAP ===")]
        [Tooltip("Minimap camera")]
        public Camera minimapCamera;

        [Tooltip("Minimap player icon")]
        public RectTransform minimapPlayerIcon;

        [Header("=== DAMAGE INDICATORS ===")]
        [Tooltip("Damage vignette overlay")]
        public Image damageVignette;

        [Tooltip("Damage indicator prefab (directional)")]
        public GameObject damageIndicatorPrefab;

        [Tooltip("Damage indicator container")]
        public Transform damageIndicatorContainer;

        [Header("Fade")]
        [Tooltip("Full screen fade image")]
        public Image fadeImage;

        [Header("Player Reference (Auto-found)")]
        [Tooltip("Assigned automatically if not set")]
        public PlayerController player;
        public PlayerHealth playerHealth;

        // Private state
        private PlayerInventory playerInventory;
        private BobCharacter bob;
        private float staminaHideTimer;
        private float lastDisplayedMoney;
        private Color staminaBarDefaultColor;
        private Coroutine fadeVignetteCoroutine;
        private Coroutine hideMoneyCoroutine;

        // FPS tracking
        private float fpsTimer;
        private int frameCount;
        private float currentFPS;

        // Item pickup queue
        private Queue<ItemPickupInfo> itemPickupQueue = new Queue<ItemPickupInfo>();
        private bool isShowingPickup = false;

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
            // Auto-find player if not assigned
            AutoFindPlayer();

            // Auto-find Bob
            bob = BobCharacter.Instance ?? FindObjectOfType<BobCharacter>();

            // Hide optional elements
            HideInteractionPrompt();
            if (lowHealthOverlay != null) lowHealthOverlay.gameObject.SetActive(false);
            if (damageVignette != null) damageVignette.color = new Color(1, 0, 0, 0);

            // Setup night display based on game mode
            UpdateNightDisplayVisibility();

            // Setup FPS display based on settings
            UpdateFPSVisibility();
        }

        /// <summary>
        /// Auto-find player and register if not assigned.
        /// </summary>
        private void AutoFindPlayer()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerController>();
            }

            if (player != null)
            {
                RegisterPlayer(player);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            if (playerHealth != null)
            {
                playerHealth.onDamaged.RemoveListener(OnPlayerDamaged);
                playerHealth.onHealed.RemoveListener(OnPlayerHealed);
            }

            if (player != null)
            {
                player.onStaminaChanged.RemoveListener(OnStaminaChanged);
            }

            // Clean up coroutines
            if (fadeVignetteCoroutine != null) StopCoroutine(fadeVignetteCoroutine);
            if (hideMoneyCoroutine != null) StopCoroutine(hideMoneyCoroutine);
        }

        private void Update()
        {
            UpdateHealthDisplay();
            UpdateStaminaDisplay();
            UpdateMoneyDisplay();
            UpdateDayTimeDisplay();
            UpdateCompass();
            UpdateLowHealthEffect();
            UpdateBobStatus();
            UpdateHamburgerCount();
            UpdateNightProgress();
            UpdateFPS();
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
            if (playerHealth.maxHealth <= 0) return; // Prevent divide by zero

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
            if (playerHealth.maxHealth <= 0) return; // Prevent divide by zero

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

        [Header("Audio")]
        [Tooltip("Sound when player takes damage")]
        public AudioClip hurtSound;

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
            if (hurtSound != null)
            {
                AudioManager.Instance?.PlaySFX2D(hurtSound);
            }
        }

        private void OnPlayerHealed(float amount)
        {
            // Optional heal flash effect
        }

        private void ShowDamageVignette()
        {
            if (damageVignette != null)
            {
                if (fadeVignetteCoroutine != null)
                {
                    StopCoroutine(fadeVignetteCoroutine);
                }
                damageVignette.color = new Color(1, 0, 0, 0.5f);
                fadeVignetteCoroutine = StartCoroutine(FadeDamageVignette());
            }
        }

        private System.Collections.IEnumerator FadeDamageVignette()
        {
            if (damageVignette == null) yield break;

            float duration = 0.5f;
            float timer = 0f;
            Color startColor = damageVignette.color;
            Color endColor = new Color(1, 0, 0, 0);

            while (timer < duration)
            {
                timer += Time.deltaTime;
                if (damageVignette != null)
                {
                    damageVignette.color = Color.Lerp(startColor, endColor, timer / duration);
                }
                yield return null;
            }

            if (damageVignette != null)
            {
                damageVignette.color = endColor;
            }
            fadeVignetteCoroutine = null;
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
                if (hideMoneyCoroutine != null)
                {
                    StopCoroutine(hideMoneyCoroutine);
                }
                hideMoneyCoroutine = StartCoroutine(HideMoneyGainedCoroutine());
            }
        }

        private System.Collections.IEnumerator HideMoneyGainedCoroutine()
        {
            yield return new WaitForSeconds(1.5f);
            if (moneyGainedText != null)
            {
                moneyGainedText.gameObject.SetActive(false);
            }
            hideMoneyCoroutine = null;
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

        #region Bob Status Display

        private void UpdateBobStatus()
        {
            if (bob == null)
            {
                bob = BobCharacter.Instance ?? FindObjectOfType<BobCharacter>();
                if (bob == null)
                {
                    if (bobStatusContainer != null) bobStatusContainer.SetActive(false);
                    return;
                }
            }

            if (bobStatusContainer != null) bobStatusContainer.SetActive(true);

            // Update Bob's health bar
            if (bobHealthBarFill != null)
            {
                float healthPercent = bob.CurrentHealth / bob.maxHealth;
                bobHealthBarFill.fillAmount = healthPercent;

                // Color based on health
                if (healthPercent <= 0.25f)
                    bobHealthBarFill.color = Color.red;
                else if (healthPercent <= 0.5f)
                    bobHealthBarFill.color = new Color(1f, 0.5f, 0f); // Orange
                else if (healthPercent <= 0.75f)
                    bobHealthBarFill.color = Color.yellow;
                else
                    bobHealthBarFill.color = Color.green;
            }

            // Update health text
            if (bobHealthText != null)
            {
                bobHealthText.text = $"Bob: {Mathf.CeilToInt(bob.CurrentHealth)}/{Mathf.CeilToInt(bob.maxHealth)}";
            }

            // Update status text
            if (bobStatusText != null)
            {
                float healthPercent = bob.CurrentHealth / bob.maxHealth;
                if (bob.IsRevived)
                    bobStatusText.text = "<color=green>REVIVED!</color>";
                else if (bob.IsDead)
                    bobStatusText.text = "<color=red>DEAD!</color>";
                else if (healthPercent < 0.25f)
                    bobStatusText.text = "<color=red>CRITICAL!</color>";
                else if (healthPercent < 0.5f)
                    bobStatusText.text = "<color=orange>DYING</color>";
                else if (healthPercent < 0.75f)
                    bobStatusText.text = "<color=yellow>RECOVERING</color>";
                else
                    bobStatusText.text = "<color=green>STABLE</color>";
            }
        }

        #endregion

        #region Hamburger Display

        private void UpdateHamburgerCount()
        {
            if (playerInventory == null || hamburgerCountText == null) return;

            int count = playerInventory.Hamburgers;

            hamburgerCountText.text = count.ToString();

            // Pulse icon when you have hamburgers
            if (hamburgerIcon != null)
            {
                hamburgerIcon.color = count > 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }

        /// <summary>
        /// Flash hamburger display when picking one up.
        /// </summary>
        public void FlashHamburgerPickup()
        {
            if (hamburgerContainer != null)
            {
                StartCoroutine(FlashUIElement(hamburgerContainer));
            }
        }

        private IEnumerator FlashUIElement(GameObject element)
        {
            var group = element.GetComponent<CanvasGroup>();
            if (group == null) group = element.AddComponent<CanvasGroup>();

            for (int i = 0; i < 3; i++)
            {
                group.alpha = 0.5f;
                yield return new WaitForSeconds(0.1f);
                group.alpha = 1f;
                yield return new WaitForSeconds(0.1f);
            }
        }

        #endregion

        #region Night Progress (7 Night Runs)

        private void UpdateNightDisplayVisibility()
        {
            var gameState = GameStateManager.Instance;
            bool is7Night = gameState != null && gameState.isSevenNightRun;

            if (nightContainer != null)
            {
                nightContainer.SetActive(is7Night);
            }
        }

        private void UpdateNightProgress()
        {
            var gameState = GameStateManager.Instance;
            if (gameState == null || !gameState.isSevenNightRun) return;

            // Update night text
            if (nightText != null)
            {
                nightText.text = $"Night {gameState.currentNight} / 7";
            }

            // Update progress bar (progress through current night)
            if (nightProgressFill != null && DayNightCycle.Instance != null)
            {
                float nightProgress = DayNightCycle.Instance.NightProgress;
                nightProgressFill.fillAmount = nightProgress;
            }
        }

        #endregion

        #region FPS Counter

        private void UpdateFPSVisibility()
        {
            // Check settings
            bool showFPS = SettingsManager.Instance?.GetShowFPS() ?? false;

            if (fpsContainer != null)
            {
                fpsContainer.SetActive(showFPS);
            }
        }

        private void UpdateFPS()
        {
            if (fpsText == null || fpsContainer == null || !fpsContainer.activeSelf) return;

            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;

            if (fpsTimer >= fpsUpdateInterval)
            {
                currentFPS = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;

                // Color based on FPS
                Color fpsColor = Color.green;
                if (currentFPS < 30) fpsColor = Color.red;
                else if (currentFPS < 60) fpsColor = Color.yellow;

                fpsText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>{Mathf.RoundToInt(currentFPS)} FPS</color>";
            }
        }

        /// <summary>
        /// Toggle FPS display visibility.
        /// </summary>
        public void ToggleFPS()
        {
            if (fpsContainer != null)
            {
                fpsContainer.SetActive(!fpsContainer.activeSelf);
            }
        }

        #endregion

        #region Item Pickup Notifications

        /// <summary>
        /// Show item pickup notification.
        /// </summary>
        public void ShowItemPickup(string itemName, Sprite icon = null, int quantity = 1)
        {
            itemPickupQueue.Enqueue(new ItemPickupInfo
            {
                name = itemName,
                icon = icon,
                quantity = quantity
            });

            if (!isShowingPickup)
            {
                StartCoroutine(ProcessPickupQueue());
            }
        }

        private IEnumerator ProcessPickupQueue()
        {
            isShowingPickup = true;

            while (itemPickupQueue.Count > 0)
            {
                var pickup = itemPickupQueue.Dequeue();

                // Update UI
                if (itemPickupText != null)
                {
                    string qtyText = pickup.quantity > 1 ? $" x{pickup.quantity}" : "";
                    itemPickupText.text = $"+ {pickup.name}{qtyText}";
                }

                if (itemPickupIcon != null)
                {
                    if (pickup.icon != null)
                    {
                        itemPickupIcon.sprite = pickup.icon;
                        itemPickupIcon.gameObject.SetActive(true);
                    }
                    else
                    {
                        itemPickupIcon.gameObject.SetActive(false);
                    }
                }

                // Play animation
                if (itemPickupAnimator != null)
                {
                    itemPickupAnimator.SetTrigger("Show");
                }

                yield return new WaitForSeconds(1.5f);
            }

            isShowingPickup = false;
        }

        private struct ItemPickupInfo
        {
            public string name;
            public Sprite icon;
            public int quantity;
        }

        #endregion

        #region Quick Messages

        /// <summary>
        /// Show a quick message in the center of screen.
        /// </summary>
        public void ShowQuickMessage(string message, float duration = 2f)
        {
            if (quickMessageText != null)
            {
                quickMessageText.text = message;
            }

            if (quickMessageAnimator != null)
            {
                quickMessageAnimator.SetTrigger("Show");
            }

            CancelInvoke(nameof(HideQuickMessage));
            Invoke(nameof(HideQuickMessage), duration);
        }

        private void HideQuickMessage()
        {
            if (quickMessageAnimator != null)
            {
                quickMessageAnimator.SetTrigger("Hide");
            }
        }

        #endregion

        #region Weapon Display

        /// <summary>
        /// Update weapon display.
        /// </summary>
        public void UpdateWeaponDisplay(string weaponName, int currentAmmo, int maxAmmo, Sprite icon = null)
        {
            if (weaponNameText != null)
            {
                weaponNameText.text = weaponName;
            }

            if (ammoText != null)
            {
                if (maxAmmo > 0)
                {
                    ammoText.text = $"{currentAmmo} / {maxAmmo}";

                    // Color based on ammo
                    float ammoPercent = (float)currentAmmo / maxAmmo;
                    if (ammoPercent <= 0.25f)
                        ammoText.color = Color.red;
                    else if (ammoPercent <= 0.5f)
                        ammoText.color = Color.yellow;
                    else
                        ammoText.color = Color.white;
                }
                else
                {
                    ammoText.text = "∞"; // Infinite ammo
                    ammoText.color = Color.white;
                }
            }

            if (weaponIcon != null && icon != null)
            {
                weaponIcon.sprite = icon;
            }
        }

        /// <summary>
        /// Hide weapon display (when unarmed).
        /// </summary>
        public void HideWeaponDisplay()
        {
            if (weaponNameText != null) weaponNameText.text = "";
            if (ammoText != null) ammoText.text = "";
        }

        #endregion
    }
}
