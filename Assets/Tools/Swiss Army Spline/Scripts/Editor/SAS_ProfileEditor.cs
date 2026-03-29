using UnityEngine;
using UnityEditor;

namespace SwissArmySpline
{
    [CustomEditor(typeof(SAS_Profile))]
    public class SAS_ProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(new GUIContent("Edit", "Edit this profile.")))
            {
                EditorWindow.GetWindow<SAS_ProfileCreator>();
            }
        }
    }
}
