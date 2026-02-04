using UnityEngine;
using System.Collections;

namespace BobsPetroleum.Combat
{
    /// <summary>
    /// Simple FPS weapon visuals - handles reload animation (move off screen),
    /// muzzle flash, and special effects like flamethrower particles.
    /// No complex animations needed - just simple transforms!
    /// </summary>
    public class WeaponVisuals : MonoBehaviour
    {
        [Header("Weapon Model")]
        [Tooltip("The weapon model to show/hide")]
        public GameObject weaponModel;

        [Tooltip("Normal position (on screen)")]
        public Vector3 normalPosition = new Vector3(0.3f, -0.3f, 0.5f);

        [Tooltip("Reload position (off screen)")]
        public Vector3 reloadPosition = new Vector3(0.3f, -1f, 0.5f);

        [Tooltip("Position lerp speed")]
        public float positionSpeed = 10f;

        [Header("Weapon Type")]
        public WeaponType weaponType = WeaponType.Pistol;

        [Header("Muzzle Flash")]
        [Tooltip("Muzzle flash object")]
        public GameObject muzzleFlash;

        [Tooltip("Muzzle flash duration")]
        public float muzzleFlashDuration = 0.05f;

        [Header("Flamethrower Settings")]
        [Tooltip("Flame particle system")]
        public ParticleSystem flameParticles;

        [Tooltip("Flame light")]
        public Light flameLight;

        [Tooltip("Flame range")]
        public float flameRange = 10f;

        [Tooltip("Flame damage per second")]
        public float flameDamage = 30f;

        [Tooltip("Flame spread angle")]
        public float flameSpread = 15f;

        [Header("Shotgun Settings")]
        [Tooltip("Number of pellets")]
        public int shotgunPellets = 8;

        [Tooltip("Pellet spread")]
        public float pelletSpread = 0.1f;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptySound;
        public AudioClip flameLoopSound;

        [Header("Recoil")]
        [Tooltip("Recoil kick back amount")]
        public float recoilKickback = 0.1f;

        [Tooltip("Recoil recovery speed")]
        public float recoilRecovery = 10f;

        // State
        private Vector3 targetPosition;
        private Vector3 currentRecoil;
        private bool isReloading = false;
        private bool isFiring = false;
        private AudioSource audioSource;
        private AudioSource loopAudioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Create loop audio source for flamethrower
            if (weaponType == WeaponType.Flamethrower)
            {
                GameObject loopObj = new GameObject("FlameLoopAudio");
                loopObj.transform.SetParent(transform);
                loopAudioSource = loopObj.AddComponent<AudioSource>();
                loopAudioSource.loop = true;
                loopAudioSource.clip = flameLoopSound;
            }

            targetPosition = normalPosition;

            // Hide muzzle flash
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(false);
            }

            // Stop flame particles
            if (flameParticles != null)
            {
                flameParticles.Stop();
            }

