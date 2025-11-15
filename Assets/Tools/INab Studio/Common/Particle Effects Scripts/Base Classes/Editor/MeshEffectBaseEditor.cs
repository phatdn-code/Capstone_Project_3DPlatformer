using UnityEditor;
using UnityEngine;
using System;

namespace INab.Common
{
    [CustomEditor(typeof(MeshEffectBase))]
    [CanEditMultipleObjects]
    public abstract class MeshEffectBaseEditor : MeshParticlesBaseEditor
    {
        #region Properties

        private SerializedProperty materials;
        private SerializedProperty useInstancedMaterials;
        protected SerializedProperty useMaterials;
        protected SerializedProperty propertiesScaleRate;

        [NonSerialized]
        private MeshEffectBase ourTarget;
        private bool drawMaterialSettings;

        #endregion

        #region Methods

        public override void OnEnable()
        {
            base.OnEnable();
            materials = serializedObject.FindProperty("materials");
            useInstancedMaterials = serializedObject.FindProperty("useInstancedMaterials");
            useMaterials = serializedObject.FindProperty("useMaterials");
            propertiesScaleRate = serializedObject.FindProperty("propertiesScaleRate");
        }


        public override void OnInspectorGUI()
        {
            ourTarget = target as MeshEffectBase;
            base.OnInspectorGUI();

            if(ourTarget.MaterialsRequired == false && useMaterials.boolValue)
            {
                drawMaterialSettings = true;
            }
            else if (ourTarget.MaterialsRequired)
            {
                drawMaterialSettings = true;
            }
            else
            {
                drawMaterialSettings = false;
            }

            UpdateKeywords();
        }

        public virtual void UpdateKeywords()
        {

        }

        #endregion

        #region InspectorMethods

        protected void DrawMaterialsInspector()
        {
            if (EditorUtilties.FoldoutGeneral(ourTarget))
            {
                EditorGUILayout.LabelField("Materials Settings", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                if (!ourTarget.MaterialsRequired)
                {
                    EditorGUILayout.PropertyField(useMaterials);
                }

                if(drawMaterialSettings)
                {
                    EditorGUILayout.PropertyField(useInstancedMaterials);
                    if (useInstancedMaterials.boolValue)
                    {
                        EditorGUILayout.HelpBox("The materials listed below are for editor testing only. Instanced materials will be automatically found at runtime. ", MessageType.Info);
                    }

                    if (ourTarget.usePerSubmeshBaking)
                    {
                        EditorGUILayout.HelpBox("If using 'Per Sub Mesh Baking' you need to add either the material of the submesh, or keep all the materials for each submesh. ", MessageType.Info);
                    }


                    EditorGUILayout.PropertyField(materials, true);

                    EditorGUI.indentLevel++;

                    bool hasMaterials = ourTarget.materials.Count > 0 && ourTarget.materials[0] != null;

                    if (!hasMaterials)
                    {
                        EditorGUILayout.HelpBox("No materials found in the materials list. Please add materials.", MessageType.Error);
                    }

                    if (!ourTarget._DoMaterialsUseProperShaders())
                    {
                        EditorGUILayout.HelpBox("Not all materials from the list use the " + ourTarget.ShaderName + " Effect shader. Change the shaders used by the materials to 'INab Studio/" + ourTarget.ShaderName + " Effect'.", MessageType.Error);
                    }

                    using (var horizontalScope = new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Find In Renderer", EditorUtilties.IndentedButtonStyleDouble))
                        {
                            Undo.RecordObject(ourTarget, "Find Materials In Renderer");
                            ourTarget.GetRendererMaterials();
                            ourTarget.SetupVfxGraph();

                        }

                        if (GUILayout.Button("Clear", EditorUtilties.DefaultButtonStyle))
                        {
                            Undo.RecordObject(ourTarget, "Clear Materials");
                            ourTarget._ClearMaterialsList();
                        }
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }
            // todo required?
            //EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected void DrawMaterialsProperties()
        {
            if (!drawMaterialSettings) return;

            EditorUtilties.SetFoldoutMaterialsProperties(ourTarget, EditorGUILayout.BeginFoldoutHeaderGroup(EditorUtilties.FoldoutMaterialsProperties(ourTarget), "Material Properties", EditorStyles.foldoutHeader));
            if (EditorUtilties.FoldoutMaterialsProperties(ourTarget))
            {
                //EditorGUI.indentLevel++;

                DrawMaterialShaderHelpers();
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(propertiesScaleRate);

                DrawMaterialProperties();
                EditorGUILayout.Space();
                //EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

        }

        protected virtual void DrawMaterialShaderHelpers()
        {
            EditorGUILayout.HelpBox("All materials need to use INab Studio/" + ourTarget.ShaderName + " Effect shader.", MessageType.Info);
        }

        private void DrawMaterialProperties()
        {
            EditorGUILayout.LabelField("Shader Properties", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            MaterialProperties();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        protected virtual void MaterialProperties()
        {

        }


        #endregion

    }
}