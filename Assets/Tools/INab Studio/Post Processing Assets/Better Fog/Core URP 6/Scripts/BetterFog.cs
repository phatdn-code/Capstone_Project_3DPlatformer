using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEditor;

using INab.BetterFog.Core;


namespace INab.BetterFog.URP
{
    public class BetterFog : ScriptableRendererFeature
    {
        [Serializable]
        public class BetterFogSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;

            public bool _UseCustomDepthTexture = false;
            public bool _UseFogOffsetTexture = false;
            public bool _DisableSceneView = false;
        }

        public BetterFogSettings m_Settings = new BetterFogSettings();

        private Material m_FogFactorMaterial;
        private Material m_FogBlendMaterial;
        private Material m_SMSSMaterial;
        private Material m_CustomDepthPassMaterial;
        
        [SerializeField][HideInInspector]private Shader m_FogFactorShader;
        [SerializeField][HideInInspector]private Shader m_FogBlendShader;
        [SerializeField][HideInInspector]private Shader m_SMSSShader;
        [SerializeField][HideInInspector] private Shader m_CustomDepthPassShader;

        private TemporaryBlitPass temporaryBlitPass;
        private FogBlendBlit fogBlendBlit;
        private FogFactorBlit fogFactorBlit;

        private SMSSPass smssPass;
        private CustomDepthPass customDepthPass;
        private FogOffsetPass fogOffsetPass;

        private BetterFogVolumeComponent m_BetterFogVolume;
        public static int kMaxIterations = 16;

        public bool m_UseSMSS
        {
            get { return m_BetterFogVolume._UseSSMS.value; }
        }

        private List<CustomRenderer> m_DepthRenderers = new List<CustomRenderer>();
        private List<CustomRenderer> m_FogOffsetRenderers = new List<CustomRenderer>();

        public override void Create()
        {
            temporaryBlitPass = new TemporaryBlitPass();
            fogBlendBlit = new FogBlendBlit();
            fogFactorBlit = new FogFactorBlit();

            smssPass = new SMSSPass();
            customDepthPass = new CustomDepthPass();
            fogOffsetPass = new FogOffsetPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)

        {
            if (renderingData.cameraData.cameraType == CameraType.SceneView && m_Settings._DisableSceneView)
                return;

            if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
                return;

            // [Update 2.1] disable better fog by toggling off Post Processing in camera settings
            if (renderingData.cameraData.postProcessEnabled == false)
                return;

            m_BetterFogVolume = VolumeManager.instance.stack?.GetComponent<BetterFogVolumeComponent>();
            if (m_BetterFogVolume == null || !m_BetterFogVolume.IsActive())
                return;

            // [Update 2.1] fixed memory leaks 
            // Create materials only once
            if (m_FogFactorMaterial == null) m_FogFactorMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/FogFactor"));
            if (m_FogBlendMaterial == null) m_FogBlendMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/FogBlend"));
            if (m_SMSSMaterial == null) m_SMSSMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/INabStudio/SSMS_URP"));
            if (m_CustomDepthPassMaterial == null) m_CustomDepthPassMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/DepthBlit"));

            // Get custom renderers
            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                var sceneCamera = FindFirstObjectByType<Camera>();
                var set = sceneCamera.GetComponent<BetterFogRenderers>();

                if (set != null)
                {
                    m_DepthRenderers = set.depthRenderers;
                    m_FogOffsetRenderers = set.fogOffsetRenderers;
                }
            }

            if (m_FogFactorMaterial) FogFactorMaterialProperties();
            if (m_FogBlendMaterial) FogBlendMaterialProperties();

            temporaryBlitPass.renderPassEvent = m_Settings.Event;
            customDepthPass.renderPassEvent = m_Settings.Event;
            fogOffsetPass.renderPassEvent = m_Settings.Event;
            fogFactorBlit.renderPassEvent = m_Settings.Event;
            fogBlendBlit.renderPassEvent = m_Settings.Event;
            smssPass.renderPassEvent = m_Settings.Event;

            temporaryBlitPass.Setup(m_UseSMSS);
            if(m_Settings._UseCustomDepthTexture) customDepthPass.Setup(m_CustomDepthPassMaterial, m_DepthRenderers);
            if(m_Settings._UseFogOffsetTexture) fogOffsetPass.Setup(m_FogOffsetRenderers);
            fogFactorBlit.Setup(m_FogFactorMaterial);

            renderer.EnqueuePass(temporaryBlitPass);
            if (m_Settings._UseCustomDepthTexture) renderer.EnqueuePass(customDepthPass);
            if (m_Settings._UseFogOffsetTexture) renderer.EnqueuePass(fogOffsetPass);
            renderer.EnqueuePass(fogFactorBlit);

