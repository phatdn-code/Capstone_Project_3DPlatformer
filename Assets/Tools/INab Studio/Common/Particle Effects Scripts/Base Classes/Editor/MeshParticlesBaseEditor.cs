using UnityEditor;
using UnityEngine;
using INab.CommonVFX;
using System;

namespace INab.Common
{
    [CustomEditor(typeof(MeshParticlesBase))]
    [CanEditMultipleObjects]
    public abstract class MeshParticlesBaseEditor : Editor
    {
        #region Properties

        private SerializedProperty useParticleEffects;
        private SerializedProperty visualEffect;
        private SerializedProperty visualEffectAsset;
        private SerializedProperty propertyBinder;
        private SerializedProperty meshRenderer;
        private SerializedProperty sampleCountMultiplier;

        private SerializedProperty usePerSubmeshBaking;
        private SerializedProperty submeshIndex;


        // Flags to control delayed updates
        public bool waitForUniformBufferSettings = false;

        [NonSerialized]
        private MeshParticlesBase ourTarget;

        #endregion

        #region Methods

        public virtual void OnEnable()
        {
            useParticleEffects = serializedObject.FindProperty("useParticleEffects");
            visualEffect = serializedObject.FindProperty("visualEffect");
            visualEffectAsset = serializedObject.FindProperty("visualEffectAsset");
            propertyBinder = serializedObject.FindProperty("propertyBinder");
            meshRenderer = serializedObject.FindProperty("meshRenderer");
            sampleCountMultiplier = serializedObject.FindProperty("sampleCountMultiplier");

            usePerSubmeshBaking = serializedObject.FindProperty("usePerSubmeshBaking");
            submeshIndex = serializedObject.FindProperty("submeshIndex");
        }

        public override void OnInspectorGUI()
        {
            ourTarget = target as MeshParticlesBase;
            serializedObject.Update();

            Inspector();

            serializedObject.ApplyModifiedProperties();
        }

        public abstract void Inspector();

        #endregion

        #region InspectorMethods

