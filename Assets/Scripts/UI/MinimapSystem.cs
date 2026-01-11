using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using BobsPetroleum.Player;

namespace BobsPetroleum.UI
{
    /// <summary>
    /// Minimap system for town navigation showing player, objectives, and POIs.
    /// Essential for open-world exploration like Schedule 1 / GTA style games.
    /// </summary>
    public class MinimapSystem : MonoBehaviour
    {
        public static MinimapSystem Instance { get; private set; }

        [Header("Minimap Camera")]
        [Tooltip("Top-down camera for minimap")]
        public Camera minimapCamera;

        [Tooltip("Height above player")]
        public float cameraHeight = 50f;

        [Tooltip("Orthographic size (zoom)")]
        public float minimapZoom = 30f;

        [Tooltip("Rotate with player")]
        public bool rotateWithPlayer = true;

        [Header("UI Elements")]
        [Tooltip("Minimap RawImage displaying camera output")]
        public RawImage minimapImage;

        [Tooltip("Player icon on minimap")]
        public RectTransform playerIcon;

        [Tooltip("Minimap container for toggling")]
        public GameObject minimapContainer;

        [Tooltip("Toggle key")]
        public KeyCode toggleKey = KeyCode.M;

        [Header("Markers")]
        [Tooltip("Marker container")]
        public RectTransform markerContainer;

        [Tooltip("Quest marker prefab")]
        public GameObject questMarkerPrefab;

        [Tooltip("POI marker prefab")]
        public GameObject poiMarkerPrefab;

        [Tooltip("Enemy marker prefab")]
        public GameObject enemyMarkerPrefab;

        [Tooltip("Player marker prefab (for co-op)")]
        public GameObject playerMarkerPrefab;

        [Header("Marker Settings")]
        [Tooltip("Maximum distance to show markers")]
        public float maxMarkerDistance = 100f;

        [Tooltip("Marker scale factor")]
        public float markerScale = 1f;

        [Header("Full Map")]
        [Tooltip("Full map panel")]
        public GameObject fullMapPanel;

        [Tooltip("Full map image")]
        public RawImage fullMapImage;

        [Tooltip("Full map zoom")]
        public float fullMapZoom = 100f;

        [Header("POI Configuration")]
        public List<POIMarker> pointsOfInterest = new List<POIMarker>();

        // State
        private PlayerController trackedPlayer;
        private Dictionary<Transform, GameObject> activeMarkers = new Dictionary<Transform, GameObject>();
        private RenderTexture minimapRenderTexture;
        private bool isFullMapOpen = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            SetupMinimapCamera();
            CreatePOIMarkers();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleFullMap();
            }

            if (trackedPlayer != null)
            {
                UpdateCameraPosition();
                UpdateMarkers();
            }