            fogBlendBlit.Setup(m_FogBlendMaterial, m_UseSMSS);
            renderer.EnqueuePass(fogBlendBlit);

            if(m_UseSMSS)
            {
                var tw = renderingData.cameraData.cameraTargetDescriptor.width;
                var th = renderingData.cameraData.cameraTargetDescriptor.height;

                // Do fog on a half resolution, full resolution doesn't bring much
                tw /= 2;
                th /= 2;

                int SMSS_iterations = -1;
                if (m_SMSSMaterial)
                {
                    SMSS_iterations = SSMSProperties(tw, th);
                }

                smssPass.Setup(m_SMSSMaterial, SMSS_iterations);
                renderer.EnqueuePass(smssPass);
            }

        }

        private void FogFactorMaterialProperties()
        {
            if(m_Settings._UseCustomDepthTexture)
            {
                m_FogFactorMaterial.EnableKeyword("_USECUSTOMDEPTH_ON");
            }
            else
            {
                m_FogFactorMaterial.DisableKeyword("_USECUSTOMDEPTH_ON");
            }

            if (m_Settings._UseFogOffsetTexture)
            {
                m_FogFactorMaterial.SetInt("_UseFogOffset", 1);

            }
            else
            {
                m_FogFactorMaterial.SetInt("_UseFogOffset", 0);

            }

            if (m_BetterFogVolume._UseDistanceFog.value)
            {
                m_FogFactorMaterial.EnableKeyword("_USEDISTANCEFOG_ON");
            }
            else
            {
                m_FogFactorMaterial.DisableKeyword("_USEDISTANCEFOG_ON");
            }

            if (m_BetterFogVolume._UseSkyboxHeightFog.value)
            {
                m_FogFactorMaterial.EnableKeyword("_USESKYBOXHEIGHTFOG_ON");
            }
            else
            {
                m_FogFactorMaterial.DisableKeyword("_USESKYBOXHEIGHTFOG_ON");
            }

            if (m_BetterFogVolume._UseHeightFog.value)
            {
                m_FogFactorMaterial.EnableKeyword("_USEHEIGHTFOG_ON");
            }
            else
            {
                m_FogFactorMaterial.DisableKeyword("_USEHEIGHTFOG_ON");
            }

            if (m_BetterFogVolume._UseNoise.value)
            {
                m_FogFactorMaterial.EnableKeyword("_USENOISE_ON");
            }
            else
            {
                m_FogFactorMaterial.DisableKeyword("_USENOISE_ON");
            }

            m_FogFactorMaterial.SetFloat("_FogIntensity", m_BetterFogVolume._FogIntensity.value);
            m_FogFactorMaterial.SetInt("_UseRadialDistance", m_BetterFogVolume._UseRadialDistance.value ? 1 : 0);

            switch (m_BetterFogVolume._FogType.value)
            {
                case FogMode.Linear:
                    m_FogFactorMaterial.EnableKeyword("_FOGTYPE_LINEAR");
                    m_FogFactorMaterial.DisableKeyword("_FOGTYPE_EXP");
                    m_FogFactorMaterial.DisableKeyword("_FOGTYPE_EXP2");

                    break;
                case FogMode.Exponential:
                    m_FogFactorMaterial.DisableKeyword("_FOGTYPE_LINEAR");
                    m_FogFactorMaterial.EnableKeyword("_FOGTYPE_EXP");
                    m_FogFactorMaterial.DisableKeyword("_FOGTYPE_EXP2");
                    break;
                case FogMode.ExponentialSquared:
                    m_FogFactorMaterial.DisableKeyword("_FOGTYPE_LINEAR");
                    m_FogFactorMaterial.DisableKeyword("_FOGTYPE_EXP");
                    m_FogFactorMaterial.EnableKeyword("_FOGTYPE_EXP2");
                    break;
            }

            m_FogFactorMaterial.SetFloat("_DistanceFogOffset", m_BetterFogVolume._DistanceFogOffset.value);

            m_FogFactorMaterial.SetFloat("_SkyboxFogIntensity", m_BetterFogVolume._SkyboxFogIntensity.value);
            m_FogFactorMaterial.SetFloat("_SkyboxFogHardness", m_BetterFogVolume._SkyboxFogHardness.value);
            m_FogFactorMaterial.SetFloat("_SkyboxFogOffset", m_BetterFogVolume._SkyboxFogOffset.value);
            m_FogFactorMaterial.SetFloat("_SkyboxFill", m_BetterFogVolume._SkyboxFill.value);

            m_FogFactorMaterial.SetFloat("_HeightDensity", Mathf.Pow(m_BetterFogVolume._HeightDensity.value, 4));
            m_FogFactorMaterial.SetFloat("_Height", m_BetterFogVolume._Height.value);
            m_FogFactorMaterial.SetInt("_HeightFogTypeExp", ((int)m_BetterFogVolume._HeightFogType.value == 2 ? 1 : 0));

            m_FogFactorMaterial.SetFloat("_Scale1", m_BetterFogVolume._Scale1.value);
            m_FogFactorMaterial.SetFloat("_NoiseTimeScale1", m_BetterFogVolume._NoiseTimeScale1.value);
            m_FogFactorMaterial.SetFloat("_Lerp1", m_BetterFogVolume._Lerp1.value);
            m_FogFactorMaterial.SetFloat("_NoiseDistanceEnd", m_BetterFogVolume._NoiseDistanceEnd.value);
            m_FogFactorMaterial.SetFloat("_NoiseIntensity", m_BetterFogVolume._NoiseIntensity.value);
            m_FogFactorMaterial.SetFloat("_NoiseEndHardness", m_BetterFogVolume._NoiseEndHardness.value);

            m_FogFactorMaterial.SetVector("_NoiseSpeed1", m_BetterFogVolume._NoiseSpeed1.value);

            int useNoiseDistance = 0;
            int useNoiseHeight = 0;

            switch (m_BetterFogVolume._NoiseAffect.value)
            {
                case NoiseAffect.DistanceOnly:
                    useNoiseDistance = 1;
                    useNoiseHeight = 0;

                    break;
                case NoiseAffect.HeightOnly:
                    useNoiseDistance = 0;
                    useNoiseHeight = 1;
                    break;
                case NoiseAffect.Both:
                    useNoiseDistance = 1;
                    useNoiseHeight = 1;
                    break;
            }

            if (m_BetterFogVolume._UseDistanceFog.value == false)
                useNoiseDistance = 0;

            if (m_BetterFogVolume._UseHeightFog.value == false)
                useNoiseHeight = 0;

            m_FogFactorMaterial.SetInt("_UseNoiseDistance", useNoiseDistance);
            m_FogFactorMaterial.SetInt("_UseNoiseHeight", useNoiseHeight);


            // Distance Fog Values
            Vector4 sceneParams;
            float diff = m_BetterFogVolume._SceneEnd.value - m_BetterFogVolume._SceneStart.value;
            float invDiff = Mathf.Abs(diff) > 0.0001f ? 1.0f / diff : 0.0f;
            sceneParams.x = m_BetterFogVolume._FogDensity.value * 1.2011224087f; // density / sqrt(ln(2)), used by Exp2 fog mode
            sceneParams.y = m_BetterFogVolume._FogDensity.value * 1.4426950408f; // density / ln(2), used by Exp fog mode
            sceneParams.z = -invDiff;
            sceneParams.w = m_BetterFogVolume._SceneEnd.value * invDiff;
            m_FogFactorMaterial.SetVector("_SceneFogParams", sceneParams);
        }

        private void FogBlendMaterialProperties()
        {
            if (m_Settings._UseCustomDepthTexture)
            {
                m_FogFactorMaterial.EnableKeyword("_USECUSTOMDEPTH_ON");
            }
            else
            {
                m_FogFactorMaterial.DisableKeyword("_USECUSTOMDEPTH_ON");
            }

            if (m_BetterFogVolume._UseSunLight.value)
            {
                m_FogBlendMaterial.EnableKeyword("_USESUNLIGHT_ON");
            }
            else
            {
                m_FogBlendMaterial.DisableKeyword("_USESUNLIGHT_ON");
            }

            if (m_BetterFogVolume._UseGradient.value)
            {
                m_FogBlendMaterial.EnableKeyword("_USEGRADIENT_ON");
            }
            else
            {
                m_FogBlendMaterial.DisableKeyword("_USEGRADIENT_ON");
            }


            m_FogBlendMaterial.SetColor("_SunColor", m_BetterFogVolume._SunColor.value);
            m_FogBlendMaterial.SetFloat("_SunPower", m_BetterFogVolume._SunPower.value);
            m_FogBlendMaterial.SetFloat("_SunIntensity", m_BetterFogVolume._SunIntensity.value);

            m_FogBlendMaterial.SetColor("_FogColor", m_BetterFogVolume._FogColor.value);

            m_FogBlendMaterial.SetFloat("_GradientStart", m_BetterFogVolume._GradientStart.value);
            m_FogBlendMaterial.SetFloat("_GradientEnd", m_BetterFogVolume._GradientEnd.value);

            // Custom texture lerping
            // TODO change to standard TextureParemeter after Unity adds Texture Interp (in 2023 not available yet)
            if (m_BetterFogVolume._GradientTexture.value != null)
            {
                m_FogBlendMaterial.SetFloat("_GradientLerp", m_BetterFogVolume._GradientTexture.LerpValue);
                m_FogBlendMaterial.SetTexture("_GradientTextureFrom", m_BetterFogVolume._GradientTexture.FromTexture);
                m_FogBlendMaterial.SetTexture("_GradientTexture", m_BetterFogVolume._GradientTexture.value);
            }

            m_FogBlendMaterial.SetFloat("_EnergyLoss", m_BetterFogVolume._EnergyLoss.value);
        }

        private int SSMSProperties(float tw, float th)
        {
            // determine the iteration count
            var logh = Mathf.Log(th, 2) + m_BetterFogVolume._Radius.value - 8;
            var logh_i = (int)logh;
            var iterations = Mathf.Clamp(logh_i, 1, kMaxIterations);

            // update the shader properties
            var lthresh = m_BetterFogVolume._Threshold.value;
            m_SMSSMaterial.SetFloat("_Threshold", lthresh);

            var knee = lthresh * m_BetterFogVolume._SoftKnee.value + 1e-5f;
            var curve = new Vector3(lthresh - knee, knee * 2, 0.25f / knee);
            m_SMSSMaterial.SetVector("_Curve", curve);

            var pfo = !m_BetterFogVolume._HighQuality.value && m_BetterFogVolume._AntiFlicker.value;
            m_SMSSMaterial.SetFloat("_PrefilterOffs", pfo ? -0.5f : 0.0f);

            m_SMSSMaterial.SetFloat("_SampleScale", 0.5f + logh - logh_i);
            m_SMSSMaterial.SetFloat("_Intensity", m_BetterFogVolume._Intensity.value);

            var fadeRampTexture = m_BetterFogVolume._FadeRamp.value;
            if (fadeRampTexture != null) m_SMSSMaterial.SetTexture("_FadeTex", fadeRampTexture);
            m_SMSSMaterial.SetFloat("_BlurWeight", m_BetterFogVolume._BlurWeight.value);
            m_SMSSMaterial.SetFloat("_Radius", m_BetterFogVolume._Radius.value);

            if (m_BetterFogVolume._AntiFlicker.value)
            {
                m_SMSSMaterial.EnableKeyword("ANTI_FLICKER_ON");
            }
            else
            {
                m_SMSSMaterial.DisableKeyword("ANTI_FLICKER_ON");
            }

            if (m_BetterFogVolume._HighQuality.value)
            {
                m_SMSSMaterial.EnableKeyword("_HIGH_QUALITY_ON");
            }
            else
            {
                m_SMSSMaterial.DisableKeyword("_HIGH_QUALITY_ON");
            }

            return iterations;
        }

        protected override void Dispose(bool disposing)
        {
            // [Update 2.1] memory leak fix
            CoreUtils.Destroy(m_FogFactorMaterial);
            CoreUtils.Destroy(m_FogBlendMaterial);
            CoreUtils.Destroy(m_SMSSMaterial);
            CoreUtils.Destroy(m_CustomDepthPassMaterial);

            // old
            /*
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                if (m_FogFactorMaterial) Destroy(m_FogFactorMaterial);
                if (m_FogBlendMaterial) Destroy(m_FogBlendMaterial);
                if (m_SMSSMaterial) Destroy(m_SMSSMaterial);
                if (m_CustomDepthPassMaterial) Destroy(m_CustomDepthPassMaterial);
            }
            else
            {
                if (m_FogFactorMaterial) DestroyImmediate(m_FogFactorMaterial);
                if (m_FogBlendMaterial) DestroyImmediate(m_FogBlendMaterial);
                if (m_SMSSMaterial) DestroyImmediate(m_SMSSMaterial);
                if (m_CustomDepthPassMaterial) DestroyImmediate(m_CustomDepthPassMaterial);
            }
#else
                if(m_FogFactorMaterial)Destroy(m_FogFactorMaterial);
                if(m_FogBlendMaterial)Destroy(m_FogBlendMaterial);
                if(m_SMSSMaterial)Destroy(m_SMSSMaterial);
                if(m_CustomDepthPassMaterial)Destroy(m_CustomDepthPassMaterial);
#endif*/
        }
    }

    public class BetterFogPassData : ContextItem, IDisposable
    {
        private static readonly int kFogFactor = Shader.PropertyToID("_FogFactorRT");

        private static readonly int kBlitTexture = Shader.PropertyToID("_BlitTexture");
        private static readonly int kBlitScaleBias = Shader.PropertyToID("_BlitScaleBias");

        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();

        private RTHandle m_FogFactor;
        TextureHandle m_FogFactorHandle;

        private RTHandle m_Temporary;
        TextureHandle m_TemporaryHandle;

        // SMSS

        private RTHandle m_TemporaryAfterBlend;
        TextureHandle m_TemporaryAfterBlendHandle;

        // Custom Depth
        private RTHandle m_CustomDepth;
        TextureHandle m_CustomDepthHandle;

        // Fog Offset
        private RTHandle m_FogOffset;
        TextureHandle m_FogOffsetHandle;


        public void Init(RenderGraph renderGraph, RenderTextureDescriptor targetDescriptor, bool useSMSS)
        {
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_Temporary, targetDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_TemporaryTexture");
            m_TemporaryHandle = renderGraph.ImportTexture(m_Temporary);

            if(useSMSS)
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_TemporaryAfterBlend, targetDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_TemporaryTextureAfterBlend");
                m_TemporaryAfterBlendHandle = renderGraph.ImportTexture(m_TemporaryAfterBlend);
            }

            // if sue fog offset
            if (true)
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_FogOffset, targetDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_FogOffset");
                m_FogOffsetHandle = renderGraph.ImportTexture(m_FogOffset);
            }

            // if sue custom depth
            if (true)
            {
                targetDescriptor.colorFormat = RenderTextureFormat.RFloat;
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_CustomDepth, targetDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_CustomDepth");
                m_CustomDepthHandle = renderGraph.ImportTexture(m_CustomDepth);
            }

            targetDescriptor.colorFormat = RenderTextureFormat.RFloat;
            RenderingUtils.ReAllocateHandleIfNeeded(ref m_FogFactor, targetDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: "_FogFactor");
            m_FogFactorHandle = renderGraph.ImportTexture(m_FogFactor);
        }

        public override void Reset()
        {
            m_FogFactorHandle = TextureHandle.nullHandle;
            m_TemporaryHandle = TextureHandle.nullHandle;
            m_TemporaryAfterBlendHandle = TextureHandle.nullHandle;
        }

        ///////////////////////////////////////


        private static void DrawRenderers(List<CustomRenderer> list, RasterCommandBuffer cmd)
        {
            foreach (var customRenderer in list)
            {
                if (customRenderer.render == false) continue;

                var material = customRenderer.material;

                var renderer = customRenderer.renderer;

                if (renderer == false || material == false) continue;

                if (!customRenderer.alwaysRender)
                {
                    if (renderer.enabled == false || renderer.gameObject.activeInHierarchy == false) continue;
                }

                if (customRenderer.drawAllSubmeshes && renderer is not ParticleSystemRenderer)
                {
                    Mesh mesh = null;
                    if (renderer is SkinnedMeshRenderer)
                        mesh = (renderer as SkinnedMeshRenderer).sharedMesh;
                    else if (renderer is MeshRenderer)
                        mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        cmd.DrawRenderer(renderer, material, i, 0);
                    }
                }
                else
                {
                    cmd.DrawRenderer(renderer, material, 0, 0);
                }
            }
        }
        
        class CustomDepthData
        {
            public TextureHandle source;
            public TextureHandle destination;

            public Material material;

            public List<CustomRenderer> depthRenderers = new List<CustomRenderer>();
        }

        ///////////////////////////////////////
        // Custom Renderers
        ///////////////////////////////////////

        public void RecordCustomDepthBlit(RenderGraph renderGraph, ContextContainer frameData, Material material, List<CustomRenderer> depthRenderers)
        {
            if (depthRenderers == null)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var descriptor = cameraData.cameraTargetDescriptor;

            using (var builder = renderGraph.AddRasterRenderPass<CustomDepthData>("Custom Depth Pass", out var passData))
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                passData.depthRenderers = depthRenderers;
                passData.material = material;
                passData.source = resourceData.activeColorTexture;
                passData.destination = m_CustomDepthHandle;

                builder.AllowPassCulling(false);

                //builder.UseTexture(passData.source);
                builder.SetRenderAttachment(passData.destination, 0);

                builder.SetRenderFunc((CustomDepthData passData, RasterGraphContext rgContext) => ExecuteCustomDepthBlitPass(passData, rgContext));
            }
        }

        static void ExecuteCustomDepthBlitPass(CustomDepthData data, RasterGraphContext rgContext)
        {
            Blitter.BlitTexture(rgContext.cmd, data.source, new Vector4(1, 1, 0, 0),  data.material, 0);

            // we need this to avoid depth testing glitches
            rgContext.cmd.ClearRenderTarget(true, false, Color.black);
            DrawRenderers(data.depthRenderers, rgContext.cmd);
        }


        class FogOffsetData
        {
            public TextureHandle source;
            public TextureHandle destination;

            public List<CustomRenderer> fogRenderers = new List<CustomRenderer>();
        }

        public void RecordFogOffsetBlit(RenderGraph renderGraph, ContextContainer frameData, List<CustomRenderer> fogRenderers)
        {
            if (fogRenderers == null)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var descriptor = cameraData.cameraTargetDescriptor;

            using (var builder = renderGraph.AddRasterRenderPass<FogOffsetData>("Fog Offset Pass", out var passData))
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                passData.fogRenderers = fogRenderers;
                passData.source = resourceData.activeColorTexture;
                passData.destination = m_FogOffsetHandle;

                builder.AllowPassCulling(false);

                builder.SetRenderAttachment(passData.destination, 0);
                builder.SetRenderFunc((FogOffsetData passData, RasterGraphContext rgContext) => ExecuteFogOffsetPass(passData, rgContext));
            }
        }

        static void ExecuteFogOffsetPass(FogOffsetData data, RasterGraphContext rgContext)
        {
            // we need this to avoid depth testing glitches
            rgContext.cmd.ClearRenderTarget(true, true, Color.black);
            DrawRenderers(data.fogRenderers, rgContext.cmd);
        }

        ///////////////////////////////////////

        class PassData
        {
            public TextureHandle source;
            public TextureHandle destination;

            public TextureHandle m_FogFactorHandle;
            public TextureHandle customDepthHandle;
            public TextureHandle fogOffsetHandle;
            
            public Material m_FogFactorMaterial;
            public Material m_FogBlendMaterial;
        }

        ///////////////////////////////////////
        // Temporary Blit
        ///////////////////////////////////////

        public void RecordTemporaryBlit(RenderGraph renderGraph, ContextContainer frameData,bool useSMSS)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var descriptor = cameraData.cameraTargetDescriptor;

            if (!m_TemporaryHandle.IsValid())
            {
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                Init(renderGraph, descriptor, useSMSS);
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Better Fog Temporary Blit", out var passData))
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                passData.source = resourceData.activeColorTexture;
                passData.destination = m_TemporaryHandle;

                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(passData.destination, 0);

                builder.SetRenderFunc((PassData passData, RasterGraphContext rgContext) => ExecuteTemporaryBlitPass(passData, rgContext));
            }
        }

        static void ExecuteTemporaryBlitPass(PassData data, RasterGraphContext rgContext)
        {
            Blitter.BlitTexture(rgContext.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), 0, false);
        }

        ///////////////////////////////////////
        // Fog Factor Blit
        ///////////////////////////////////////

        public void RecordFogFactorPass(RenderGraph renderGraph, ContextContainer frameData, Material material)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var descriptor = cameraData.cameraTargetDescriptor;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fog Factor Pass", out var passData))
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                material.SetMatrix("_InverseView", cameraData.camera.cameraToWorldMatrix);

                passData.m_FogFactorMaterial = material;
                passData.customDepthHandle = m_CustomDepthHandle;
                passData.fogOffsetHandle = m_FogOffsetHandle;
                
                //passData.source = resourceData.activeColorTexture;
                passData.destination = m_FogFactorHandle;

               // builder.UseTexture(passData.source);
                builder.SetRenderAttachment(passData.destination, 0);

                builder.SetRenderFunc((PassData passData, RasterGraphContext rgContext) => ExecuteFogFactorPass(passData, rgContext));
            }
        }

        private static void ExecuteFogFactorPass(PassData data, RasterGraphContext context)
        {
            s_SharedPropertyBlock.Clear();
            if (data.customDepthHandle.IsValid()) s_SharedPropertyBlock.SetTexture("_CustomDepth", data.customDepthHandle);
            if (data.fogOffsetHandle.IsValid()) s_SharedPropertyBlock.SetTexture("_FogOffset", data.fogOffsetHandle);

            context.cmd.DrawProcedural(Matrix4x4.identity, data.m_FogFactorMaterial, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        ///////////////////////////////////////
        // Fog Blend Blit
        ///////////////////////////////////////

        public void RecordFogBlendPass(RenderGraph renderGraph, ContextContainer frameData, Material material, bool useSMSS)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var descriptor = cameraData.cameraTargetDescriptor;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fog Blend Pass", out var passData))
            {
                var resourceData = frameData.Get<UniversalResourceData>();

                material.SetMatrix("_InverseView", cameraData.camera.cameraToWorldMatrix);

                passData.m_FogBlendMaterial = material;

                passData.m_FogFactorHandle = m_FogFactorHandle;
                builder.UseTexture(m_FogFactorHandle);

                passData.source = m_TemporaryHandle;

                if(useSMSS== false)
                {
                    passData.destination = resourceData.activeColorTexture;
                }
                else
                {
                    passData.destination = m_TemporaryAfterBlendHandle;
                }

                builder.UseTexture(passData.source);
                builder.SetRenderAttachment(passData.destination, 0);

                builder.SetRenderFunc((PassData passData, RasterGraphContext rgContext) => ExecuteFogBlendPass(passData, rgContext));
            }
        }

        private static void ExecuteFogBlendPass(PassData data, RasterGraphContext context)
        {
            s_SharedPropertyBlock.Clear();
            if (data.source.IsValid()) s_SharedPropertyBlock.SetTexture(kBlitTexture, data.source);
            if (data.m_FogFactorHandle.IsValid()) s_SharedPropertyBlock.SetTexture(kFogFactor, data.m_FogFactorHandle);

            s_SharedPropertyBlock.SetVector(kBlitScaleBias, new Vector4(1, 1, 0, 0));

            context.cmd.DrawProcedural(Matrix4x4.identity, data.m_FogBlendMaterial, 0, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        ///////////////////////////////////////
        // SMSS
        ///////////////////////////////////////

        class PassDataSMSS
        {
            public Material material;
            public int iterations;

            public TextureHandle temporaryAfterBlendHandle;
            public TextureHandle prefiltered;
            public TextureHandle last;
            public TextureHandle cameraTarget;
            public TextureHandle fogFactor;

            public TextureHandle[] _blurBuffer1 = new TextureHandle[BetterFog.kMaxIterations];
            public TextureHandle[] _blurBuffer2 = new TextureHandle[BetterFog.kMaxIterations];
        }

        public void RecordSMSSPass(RenderGraph renderGraph, ContextContainer frameData, Material material, int iterations)
        {
            using (var builder = renderGraph.AddUnsafePass<PassDataSMSS>("SMSS Pass", out var passData))
            {

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                passData.temporaryAfterBlendHandle = m_TemporaryAfterBlendHandle;
                passData.cameraTarget = resourceData.activeColorTexture;
                passData.fogFactor = m_FogFactorHandle;
                passData.material = material;
                passData.iterations = iterations;


                TextureHandle prefiltered = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_Prefiltered_Unsafe", false);
                passData.prefiltered = prefiltered;

                int tw = desc.width;
                int th = desc.height;

                for (var level = 0; level < iterations; level++)
                {
                    tw = Mathf.Max(tw / 2, 1);
                    th = Mathf.Max(th / 2, 1);

                    desc.width = tw;
                    desc.height = th;

                    TextureHandle blurBuffer1 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurBuffer_"+level, false,FilterMode.Bilinear);
                    TextureHandle blurBuffer2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BlurBuffer_"+(level+1).ToString(), false, FilterMode.Bilinear);
                    passData._blurBuffer1[level] = blurBuffer1;
                    passData._blurBuffer2[level] = blurBuffer2;

                    builder.UseTexture(passData._blurBuffer1[level], AccessFlags.Write);
                    builder.UseTexture(passData._blurBuffer2[level], AccessFlags.Write);
                }

                // Without these the pass works, but it shouldn't?
                builder.UseTexture(passData.temporaryAfterBlendHandle, AccessFlags.Read);
                builder.UseTexture(passData.cameraTarget, AccessFlags.Write);


                builder.UseTexture(passData.prefiltered, AccessFlags.Write);
                builder.UseTexture(passData.fogFactor, AccessFlags.Read);


                builder.AllowPassCulling(false);


                builder.SetRenderFunc((PassDataSMSS data, UnsafeGraphContext context) => ExecuteSMSSPass(data, context));
            }
        }

        private static void ExecuteSMSSPass(PassDataSMSS data, UnsafeGraphContext context)
        {
            CommandBuffer unsafeCmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            context.cmd.SetGlobalTexture("_FogFactor_RT", data.fogFactor);

            int pass = 0;

            // Pass: 0 - temporary -> prefiltered
            context.cmd.SetRenderTarget(data.prefiltered);
            Blitter.BlitTexture(unsafeCmd, data.temporaryAfterBlendHandle, new Vector4(1, 1, 0, 0),  data.material, pass);

            // construct a mip pyramid
            data.last = data.prefiltered;

            for (var level = 0; level < data.iterations; level++)
            {
                pass = (level == 0) ? 1 : 2;
                //context.cmd.SetGlobalTexture("_MainTex", data.last);

                context.cmd.SetRenderTarget(data._blurBuffer1[level]);
                Blitter.BlitTexture(unsafeCmd, data.last, new Vector4(1, 1, 0, 0),  data.material, pass);

                data.last = data._blurBuffer1[level];
            }


            // upsample and combine loop
            for (var level = data.iterations - 2; level >= 0; level--)
            {
                var basetex = data._blurBuffer1[level];
                context.cmd.SetGlobalTexture("_BaseTextureUpscale", basetex);

                pass = 3;
                context.cmd.SetRenderTarget(data._blurBuffer2[level]);
                Blitter.BlitTexture(unsafeCmd, data.last, new Vector4(1, 1, 0, 0), data.material, pass);

                data.last = data._blurBuffer2[level];
            }


            context.cmd.SetRenderTarget(data.cameraTarget);
            Blitter.BlitTexture(unsafeCmd, data.last, new Vector4(1, 1, 0, 0), 0, false);


            // finish process
            context.cmd.SetGlobalTexture("_BaseTexture", data.temporaryAfterBlendHandle);

            pass = 4;
            context.cmd.SetRenderTarget(data.cameraTarget);
            Blitter.BlitTexture(unsafeCmd, data.last, new Vector4(1, 1, 0, 0), data.material, pass);
        }

        ///////////////////////////////////////


        public void Dispose()
        {
            m_FogFactor?.Release();
            m_Temporary?.Release();
            m_TemporaryAfterBlend?.Release();
            m_CustomDepth?.Release();
            m_FogOffset?.Release();
        }

    }

    ///////////////////////////////////////
    // Custom Renderers
    ///////////////////////////////////////

    class CustomDepthPass : ScriptableRenderPass
    {
        private Material material;
        private List<CustomRenderer> depthRenderers;

        public void Setup(Material material, List<CustomRenderer> depthRenderers)
        {
            this.material = material;
            this.depthRenderers = depthRenderers;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var blitTextureData = frameData.GetOrCreate<BetterFogPassData>();
            blitTextureData.RecordCustomDepthBlit(renderGraph, frameData, material, depthRenderers);
        }
    }

    class FogOffsetPass : ScriptableRenderPass
    {
        private List<CustomRenderer> fogRenderers;

        public void Setup( List<CustomRenderer> fogRenderers)
        {
            this.fogRenderers = fogRenderers;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var blitTextureData = frameData.GetOrCreate<BetterFogPassData>();
            blitTextureData.RecordFogOffsetBlit(renderGraph, frameData, fogRenderers);
        }
    }

    ///////////////////////////////////////
    // Temporary Blit
    ///////////////////////////////////////

    class TemporaryBlitPass : ScriptableRenderPass
    {
        private bool m_UseSMSS;

        public void Setup(bool useSMSS)
        {
            m_UseSMSS = useSMSS;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var blitTextureData = frameData.GetOrCreate<BetterFogPassData>();
            blitTextureData.RecordTemporaryBlit(renderGraph, frameData, m_UseSMSS);
        }
    }

    ///////////////////////////////////////
    // Fog Factor Blit
    ///////////////////////////////////////

    class FogFactorBlit : ScriptableRenderPass
    {
        private Material m_Material;

        public void Setup(Material material)
        {
            m_Material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var blitTextureData = frameData.GetOrCreate<BetterFogPassData>();
            blitTextureData.RecordFogFactorPass(renderGraph, frameData, m_Material);
        }
    }

    ///////////////////////////////////////


    ///////////////////////////////////////
    // Fog Blend Blit
    ///////////////////////////////////////

    class FogBlendBlit : ScriptableRenderPass
    {
        private Material m_Material;
        private bool m_UseSMSS;

        public void Setup(Material material, bool useSMSS)
        {
            m_Material = material;
            m_UseSMSS = useSMSS;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var blitTextureData = frameData.GetOrCreate<BetterFogPassData>();
            blitTextureData.RecordFogBlendPass(renderGraph, frameData, m_Material,m_UseSMSS);
        }
    }


    ///////////////////////////////////////
    // SMSS
    ///////////////////////////////////////

    class SMSSPass : ScriptableRenderPass
    {
        private Material m_Material;
        private int m_Iterations;

        public void Setup(Material material, int iterations)
        {
            m_Material = material;
            m_Iterations = iterations;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var blitTextureData = frameData.GetOrCreate<BetterFogPassData>();
            blitTextureData.RecordSMSSPass(renderGraph, frameData, m_Material, m_Iterations);
        }
    }

    ///////////////////////////////////////

}