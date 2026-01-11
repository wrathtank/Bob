using UnityEngine;
using UnityEngine.Events;
using BobsPetroleum.Core;

namespace BobsPetroleum.Player
{
    /// <summary>
    /// Player flashlight with battery system, flicker effects, and horror integration.
    /// Essential for night exploration in horror games.
    /// </summary>
    public class Flashlight : MonoBehaviour
    {
        [Header("Light Settings")]
        [Tooltip("Spotlight component for the flashlight")]
        public Light spotLight;

        [Tooltip("Toggle key")]
        public KeyCode toggleKey = KeyCode.G;

        [Tooltip("Is flashlight on by default")]
        public bool startOn = false;

        [Header("Light Properties")]
        [Tooltip("Normal light intensity")]
        public float normalIntensity = 1.5f;

        [Tooltip("Light range")]
        public float lightRange = 20f;

        [Tooltip("Spotlight angle")]
        public float spotAngle = 50f;

        [Tooltip("Inner spot angle")]
        public float innerSpotAngle = 30f;

        [Header("Battery System")]
        [Tooltip("Enable battery drain")]
        public bool useBattery = true;

        [Tooltip("Maximum battery (seconds of use)")]
        public float maxBattery = 300f;

        [Tooltip("Current battery level")]
        public float currentBattery = 300f;

        [Tooltip("Battery drain per second")]
        public float drainRate = 1f;

        [Tooltip("Low battery threshold (0-1)")]
        [Range(0f, 1f)]
        public float lowBatteryThreshold = 0.2f;

        [Header("Flicker Effects")]
        [Tooltip("Enable flickering when low battery")]
        public bool flickerOnLowBattery = true;

        [Tooltip("Flicker intensity variation")]
        public float flickerIntensity = 0.3f;

        [Tooltip("Flicker speed")]
        public float flickerSpeed = 15f;

        [Tooltip("Chance for random horror flicker")]
        [Range(0f, 1f)]
        public float horrorFlickerChance = 0.01f;

        [Header("Audio")]
        [Tooltip("Toggle on sound")]
        public AudioClip toggleOnSound;

        [Tooltip("Toggle off sound")]
        public AudioClip toggleOffSound;

        [Tooltip("Low battery warning sound")]
        public AudioClip lowBatterySound;

        [Tooltip("Battery dead sound")]
        public AudioClip batteryDeadSound;

        [Tooltip("Flicker sound")]
        public AudioClip flickerSound;

        [Header("Visual")]
        [Tooltip("Light cone mesh (optional)")]
        public MeshRenderer lightConeMesh;

        [Tooltip("Volumetric light material (optional)")]
        public Material volumetricMaterial;

        [Header("Events")]
        public UnityEvent onTurnOn;
        public UnityEvent onTurnOff;
        public UnityEvent onBatteryDepleted;
        public UnityEvent onBatteryLow;
        public UnityEvent<float> onBatteryChanged;

        // State
        private bool isOn = false;
        private bool isLowBattery = false;
        private AudioSource audioSource;
        private float flickerTimer = 0f;
        private float horrorFlickerTimer = 0f;
        private bool wasLowBattery = false;
        private float targetIntensity;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // Create light if not assigned
            if (spotLight == null)
            {
                spotLight = GetComponentInChildren<Light>();
                if (spotLight == null)
                {
                    GameObject lightObj = new GameObject("FlashlightSpot");
                    lightObj.transform.SetParent(transform);
                    lightObj.transform.localPosition = Vector3.zero;
                    lightObj.transform.localRotation = Quaternion.identity;
                    spotLight = lightObj.AddComponent<Light>();
                    spotLight.type = LightType.Spot;
                }
            }

            // Configure light
            spotLight.range = lightRange;
            spotLight.spotAngle = spotAngle;
            spotLight.innerSpotAngle = innerSpotAngle;
            spotLight.intensity = normalIntensity;

            targetIntensity = normalIntensity;
        }

        private void Start()
        {
            if (startOn)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }

        private void Update()
        {
            // Toggle input
            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }

