using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// Required for DefaultFormat
using UnityEngine.Experimental.Rendering;


namespace kTools.Mirrors
{
    /// <summary>
    /// Mirror Object component.
    /// </summary>
    [AddComponentMenu("kTools/Mirror"), ExecuteInEditMode]
    [RequireComponent(typeof(Camera), typeof(UniversalAdditionalCameraData))]
    public class Mirror : MonoBehaviour
    {
#region Enumerations
        /// <summary>
        /// Camera override enumeration for Mirror properties
        /// </summary>
        public enum MirrorCameraOverride
        {
            UseSourceCameraSettings,
            Off,
        }

        /// <summary>
        /// Scope enumeration for Mirror output destination
        /// </summary>
        public enum OutputScope
        {
            Global,
            Local,
        }
#endregion

#region Serialized Fields
        [SerializeField]
        float m_Offset;

        [SerializeField]
        LayerMask m_LayerMask = -1;

        [SerializeField]
        OutputScope m_Scope;

        [SerializeField]
        List<Renderer> m_Renderers;

        [SerializeField]
        float m_TextureScale;

        [SerializeField]
        MirrorCameraOverride m_AllowHDR;

        [SerializeField]
        MirrorCameraOverride m_AllowMSAA;
#endregion

#region Fields
        const string kGizmoPath = "Packages/com.kink3d.mirrors/Gizmos/Mirror.png";
        Camera m_ReflectionCamera;
        UniversalAdditionalCameraData m_CameraData;
        RenderTexture m_RenderTexture;
        RenderTextureDescriptor m_PreviousDescriptor;
        private static readonly ProfilingSampler s_MirrorProfilingSampler = new ProfilingSampler("Mirror Rendering");
#endregion

#region Constructors
        public Mirror()
        {
            m_Offset = 0.01f;
            m_Scope = OutputScope.Global;
            m_Renderers = new List<Renderer>();
            m_TextureScale = 1.0f;
            m_AllowHDR = MirrorCameraOverride.UseSourceCameraSettings;
            m_AllowMSAA = MirrorCameraOverride.UseSourceCameraSettings;
        }
#endregion

#region Properties
        public float offset
        {
            get => m_Offset;
            set => m_Offset = value;
        }

        public LayerMask layerMask
        {
            get => m_LayerMask;
            set => m_LayerMask = value;
        }

        public OutputScope scope
        {
            get => m_Scope;
            set => m_Scope = value;
        }

        public List<Renderer> renderers
        {
            get => m_Renderers;
            set => m_Renderers = value;
        }

        public float textureScale
        {
            get => m_TextureScale;
            set => m_TextureScale = value;
        }

        public MirrorCameraOverride allowHDR
        {
            get => m_AllowHDR;
            set => m_AllowHDR = value;
        }

        public MirrorCameraOverride allowMSAA
        {
            get => m_AllowMSAA;
            set => m_AllowMSAA = value;
        }

        Camera reflectionCamera
        {
            get
            {
                // Ensure component is fetched only once if null
                if (m_ReflectionCamera == null)
                    m_ReflectionCamera = GetComponent<Camera>();
                return m_ReflectionCamera;
            }
        }

        UniversalAdditionalCameraData cameraData
        {
            get
            {
                 // Ensure component is fetched only once if null
                if (m_CameraData == null)
                    m_CameraData = GetComponent<UniversalAdditionalCameraData>();
                return m_CameraData;
            }
        }
#endregion

#region State
        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            InitializeCamera(); // Initialize camera settings on enable
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            SafeDestroyObject(m_RenderTexture);
            m_RenderTexture = null;
            m_PreviousDescriptor = default;
        }
#endregion

#region Initialization
        void InitializeCamera()
        {
            // Check if component exists before accessing
            if (reflectionCamera == null || cameraData == null)
            {
                 Debug.LogWarning("Mirror requires both Camera and UniversalAdditionalCameraData components.", this);
                 enabled = false; // Disable the component if setup is invalid
                 return;
            }

            reflectionCamera.cameraType = CameraType.Reflection;
            reflectionCamera.enabled = false;
            reflectionCamera.targetTexture = m_RenderTexture;

            cameraData.renderShadows = false;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
            cameraData.renderPostProcessing = false;
        }
#endregion

#region RenderTexture
        RenderTextureDescriptor GetDescriptor(Camera camera)
        {
            var width = (int)Mathf.Max(camera.pixelWidth * textureScale, 4);
            var height = (int)Mathf.Max(camera.pixelHeight * textureScale, 4);

            var hdr = allowHDR == MirrorCameraOverride.UseSourceCameraSettings ? camera.allowHDR : false;
             // --- FIX: Use UnityEngine.Experimental.Rendering.DefaultFormat ---
            var graphicsFormat = hdr ? SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.HDR)
                                     : SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.LDR);

            int msaaSamples = 1;
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            bool cameraAllowsMSAA = allowMSAA == MirrorCameraOverride.UseSourceCameraSettings ? camera.allowMSAA : false;

            if (cameraAllowsMSAA && urpAsset != null)
            {
                msaaSamples = urpAsset.msaaSampleCount;
            }
            // Ensure MSAA samples is a power of 2 (1, 2, 4, 8) as expected by graphics APIs
             msaaSamples = Mathf.Clamp(Mathf.NextPowerOfTwo(msaaSamples)/2, 1, 8);


            // --- FIX: Use UnityEngine.Experimental.Rendering.DefaultFormat ---
            return new RenderTextureDescriptor(width, height, graphicsFormat, SystemInfo.GetGraphicsFormat(UnityEngine.Experimental.Rendering.DefaultFormat.DepthStencil))
            {
                 msaaSamples = msaaSamples,
                 autoGenerateMips = true,
                 useMipMap = true,
                 // Ensure depth buffer matches potential MSAA samples if needed (can sometimes cause issues if mismatched)
                 // depthStencilFormat = GraphicsFormatUtility.GetDepthStencilFormat(24, graphicsFormat) // More advanced if needed
            };
        }
