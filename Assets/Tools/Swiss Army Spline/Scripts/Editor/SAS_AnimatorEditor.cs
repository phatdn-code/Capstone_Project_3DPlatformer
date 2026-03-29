using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SwissArmySpline
{
    [CustomEditor(typeof(SAS_Animator))]
    public class SAS_AnimatorEditor : Editor
    {

        private SAS_Animator c;
        Rect r;

        private void OnEnable()
        {
            c = target as SAS_Animator;
        }
        private void OnDisable()
        {

        }



        [MenuItem("GameObject/Swiss Army Spline/New Animator", false, 9)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            // Create a custom game object
            GameObject go = new GameObject("SAS Animator", typeof(SAS_Animator));
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }








        public override void OnInspectorGUI()
        {
            c = target as SAS_Animator;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 16;



            GUILayout.Space(10);


            // Animation ==================================================================================================
            GUI.backgroundColor = Color.blue;
            EditorGUILayout.BeginVertical("HelpBox");
            GUI.backgroundColor = Color.white;
            GUILayout.Label("Path", style);

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("path"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);



            // Animation ==================================================================================================
            GUI.backgroundColor = Color.magenta;
            EditorGUILayout.BeginVertical("HelpBox");
            GUI.backgroundColor = Color.white;
            GUILayout.Label("Animation", style);


            EditorGUI.indentLevel++;

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoUpdate"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("HelpBox");
            if (GUILayout.Button("Move wagons to start position")) c.MoveObjectsToStartPosition();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wagons"), true);
            EditorGUILayout.EndVertical();


            // Trains
            EditorGUILayout.BeginVertical("HelpBox");



            

            EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelSpacing"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("offset"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("distance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useGravity"));
            if (c.useGravity) EditorGUILayout.PropertyField(serializedObject.FindProperty("drag"));
            else EditorGUILayout.PropertyField(serializedObject.FindProperty("velocityCurve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("flipForward"));



            EditorGUI.indentLevel--;

           
            EditorGUILayout.EndVertical();











            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();
        }
    }
}