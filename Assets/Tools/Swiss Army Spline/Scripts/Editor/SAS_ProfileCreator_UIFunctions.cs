using UnityEngine;
using UnityEditor;


namespace SwissArmySpline
{
    public partial class SAS_ProfileCreator : EditorWindow
    {
        bool GUITabs()
        {
            // TABS _________________________________________________________________________
            GUIStyle tab = new GUIStyle("Label");
            grey = 0.9f;
            tab.normal.textColor = new Color(grey, grey, grey, 1);
            grey = 0.5f;
            tab.hover.textColor = new Color(grey, grey, grey, 1);
            tab.fontStyle = FontStyle.Bold;
            tab.fontSize = 14;
            tab.contentOffset = new Vector2(8, 0);
            GUI.color = Color.white;


            Rect tabRect = new Rect(0, 0, 180 / 2, 30);


            grey = (mode == Mode.Shape) ? 0.9f : 0.4f;
            if (tabRect.Contains(e.mousePosition) && mode != Mode.Shape)
            {
                GUI.color = new Color(grey, grey, grey, 0.2f);
                GUI.DrawTexture(tabRect, Texture2D.whiteTexture);

                if (e.button == 0 && e.type == EventType.MouseUp)
                {
                    mode = Mode.Shape;
                    if (selection.Contains(vertices.Count)) selection.Remove(vertices.Count);
                    Frame();
                    return true;
                }
            }
            GUI.color = new Color(grey, grey, grey, 1);
            GUI.Label(tabRect, new GUIContent("SHAPES", "Edit the profile shapes."), tab);
            tabRect.height = 3;
            tabRect.y = 30 - 3;
            GUI.DrawTexture(tabRect, Texture2D.whiteTexture);


            tabRect = new Rect(180 / 2, 0, 180 / 2, 30);


            grey = (mode == Mode.UV) ? 0.9f : 0.4f;
            if (tabRect.Contains(e.mousePosition) && mode != Mode.UV)
            {
                GUI.color = new Color(grey, grey, grey, 0.2f);
                GUI.DrawTexture(tabRect, Texture2D.whiteTexture);

                if (e.button == 0 && e.type == EventType.MouseUp)
                {
                    mode = Mode.UV;
                    Frame();
                    return true;
                }
            }
            GUI.color = new Color(grey, grey, grey, 1);
            GUI.Label(tabRect, new GUIContent("UVs", "Edit the profile uvs."), tab);
            tabRect.height = 3;
            tabRect.y = 30 - 3;
            GUI.DrawTexture(tabRect, Texture2D.whiteTexture);

            GUILayout.Space(30);
            return false;
        } //===========================================================================================================================================================



        void GUIMove()
        {
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Move", "Offsets the selected vertices by the specified amount."), menu, GUILayout.Width(labelWidthFloatField));

            if (mode == Mode.Shape)
            {
                EditorGUI.BeginChangeCheck();
                float x = EditorGUILayout.DelayedFloatField(0, GUILayout.Width(fieldWidthFloatField));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(p, "Move Selection");
                    foreach (int i in selection)
                    {
                        Vector4 v = vertices[i];
                        v.x += x;
                        vertices[i] = v;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            float y = (mode == Mode.Shape) ? EditorGUILayout.DelayedFloatField(0, GUILayout.Width(fieldWidthFloatField)) : EditorGUILayout.DelayedFloatField(0);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, "Move Selection");
                foreach (int i in selection)
                {
                    if (i < vertices.Count)
                    {
                        Vector4 v = vertices[i];
                        if (mode == Mode.Shape) v.y += y;
                        else v.w += y;
                        vertices[i] = v;
                    }
                    else selectedProfile.loopUV += y;
                }
            }
            GUILayout.EndHorizontal();
        } //===========================================================================================================================================================



