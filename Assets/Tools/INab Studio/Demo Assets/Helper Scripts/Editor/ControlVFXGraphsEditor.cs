using UnityEditor;
using UnityEngine;

namespace INab.Demo
{
    [CustomEditor(typeof(ControlVFXGraphs))]
    [CanEditMultipleObjects]
    public class ControlVFXGraphsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var ourTarget = target as ControlVFXGraphs;
            serializedObject.Update();

            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();

            if (GUILayout.Button("Play Events"))
            {
                ourTarget.SendPlayEvents();
            }

            if (GUILayout.Button("Stop Events"))
            {
                ourTarget.SendStopEvents();
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Start Loops"))
            {
                ourTarget.StartCoroutines();
            }

            if (GUILayout.Button("Stop Coroutines"))
            {
                ourTarget.StopCoroutines();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Play Effects"))
            {
                ourTarget.PlayEffects();
            }

            if (GUILayout.Button("Reverse Effects"))
            {
                ourTarget.ReverseEffects();
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Get Renderer Materials"))
            {
                ourTarget.GetRendererMaterials();
            }

            if (GUILayout.Button("Refresh Effects"))
            {
                ourTarget.RefreshEffects();
            }
        }
    }
}