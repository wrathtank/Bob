using UnityEngine;
using System.Collections;

namespace BobsPetroleum.Environment
{
    /// <summary>
    /// Easy water system - just attach to a plane and it becomes wavy water!
    /// Works with WebGL. Uses vertex displacement for waves.
    ///
    /// SETUP:
    /// 1. Create a Plane (GameObject > 3D > Plane)
    /// 2. Add this WaterSystem component
    /// 3. Click "Setup Water" in inspector
    /// 4. Done! Wavy water.
    ///
    /// OR drag the "Water" prefab from prefabs folder.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class WaterSystem : MonoBehaviour
    {
        [Header("=== WAVE SETTINGS ===")]
        [Tooltip("Wave speed")]
        public float waveSpeed = 1f;

        [Tooltip("Wave height")]
        public float waveHeight = 0.5f;

        [Tooltip("Wave frequency")]
        public float waveFrequency = 1f;

        [Tooltip("Secondary wave scale")]
        public float secondaryWaveScale = 0.5f;

        [Header("=== WATER APPEARANCE ===")]
        [Tooltip("Shallow water color")]
        public Color shallowColor = new Color(0.2f, 0.6f, 0.8f, 0.8f);

        [Tooltip("Deep water color")]
        public Color deepColor = new Color(0.1f, 0.2f, 0.4f, 0.95f);

        [Tooltip("Foam color")]
        public Color foamColor = Color.white;

        [Tooltip("Water texture (optional)")]
        public Texture2D waterTexture;

        [Tooltip("Normal map (optional)")]
        public Texture2D normalMap;

        [Header("=== REFLECTION/REFRACTION ===")]
        [Tooltip("Reflectivity (0-1)")]
        [Range(0f, 1f)]
        public float reflectivity = 0.5f;

        [Tooltip("Transparency (0-1)")]
        [Range(0f, 1f)]
        public float transparency = 0.7f;

        [Tooltip("Fresnel power")]
        public float fresnelPower = 2f;

        [Header("=== FOAM ===")]
        [Tooltip("Enable foam at edges")]
        public bool enableFoam = true;

        [Tooltip("Foam threshold")]
        public float foamThreshold = 0.5f;

        [Tooltip("Foam texture")]
        public Texture2D foamTexture;

        [Header("=== PERFORMANCE ===")]
        [Tooltip("Use vertex waves (better performance)")]
        public bool useVertexWaves = true;

        [Tooltip("Mesh subdivisions for waves")]
        public int meshSubdivisions = 32;

        [Tooltip("Update material in editor")]
        public bool livePreview = true;

        // Runtime
        private Material waterMaterial;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh waterMesh;
        private Vector3[] originalVertices;
        private Vector3[] vertices;

        private static readonly int WaveSpeedID = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveHeightID = Shader.PropertyToID("_WaveHeight");
        private static readonly int WaveFrequencyID = Shader.PropertyToID("_WaveFrequency");
        private static readonly int ShallowColorID = Shader.PropertyToID("_ShallowColor");
        private static readonly int DeepColorID = Shader.PropertyToID("_DeepColor");
        private static readonly int TimeID = Shader.PropertyToID("_Time");

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            SetupWater();
        }

        private void Update()
        {
            if (useVertexWaves && waterMesh != null)
            {
                AnimateVertexWaves();
            }

            // Update shader time if using shader waves
            if (waterMaterial != null)
            {
                waterMaterial.SetFloat(TimeID, Time.time);
            }
        }

        #region Setup

        /// <summary>
        /// Set up water material and mesh
        /// </summary>
        [ContextMenu("Setup Water")]
        public void SetupWater()
        {
            // Create or get mesh
            if (useVertexWaves)
            {
                CreateSubdividedMesh();
            }

            // Create material
            CreateWaterMaterial();

            // Apply settings
            UpdateMaterialSettings();

            Debug.Log("[Water] Water system set up!");
        }