#endregion

#region Rendering
        void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
                return;

            // Ensure reflection camera is valid and initialized
            if (reflectionCamera == null || cameraData == null) {
                 InitializeCamera(); // Try initializing again if null
                 if (reflectionCamera == null || cameraData == null) return; // Still invalid, exit
            }

            CommandBuffer cmd = CommandBufferPool.Get("Mirror Rendering");
            using (new ProfilingScope(cmd, s_MirrorProfilingSampler))
            {
                ExecuteCommand(context, cmd); // Execute setup commands if any

                var descriptor = GetDescriptor(camera);
                if (m_RenderTexture == null || !DescriptorsMatch(m_PreviousDescriptor, descriptor))
                {
                    SafeDestroyObject(m_RenderTexture);
                    m_RenderTexture = new RenderTexture(descriptor);
                    m_RenderTexture.name = $"Mirror_{gameObject.GetInstanceID()}_RT";
                    m_PreviousDescriptor = descriptor; // Store the new descriptor
                    reflectionCamera.targetTexture = m_RenderTexture;
                }

                RenderMirror(context, camera);
                SetShaderUniforms(context, m_RenderTexture, cmd);
            }
            ExecuteCommand(context, cmd);
            CommandBufferPool.Release(cmd);
        }

        // Helper to compare relevant descriptor fields as Equals might be too strict
        bool DescriptorsMatch(RenderTextureDescriptor d1, RenderTextureDescriptor d2)
        {
            return d1.width == d2.width &&
                   d1.height == d2.height &&
                   d1.graphicsFormat == d2.graphicsFormat &&
                   d1.depthStencilFormat == d2.depthStencilFormat &&
                   d1.msaaSamples == d2.msaaSamples &&
                   d1.useMipMap == d2.useMipMap &&
                   d1.autoGenerateMips == d2.autoGenerateMips;
        }


        void RenderMirror(ScriptableRenderContext context, Camera camera)
        {
            if (reflectionCamera.targetTexture == null)
            {
                // This case should be handled by the descriptor check in BeginCameraRendering,
                // but add a safeguard.
                Debug.LogError("Mirror Reflection Camera has no target texture.", this);
                return;
            }

            var mirrorMatrix = GetMirrorMatrix();
            reflectionCamera.worldToCameraMatrix = camera.worldToCameraMatrix * mirrorMatrix;

            var mirrorPlane = GetMirrorPlane(reflectionCamera);
            var projectionMatrix = camera.CalculateObliqueMatrix(mirrorPlane);
            reflectionCamera.projectionMatrix = projectionMatrix;

            reflectionCamera.cullingMask = layerMask;
            reflectionCamera.allowHDR = allowHDR == MirrorCameraOverride.UseSourceCameraSettings ? camera.allowHDR : false;
            reflectionCamera.allowMSAA = allowMSAA == MirrorCameraOverride.UseSourceCameraSettings ? camera.allowMSAA : false;


            if (!reflectionCamera.TryGetCullingParameters(false, out var cullingParams))
            {
                return;
            }
             // --- FIX: Removed ForceEvenDepthRange ---
            // cullingParams.cullingOptions &= ~CullingOptions.ForceEvenDepthRange;
             // --- FIX: Removed maxAdditionalLightsCount setting ---
            // cullingParams.maximumVisibleLights = cameraData.maxAdditionalLightsCount; // Not needed/available


             // --- FIX: Simplified RenderRequest constructor ---
            GL.invertCulling = true;
            context.ExecuteCommandBuffer(CommandBufferPool.Get("Mirror Rendering"));
            reflectionCamera.Render();
            GL.invertCulling = false;
        }
#endregion

