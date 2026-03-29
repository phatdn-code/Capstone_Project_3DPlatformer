using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SwissArmySpline
{
    [CustomEditor(typeof(SAS_SetMeshPivot))]
    public class SAS_SetMeshPivotEditor : Editor
    {
        private SAS_SetMeshPivot c;

        private void OnEnable()
        {
            c = target as SAS_SetMeshPivot;
        }

        public override void OnInspectorGUI()
        {
            SerializedObject so = serializedObject;

            GUI.backgroundColor = Color.green;
            EditorGUILayout.BeginVertical("HelpBox");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = c.editMode ? new Color(0.9f, 0.3f, 0.25f, 0.8f) : new Color(0.3f, 0.8f, 0.25f, 0.8f);
            GUIStyle style = new GUIStyle("Button");
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.focused.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            if (GUILayout.Button(c.editMode ? "Stop Editing" : "Edit Pivot", style, GUILayout.Width(90)))
            {
                Undo.RecordObject(c, "Set Editmode");
                c.editMode = !c.editMode;
                Tools.hidden = c.editMode;
            }
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Set Mesh Pivot"))
            {
                Undo.RegisterCompleteObjectUndo(c, "Set Mesh Pivot");
                Tools.hidden = false;
                c.editMode = false;
                c.Execute();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.PropertyField(so.FindProperty("position"));
            EditorGUILayout.PropertyField(so.FindProperty("rotation"));
            EditorGUILayout.PropertyField(so.FindProperty("scale"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.PropertyField(so.FindProperty("updateTransform"));

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }


        private void OnSceneGUI()
        {
            if (!c.editMode) return;

            if (Tools.current == Tool.Move  ) Move();
            if (Tools.current == Tool.Rotate) Rotate();
            if (Tools.current == Tool.Scale ) Scale();
        }

        private void Move()
        {
            EditorGUI.BeginChangeCheck();
            Vector3 pos = c.position;
            if (Tools.pivotRotation == PivotRotation.Local ) pos = Handles.PositionHandle(c.position, Quaternion.Euler(c.rotation));
            if (Tools.pivotRotation == PivotRotation.Global) pos = Handles.PositionHandle(c.position, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Move Pivot");
                c.position = pos;
            }
        } //================================================================================================================================================


        Quaternion startRot;
        private void Rotate()
        {
            if (Tools.pivotRotation == PivotRotation.Global && Event.current.type == EventType.MouseDown) startRot = Quaternion.Euler(c.rotation);

            EditorGUI.BeginChangeCheck();
            Quaternion rot = Quaternion.Euler(c.rotation);
            if (Tools.pivotRotation == PivotRotation.Local ) rot = Handles.RotationHandle(rot, c.position);
            if (Tools.pivotRotation == PivotRotation.Global) rot = Handles.RotationHandle(Quaternion.identity, c.position);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Rotate Pivot");
                c.rotation = rot.eulerAngles;
                if (Tools.pivotRotation == PivotRotation.Global) c.rotation = (Quaternion.Euler(c.rotation) * startRot).eulerAngles;
            }
        } //================================================================================================================================================


        private void Scale()
        {
            EditorGUI.BeginChangeCheck();
            Vector3 scale = c.scale;
            float size = HandleUtility.GetHandleSize(c.position);
            if (Tools.pivotRotation == PivotRotation.Local ) scale = Handles.DoScaleHandle(c.scale, c.position, Quaternion.Euler(c.rotation), size);
            if (Tools.pivotRotation == PivotRotation.Global) scale = Handles.DoScaleHandle(c.scale, c.position, Quaternion.identity, size);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Scale Pivot");
                c.scale = scale;
            }
        } //================================================================================================================================================
    }
}