        private void CreateSubdividedMesh()
        {
            waterMesh = new Mesh();
            waterMesh.name = "WaterMesh";

            int vertCount = (meshSubdivisions + 1) * (meshSubdivisions + 1);
            vertices = new Vector3[vertCount];
            originalVertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[meshSubdivisions * meshSubdivisions * 6];

            // Generate vertices
            float step = 10f / meshSubdivisions; // Default plane is 10 units
            int v = 0;

            for (int z = 0; z <= meshSubdivisions; z++)
            {
                for (int x = 0; x <= meshSubdivisions; x++)
                {
                    float xPos = x * step - 5f;
                    float zPos = z * step - 5f;

                    vertices[v] = new Vector3(xPos, 0, zPos);
                    originalVertices[v] = vertices[v];
                    uvs[v] = new Vector2((float)x / meshSubdivisions, (float)z / meshSubdivisions);
                    v++;
                }
            }

            // Generate triangles
            int t = 0;
            for (int z = 0; z < meshSubdivisions; z++)
            {
                for (int x = 0; x < meshSubdivisions; x++)
                {
                    int topLeft = z * (meshSubdivisions + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + (meshSubdivisions + 1);
                    int bottomRight = bottomLeft + 1;

                    triangles[t++] = topLeft;
                    triangles[t++] = bottomLeft;
                    triangles[t++] = topRight;

                    triangles[t++] = topRight;
                    triangles[t++] = bottomLeft;
                    triangles[t++] = bottomRight;
                }
            }

            waterMesh.vertices = vertices;
            waterMesh.uv = uvs;
            waterMesh.triangles = triangles;
            waterMesh.RecalculateNormals();

            meshFilter.mesh = waterMesh;
        }

        private void CreateWaterMaterial()
        {
            // Try to find existing water shader, otherwise use Standard
            Shader waterShader = Shader.Find("BobsPetroleum/Water");

            if (waterShader == null)
            {
                // Use Standard shader with transparency
                waterShader = Shader.Find("Standard");
            }

            waterMaterial = new Material(waterShader);
            waterMaterial.name = "WaterMaterial";

            // Enable transparency
            waterMaterial.SetFloat("_Mode", 3); // Transparent mode
            waterMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            waterMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterMaterial.SetInt("_ZWrite", 0);
            waterMaterial.DisableKeyword("_ALPHATEST_ON");
            waterMaterial.EnableKeyword("_ALPHABLEND_ON");
            waterMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMaterial.renderQueue = 3000;

            meshRenderer.material = waterMaterial;
        }

        public void UpdateMaterialSettings()
        {
            if (waterMaterial == null) return;

            // Color
            waterMaterial.color = shallowColor;

            // Set custom properties if shader supports them
            if (waterMaterial.HasProperty(WaveSpeedID))
            {
                waterMaterial.SetFloat(WaveSpeedID, waveSpeed);
            }

            if (waterMaterial.HasProperty(WaveHeightID))
            {
                waterMaterial.SetFloat(WaveHeightID, waveHeight);
            }

            if (waterMaterial.HasProperty(WaveFrequencyID))
            {
                waterMaterial.SetFloat(WaveFrequencyID, waveFrequency);
            }

            if (waterMaterial.HasProperty(ShallowColorID))
            {
                waterMaterial.SetColor(ShallowColorID, shallowColor);
            }

            if (waterMaterial.HasProperty(DeepColorID))
            {
                waterMaterial.SetColor(DeepColorID, deepColor);
            }

            // Textures
            if (waterTexture != null)
            {
                waterMaterial.mainTexture = waterTexture;
            }

            if (normalMap != null && waterMaterial.HasProperty("_BumpMap"))
            {
                waterMaterial.SetTexture("_BumpMap", normalMap);
            }

            // Smoothness/metallic for Standard shader
            waterMaterial.SetFloat("_Metallic", reflectivity * 0.5f);
            waterMaterial.SetFloat("_Glossiness", 0.8f);
        }

        #endregion

        #region Vertex Wave Animation

        private void AnimateVertexWaves()
        {
            if (vertices == null || originalVertices == null) return;

            float time = Time.time * waveSpeed;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 original = originalVertices[i];

                // Primary wave
                float wave1 = Mathf.Sin(original.x * waveFrequency + time) * waveHeight;

                // Secondary wave (perpendicular)
                float wave2 = Mathf.Sin(original.z * waveFrequency * 0.8f + time * 1.3f) * waveHeight * secondaryWaveScale;

                // Combine waves
                vertices[i] = new Vector3(original.x, wave1 + wave2, original.z);
            }

            waterMesh.vertices = vertices;
            waterMesh.RecalculateNormals();
        }

        #endregion

        #region Interaction

        /// <summary>
        /// Check if a position is underwater
        /// </summary>
        public bool IsUnderwater(Vector3 position)
        {
            // Simple check - is position below water surface
            float waterY = transform.position.y + GetWaveHeightAt(position);
            return position.y < waterY;
        }