            if (isOn)
            {
                UpdateBattery();
                UpdateFlicker();
            }
        }

        #region Toggle Control

        /// <summary>
        /// Toggle flashlight on/off.
        /// </summary>
        public void Toggle()
        {
            if (isOn)
            {
                TurnOff();
            }
            else
            {
                TurnOn();
            }
        }

        /// <summary>
        /// Turn flashlight on.
        /// </summary>
        public void TurnOn()
        {
            if (useBattery && currentBattery <= 0)
            {
                // Can't turn on without battery
                PlaySound(batteryDeadSound);
                return;
            }

            isOn = true;
            spotLight.enabled = true;

            if (lightConeMesh != null)
            {
                lightConeMesh.enabled = true;
            }

            PlaySound(toggleOnSound);
            onTurnOn?.Invoke();
        }

        /// <summary>
        /// Turn flashlight off.
        /// </summary>
        public void TurnOff()
        {
            isOn = false;
            spotLight.enabled = false;

            if (lightConeMesh != null)
            {
                lightConeMesh.enabled = false;
            }

            PlaySound(toggleOffSound);
            onTurnOff?.Invoke();
        }

        #endregion

        #region Battery System

        private void UpdateBattery()
        {
            if (!useBattery) return;

            // Drain battery
            currentBattery -= drainRate * Time.deltaTime;
            currentBattery = Mathf.Max(0, currentBattery);

            onBatteryChanged?.Invoke(currentBattery / maxBattery);

            // Check low battery
            float batteryPercent = currentBattery / maxBattery;
            isLowBattery = batteryPercent <= lowBatteryThreshold;

            if (isLowBattery && !wasLowBattery)
            {
                // Just became low
                PlaySound(lowBatterySound);
                onBatteryLow?.Invoke();
            }
            wasLowBattery = isLowBattery;

            // Battery depleted
            if (currentBattery <= 0)
            {
                TurnOff();
                PlaySound(batteryDeadSound);
                onBatteryDepleted?.Invoke();
            }
        }

        /// <summary>
        /// Recharge battery.
        /// </summary>
        public void RechargeBattery(float amount)
        {
            currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
            onBatteryChanged?.Invoke(currentBattery / maxBattery);
        }

        /// <summary>
        /// Fully recharge battery.
        /// </summary>
        public void FullRecharge()
        {
            currentBattery = maxBattery;
            onBatteryChanged?.Invoke(1f);
        }

        /// <summary>
        /// Set battery level directly (0-1).
        /// </summary>
        public void SetBatteryLevel(float percent)
        {
            currentBattery = maxBattery * Mathf.Clamp01(percent);
            onBatteryChanged?.Invoke(currentBattery / maxBattery);
        }

        #endregion

        #region Flicker Effects

        private void UpdateFlicker()
        {
            targetIntensity = normalIntensity;

            // Low battery flicker
            if (flickerOnLowBattery && isLowBattery)
            {
                flickerTimer += Time.deltaTime * flickerSpeed;
                float flicker = Mathf.PerlinNoise(flickerTimer, 0f) * 2f - 1f;
                targetIntensity = normalIntensity + flicker * flickerIntensity;

                // Occasional full flicker off
                if (Random.value < 0.005f)
                {
                    targetIntensity = 0f;
                    PlaySound(flickerSound);
                }
            }

            // Horror random flicker
            horrorFlickerTimer -= Time.deltaTime;
            if (horrorFlickerTimer <= 0f)
            {
                horrorFlickerTimer = Random.Range(5f, 30f);

                if (Random.value < horrorFlickerChance)
                {
                    StartCoroutine(HorrorFlicker());
                }
            }

            // Apply intensity
            spotLight.intensity = Mathf.Lerp(spotLight.intensity, targetIntensity, Time.deltaTime * 10f);
        }

        private System.Collections.IEnumerator HorrorFlicker()
        {
            // Creepy flicker sequence
            float originalIntensity = spotLight.intensity;

            for (int i = 0; i < Random.Range(3, 8); i++)
            {
                spotLight.intensity = Random.value < 0.5f ? 0f : normalIntensity * 0.3f;
                PlaySound(flickerSound);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
            }

            spotLight.intensity = originalIntensity;
        }

        /// <summary>
        /// Force a horror flicker effect.
        /// </summary>
        public void TriggerHorrorFlicker()
        {
            if (isOn)
            {
                StartCoroutine(HorrorFlicker());
            }
        }

        /// <summary>
        /// Force flashlight off temporarily (for horror events).
        /// </summary>
        public void ForceOffTemporarily(float duration)
        {
            if (isOn)
            {
                StartCoroutine(TemporaryOff(duration));
            }
        }

        private System.Collections.IEnumerator TemporaryOff(float duration)
        {
            spotLight.enabled = false;
            PlaySound(flickerSound);

            yield return new WaitForSeconds(duration);

            if (isOn && currentBattery > 0)
            {
                spotLight.enabled = true;
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

        public bool IsOn => isOn;
        public float BatteryPercent => currentBattery / maxBattery;
        public bool IsLowBattery => isLowBattery;
        public bool HasBattery => currentBattery > 0;

        #endregion
    }

    /// <summary>
    /// Battery pickup for flashlight.
    /// </summary>
    public class BatteryPickup : MonoBehaviour, IInteractable
    {
        [Header("Battery Settings")]
        [Tooltip("Recharge amount (seconds)")]
        public float rechargeAmount = 120f;

        [Tooltip("Full recharge instead of partial")]
        public bool fullRecharge = false;

        [Header("Audio")]
        public AudioClip pickupSound;

        public void Interact(PlayerController player)
        {
            var flashlight = player.GetComponentInChildren<Flashlight>();
            if (flashlight != null)
            {
                if (fullRecharge)
                {
                    flashlight.FullRecharge();
                }
                else
                {
                    flashlight.RechargeBattery(rechargeAmount);
                }

                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }

                Destroy(gameObject);
            }
        }

        public string GetInteractionPrompt()
        {
            return fullRecharge ? "Pick up Battery Pack" : "Pick up Battery";
        }
    }
}