            if (flameLight != null)
            {
                flameLight.enabled = false;
            }
        }

        private void Update()
        {
            // Lerp to target position
            if (weaponModel != null)
            {
                Vector3 finalPos = targetPosition + currentRecoil;
                weaponModel.transform.localPosition = Vector3.Lerp(
                    weaponModel.transform.localPosition,
                    finalPos,
                    Time.deltaTime * positionSpeed
                );
            }

            // Recover recoil
            currentRecoil = Vector3.Lerp(currentRecoil, Vector3.zero, Time.deltaTime * recoilRecovery);

            // Flamethrower continuous damage
            if (weaponType == WeaponType.Flamethrower && isFiring)
            {
                DoFlameDamage();
            }
        }

        #region Firing

        /// <summary>
        /// Fire the weapon (called by SimpleGunSystem).
        /// </summary>
        public void Fire()
        {
            if (isReloading) return;

            switch (weaponType)
            {
                case WeaponType.Pistol:
                case WeaponType.Shotgun:
                    FireBullet();
                    break;
                case WeaponType.Flamethrower:
                    StartFlame();
                    break;
            }
        }

        /// <summary>
        /// Stop firing (for flamethrower).
        /// </summary>
        public void StopFire()
        {
            if (weaponType == WeaponType.Flamethrower)
            {
                StopFlame();
            }
        }

        private void FireBullet()
        {
            // Muzzle flash
            StartCoroutine(ShowMuzzleFlash());

            // Recoil
            currentRecoil = Vector3.back * recoilKickback;

            // Sound
            PlaySound(fireSound);

            // Shotgun fires multiple pellets
            if (weaponType == WeaponType.Shotgun)
            {
                for (int i = 0; i < shotgunPellets; i++)
                {
                    FirePellet();
                }
            }
            else
            {
                // Single shot
                FireSingleShot();
            }
        }

        private void FireSingleShot()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                // Damage
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(25f, gameObject);
                }

                // Impact force
                var rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForceAtPosition(ray.direction * 100f, hit.point);
                }
            }
        }

        private void FirePellet()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Add spread
            Vector3 spread = Random.insideUnitSphere * pelletSpread;
            Vector3 direction = cam.transform.forward + spread;

            Ray ray = new Ray(cam.transform.position, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 50f))
            {
                var damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(10f, gameObject); // Less per pellet
                }

                var rb = hit.collider.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForceAtPosition(direction * 50f, hit.point);
                }
            }
        }

        #endregion

        #region Flamethrower

        private void StartFlame()
        {
            isFiring = true;

            if (flameParticles != null)
            {
                flameParticles.Play();
            }

            if (flameLight != null)
            {
                flameLight.enabled = true;
            }

            if (loopAudioSource != null && flameLoopSound != null)
            {
                loopAudioSource.Play();
            }
        }

        private void StopFlame()
        {
            isFiring = false;

            if (flameParticles != null)
            {
                flameParticles.Stop();
            }

            if (flameLight != null)
            {
                flameLight.enabled = false;
            }

            if (loopAudioSource != null)
            {
                loopAudioSource.Stop();
            }
        }

        private void DoFlameDamage()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // Cone raycast for flame
            Vector3 origin = cam.transform.position;
            Vector3 forward = cam.transform.forward;

            // Check multiple rays in cone
            int rays = 5;
            for (int i = 0; i < rays; i++)
            {
                Vector3 direction = Quaternion.Euler(
                    Random.Range(-flameSpread, flameSpread),
                    Random.Range(-flameSpread, flameSpread),
                    0
                ) * forward;

                RaycastHit hit;
                if (Physics.Raycast(origin, direction, out hit, flameRange))
                {
                    var damageable = hit.collider.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(flameDamage * Time.deltaTime / rays, gameObject);
                    }

                    // Set on fire effect
                    var burnable = hit.collider.GetComponent<Burnable>();
                    if (burnable != null)
                    {
                        burnable.Ignite();
                    }
                }
            }
        }

        #endregion

        #region Reload Animation

        /// <summary>
        /// Play simple reload animation (move off screen).
        /// </summary>
        public void PlayReload(float duration)
        {
            if (isReloading) return;
            StartCoroutine(ReloadAnimation(duration));
        }

        private IEnumerator ReloadAnimation(float duration)
        {
            isReloading = true;

            // Stop flame if flamethrower
            StopFlame();

            // Play sound
            PlaySound(reloadSound);

            // Move off screen
            targetPosition = reloadPosition;

            // Wait
            yield return new WaitForSeconds(duration * 0.8f);

            // Move back
            targetPosition = normalPosition;

            yield return new WaitForSeconds(duration * 0.2f);

            isReloading = false;
        }

        #endregion

        #region Effects

        private IEnumerator ShowMuzzleFlash()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(true);
                yield return new WaitForSeconds(muzzleFlashDuration);
                muzzleFlash.SetActive(false);
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

        public void PlayEmpty()
        {
            PlaySound(emptySound);
        }

        #endregion

        #region Show/Hide

        public void Show()
        {
            if (weaponModel != null)
            {
                weaponModel.SetActive(true);
            }
            targetPosition = normalPosition;
        }

        public void Hide()
        {
            if (weaponModel != null)
            {
                weaponModel.SetActive(false);
            }
            StopFlame();
        }

        #endregion

        public bool IsReloading => isReloading;
        public bool IsFiring => isFiring;
    }

    public enum WeaponType
    {
        Pistol,
        Shotgun,
        Flamethrower
    }

    /// <summary>
    /// Component for objects that can be set on fire.
    /// </summary>
    public class Burnable : MonoBehaviour
    {
        public ParticleSystem fireEffect;
        public float burnDamagePerSecond = 5f;
        public float burnDuration = 5f;

        private bool isBurning = false;
        private float burnTimer = 0f;

        public void Ignite()
        {
            if (isBurning) return;

            isBurning = true;
            burnTimer = burnDuration;

            if (fireEffect != null)
            {
                fireEffect.Play();
            }
        }

        private void Update()
        {
            if (!isBurning) return;

            burnTimer -= Time.deltaTime;

            // Apply burn damage
            var damageable = GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(burnDamagePerSecond * Time.deltaTime, gameObject);
            }

            if (burnTimer <= 0)
            {
                isBurning = false;
                if (fireEffect != null)
                {
                    fireEffect.Stop();
                }
            }
        }

        public bool IsBurning => isBurning;
    }
}