            // Rotate player icon if not rotating camera
            if (!rotateWithPlayer && playerIcon != null && trackedPlayer != null)
            {
                playerIcon.rotation = Quaternion.Euler(0, 0, -trackedPlayer.transform.eulerAngles.y);
            }
        }

        #region Setup

        private void SetupMinimapCamera()
        {
            if (minimapCamera == null)
            {
                // Create minimap camera
                GameObject camObj = new GameObject("MinimapCamera");
                camObj.transform.SetParent(transform);
                minimapCamera = camObj.AddComponent<Camera>();
            }

            // Configure camera
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = minimapZoom;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            minimapCamera.cullingMask = LayerMask.GetMask("Default", "Terrain", "Buildings");
            minimapCamera.depth = -10;

            // Create render texture
            minimapRenderTexture = new RenderTexture(256, 256, 16);
            minimapCamera.targetTexture = minimapRenderTexture;

            // Assign to UI
            if (minimapImage != null)
            {
                minimapImage.texture = minimapRenderTexture;
            }
        }

        private void CreatePOIMarkers()
        {
            foreach (var poi in pointsOfInterest)
            {
                if (poi.worldPosition != null)
                {
                    CreateMarkerForPOI(poi);
                }
            }
        }

        #endregion

        #region Player Tracking

        /// <summary>
        /// Set the player to track on minimap.
        /// </summary>
        public void SetTrackedPlayer(PlayerController player)
        {
            trackedPlayer = player;
        }

        private void UpdateCameraPosition()
        {
            if (minimapCamera == null || trackedPlayer == null) return;

            // Position camera above player
            Vector3 camPos = trackedPlayer.transform.position;
            camPos.y += cameraHeight;
            minimapCamera.transform.position = camPos;

            // Rotate with player if enabled
            if (rotateWithPlayer)
            {
                float yRotation = trackedPlayer.transform.eulerAngles.y;
                minimapCamera.transform.rotation = Quaternion.Euler(90f, yRotation, 0f);
            }
        }

        #endregion

        #region Markers

        /// <summary>
        /// Add a marker for a transform.
        /// </summary>
        public void AddMarker(Transform target, MarkerType type, string label = "")
        {
            if (target == null || markerContainer == null) return;

            // Check if already exists
            if (activeMarkers.ContainsKey(target)) return;

            // Get prefab based on type
            GameObject prefab = GetMarkerPrefab(type);
            if (prefab == null) return;

            // Create marker
            GameObject marker = Instantiate(prefab, markerContainer);
            var markerComponent = marker.GetComponent<MinimapMarker>();
            if (markerComponent != null)
            {
                markerComponent.Initialize(target, label);
            }

            activeMarkers[target] = marker;
        }

        /// <summary>
        /// Remove a marker.
        /// </summary>
        public void RemoveMarker(Transform target)
        {
            if (target == null) return;

            if (activeMarkers.TryGetValue(target, out GameObject marker))
            {
                Destroy(marker);
                activeMarkers.Remove(target);
            }
        }

        /// <summary>
        /// Clear all markers of a type.
        /// </summary>
        public void ClearMarkers(MarkerType type)
        {
            List<Transform> toRemove = new List<Transform>();

            foreach (var kvp in activeMarkers)
            {
                var marker = kvp.Value.GetComponent<MinimapMarker>();
                if (marker != null && marker.markerType == type)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var target in toRemove)
            {
                RemoveMarker(target);
            }
        }

        private void CreateMarkerForPOI(POIMarker poi)
        {
            if (markerContainer == null || poiMarkerPrefab == null) return;
            if (poi.worldPosition == null) return;

            GameObject marker = Instantiate(poiMarkerPrefab, markerContainer);
            var markerComponent = marker.GetComponent<MinimapMarker>();
            if (markerComponent != null)
            {
                markerComponent.Initialize(poi.worldPosition, poi.label);
                markerComponent.SetIcon(poi.icon);
                markerComponent.SetColor(poi.color);
            }

            activeMarkers[poi.worldPosition] = marker;
        }

        private void UpdateMarkers()
        {
            if (markerContainer == null || trackedPlayer == null) return;

            foreach (var kvp in activeMarkers)
            {
                Transform target = kvp.Key;
                GameObject markerObj = kvp.Value;

                if (target == null || markerObj == null) continue;

                // Calculate position on minimap
                Vector3 offset = target.position - trackedPlayer.transform.position;
                float distance = offset.magnitude;

                // Hide if too far
                if (distance > maxMarkerDistance)
                {
                    markerObj.SetActive(false);
                    continue;
                }

                markerObj.SetActive(true);

                // Convert to minimap coordinates
                float minimapSize = minimapImage?.rectTransform.rect.width ?? 200f;
                float scale = (minimapSize / 2f) / minimapZoom;

                Vector3 flatOffset = new Vector3(offset.x, offset.z, 0f);

                // Rotate if camera rotates with player
                if (rotateWithPlayer)
                {
                    float angle = -trackedPlayer.transform.eulerAngles.y * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    flatOffset = new Vector3(
                        flatOffset.x * cos - flatOffset.y * sin,
                        flatOffset.x * sin + flatOffset.y * cos,
                        0f
                    );
                }

                // Apply to marker
                RectTransform markerRect = markerObj.GetComponent<RectTransform>();
                if (markerRect != null)
                {
                    markerRect.anchoredPosition = flatOffset * scale * markerScale;
                }
            }
        }

        private GameObject GetMarkerPrefab(MarkerType type)
        {
            switch (type)
            {
                case MarkerType.Quest:
                    return questMarkerPrefab;
                case MarkerType.POI:
                    return poiMarkerPrefab;
                case MarkerType.Enemy:
                    return enemyMarkerPrefab;
                case MarkerType.Player:
                    return playerMarkerPrefab;
                default:
                    return poiMarkerPrefab;
            }
        }

        #endregion

        #region Full Map

        /// <summary>
        /// Toggle full map view.
        /// </summary>
        public void ToggleFullMap()
        {
            if (isFullMapOpen)
            {
                CloseFullMap();
            }
            else
            {
                OpenFullMap();
            }
        }

        /// <summary>
        /// Open full map view.
        /// </summary>
        public void OpenFullMap()
        {
            if (fullMapPanel == null) return;

            isFullMapOpen = true;
            fullMapPanel.SetActive(true);

            // Zoom out camera
            if (minimapCamera != null)
            {
                minimapCamera.orthographicSize = fullMapZoom;
            }

            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Pause game (optional)
            // Time.timeScale = 0f;
        }

        /// <summary>
        /// Close full map view.
        /// </summary>
        public void CloseFullMap()
        {
            if (fullMapPanel == null) return;

            isFullMapOpen = false;
            fullMapPanel.SetActive(false);

            // Restore zoom
            if (minimapCamera != null)
            {
                minimapCamera.orthographicSize = minimapZoom;
            }

            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Resume game
            // Time.timeScale = 1f;
        }

        #endregion

        #region Zoom Control

        /// <summary>
        /// Set minimap zoom level.
        /// </summary>
        public void SetZoom(float zoom)
        {
            minimapZoom = Mathf.Clamp(zoom, 10f, 100f);
            if (minimapCamera != null && !isFullMapOpen)
            {
                minimapCamera.orthographicSize = minimapZoom;
            }
        }

        /// <summary>
        /// Zoom in.
        /// </summary>
        public void ZoomIn()
        {
            SetZoom(minimapZoom - 5f);
        }

        /// <summary>
        /// Zoom out.
        /// </summary>
        public void ZoomOut()
        {
            SetZoom(minimapZoom + 5f);
        }

        #endregion

        #region Quest Waypoint

        /// <summary>
        /// Set quest waypoint marker.
        /// </summary>
        public void SetQuestWaypoint(Vector3 worldPosition)
        {
            // Create a temporary transform holder
            GameObject waypointObj = new GameObject("QuestWaypoint");
            waypointObj.transform.position = worldPosition;

            ClearMarkers(MarkerType.Quest);
            AddMarker(waypointObj.transform, MarkerType.Quest, "Objective");
        }

        /// <summary>
        /// Clear quest waypoint.
        /// </summary>
        public void ClearQuestWaypoint()
        {
            ClearMarkers(MarkerType.Quest);
        }

        #endregion

        public bool IsFullMapOpen => isFullMapOpen;

        private void OnDestroy()
        {
            if (minimapRenderTexture != null)
            {
                minimapRenderTexture.Release();
            }
        }
    }

    /// <summary>
    /// Marker component for minimap icons.
    /// </summary>
    public class MinimapMarker : MonoBehaviour
    {
        public MarkerType markerType;
        public Transform trackedTarget;
        public string label;

        [Header("UI References")]
        public Image iconImage;
        public TMPro.TMP_Text labelText;

        public void Initialize(Transform target, string markerLabel)
        {
            trackedTarget = target;
            label = markerLabel;

            if (labelText != null)
            {
                labelText.text = label;
            }
        }

        public void SetIcon(Sprite icon)
        {
            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
            }
        }

        public void SetColor(Color color)
        {
            if (iconImage != null)
            {
                iconImage.color = color;
            }
        }
    }

    public enum MarkerType
    {
        Quest,
        POI,
        Enemy,
        Player,
        Custom
    }

    [System.Serializable]
    public class POIMarker
    {
        public string label;
        public Transform worldPosition;
        public Sprite icon;
        public Color color = Color.white;
        public bool showOnMinimap = true;
    }
}