        void GUIRotate()
        {
            // Rotate Selection
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Rotate", "Rotates the selected vertices by the specified amount(in degrees) around the pivot position. Make sure to have the pivot visible."), menu, GUILayout.Width(labelWidthFloatField));
            EditorGUI.BeginChangeCheck();
            float newAngle = EditorGUILayout.DelayedFloatField(0) * -Mathf.Deg2Rad;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, "Rotate Selection");
                foreach (int i in selection)
                {
                    Vector2 pos = RotateAround(vertices[i], p.profiles[p.selectedProfile].pivot, newAngle);
                    if (vertices[i].z != 999) vertices[i] = new Vector4(pos.x, pos.y, vertices[i].z - newAngle, vertices[i].w);
                    else vertices[i] = new Vector4(pos.x, pos.y, 999, vertices[i].w);
                }
            }
            GUILayout.EndHorizontal();
        } //===========================================================================================================================================================



        void GUIScale()
        {
            // Scale Selection
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Scale", "Scales the selected vertices from the pivot position. Make sure to have the pivot visible."), menu, GUILayout.Width(labelWidthFloatField));

            if (mode == Mode.Shape)
            {
                EditorGUI.BeginChangeCheck();
                float scaleX = EditorGUILayout.DelayedFloatField(1, GUILayout.Width(fieldWidthFloatField));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(p, "Scale Selection");
                    foreach (int i in selection)
                    {
                        Vector4 v = vertices[i];
                        v.x -= p.profiles[p.selectedProfile].pivot.x;
                        v.x *= scaleX;
                        v.x += p.profiles[p.selectedProfile].pivot.x;
                        vertices[i] = v;
                    }
                }
            }



            EditorGUI.BeginChangeCheck();
            float scaleY = (mode == Mode.Shape) ? EditorGUILayout.DelayedFloatField(1, GUILayout.Width(fieldWidthFloatField)) : EditorGUILayout.DelayedFloatField(1);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, "Scale Selection");

                Vector2 pivot = p.profiles[p.selectedProfile].pivot;
                if (!p.profiles[p.selectedProfile].uvIsU)
                {
                    pivot = new Vector2(pivot.y, pivot.x);

                }

                foreach (int i in selection)
                {
                    if (i < vertices.Count)
                    {
                        Vector4 v = vertices[i];
                        if (mode == Mode.Shape)
                        {
                            v.y -= pivot.y;
                            v.y *= scaleY;
                            v.y += pivot.y;
                        }
                        else
                        {
                            v.w -= pivot.x;
                            v.w *= scaleY;
                            v.w += pivot.x;
                        }
                        vertices[i] = v;
                    }
                    else
                    {
                        selectedProfile.loopUV -= pivot.x;
                        selectedProfile.loopUV *= scaleY;
                        selectedProfile.loopUV += pivot.x;
                    }
                }
            }

            GUILayout.EndHorizontal();
        } //===========================================================================================================================================================



        void GUINormal()
        {
            GUI.color = Color.white;

            if (selection.Contains(vertices.Count)) selection.Remove(vertices.Count);

            EditorGUI.BeginChangeCheck();

            float inAngle = (selection.Count == 1) ? Mathf.Round(((vertices[selection[0]].z * Mathf.Rad2Deg + 180) % 360) * 100) / 100 : -1;
            if (selection.Count == 1)
            {
                if (vertices[selection[0]].z == 999) inAngle = -1;
                else inAngle = Mathf.Round(((vertices[selection[0]].z * Mathf.Rad2Deg + 180) % 360) * 100) / 100;
            }
            else
            {
                inAngle = -1;
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Set Normal", "Sets the normals of the selected vertices to the specified angle. A value of 0 points the normal up, a value of 90 points the normal to the right, and so on..."), menu, GUILayout.Width(labelWidthFloatField));
            float outAngle = EditorGUILayout.DelayedFloatField(inAngle);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, "Set Normal Angle");
                foreach (int i in selection)
                {
                    Vector4 v = vertices[i];
                    v.z = ((outAngle + 180) % 360) * Mathf.Deg2Rad;
                    vertices[i] = v;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(2);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Avg.", "Sets the normals of the selected vertices to average. That means an angle orthogonal to the average directions to the neighbouring vertices."), button, GUILayout.Width(38)))
            {
                Undo.RegisterCompleteObjectUndo(p, "Set Normal Angle");
                for (int i = 0; i < selection.Count; i++) AutoNormal(selection[i], 0, true);
            }
            if (GUILayout.Button(new GUIContent("Hard", "Sets the normals of the selected vertices to hard edge mode. Basically this splits the vertices in 2 colocated vertices with different normals."), button, GUILayout.Width(38)))
            {
                Undo.RegisterCompleteObjectUndo(p, "Set Normal Angle");
                for (int i = 0; i < selection.Count; i++) AutoNormal(selection[i], 2, true);
            }
            if (GUILayout.Button(new GUIContent("Prev", "Sets the normals of the selected vertices orthogonal to the direction to the previous vertices. Useful to create proper bevels."), button, GUILayout.Width(38)))
            {
                Undo.RegisterCompleteObjectUndo(p, "Set Normal Angle");
                for (int i = 0; i < selection.Count; i++) AutoNormal(selection[i], -1, true);
            }
            if (GUILayout.Button(new GUIContent("Next", "Sets the normals of the selected vertices orthogonal to the direction to the next vertices. Useful to create proper bevels."), button, GUILayout.Width(38)))
            {
                Undo.RegisterCompleteObjectUndo(p, "Set Normal Angle");
                for (int i = 0; i < selection.Count; i++) AutoNormal(selection[i], 1, true);
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Auto Update Normals", "Tries to update the normals of the selected vertices to average when you move them."), menu, GUILayout.Width(labelWidthToggle));
            EditorGUI.BeginChangeCheck();
            bool autoUpdateNormals = EditorGUILayout.Toggle(p.autoUpdateNormals);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, autoUpdateNormals ? "Enable AutoUpdateNormals" : "Disable AutoUpdateNormals");
                p.autoUpdateNormals = autoUpdateNormals;
            }
            GUILayout.EndHorizontal();
        } //===========================================================================================================================================================


        void GUIKeyBindings()
        {
            GUILayout.Space(2);
            GUILayout.BeginVertical("HelpBox");

            GUIStyle style = new GUIStyle("Label");
            grey = 0.9f;
            style.normal.textColor = new Color(grey, grey, grey, 1);
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 14;
            GUI.color = Color.white;

            EditorGUILayout.LabelField("KEYS:", style, GUILayout.Width(160));

            KeyLabel("RMB Drag / Wheel", "Zoom View");
            KeyLabel("MMB Drag", "Pan View");

            KeyLabel("LMB Click / Drag", "Select/Move/Selection Rect");
            KeyLabel("Shift + LMB Click / Drag", "Add to Selection");
            KeyLabel("Control + LMB Click / Drag", "Remove from Selection");
            if (mode == Mode.Shape)
            {
                KeyLabel("Alt + LMB Click", "Add new Vertices");
                KeyLabel("Delete", "Deletes Selection");
            }
            GUILayout.EndVertical();
        } //===========================================================================================================================================================
        void KeyLabel(string key, string function)
        {
            EditorGUILayout.LabelField(key, menu, GUILayout.Width(160));
            GUILayout.Space(-7);
            EditorGUILayout.LabelField(function, GUILayout.Width(160));
        }
    }
}
