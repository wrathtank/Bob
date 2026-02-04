using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace BobsPetroleum.Battle
{
    /// <summary>
    /// Automatic camera positioning for Pokemon-style pet battles.
    /// Handles transitions, attack zooms, and dynamic camera angles.
    /// </summary>
    public class BattleCameraSystem : MonoBehaviour
    {
        public static BattleCameraSystem Instance { get; private set; }

        [Header("Camera")]
        [Tooltip("Battle camera (can be same as main)")]
        public Camera battleCamera;

        [Header("Battle Positions")]
        [Tooltip("Player's creature spawn position")]
        public Transform playerBattlePosition;

        [Tooltip("Enemy creature spawn position")]
        public Transform enemyBattlePosition;

        [Tooltip("Default camera position")]
        public Transform defaultCameraPosition;

        [Header("Camera Angles")]
        [Tooltip("Overview angle (shows both creatures)")]
        public Transform overviewAngle;

        [Tooltip("Focus on player creature angle")]
        public Transform playerFocusAngle;

        [Tooltip("Focus on enemy creature angle")]
        public Transform enemyFocusAngle;

        [Tooltip("Attack camera angle (dramatic)")]
        public Transform attackAngle;

        [Tooltip("Victory camera angle")]
        public Transform victoryAngle;

        [Header("Transition Settings")]
        [Tooltip("Camera transition speed")]
        public float transitionSpeed = 2f;

        [Tooltip("Attack zoom speed (faster)")]
        public float attackZoomSpeed = 4f;

        [Tooltip("Default field of view")]
        public float defaultFOV = 60f;

        [Tooltip("Zoomed FOV for attacks")]
        public float zoomFOV = 40f;

        [Header("Camera Shake")]
        [Tooltip("Shake intensity on hit")]
        public float shakeIntensity = 0.3f;

        [Tooltip("Shake duration")]
        public float shakeDuration = 0.2f;

        [Header("Events")]
        public UnityEvent onBattleCameraActivated;
        public UnityEvent onBattleCameraDeactivated;
        public UnityEvent<string> onCameraAngleChanged;

        // State
        private bool isActive;
        private Transform currentTarget;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Camera mainCamera;
        private Coroutine transitionCoroutine;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mainCamera = Camera.main;

            // Create default positions if not assigned
            CreateDefaultPositions();
        }

        private void CreateDefaultPositions()
        {
            if (playerBattlePosition == null)
            {
                GameObject obj = new GameObject("PlayerBattlePosition");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(-3f, 0f, 0f);
                playerBattlePosition = obj.transform;
            }

            if (enemyBattlePosition == null)
            {
                GameObject obj = new GameObject("EnemyBattlePosition");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(3f, 0f, 0f);
                enemyBattlePosition = obj.transform;
            }

            if (overviewAngle == null)
            {
                GameObject obj = new GameObject("OverviewAngle");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(0f, 3f, -6f);
                obj.transform.LookAt(Vector3.zero);
                overviewAngle = obj.transform;
            }

            if (playerFocusAngle == null)
            {
                GameObject obj = new GameObject("PlayerFocusAngle");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(-1f, 1.5f, -3f);
                obj.transform.LookAt(playerBattlePosition.position + Vector3.up);
                playerFocusAngle = obj.transform;
            }

            if (enemyFocusAngle == null)
            {
                GameObject obj = new GameObject("EnemyFocusAngle");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(1f, 1.5f, -3f);
                obj.transform.LookAt(enemyBattlePosition.position + Vector3.up);
                enemyFocusAngle = obj.transform;
            }

            if (attackAngle == null)
            {
                GameObject obj = new GameObject("AttackAngle");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(0f, 2f, -2f);
                obj.transform.LookAt(Vector3.zero);
                attackAngle = obj.transform;
            }
        }

        #region Battle Camera Control

        /// <summary>
        /// Activate the battle camera.
        /// </summary>
        public void ActivateBattleCamera()
        {
            if (isActive) return;

            isActive = true;

            // Store main camera position
            if (mainCamera != null)
            {
                originalPosition = mainCamera.transform.position;
                originalRotation = mainCamera.transform.rotation;
            }

            // Use separate battle camera if assigned
            if (battleCamera != null && battleCamera != mainCamera)
            {
                battleCamera.gameObject.SetActive(true);
                mainCamera.gameObject.SetActive(false);
            }

            // Move to overview
            SetCameraAngle(BattleCameraAngle.Overview);

            onBattleCameraActivated?.Invoke();
        }

        /// <summary>
        /// Deactivate battle camera and return to main.
        /// </summary>
        public void DeactivateBattleCamera()
        {
            if (!isActive) return;

            isActive = false;

            // Restore main camera
            if (battleCamera != null && battleCamera != mainCamera)
            {
                battleCamera.gameObject.SetActive(false);
                mainCamera.gameObject.SetActive(true);
            }

            // Restore position
            if (mainCamera != null)
            {
                mainCamera.transform.position = originalPosition;
                mainCamera.transform.rotation = originalRotation;
            }

            onBattleCameraDeactivated?.Invoke();
        }

        /// <summary>
        /// Set camera to a specific battle angle.
        /// </summary>
        public void SetCameraAngle(BattleCameraAngle angle, bool instant = false)
        {
            Transform targetTransform = GetAngleTransform(angle);
            if (targetTransform == null) return;

            Camera cam = GetActiveCamera();
            if (cam == null) return;

            if (transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            if (instant)
            {
                cam.transform.position = targetTransform.position;
                cam.transform.rotation = targetTransform.rotation;
            }
            else
            {
                transitionCoroutine = StartCoroutine(TransitionToAngle(cam.transform, targetTransform));
            }

            onCameraAngleChanged?.Invoke(angle.ToString());
        }

        /// <summary>
        /// Focus camera on a specific creature.
        /// </summary>
        public void FocusOnCreature(Transform creature, bool isPlayer)
        {
            SetCameraAngle(isPlayer ? BattleCameraAngle.PlayerFocus : BattleCameraAngle.EnemyFocus);
        }

        /// <summary>
        /// Play attack camera sequence.
        /// </summary>
        public void PlayAttackSequence(Transform attacker, Transform target, System.Action onComplete = null)
        {
            StartCoroutine(AttackCameraSequence(attacker, target, onComplete));
        }

        /// <summary>
        /// Shake the camera (on hit).
        /// </summary>
        public void ShakeCamera(float intensity = -1f)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            float shake = intensity > 0 ? intensity : shakeIntensity;
            shakeCoroutine = StartCoroutine(CameraShake(shake));
        }

        /// <summary>
        /// Zoom camera for dramatic effect.
        /// </summary>
        public void ZoomCamera(bool zoomIn, float duration = 0.5f)
        {
            Camera cam = GetActiveCamera();
            if (cam == null) return;

            float targetFOV = zoomIn ? zoomFOV : defaultFOV;
            StartCoroutine(ZoomCoroutine(cam, targetFOV, duration));
        }

        #endregion

        #region Camera Sequences

        private IEnumerator AttackCameraSequence(Transform attacker, Transform target, System.Action onComplete)
        {
            Camera cam = GetActiveCamera();
            if (cam == null) yield break;

            // 1. Quick zoom to attacker
            Vector3 attackerPos = attacker.position + attacker.forward * -2f + Vector3.up * 1.5f;
            yield return StartCoroutine(QuickTransition(cam.transform, attackerPos, attacker.position + Vector3.up, attackZoomSpeed));

            // Wait briefly
            yield return new WaitForSeconds(0.2f);

            // 2. Quick zoom to target (impact moment)
            Vector3 targetPos = target.position + target.forward * 2f + Vector3.up * 1f;
            yield return StartCoroutine(QuickTransition(cam.transform, targetPos, target.position + Vector3.up, attackZoomSpeed));

            // Shake on impact
            ShakeCamera();

            // Wait
            yield return new WaitForSeconds(0.3f);

            // 3. Return to overview
            SetCameraAngle(BattleCameraAngle.Overview);

            yield return new WaitForSeconds(0.5f);

            onComplete?.Invoke();
        }

        private IEnumerator TransitionToAngle(Transform camTransform, Transform target)
        {
            float elapsed = 0f;
            Vector3 startPos = camTransform.position;
            Quaternion startRot = camTransform.rotation;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * transitionSpeed;
                float t = Mathf.SmoothStep(0f, 1f, elapsed);

                camTransform.position = Vector3.Lerp(startPos, target.position, t);
                camTransform.rotation = Quaternion.Slerp(startRot, target.rotation, t);

                yield return null;
            }

            camTransform.position = target.position;
            camTransform.rotation = target.rotation;
        }

        private IEnumerator QuickTransition(Transform camTransform, Vector3 targetPos, Vector3 lookAt, float speed)
        {
            float elapsed = 0f;
            Vector3 startPos = camTransform.position;
            Quaternion startRot = camTransform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(lookAt - targetPos);

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * speed;
                float t = Mathf.SmoothStep(0f, 1f, elapsed);

                camTransform.position = Vector3.Lerp(startPos, targetPos, t);
                camTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }
        }

        private IEnumerator CameraShake(float intensity)
        {
            Camera cam = GetActiveCamera();
            if (cam == null) yield break;

            Vector3 originalPos = cam.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity;

                cam.transform.localPosition = originalPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            cam.transform.localPosition = originalPos;
        }

        private IEnumerator ZoomCoroutine(Camera cam, float targetFOV, float duration)
        {
            float startFOV = cam.fieldOfView;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cam.fieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / duration);
                yield return null;
            }

            cam.fieldOfView = targetFOV;
        }

        #endregion

        #region Helpers

        private Camera GetActiveCamera()
        {
            if (battleCamera != null && battleCamera.gameObject.activeInHierarchy)
            {
                return battleCamera;
            }
            return mainCamera;
        }

        private Transform GetAngleTransform(BattleCameraAngle angle)
        {
            switch (angle)
            {
                case BattleCameraAngle.Overview:
                    return overviewAngle;
                case BattleCameraAngle.PlayerFocus:
                    return playerFocusAngle;
                case BattleCameraAngle.EnemyFocus:
                    return enemyFocusAngle;
                case BattleCameraAngle.Attack:
                    return attackAngle;
                case BattleCameraAngle.Victory:
                    return victoryAngle != null ? victoryAngle : overviewAngle;
                default:
                    return overviewAngle;
            }
        }

        /// <summary>
        /// Get spawn position for player's creature.
        /// </summary>
        public Vector3 GetPlayerSpawnPosition()
        {
            return playerBattlePosition != null ? playerBattlePosition.position : Vector3.left * 3f;
        }

        /// <summary>
        /// Get spawn position for enemy creature.
        /// </summary>
        public Vector3 GetEnemySpawnPosition()
        {
            return enemyBattlePosition != null ? enemyBattlePosition.position : Vector3.right * 3f;
        }

        #endregion

        #region Properties

        public bool IsActive => isActive;

        #endregion
    }

    public enum BattleCameraAngle
    {
        Overview,
        PlayerFocus,
        EnemyFocus,
        Attack,
        Victory
    }
}
