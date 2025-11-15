using UnityEditor;
using UnityEngine;

namespace INab.Common
{

    public static class EditorUtilties
    {
        private static GUIStyle _indentedFoldoutHeader;
        public static GUIStyle IndentedFoldoutHeader
        {
            get
            {
                if (_indentedFoldoutHeader == null)
                {
                    _indentedFoldoutHeader = new GUIStyle(EditorStyles.foldoutHeader)
                    {
                        margin = new RectOffset(35, 0, 0, 0),
                    };
                }

                return _indentedFoldoutHeader;
            }
        }

        private static GUIStyle _indentedButtonStyle;
        public static GUIStyle IndentedButtonStyle
        {
            get
            {
                if (_indentedButtonStyle == null)
                {
                    _indentedButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(35, 0, 0, 0),
                        //fontStyle = FontStyle.Bold
                    };
                }

                return _indentedButtonStyle;
            }
        }

        private static GUIStyle _indentedButtonStyleDouble;
        public static GUIStyle IndentedButtonStyleDouble
        {
            get
            {
                if (_indentedButtonStyleDouble == null)
                {
                    _indentedButtonStyleDouble = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(48, 0, 0, 0)
                    };
                }

                return _indentedButtonStyleDouble;
            }
        }

        private static GUIStyle _defaultButtonStyle;
        public static GUIStyle DefaultButtonStyle
        {
            get
            {
                if (_defaultButtonStyle == null)
                {
                    _defaultButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        margin = new RectOffset(0, 0, 0, 0)
                    };
                }

                return _defaultButtonStyle;
            }
        }

        private static GUIStyle _centeredBoldLabel;
        public static GUIStyle CenteredBoldLabel
        {
            get
            {
                if (_centeredBoldLabel == null)
                {
                    _centeredBoldLabel = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter
                    };
                }

                return _centeredBoldLabel;
            }
        }


        public static bool GetFoldoutState(string key, Object gameObject)
        {
            string fullKey = key + "_" + gameObject.GetInstanceID();
            return SessionState.GetBool(fullKey, false);
        }

        public static void SetFoldoutState(string key, Object gameObject, bool value)
        {
            string fullKey = key + "_" + gameObject.GetInstanceID();
            SessionState.SetBool(fullKey, value);
        }

        //==============================================================================
        // Particle Effects
        //==============================================================================

        public static bool FoldoutGeneral(Object gameObject)
        {
            return GetFoldoutState("FoldoutGeneral", gameObject);
        }

        public static void SetFoldoutGeneral(Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutGeneral", gameObject, value);
        }

        public static bool FoldoutEditorTesting(Object gameObject)
        {
            return GetFoldoutState("FoldoutEditorTesting", gameObject);
        }

        public static void SetFoldoutEditorTesting(Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutEditorTesting", gameObject, value);
        }

        public static bool FoldoutEffectSettings(Object gameObject)
        {
            return GetFoldoutState("FoldoutEffectSettings", gameObject);
        }

        public static void SetFoldoutEffectSettings(Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutEffectSettings", gameObject, value);
        }

        public static bool FoldoutMaterialsProperties(Object gameObject)
        {
            return GetFoldoutState("FoldoutMaterialsProperties", gameObject);
        }

        public static void SetFoldoutMaterialsProperties(Object gameObject, bool value)
        {
            SetFoldoutState("FoldoutMaterialsProperties", gameObject, value);
        }

        //==============================================================================

    }
}