        protected virtual void DrawGeneralSettings(string initialEventName = "OnPlay")
        {
            EditorUtilties.SetFoldoutGeneral(ourTarget, EditorGUILayout.BeginFoldoutHeaderGroup(EditorUtilties.FoldoutGeneral(ourTarget), "Setup", EditorStyles.foldoutHeader));
            if (EditorUtilties.FoldoutGeneral(ourTarget))
            {
                EditorGUILayout.LabelField("Particle Settings", EditorStyles.boldLabel);

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(useParticleEffects);

                if (useParticleEffects.boolValue)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(meshRenderer);
                    if (!ourTarget.IsMeshReadable)
                    {
                        EditorGUILayout.HelpBox("The mesh in the Mesh Renderer is not readable. Enable 'Read/Write' in the Mesh Inspector to fix this.", MessageType.Error);

                    }
                    if (ourTarget.meshRenderer == null)
                    {
                        if (GUILayout.Button("Find Renderer", EditorUtilties.IndentedButtonStyleDouble))
                        {
                            ourTarget._FindRenderer();

                        }
                        EditorGUILayout.HelpBox("Please assign a Mesh Renderer.", MessageType.Error);
                        EditorGUILayout.Space();
                    }


                    EditorGUILayout.PropertyField(visualEffect);

                    if (ourTarget.visualEffect == null)
                    {
                        if (GUILayout.Button("Setup Vfx Graph Game Object", EditorUtilties.IndentedButtonStyle))
                        {
                            ourTarget._SetupVfxGraphGameObject(initialEventName);
                        }
                        EditorGUILayout.HelpBox("Please assign Visual Effect Graph component.", MessageType.Error);
                    }
                    else if (!ourTarget.visualEffect.HasGraphicsBuffer(VFXUniformMeshBaker.GraphicsBufferName))
                    {
                        EditorGUILayout.PropertyField(visualEffectAsset);
                        if(ourTarget.visualEffectAsset != null)
                        {
                            ourTarget.visualEffect.visualEffectAsset = ourTarget.visualEffectAsset;
                        }
                        EditorGUILayout.HelpBox("Change the VFX graph asset template to the one provided by INab Studio.", MessageType.Error);
                    }

                    EditorGUILayout.PropertyField(propertyBinder);
                    if (ourTarget.propertyBinder == null)
                    {
                        EditorGUILayout.HelpBox("Please assign a Property Binder.", MessageType.Error);
                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.PropertyField(sampleCountMultiplier);


                    Color oldGUIColor = GUI.backgroundColor;
                    int sampleCount = ourTarget.meshBaker.SampleCount;

                    float colorsIntensity = 2f;

                    Gradient gradient = new Gradient();
                    GradientColorKey[] colorKeys = new GradientColorKey[2];
                    colorKeys[0] = new GradientColorKey(Color.green * colorsIntensity, 0.35f); // Lower count color
                    colorKeys[1] = new GradientColorKey(Color.red * colorsIntensity, 1f);   // 20000 count color
                    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                    alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                    alphaKeys[1] = new GradientAlphaKey(1f, 1f);
                    gradient.SetKeys(colorKeys, alphaKeys);

                    float t = Mathf.Clamp01(sampleCount / 20000f);
                    Color sampleCountColor = gradient.Evaluate(t);
                    GUI.backgroundColor = sampleCountColor;

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntField("Sample Count", sampleCount);
                    EditorGUI.EndDisabledGroup();
                    GUI.backgroundColor = oldGUIColor;

                    if (sampleCount > 20000)
                    {
                        EditorGUILayout.HelpBox("Sample count exceeds 20,000. Reduce the Sample Count Multiplier to lower the sample count.", MessageType.Warning);
                    }

                    EditorGUILayout.PropertyField(usePerSubmeshBaking);

                    if(usePerSubmeshBaking.boolValue)
                    {
                        var submeshCount = VFXMeshSamplingHelper.RendererToMesh(ourTarget.meshRenderer).subMeshCount;
                        EditorGUILayout.IntSlider(submeshIndex, 0, submeshCount - 1);
                    }


                    if (ourTarget.sampleCountMultiplier != sampleCountMultiplier.floatValue)
                    {
                        waitForUniformBufferSettings = true;
                    }

                    if (ourTarget.usePerSubmeshBaking != usePerSubmeshBaking.boolValue)
                    {
                        waitForUniformBufferSettings = true;
                    }

                    if (ourTarget.submeshIndex != submeshIndex.intValue)
                    {
                        waitForUniformBufferSettings = true;
                    }

                    if (waitForUniformBufferSettings)
                    {
                        if (GUILayout.Button("Bake", EditorUtilties.IndentedButtonStyle))
                        {
                            ourTarget._BakeUniformMesh();
                            waitForUniformBufferSettings = false;
                        }
                    }


                    EditorGUILayout.Space();
                    EditorGUI.indentLevel--;


                    EditorGUILayout.LabelField("Important", EditorStyles.boldLabel);

                    EditorGUILayout.HelpBox("After configuring the settings, or if something isn't working, click the button.", MessageType.Info);
                    if (GUILayout.Button("Setup Vfx Graph", EditorUtilties.IndentedButtonStyle))
                    {
                        ourTarget.SetupVfxGraph();
                        waitForUniformBufferSettings = false;

                    }

                    EditorGUILayout.Space();
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected virtual void DrawEditorTestingInspector()
        {
            EditorUtilties.SetFoldoutEditorTesting(ourTarget,EditorGUILayout.BeginFoldoutHeaderGroup(EditorUtilties.FoldoutEditorTesting(ourTarget), "Testing", EditorStyles.foldoutHeader));
            if (EditorUtilties.FoldoutEditorTesting(ourTarget))
            {
                EditorTestingVfxGraph();
                EditorTesting();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected virtual void EditorTestingVfxGraph()
        {
            if (useParticleEffects.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Vfx Graph Events", EditorStyles.boldLabel);

                using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Play Event", EditorUtilties.IndentedButtonStyleDouble))
                    {
                        ourTarget.SendPlayEvent();
                    }

                    if (GUILayout.Button("Stop Event", EditorUtilties.DefaultButtonStyle))
                    {
                        ourTarget.SendStopEvent();
                    }
                }
                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }
        }

        protected virtual void EditorTesting()
        {
        }

        #endregion

    }
}