        /// <summary>
        /// Get wave height at world position
        /// </summary>
        public float GetWaveHeightAt(Vector3 worldPos)
        {
            if (!useVertexWaves) return 0f;

            // Convert to local space
            Vector3 local = transform.InverseTransformPoint(worldPos);

            float time = Time.time * waveSpeed;
            float wave1 = Mathf.Sin(local.x * waveFrequency + time) * waveHeight;
            float wave2 = Mathf.Sin(local.z * waveFrequency * 0.8f + time * 1.3f) * waveHeight * secondaryWaveScale;

            return wave1 + wave2;
        }

        /// <summary>
        /// Get buoyancy force at position
        /// </summary>
        public Vector3 GetBuoyancyForce(Vector3 position, float mass = 1f)
        {
            if (!IsUnderwater(position)) return Vector3.zero;

            float depth = (transform.position.y + GetWaveHeightAt(position)) - position.y;
            float force = depth * 10f * mass; // Buoyancy factor

            return Vector3.up * force;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (livePreview && Application.isPlaying)
            {
                UpdateMaterialSettings();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw water surface bounds
            Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(10, 0.1f, 10));
        }
#endif

        #endregion
    }

    /// <summary>
    /// Add to objects that should float on water
    /// </summary>
    public class WaterFloater : MonoBehaviour
    {
        [Tooltip("Reference to water system")]
        public WaterSystem water;

        [Tooltip("Buoyancy strength")]
        public float buoyancy = 10f;

        [Tooltip("Damping")]
        public float damping = 1f;

        [Tooltip("Float points (empty = use center)")]
        public Transform[] floatPoints;

        private Rigidbody rb;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();

            // Find water if not assigned
            if (water == null)
            {
                water = FindObjectOfType<WaterSystem>();
            }

            // Create default float point
            if (floatPoints == null || floatPoints.Length == 0)
            {
                floatPoints = new Transform[] { transform };
            }
        }

        private void FixedUpdate()
        {
            if (water == null || rb == null) return;

            foreach (var point in floatPoints)
            {
                if (point == null) continue;

                if (water.IsUnderwater(point.position))
                {
                    // Apply buoyancy
                    Vector3 force = water.GetBuoyancyForce(point.position, rb.mass) * buoyancy;
                    rb.AddForceAtPosition(force, point.position);

                    // Apply damping
                    rb.velocity *= (1f - damping * Time.fixedDeltaTime);
                }
            }
        }
    }

    /// <summary>
    /// Simple underwater effects for player
    /// </summary>
    public class UnderwaterEffect : MonoBehaviour
    {
        [Tooltip("Water system reference")]
        public WaterSystem water;

        [Tooltip("Underwater fog color")]
        public Color underwaterFogColor = new Color(0.1f, 0.3f, 0.5f);

        [Tooltip("Underwater fog density")]
        public float underwaterFogDensity = 0.1f;

        [Tooltip("Drowning damage per second")]
        public float drowningDamage = 5f;

        [Tooltip("Time before drowning starts")]
        public float breathHoldTime = 10f;

        // Runtime
        private Color originalFogColor;
        private float originalFogDensity;
        private bool wasUnderwater;
        private float underwaterTime;

        private void Start()
        {
            if (water == null)
            {
                water = FindObjectOfType<WaterSystem>();
            }

            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;
        }

        private void Update()
        {
            if (water == null) return;

            bool isUnderwater = water.IsUnderwater(transform.position);

            if (isUnderwater != wasUnderwater)
            {
                if (isUnderwater)
                {
                    EnterWater();
                }
                else
                {
                    ExitWater();
                }

                wasUnderwater = isUnderwater;
            }

            if (isUnderwater)
            {
                underwaterTime += Time.deltaTime;

                // Start drowning
                if (underwaterTime > breathHoldTime)
                {
                    var respawn = GetComponent<Player.DeathRespawnSystem>();
                    if (respawn != null)
                    {
                        respawn.TakeDamage(drowningDamage * Time.deltaTime);
                    }
                }
            }
            else
            {
                underwaterTime = 0f;
            }
        }

        private void EnterWater()
        {
            // Apply underwater fog
            RenderSettings.fogColor = underwaterFogColor;
            RenderSettings.fogDensity = underwaterFogDensity;
            RenderSettings.fog = true;

            Debug.Log("[Water] Entered water");
        }

        private void ExitWater()
        {
            // Restore normal fog
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;

            Debug.Log("[Water] Exited water");
        }

        private void OnDestroy()
        {
            // Restore fog on destroy
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
        }
    }
}
