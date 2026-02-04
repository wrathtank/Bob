using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace BobsPetroleum.Combat
{
    /// <summary>
    /// Simple gun system - point and shoot style.
    /// No need for complex holding animations - just arms out and fire!
    /// Perfect for Lego Island / Goat Simulator style gameplay.
    /// </summary>
    public class SimpleGunSystem : MonoBehaviour
    {
        public static SimpleGunSystem Instance { get; private set; }

        [Header("Current Weapon")]
        [Tooltip("Currently equipped weapon")]
        public WeaponData currentWeapon;

        [Tooltip("List of owned weapons")]
        public List<WeaponData> ownedWeapons = new List<WeaponData>();

        [Header("Input")]
        [Tooltip("Fire button")]
        public KeyCode fireKey = KeyCode.Mouse0;

        [Tooltip("Aim button (optional)")]
        public KeyCode aimKey = KeyCode.Mouse1;

        [Tooltip("Reload key")]
        public KeyCode reloadKey = KeyCode.R;

        [Tooltip("Switch weapon key")]
        public KeyCode switchKey = KeyCode.Q;

        [Header("Shooting Origin")]
        [Tooltip("Where bullets come from (usually camera)")]
        public Transform shootOrigin;

        [Tooltip("Muzzle flash position")]
        public Transform muzzlePoint;

        [Header("Visual Effects")]
        [Tooltip("Muzzle flash prefab")]
        public GameObject muzzleFlashPrefab;

        [Tooltip("Bullet trail prefab (line renderer)")]
        public GameObject bulletTrailPrefab;

        [Tooltip("Impact effect prefab")]
        public GameObject impactEffectPrefab;

        [Tooltip("Blood impact prefab")]
        public GameObject bloodEffectPrefab;

        [Header("Audio")]
        [Tooltip("Empty/no ammo sound")]
        public AudioClip emptySound;

        [Tooltip("Reload sound")]
        public AudioClip reloadSound;

        [Tooltip("Weapon switch sound")]
        public AudioClip switchSound;

        [Header("Arm Visual (Simple Mode)")]
        [Tooltip("Arms GameObject - show when weapon equipped")]
        public GameObject armsVisual;

        [Tooltip("Arm animator for simple recoil")]
        public Animator armAnimator;

        [Header("UI")]
        [Tooltip("Show ammo on HUD")]
        public bool showAmmoUI = true;

        [Tooltip("Crosshair changes when aiming")]
        public bool dynamicCrosshair = true;

        [Header("Events")]
        public UnityEvent<WeaponData> onWeaponFired;
        public UnityEvent<WeaponData> onWeaponReloaded;
        public UnityEvent<WeaponData> onWeaponSwitched;
        public UnityEvent<GameObject, float> onTargetHit;

        // Runtime
        private int currentAmmo;
        private int currentMagazine;
        private float fireTimer;
        private float reloadTimer;
        private bool isReloading;
        private bool isAiming;
        private int currentWeaponIndex;
        private AudioSource audioSource;
        private Camera mainCam;

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

            mainCam = Camera.main;

            // Auto-find shoot origin
            if (shootOrigin == null && mainCam != null)
            {
                shootOrigin = mainCam.transform;
            }
        }

        private void Start()
        {
            // Initialize with first weapon if we have any
            if (ownedWeapons.Count > 0 && currentWeapon == null)
            {
                EquipWeapon(0);
            }
            else if (currentWeapon != null)
            {
                InitializeWeapon();
            }

            UpdateArmsVisual();
        }

        private void Update()
        {
            // Timers
            if (fireTimer > 0) fireTimer -= Time.deltaTime;
            if (reloadTimer > 0)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0)
                {
                    FinishReload();
                }
            }

            // Input
            HandleInput();
        }

        private void HandleInput()
        {
            if (currentWeapon == null) return;
            if (isReloading) return;

            // Fire
            if (currentWeapon.isAutomatic)
            {
                if (Input.GetKey(fireKey))
                {
                    TryFire();
                }
            }
            else
            {
                if (Input.GetKeyDown(fireKey))
                {
                    TryFire();
                }
            }

            // Aim
            isAiming = Input.GetKey(aimKey);

            // Reload
            if (Input.GetKeyDown(reloadKey))
            {
                TryReload();
            }

            // Switch weapon
            if (Input.GetKeyDown(switchKey))
            {
                SwitchToNextWeapon();
            }

            // Number keys for weapon selection
            for (int i = 0; i < 9 && i < ownedWeapons.Count; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    EquipWeapon(i);
                }
            }
        }

        #region Shooting

        /// <summary>
        /// Try to fire the current weapon.
        /// </summary>
        public void TryFire()
        {
            if (currentWeapon == null) return;
            if (fireTimer > 0) return;
            if (isReloading) return;

            if (currentMagazine <= 0)
            {
                // Out of ammo in magazine
                PlaySound(emptySound);
                TryReload();
                return;
            }

            // Fire!
            Fire();
        }

        private void Fire()
        {
            fireTimer = 1f / currentWeapon.fireRate;
            currentMagazine--;

            // Play sound
            PlaySound(currentWeapon.fireSound);

            // Muzzle flash
            SpawnMuzzleFlash();

            // Arm recoil animation
            if (armAnimator != null)
            {
                armAnimator.SetTrigger("Fire");
            }

            // Camera shake (simple)
            if (currentWeapon.cameraShake > 0 && mainCam != null)
            {
                StartCoroutine(CameraShake(currentWeapon.cameraShake));
            }

            // Raycast for hit
            PerformShot();

            // Event
            onWeaponFired?.Invoke(currentWeapon);

            // Auto-reload if empty
            if (currentMagazine <= 0 && currentAmmo > 0)
            {
                Invoke(nameof(TryReload), 0.5f);
            }
        }

        private void PerformShot()
        {
            if (shootOrigin == null) return;

            Vector3 direction = shootOrigin.forward;

            // Add spread
            if (currentWeapon.spread > 0)
            {
                direction += Random.insideUnitSphere * currentWeapon.spread;
                direction.Normalize();
            }

            // Raycast
            Ray ray = new Ray(shootOrigin.position, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, currentWeapon.range))
            {
                // Spawn trail
                SpawnBulletTrail(shootOrigin.position, hit.point);

                // Spawn impact
                SpawnImpact(hit);

                // Deal damage
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(currentWeapon.damage, gameObject);
                    onTargetHit?.Invoke(hit.collider.gameObject, currentWeapon.damage);
                }

                // Apply force
                var rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForceAtPosition(direction * currentWeapon.impactForce, hit.point);
                }
            }
            else
            {
                // Spawn trail to max range
                SpawnBulletTrail(shootOrigin.position, shootOrigin.position + direction * currentWeapon.range);
            }
        }

        #endregion

        #region Reload

        /// <summary>
        /// Try to reload current weapon.
        /// </summary>
        public void TryReload()
        {
            if (currentWeapon == null) return;
            if (isReloading) return;
            if (currentMagazine >= currentWeapon.magazineSize) return;
            if (currentAmmo <= 0) return;

            StartReload();
        }

        private void StartReload()
        {
            isReloading = true;
            reloadTimer = currentWeapon.reloadTime;

            PlaySound(reloadSound);
            PlaySound(currentWeapon.reloadSound);

            if (armAnimator != null)
            {
                armAnimator.SetTrigger("Reload");
            }
        }

        private void FinishReload()
        {
            isReloading = false;

            int ammoNeeded = currentWeapon.magazineSize - currentMagazine;
            int ammoToLoad = Mathf.Min(ammoNeeded, currentAmmo);

            currentMagazine += ammoToLoad;
            currentAmmo -= ammoToLoad;

            onWeaponReloaded?.Invoke(currentWeapon);
        }

        #endregion

        #region Weapon Switching

        /// <summary>
        /// Equip weapon by index.
        /// </summary>
        public void EquipWeapon(int index)
        {
            if (index < 0 || index >= ownedWeapons.Count) return;

            currentWeaponIndex = index;
            currentWeapon = ownedWeapons[index];
            InitializeWeapon();

            PlaySound(switchSound);
            UpdateArmsVisual();

            onWeaponSwitched?.Invoke(currentWeapon);
        }

        /// <summary>
        /// Equip weapon by data.
        /// </summary>
        public void EquipWeapon(WeaponData weapon)
        {
            int index = ownedWeapons.IndexOf(weapon);
            if (index >= 0)
            {
                EquipWeapon(index);
            }
        }

        /// <summary>
        /// Switch to next weapon.
        /// </summary>
        public void SwitchToNextWeapon()
        {
            if (ownedWeapons.Count <= 1) return;

            int nextIndex = (currentWeaponIndex + 1) % ownedWeapons.Count;
            EquipWeapon(nextIndex);
        }

        /// <summary>
        /// Add a weapon to inventory.
        /// </summary>
        public void AddWeapon(WeaponData weapon)
        {
            if (!ownedWeapons.Contains(weapon))
            {
                ownedWeapons.Add(weapon);
            }

            // Auto-equip if first weapon
            if (currentWeapon == null)
            {
                EquipWeapon(weapon);
            }
        }

        /// <summary>
        /// Remove weapon.
        /// </summary>
        public void RemoveWeapon(WeaponData weapon)
        {
            ownedWeapons.Remove(weapon);

            if (currentWeapon == weapon)
            {
                currentWeapon = null;
                if (ownedWeapons.Count > 0)
                {
                    EquipWeapon(0);
                }
            }

            UpdateArmsVisual();
        }

        private void InitializeWeapon()
        {
            if (currentWeapon == null) return;

            currentMagazine = currentWeapon.magazineSize;
            currentAmmo = currentWeapon.startingAmmo;
            isReloading = false;
            fireTimer = 0;
        }

        #endregion

        #region Ammo

        /// <summary>
        /// Add ammo for current weapon.
        /// </summary>
        public void AddAmmo(int amount)
        {
            if (currentWeapon == null) return;
            currentAmmo += amount;
            currentAmmo = Mathf.Min(currentAmmo, currentWeapon.maxAmmo);
        }

        /// <summary>
        /// Get current ammo count.
        /// </summary>
        public int GetCurrentAmmo()
        {
            return currentMagazine;
        }

        /// <summary>
        /// Get reserve ammo count.
        /// </summary>
        public int GetReserveAmmo()
        {
            return currentAmmo;
        }

        /// <summary>
        /// Get ammo display string.
        /// </summary>
        public string GetAmmoString()
        {
            if (currentWeapon == null) return "-- / --";
            return $"{currentMagazine} / {currentAmmo}";
        }

        #endregion

        #region Effects

        private void SpawnMuzzleFlash()
        {
            if (muzzleFlashPrefab == null) return;

            Transform spawnPoint = muzzlePoint != null ? muzzlePoint : shootOrigin;
            if (spawnPoint == null) return;

            GameObject flash = Instantiate(muzzleFlashPrefab, spawnPoint.position, spawnPoint.rotation);
            flash.transform.SetParent(spawnPoint);
            Destroy(flash, 0.1f);
        }

        private void SpawnBulletTrail(Vector3 start, Vector3 end)
        {
            if (bulletTrailPrefab == null) return;

            GameObject trail = Instantiate(bulletTrailPrefab);
            var line = trail.GetComponent<LineRenderer>();
            if (line != null)
            {
                line.SetPosition(0, start);
                line.SetPosition(1, end);
            }
            Destroy(trail, 0.1f);
        }

        private void SpawnImpact(RaycastHit hit)
        {
            bool isEnemy = hit.collider.CompareTag("Enemy") ||
                          hit.collider.GetComponent<IDamageable>() != null;

            GameObject prefab = isEnemy ? bloodEffectPrefab : impactEffectPrefab;
            if (prefab == null) return;

            GameObject impact = Instantiate(prefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f);
        }

        private System.Collections.IEnumerator CameraShake(float intensity)
        {
            if (mainCam == null) yield break;

            Vector3 originalPos = mainCam.transform.localPosition;
            float elapsed = 0f;
            float duration = 0.1f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;

                mainCam.transform.localPosition = originalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCam.transform.localPosition = originalPos;
        }

        #endregion

        #region Visual

        private void UpdateArmsVisual()
        {
            if (armsVisual != null)
            {
                armsVisual.SetActive(currentWeapon != null);
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

        public bool IsReloading => isReloading;
        public bool IsAiming => isAiming;
        public bool HasWeapon => currentWeapon != null;
        public WeaponData CurrentWeapon => currentWeapon;

        #endregion
    }

    /// <summary>
    /// Weapon data configuration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Bob's Petroleum/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Info")]
        public string weaponName = "Pistol";
        public string description = "Standard sidearm";
        public Sprite icon;

        [Header("Stats")]
        public float damage = 25f;
        public float range = 50f;
        public float fireRate = 2f; // shots per second
        public float spread = 0.02f;
        public float impactForce = 100f;

        [Header("Ammo")]
        public int magazineSize = 12;
        public int startingAmmo = 60;
        public int maxAmmo = 120;
        public float reloadTime = 1.5f;

        [Header("Behavior")]
        public bool isAutomatic = false;
        public float cameraShake = 0.05f;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;

        [Header("Visuals")]
        public GameObject weaponModelPrefab;
        public GameObject muzzleFlashPrefab;

        [Header("Shop")]
        public int price = 500;
        public bool isStarter = false;
    }

    /// <summary>
    /// Interface for damageable objects.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float damage, GameObject source);
    }
}
