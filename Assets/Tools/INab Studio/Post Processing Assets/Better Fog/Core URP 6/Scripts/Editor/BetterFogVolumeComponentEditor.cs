using INab.BetterFog.Core;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Rendering;

namespace INab.BetterFog.URP
{
    [CustomEditor(typeof(INab.BetterFog.URP.BetterFogVolumeComponent))]
    public class BetterFogVolumeComponentEditor : VolumeComponentEditor
    {
        // FogParameters
        SerializedDataParameter _FogIntensity;
        SerializedDataParameter _EnergyLoss;
        SerializedDataParameter _FogColor;
        SerializedDataParameter _UseGradient;
        SerializedDataParameter _GradientStart;
        SerializedDataParameter _GradientEnd;
        SerializedDataParameter _GradientTexture;
        SerializedDataParameter _UseSunLight;
        SerializedDataParameter _SunColor;
        SerializedDataParameter _SunIntensity;
        SerializedDataParameter _SunPower;
        SerializedDataParameter _UseDistanceFog;
        SerializedDataParameter _UseRadialDistance;
        SerializedDataParameter _FogType;
        SerializedDataParameter _DistanceFogOffset;
        SerializedDataParameter _SceneStart;
        SerializedDataParameter _SceneEnd;
        SerializedDataParameter _FogDensity;
        SerializedDataParameter _UseSkyboxHeightFog;
        SerializedDataParameter _SkyboxFogOffset;
        SerializedDataParameter _SkyboxFogHardness;
        SerializedDataParameter _SkyboxFogIntensity;
        SerializedDataParameter _SkyboxFill;
        SerializedDataParameter _UseHeightFog;
        SerializedDataParameter _Height;
        SerializedDataParameter _HeightDensity;
        SerializedDataParameter _HeightFogType;
        SerializedDataParameter _UseNoise;
        SerializedDataParameter _NoiseAffect;
        SerializedDataParameter _NoiseIntensity;
        SerializedDataParameter _NoiseDistanceEnd;
        SerializedDataParameter _NoiseEndHardness;
        SerializedDataParameter _Scale1;
        SerializedDataParameter _Lerp1;
        SerializedDataParameter _NoiseSpeed1;
        SerializedDataParameter _NoiseTimeScale1;

        // SSMSParameters
        SerializedDataParameter _UseSSMS;
        SerializedDataParameter _Threshold;
        SerializedDataParameter _SoftKnee;
        SerializedDataParameter _Radius;
        SerializedDataParameter _BlurWeight;
        SerializedDataParameter _Intensity;
        SerializedDataParameter _HighQuality;
        SerializedDataParameter _AntiFlicker;
        SerializedDataParameter _FadeRamp;