#region Projection
        Matrix4x4 GetMirrorMatrix()
        {
            var position = transform.position;
            var normal = transform.forward;

            // Plane equation: n.p + d = 0 => d = -n.p
            // Offset is applied along the normal *before* calculating d
            float d = -Vector3.Dot(normal, position + normal * offset);

            Matrix4x4 reflectionMat = Matrix4x4.identity;
            reflectionMat.m00 = 1 - 2 * normal.x * normal.x;
            reflectionMat.m01 = -2 * normal.x * normal.y;
            reflectionMat.m02 = -2 * normal.x * normal.z;
            reflectionMat.m03 = -2 * d * normal.x;

            reflectionMat.m10 = -2 * normal.y * normal.x;
            reflectionMat.m11 = 1 - 2 * normal.y * normal.y;
            reflectionMat.m12 = -2 * normal.y * normal.z;
            reflectionMat.m13 = -2 * d * normal.y;

            reflectionMat.m20 = -2 * normal.z * normal.x;
            reflectionMat.m21 = -2 * normal.z * normal.y;
            reflectionMat.m22 = 1 - 2 * normal.z * normal.z;
            reflectionMat.m23 = -2 * d * normal.z;

            // m30, m31, m32, m33 remain 0, 0, 0, 1

            return reflectionMat;
        }

        Vector4 GetMirrorPlane(Camera camera)
        {
            // Plane in world space
            var pos = transform.position;
            var normal = transform.forward;
            var offsetPos = pos + normal * offset;
            var worldPlane = new Plane(normal, offsetPos);

            // Transform plane to view space for CalculateObliqueMatrix
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 viewMatrixInv = Matrix4x4.Inverse(viewMatrix); // More stable than transposing inverse
            Vector3 viewPos = viewMatrix.MultiplyPoint(offsetPos);
            Vector3 viewNormal = viewMatrix.MultiplyVector(normal).normalized; // Use MultiplyVector for normals

            // Plane equation in view space: viewNormal . (p - viewPos) = 0
            // viewNormal.x*x + viewNormal.y*y + viewNormal.z*z - dot(viewNormal, viewPos) = 0
            // Vector4 is (normal.x, normal.y, normal.z, -dot(normal, pos))
            return new Vector4(viewNormal.x, viewNormal.y, viewNormal.z, -Vector3.Dot(viewNormal, viewPos));
        }
#endregion

#region Output
        private static readonly int s_ReflectionMapID = Shader.PropertyToID("_ReflectionMap");
        private static readonly int s_LocalMirrorID = Shader.PropertyToID("_LocalMirror");
        private static readonly int s_LocalReflectionMapID = Shader.PropertyToID("_LocalReflectionMap");
        private const string s_BlendMirrorsKeyword = "_BLEND_MIRRORS";

        void SetShaderUniforms(ScriptableRenderContext context, RenderTexture renderTexture, CommandBuffer cmd)
        {
             if (renderTexture == null) return;

            var block = new MaterialPropertyBlock(); // Pool this if called extremely frequently
            switch(scope)
            {
                case OutputScope.Global:
                    cmd.DisableShaderKeyword(s_BlendMirrorsKeyword);
                    cmd.SetGlobalTexture(s_ReflectionMapID, renderTexture);

                    block.SetFloat(s_LocalMirrorID, 0.0f);
                    if (m_Renderers != null) {
                        foreach(var renderer in m_Renderers)
                        {
                             if (renderer != null) renderer.SetPropertyBlock(block);
                        }
                    }
                    break;

                case OutputScope.Local:
                    cmd.EnableShaderKeyword(s_BlendMirrorsKeyword);

                    block.SetTexture(s_LocalReflectionMapID, renderTexture);
                    block.SetFloat(s_LocalMirrorID, 1.0f);
                    if (m_Renderers != null) {
                        foreach(var renderer in m_Renderers)
                        {
                             if (renderer != null) renderer.SetPropertyBlock(block);
                        }
                    }
                    break;
            }
        }
#endregion

#region CommandBuffer
        void ExecuteCommand(ScriptableRenderContext context, CommandBuffer cmd)
        {
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
#endregion

#region Object
        void SafeDestroyObject(Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
#endregion

#region AssetMenu
#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/kTools/Mirror", false, 10)]
        static void CreateMirrorObject(UnityEditor.MenuCommand menuCommand)
        {
            GameObject go = new GameObject("New Mirror", typeof(Mirror));
            UnityEditor.GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            UnityEditor.Selection.activeObject = go;
        }
#endif
#endregion

#region Gizmos
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            var bounds = new Vector3(1.0f, 1.0f, 0.0f);
            var color = new Color32(0, 120, 255, 255);
            var selectedColor = Color.white; // Use white for selected outline
            var isSelected = UnityEditor.Selection.activeGameObject == gameObject;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = isSelected ? selectedColor : color;
            Gizmos.DrawWireCube(Vector3.zero, bounds);

            Gizmos.color = Color.blue; // Draw normal vector
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.5f);
        }

        void OnDrawGizmosSelected()
        {
             // Keep icon draw for selected only
             Gizmos.DrawIcon(transform.position, kGizmoPath, true, Color.white);
        }
#endif
#endregion
    }
}