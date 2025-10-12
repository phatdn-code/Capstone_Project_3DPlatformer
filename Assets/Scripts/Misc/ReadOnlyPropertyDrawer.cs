using UnityEngine;
using UnityEditor;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// PropertyDrawer cho ReadOnly attribute
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Disable GUI để không cho phép edit
            GUI.enabled = false;
            
            // Vẽ property như bình thường nhưng không thể edit
            EditorGUI.PropertyField(position, property, label, true);
            
            // Re-enable GUI
            GUI.enabled = true;
        }
    }
}