        public override void OnEnable()
        {
            var o = new PropertyFetcher<BetterFogVolumeComponent>(serializedObject);

            _FogIntensity = Unpack(o.Find(x => x._FogIntensity));
            _EnergyLoss = Unpack(o.Find(x => x._EnergyLoss));
            _FogColor = Unpack(o.Find(x => x._FogColor));
            _UseGradient = Unpack(o.Find(x => x._UseGradient));
            _GradientStart = Unpack(o.Find(x => x._GradientStart));
            _GradientEnd = Unpack(o.Find(x => x._GradientEnd));
            _GradientTexture = Unpack(o.Find(x => x._GradientTexture));
            _UseSunLight = Unpack(o.Find(x => x._UseSunLight));
            _SunColor = Unpack(o.Find(x => x._SunColor));
            _SunIntensity = Unpack(o.Find(x => x._SunIntensity));
            _SunPower = Unpack(o.Find(x => x._SunPower));
            _UseDistanceFog = Unpack(o.Find(x => x._UseDistanceFog));
            _UseRadialDistance = Unpack(o.Find(x => x._UseRadialDistance));
            _FogType = Unpack(o.Find(x => x._FogType));
            _DistanceFogOffset = Unpack(o.Find(x => x._DistanceFogOffset));
            _SceneStart = Unpack(o.Find(x => x._SceneStart));
            _SceneEnd = Unpack(o.Find(x => x._SceneEnd));
            _FogDensity = Unpack(o.Find(x => x._FogDensity));
            _UseSkyboxHeightFog = Unpack(o.Find(x => x._UseSkyboxHeightFog));
            _SkyboxFogOffset = Unpack(o.Find(x => x._SkyboxFogOffset));
            _SkyboxFogHardness = Unpack(o.Find(x => x._SkyboxFogHardness));
            _SkyboxFogIntensity = Unpack(o.Find(x => x._SkyboxFogIntensity)); 
             _SkyboxFill = Unpack(o.Find(x => x._SkyboxFill));
            _UseHeightFog = Unpack(o.Find(x => x._UseHeightFog));
            _Height = Unpack(o.Find(x => x._Height));
            _HeightDensity = Unpack(o.Find(x => x._HeightDensity));
            _HeightFogType = Unpack(o.Find(x => x._HeightFogType));
            _UseNoise = Unpack(o.Find(x => x._UseNoise));
            _NoiseAffect = Unpack(o.Find(x => x._NoiseAffect));
            _NoiseIntensity = Unpack(o.Find(x => x._NoiseIntensity));
            _NoiseDistanceEnd = Unpack(o.Find(x => x._NoiseDistanceEnd));
            _NoiseEndHardness = Unpack(o.Find(x => x._NoiseEndHardness));
            _Scale1 = Unpack(o.Find(x => x._Scale1));
            _Lerp1 = Unpack(o.Find(x => x._Lerp1));
            _NoiseSpeed1 = Unpack(o.Find(x => x._NoiseSpeed1));
            _NoiseTimeScale1 = Unpack(o.Find(x => x._NoiseTimeScale1));

            // SSMSParameters
            _UseSSMS = Unpack(o.Find(x => x._UseSSMS));
            _Threshold = Unpack(o.Find(x => x._Threshold));
            _SoftKnee = Unpack(o.Find(x => x._SoftKnee));
            _Radius = Unpack(o.Find(x => x._Radius));
            _BlurWeight = Unpack(o.Find(x => x._BlurWeight));
            _Intensity = Unpack(o.Find(x => x._Intensity));
            _HighQuality = Unpack(o.Find(x => x._HighQuality));
            _AntiFlicker = Unpack(o.Find(x => x._AntiFlicker));
            _FadeRamp = Unpack(o.Find(x => x._FadeRamp));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Main", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyField(_FogIntensity);
                PropertyField(_EnergyLoss);
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Colors", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyField(_UseGradient);

                if (_UseGradient.value.boolValue)
                {
                    PropertyField(_GradientStart);
                    PropertyField(_GradientEnd);
                    PropertyField(_GradientTexture);
                }
                else
                {
                    PropertyField(_FogColor);
                }
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Sun Light", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyField(_UseSunLight);

                if (_UseSunLight.value.boolValue)
                {
                    PropertyField(_SunColor);
                    PropertyField(_SunIntensity);
                    PropertyField(_SunPower);
                }
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Distance Fog", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyField(_UseDistanceFog);

                if (_UseDistanceFog.value.boolValue)
                {
                    PropertyField(_UseRadialDistance);
                    PropertyField(_DistanceFogOffset);
                    PropertyField(_FogType);

                    if (_FogType.value.enumValueIndex == 0)
                    {
                        PropertyField(_SceneStart);
                        PropertyField(_SceneEnd);
                    }
                    else
                    {
                        PropertyField(_FogDensity);
                    }

                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Skybox Height Fog", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyField(_UseSkyboxHeightFog);

                if (_UseSkyboxHeightFog.value.boolValue)
                {
                    PropertyField(_SkyboxFogOffset);
                    PropertyField(_SkyboxFogHardness);
                    PropertyField(_SkyboxFogIntensity);
                    PropertyField(_SkyboxFill);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Height Fog", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyField(_UseHeightFog);

                if (_UseHeightFog.value.boolValue)
                {
                    PropertyField(_Height);
                    PropertyField(_HeightDensity);
                    PropertyField(_HeightFogType);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("3D Noise", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyField(_UseNoise);


                if (_UseNoise.value.boolValue)
                {
                    PropertyField(_NoiseAffect);
                    PropertyField(_NoiseIntensity);
                    PropertyField(_NoiseDistanceEnd);
                    PropertyField(_NoiseEndHardness);
                    PropertyField(_Scale1);
                    PropertyField(_Lerp1);
                    PropertyField(_NoiseSpeed1);
                    PropertyField(_NoiseTimeScale1);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Screen Space Multiple Scattering", BetterFogUtility.centeredBoldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // SSMSParameters
                PropertyField(_UseSSMS);

                if (!_UseSSMS.value.boolValue)
                {
                    //EditorGUILayout.Space();
                    //EditorGUILayout.HelpBox("SSMS is ", MessageType.Info);
                    //EditorGUILayout.Space();
                }
                else
                {
                    PropertyField(_Threshold);
                    PropertyField(_SoftKnee);
                    PropertyField(_Radius);
                    PropertyField(_BlurWeight);
                    PropertyField(_Intensity);
                    PropertyField(_HighQuality);
                    PropertyField(_AntiFlicker);
                    PropertyField(_FadeRamp);
                }
            }
        }
    }
}
