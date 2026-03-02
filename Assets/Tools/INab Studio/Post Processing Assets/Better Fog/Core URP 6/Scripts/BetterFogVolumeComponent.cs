using INab.BetterFog.Core;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace INab.BetterFog.URP
{
    [VolumeComponentMenu("INab Studio/Better Fog")]
    [VolumeRequiresRendererFeatures(typeof(BetterFog))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    public sealed class BetterFogVolumeComponent : VolumeComponent, IPostProcessComponent
    {
        [Serializable]
        public sealed class FogParameterURP : VolumeParameter<FogMode> { public FogParameterURP(FogMode value, bool overrideState = false) : base(value, overrideState) { } }

        [Serializable]
        public sealed class HeightFogParameterURP : VolumeParameter<HeightFogType> { public HeightFogParameterURP(HeightFogType value, bool overrideState = false) : base(value, overrideState) { } }

        [Serializable]
        public sealed class NoiseAffectParameterURP : VolumeParameter<NoiseAffect> { public NoiseAffectParameterURP(NoiseAffect value, bool overrideState = false) : base(value, overrideState) { } }

        [Serializable]
        public class MyTextureParameter : TextureParameter
        {
            public MyTextureParameter(Texture value, bool overrideState = false)
                : this(value, TextureDimension.Any, overrideState) { }

            public MyTextureParameter(Texture value, TextureDimension dimension, bool overrideState = false)
                : base(value, overrideState)
            {
                this.dimension = dimension;
            }

            public float LerpValue;
            public Texture FromTexture;

            public override void Interp(Texture from, Texture to, float t)
            {
                LerpValue = t;
                FromTexture = from;
                value = to;
            }

        }

        public BetterFogVolumeComponent()
        {
            displayName = "Better Fog";
        }

        #region FogParameters

        public ClampedFloatParameter _FogIntensity = new ClampedFloatParameter(value: 1, min: 0, max: 1);
        public FloatParameter _EnergyLoss = new ClampedFloatParameter(value: 0, min: 0, max: 1);
        public BoolParameter _UseGradient = new BoolParameter(false);
        public ColorParameter _FogColor = new ColorParameter(new Color(.8f, .8f, .8f, 1f));
        public FloatParameter _GradientStart = new FloatParameter(0);
        public FloatParameter _GradientEnd = new FloatParameter(100);
        public MyTextureParameter _GradientTexture = new MyTextureParameter(null);

        // Sun Light Parameters
        public BoolParameter _UseSunLight = new BoolParameter(false);
        public ColorParameter _SunColor = new ColorParameter(new Color(.9f, .85f, .8f, 1f));
        public ClampedFloatParameter _SunIntensity = new ClampedFloatParameter(value: 0, min: 0, max: 1);
        public ClampedFloatParameter _SunPower = new ClampedFloatParameter(value: 2, min: .1f, max: 12);

        // Distance Fog Parameters
        public BoolParameter _UseDistanceFog = new BoolParameter(true);
        public BoolParameter _UseRadialDistance = new BoolParameter(false);
        public FogParameterURP _FogType = new FogParameterURP(FogMode.ExponentialSquared);
        public FloatParameter _DistanceFogOffset = new FloatParameter(-20);

        // Scene Parameters
        public FloatParameter _SceneStart = new FloatParameter(10);
        public FloatParameter _SceneEnd = new FloatParameter(100);

        // Fog Density
        public ClampedFloatParameter _FogDensity = new ClampedFloatParameter(value: 0, min: 0, max: .1f);

        // Skybox Height Fog Parameters
        public BoolParameter _UseSkyboxHeightFog = new BoolParameter(false);
        public ClampedFloatParameter _SkyboxFogOffset = new ClampedFloatParameter(value: 0, min: -.1f, max: .1f);
        public ClampedFloatParameter _SkyboxFogHardness = new ClampedFloatParameter(value: .75f, min: 0, max: .999f);
        public ClampedFloatParameter _SkyboxFogIntensity = new ClampedFloatParameter(value: 1, min: 0, max: 1);
        public ClampedFloatParameter _SkyboxFill = new ClampedFloatParameter(value: 0, min: 0, max: 1);

        // Height Fog Parameters
        public BoolParameter _UseHeightFog = new BoolParameter(false);
        public FloatParameter _Height = new FloatParameter(4);
        public ClampedFloatParameter _HeightDensity = new ClampedFloatParameter(value: 0, min: 0, max: .5f);
        public HeightFogParameterURP _HeightFogType = new HeightFogParameterURP(HeightFogType.ExponentialSquared);

        // Noise Parameters
        public BoolParameter _UseNoise = new BoolParameter(false);
        public NoiseAffectParameterURP _NoiseAffect = new NoiseAffectParameterURP(NoiseAffect.Both);
        public ClampedFloatParameter _NoiseIntensity = new ClampedFloatParameter(value: 0, min: 0, max: 1);
        public FloatParameter _NoiseDistanceEnd = new FloatParameter(80);
        public ClampedFloatParameter _NoiseEndHardness = new ClampedFloatParameter(value: 0.35f, min: 1, max: 16);

        // First Noise Layer
        public ClampedFloatParameter _Scale1 = new ClampedFloatParameter(value: 40, min: 5, max: 140);
        public ClampedFloatParameter _Lerp1 = new ClampedFloatParameter(value: .5f, min: 0, max: 1);
        public Vector3Parameter _NoiseSpeed1 = new Vector3Parameter(new Vector3(0, 0, 0));
        public ClampedFloatParameter _NoiseTimeScale1 = new ClampedFloatParameter(value: .1f, min: 0, max: .5f);

        #endregion

        #region SSMSParameters
        public BoolParameter _UseSSMS = new BoolParameter(false);
        public ClampedFloatParameter _Threshold = new ClampedFloatParameter(value: 0, min: -1, max: 1);
        public FloatParameter _SoftKnee = new FloatParameter(0.5f);
        public ClampedFloatParameter _Radius = new ClampedFloatParameter(value: 7, min: 1, max: 7);
        public ClampedFloatParameter _BlurWeight = new ClampedFloatParameter(value: 1, min: 0.1f, max: 100);
        public ClampedFloatParameter _Intensity = new ClampedFloatParameter(value: 1, min: 0, max: 1);
        public BoolParameter _HighQuality = new BoolParameter(false);
        public BoolParameter _AntiFlicker = new BoolParameter(false);
        public TextureParameter _FadeRamp = new TextureParameter(null);
        #endregion

        public bool IsActive() => _FogIntensity.value > 0;

        //public bool IsTileCompatible() => true;
    }
}