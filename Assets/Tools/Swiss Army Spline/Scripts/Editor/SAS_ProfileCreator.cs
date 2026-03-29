using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SwissArmySpline
{
    public partial class SAS_ProfileCreator : EditorWindow
    {
        SAS_ProfileCreator window;

        static Vector2 windowSize;
        static Vector2 offset = new Vector2(Screen.width / 2, Screen.height / 2);
        static float zoom = 25;
        static float increment = 1;

        static Event e;
        static int indexHover = -1;
        static int indexDrag = -1;
        static int indexNormal = -1;
        static bool isDragging = false;

        static bool showSelectionFrame = false;
        static Vector2 dragStartPos;
        static float rotateDelta;

        static Rect selectionFrameMinMax;


        static SAS_Profile p;
        static SAS_Profile.Profile selectedProfile;
        List<Vector4> vertices = new List<Vector4>();
        List<int> selection = new List<int>();

        enum Mode { Shape, UV };
        static Mode mode = Mode.Shape;

        static readonly Rect vertexSize0 = new Rect(3, 3, 5, 5);
        static readonly Rect vertexSize1 = new Rect(4, 4, 7, 7);
        static readonly Rect vertexSize2 = new Rect(5, 5, 9, 9);
        static readonly Rect vertexSize3 = new Rect(6, 6, 11, 11);

        static readonly Color colorVertex = new Color32(64, 64, 255, 255);
        static readonly Color colorVertexUnselected = new Color32(64, 128, 64, 255);
        static readonly Color colorVertexHover = new Color32(64, 200, 64, 255);
        static readonly Color colorVertexColocated = new Color32(255, 64, 64, 255);



        GUIStyle vertexLabels = new GUIStyle();
        GUIStyle menu = new GUIStyle();
        GUIStyle button = new GUIStyle();

        float labelWidthFloatField = 78;
        float labelWidthToggle = 144;
        float fieldWidthFloatField = 39;
        static Texture2D tex;

        float grey;


        [InitializeOnLoadMethod]
        static void SaveSettingsOnExit()
        {
            //Debug.Log("init");
            EditorApplication.quitting += () =>
            {
                if (p == null) return;
                EditorUtility.SetDirty(p);
                AssetDatabase.SaveAssets();
            };
        }


        private void OnEnable()
        {
            window = GetWindow<SAS_ProfileCreator>();
            windowSize = window.position.size;
            gridNumbers.normal.textColor = new Color32(255, 255, 255, 150);
            gridNumbers.fontSize = 10;
            gridNumbers.contentOffset = new Vector2(2, 0);

            vertexLabels.normal.textColor = new Color32(255, 255, 255, 255);
            vertexLabels.fontSize = 10;
            vertexLabels.alignment = TextAnchor.UpperLeft;
            vertexLabels.contentOffset = new Vector2(6, 4);

            SAS_Profile[] temp = Selection.GetFiltered<SAS_Profile>(SelectionMode.Assets);
            if (temp == null || temp.Length == 0) { }// p = null;
            else p = temp[0];

            if (p == null) return;
            if (p.profiles.Count == 0) p.profiles.Add(new SAS_Profile.Profile());
            selectedProfile = p.profiles[p.selectedProfile];
            vertices = selectedProfile.vertices;
            selection = selectedProfile.selection;
            if (selection.Contains(vertices.Count)) selection.Remove(vertices.Count);

            window.titleContent = new GUIContent(p.name);

            Frame();
        } //===========================================================================================================================================================


        private void OnDisable()
        {
            if (p == null) return;
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } //===========================================================================================================================================================

        private void OnDestroy()
        {
            if (p == null) return;
            EditorUtility.SetDirty(p);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } //===========================================================================================================================================================




        private void OnSelectionChange()
        {
            SAS_Profile[] temp = Selection.GetFiltered<SAS_Profile>(SelectionMode.Assets);
            if (temp == null || temp.Length == 0) { }// p = null;
            else p = temp[0];
            if (p == null) return;
            if (p.profiles.Count == 0) p.profiles.Add(new SAS_Profile.Profile());
            OnEnable();
            Repaint();
        } //===========================================================================================================================================================
        public void ChooseProfile(SAS_Profile profile)
        {
            p = profile;
            if (p.profiles.Count == 0) p.profiles.Add(new SAS_Profile.Profile());
            OnEnable();
            Repaint();
        } //===========================================================================================================================================================


        static bool mouseOnGUI = false;
        static bool mouseDownOnGui = false;

        void OnGUI()
        {
            if (p == null) return;

            windowSize = window.position.size;
            e = Event.current;



            DrawGrid();

            selectedProfile = p.profiles[p.selectedProfile];
            vertices = selectedProfile.vertices;
            selection = selectedProfile.selection;


            Rect box = new Rect(0, 0, 180, windowSize.y);

            mouseOnGUI = box.Contains(e.mousePosition);
            if (mouseOnGUI && e.button == 0 && e.type == EventType.MouseDown) mouseDownOnGui = true;


            if (!mouseOnGUI) GUI.FocusControl(null);


            if (mode == Mode.Shape)
            {
                if (indexNormal == -1 && !mouseDownOnGui) GetInputShape();
                DrawProfile();
            }
            else if (mode == Mode.UV)
            {
                if (!mouseDownOnGui) GetInputUVs();
                DrawUVs();
            }

            if (indexNormal == -1 && !mouseDownOnGui) DrawDragRectangle();
            if (p.showPivot) DrawPivot();


            if (e.button == 0 && e.type == EventType.MouseUp || e.type == EventType.Ignore) mouseDownOnGui = false;








            grey = 0.85f;
            menu.normal.textColor = new Color(grey, grey, grey, 1);
            grey = 0.35f;
            GUI.color = new Color(grey, grey, grey, 1);
            menu.contentOffset = new Vector2(0, 1);
            GUI.Box(box, "");

            button = new GUIStyle(GUI.skin.button);
            grey = 0.2f;
            button.normal.textColor = new Color(grey, grey, grey, 1);
            grey = 0.4f;
            button.hover.textColor = new Color(grey, grey, grey, 1);
            button.fontStyle = FontStyle.Bold;






            if (GUITabs()) return;


            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.BeginVertical(GUILayout.Width(170));





















            //__________________________________________________________________________________________________________
            GUI.color = new Color(1, 0.7f, 0, 1);
            GUILayout.BeginVertical("HelpBox");
            GUI.color = Color.white;
            // Snap On / Off
            GUILayout.BeginHorizontal(); // H2
            EditorGUILayout.LabelField(new GUIContent("Snap", "Enable/Disable grid snapping."), menu, GUILayout.Width(labelWidthToggle));
            EditorGUI.BeginChangeCheck();
            bool snap = EditorGUILayout.Toggle(p.snap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, snap ? "Enable Snapping" : "Disable Snapping");
                p.snap = snap;
            }
            GUILayout.EndHorizontal();



            // Show Pivot
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Show Pivot", "Show/Hide the pivot."), menu, GUILayout.Width(labelWidthToggle));
            EditorGUI.BeginChangeCheck();
            bool showPivot = EditorGUILayout.Toggle(p.showPivot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, showPivot ? "Show Pivot" : "Hide Pivot");
                p.showPivot = showPivot;
            }
            GUILayout.EndHorizontal();



            // Show / Hide Vertex IDs
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Show Vertex IDs", "Show/Hide the vertex IDs."), menu, GUILayout.Width(labelWidthToggle));
            EditorGUI.BeginChangeCheck();
            bool showLabels = EditorGUILayout.Toggle(p.showLabels);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RegisterCompleteObjectUndo(p, showLabels ? "Show Vertex IDs" : "Hide Vertex IDs");
                p.showLabels = showLabels;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            //__________________________________________________________________________________________________________

















            GUILayout.Space(2);


            // Transform Section ______________________________________
            GUILayout.BeginVertical("HelpBox", GUILayout.Width(170));

            GUIMove();
            GUILayout.Space(2);
            if (mode == Mode.Shape)
            {
                GUIRotate();
                GUILayout.Space(2);
            }
            GUIScale();
            GUILayout.Space(0);
            GUILayout.EndVertical();



            GUILayout.Space(2);
            GUI.color = new Color(0.5f, 0.5f, 1, 1);
            GUILayout.BeginVertical("HelpBox", GUILayout.Width(170));
            if (mode == Mode.Shape)
            {
                GUINormal();
            }
            GUILayout.Space(0);
            GUILayout.EndVertical();



            GUILayout.Space(2);


            GUI.color = new Color(0.2f, 0.7f, 0.2f, 1);
            GUILayout.BeginVertical("HelpBox");
            GUI.color = Color.white;



            // Cycle Profiles
            GUILayout.Space(1);
            GUILayout.BeginHorizontal("HelpBox");
            if (GUILayout.Button(new GUIContent("<<", "Select the previous sub profile")))
            {
                Undo.RegisterCompleteObjectUndo(p, "Cycle Profile");
                p.selectedProfile--;
                if (p.selectedProfile < 0) p.selectedProfile = p.profiles.Count - 1;
                p.profiles[p.selectedProfile].selection.Clear();
            }
            if (GUILayout.Button(new GUIContent(">>", "Select the next sub profile.")))
            {
                Undo.RegisterCompleteObjectUndo(p, "Cycle Profile");
                p.selectedProfile++;
                if (p.selectedProfile >= p.profiles.Count) p.selectedProfile = 0;
                p.profiles[p.selectedProfile].selection.Clear();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);



            if (mode == Mode.Shape)
            {
                // Open / Close
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Close Profile", "If enabled the profile will be closed."), menu, GUILayout.Width(labelWidthToggle));
                EditorGUI.BeginChangeCheck();
                bool close = EditorGUILayout.Toggle(selectedProfile.close);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(p, close ? "Close Profile" : "Open Profile");
                    selectedProfile.close = close;
                    AutoNormal(0);
                    AutoNormal(vertices.Count - 1);
                }
                GUILayout.EndHorizontal();


                GUILayout.Space(2);

                if (GUILayout.Button(new GUIContent("Invert Order","Reverses the vertex ID order. This is important as it allows you to flip the faces inside out."), button, GUILayout.Width(161)))
                {
                    Undo.RegisterCompleteObjectUndo(p, "Invert Vertex Order");
                    vertices.Reverse(1, vertices.Count - 1);
                    Vector4 v0 = vertices[0];
                    v0.w = selectedProfile.loopUV;
                    selectedProfile.loopUV = vertices[0].w;
                    vertices[0] = v0;

                    if (p.autoUpdateNormals) for (int i = 0; i < vertices.Count; i++) AutoNormal(i, vertices[i].z == 999 ? 2 : 0);
                }

                GUILayout.Space(2);


                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Submesh Index", "If you want to use multiple materials on the mesh you can specify an index(Be aware that you should maintain a sequential indexing - e.g. 0, 1, 2, ...not 0, 2, 5)."), menu, GUILayout.Width(labelWidthFloatField + fieldWidthFloatField + 2));

                EditorGUI.BeginChangeCheck();
                int submeshIndex = EditorGUILayout.DelayedIntField(selectedProfile.submeshIndex, GUILayout.Width(fieldWidthFloatField));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(p, "Set Submesh Index");
                    selectedProfile.submeshIndex = submeshIndex;

                }
                GUILayout.EndHorizontal();

                GUILayout.Space(2);

                if (GUILayout.Button(new GUIContent("Add new Profile", "Add a new sub profile."), button, GUILayout.Width(161)))
                {
                    Undo.RegisterCompleteObjectUndo(p, "Add Profile");
                    if (p.profiles[p.profiles.Count - 1].vertices.Count >= 2) p.profiles.Add(new SAS_Profile.Profile());
                    p.selectedProfile = p.profiles.Count - 1;
                }

                if (GUILayout.Button(new GUIContent("Duplicate Profile", "Duplicates the selected (sub)profile."), button, GUILayout.Width(161)))
                {
                    Undo.RegisterCompleteObjectUndo(p, "Duplicate Profile");

                    SAS_Profile.Profile clone = new SAS_Profile.Profile();
                    clone.Clone(selectedProfile);

                    p.profiles.Insert(p.selectedProfile + 1, clone);
                    p.selectedProfile++;
                }

            }
            else
            {
                if (GUILayout.Button(new GUIContent("Calculate UVs", "This will automatically generate UVs based on the distance between the profile vertices."), button)) CalculateUVs();

                // Normalize UVs
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Normalized Calculation", "If this is enabled the uv will be remapped to have a total length of 1. If this is disabled the uv calculation will be solely based on the distance between the profile vertices."), menu, GUILayout.Width(labelWidthToggle));
                EditorGUI.BeginChangeCheck();
                bool normalize = EditorGUILayout.Toggle(p.normalizeUVs);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(p, "Change UV normalization");
                    p.normalizeUVs = normalize;
                }
                GUILayout.EndHorizontal();

                // U or V
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("UV = U", "Should the UVs of this profile be extruded horizontal or vertically in uv space?"), menu, GUILayout.Width(labelWidthToggle));
                EditorGUI.BeginChangeCheck();
                bool uvIsU = EditorGUILayout.Toggle(selectedProfile.uvIsU);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RegisterCompleteObjectUndo(p, "Change Profile UV Orientation");
                    selectedProfile.uvIsU = uvIsU;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            GUILayout.Space(2);
            if (mode == Mode.UV)
            {
                tex = EditorGUILayout.ObjectField(tex, typeof(Texture2D), false) as Texture2D;
                if (Event.current.commandName == "ObjectSelectorUpdated")
                {
                    tex = EditorGUIUtility.GetObjectPickerObject() as Texture2D;
                    Repaint();
                }
            }

            GUIKeyBindings();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();


            if (GUI.changed)
            {
                EditorUtility.SetDirty(p);
                //AssetDatabase.SaveAssets();
                //AssetDatabase.Refresh();
            }



            Repaint();
        } //===========================================================================================================================================================












        void RegisterDragUndo(string text)
        {
            if (!isDragging)
            {
                isDragging = true;
                Undo.RegisterCompleteObjectUndo(p, text);
            }
        } //===========================================================================================================================================================





        void GetInputShape()
        {
            // Mouse on empty Space
            indexHover = -1;



            if (p.showPivot)
            {
                SetupPivotRects();
                // Mouse On Pivot Position Handle
                if (pivotRect.Contains(e.mousePosition)) indexHover = -2;
                // Mouse On Move Handle
                else if (pivotRectT.Contains(e.mousePosition)) indexHover = -3;
                // Mouse On Rotation Handle
                else if (pivotRectR.Contains(e.mousePosition)) indexHover = -4;
                // Mouse On Scale Handle
                else if (pivotRectS.Contains(e.mousePosition)) indexHover = -5;
            }
            if (indexHover == -1) // Mouse is not on Pivot
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    Rect r = new Rect(WorldToGUIPoint(vertices[i]) - vertexSize3.position, vertexSize3.size);
                    if (r.Contains(e.mousePosition)) indexHover = i;
                }
            }



            if (e.button == 0)
            {
                if (e.type == EventType.MouseDown && !mouseOnGUI)
                {
                    indexDrag = indexHover;

                    if (indexHover == -1) // Clicked in empty Area
                    {
                        dragStartPos = e.mousePosition;
                        if (!e.shift) selection.Clear();
                    }
                    else if (indexHover == -2) // Clicked On Pivot Handle
                    {
                        dragStartPos = p.profiles[p.selectedProfile].pivot;
                    }
                    else if (indexHover == -3) // Clicked On Pivot Move Handle
                    {
                        dragStartPos = p.profiles[p.selectedProfile].pivot;
                    }
                    else if (indexHover == -4) // Clicked On Pivot Rotate Handle
                    {
                        dragStartPos = Vector3.zero;
                    }
                    else if (indexHover == -5) // Clicked On Pivot Scale Handle
                    {
                        dragStartPos = Vector3.one;
                    }
                    else // Clicked On Vertex
                    {
                        dragStartPos = vertices[indexDrag];

                        if (e.shift && !selection.Contains(indexDrag))
                        {
                            Undo.RegisterCompleteObjectUndo(p, "Add to Selection");
                            selection.Add(indexDrag);
                        }
                        else
                        {
                            if (!selection.Contains(indexDrag))
                            {
                                Undo.RegisterCompleteObjectUndo(p, "New Selection");
                                selection.Clear();
                                selection.Add(indexDrag);
                            }
                            else if (e.control || e.command)
                            {
                                Undo.RegisterCompleteObjectUndo(p, "Remove From Selection");
                                selection.Remove(indexDrag);
                            }
                        }
                    }
                }


                if (e.type == EventType.MouseDrag)
                {
                    if (indexDrag == -1) // Drag Selection Frame
                    {
                        showSelectionFrame = true;

                        Vector2 min = new Vector2(Mathf.Min(dragStartPos.x, e.mousePosition.x), Mathf.Min(dragStartPos.y, e.mousePosition.y));
                        Vector2 max = new Vector2(Mathf.Max(dragStartPos.x, e.mousePosition.x), Mathf.Max(dragStartPos.y, e.mousePosition.y));
                        selectionFrameMinMax = new Rect { min = min, max = max + Vector2.one };

                        for (int i = 0; i < vertices.Count; i++)
                        {
                            if (selectionFrameMinMax.Contains(WorldToGUIPoint(vertices[i])))
                            {
                                if (e.control || e.command)
                                {
                                    selection.Remove(i);
                                }
                                else if (!selection.Contains(i))
                                {
                                    selection.Add(i);
                                }
                            }
                            else if (!e.shift)
                            {
                                selection.Remove(i);
                            }
                        }
                    }


                    else if (indexDrag == -2) // Translate Pivot
                    {
                        RegisterDragUndo("Move Pivot");
                        dragStartPos += new Vector2(e.delta.x, -e.delta.y) / zoom;
                        Vector2 oldPos = p.profiles[p.selectedProfile].pivot;
                        Vector2 newPos = dragStartPos;
                        if (p.snap) newPos = new Vector2(Mathf.Round(dragStartPos.x / increment) * increment, Mathf.Round(dragStartPos.y / increment) * increment);
                        if (newPos != oldPos) p.profiles[p.selectedProfile].pivot += newPos - oldPos;
                    }


                    else if (indexDrag == -3) // Move Selection with Pivot
                    {
                        RegisterDragUndo("Move Selection with Pivot");

                        dragStartPos += new Vector2(e.delta.x, -e.delta.y) / zoom;
                        Vector2 oldPos = p.profiles[p.selectedProfile].pivot;
                        Vector2 newPos = dragStartPos;
                        if (p.snap) newPos = new Vector2(Mathf.Round(dragStartPos.x / increment) * increment, Mathf.Round(dragStartPos.y / increment) * increment);
                        if (newPos != oldPos) p.profiles[p.selectedProfile].pivot += newPos - oldPos;

                        if (newPos != oldPos)
                        {
                            foreach (int i in selection)
                            {
                                Vector2 delta = newPos - oldPos;
                                vertices[i] += new Vector4(delta.x, delta.y, 0, 0);
                            }
                            foreach (int i in selection)
                            {
                                AutoNormalPrevNext(i);
                            }
                        }
                    }


                    else if (indexDrag == -4) // Rotate Selection with Pivot
                    {
                        RegisterDragUndo("Rotate Selection with Pivot");

                        rotateDelta = (Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y) ? e.delta.x : -e.delta.y) * 0.01f;

                        foreach (int i in selection)
                        {
                            Vector2 pos = RotateAround(vertices[i], p.profiles[p.selectedProfile].pivot, -rotateDelta);
                            if (vertices[i].z != 999) vertices[i] = new Vector4(pos.x, pos.y, vertices[i].z + rotateDelta, vertices[i].w);
                            else vertices[i] = new Vector4(pos.x, pos.y, 999, vertices[i].w);
                        }
                    }


                    else if (indexDrag == -5) // Scale Selection with Pivot
                    {
                        RegisterDragUndo("Scale Selection with Pivot");

                        float scaleDelta = (Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y) ? e.delta.x : -e.delta.y) * 0.01f;

                        foreach (int i in selection)
                        {
                            Vector2 temp = vertices[i];
                            temp -= p.profiles[p.selectedProfile].pivot;
                            temp *= 1 + scaleDelta;
                            temp += p.profiles[p.selectedProfile].pivot;
                            vertices[i] = new Vector4(temp.x, temp.y, vertices[i].z, vertices[i].w);
                        }
                    }


                    else // Move Point(s)
                    {
                        RegisterDragUndo("Move Selection");

                        dragStartPos += new Vector2(e.delta.x, -e.delta.y) / zoom;

                        Vector2 oldPos = vertices[indexDrag];
                        Vector2 newPos = dragStartPos;
                        if (p.snap) newPos = new Vector2(Mathf.Round(dragStartPos.x / increment) * increment, Mathf.Round(dragStartPos.y / increment) * increment);

                        if (newPos != oldPos)
                        {
                            foreach (int i in selection)
                            {
                                Vector2 delta = newPos - oldPos;
                                vertices[i] += new Vector4(delta.x, delta.y, 0, 0);
                            }
                            foreach (int i in selection)
                            {
                                AutoNormalPrevNext(i);
                            }
                        }
                    }
                }


                if (e.type == EventType.MouseUp || e.type == EventType.Ignore)
                {
                    indexDrag = -1;
                    showSelectionFrame = false;

                    isDragging = false;

                    //Check For Colocated Does not work because of selection indices
                    //for (int j = vertices.Count - 1; j >= 0; j--)
                    //{
                    //    foreach (int i in selection)
                    //    {
                    //        if (i == j) continue;

                    //        Vector2 a = vertices[j];
                    //        Vector2 b = vertices[i];
                    //        if (a == b) vertices.RemoveAt(j--);
                    //    }
                    //}
                    //EditorUtility.SetDirty(p);
                }
            }

            if (e.keyCode == KeyCode.Delete)
            {
                Undo.RegisterCompleteObjectUndo(p, "Delete Selection");
                for (int i = vertices.Count - 1; i >= 0; i--)
                {
                    if (selection.Contains(i))
                    {
                        vertices.RemoveAt(i);
                        selection.Remove(i);
                        //AutoNormal(i);
                        //AutoNormal(i-1);
                    }
                }
                if (vertices.Count == 0 && p.profiles.Count > 1)
                {
                    p.profiles.RemoveAt(p.selectedProfile);
                    if (p.selectedProfile != 0) p.selectedProfile--;
                }
            }
        } //===========================================================================================================================================================


        void GetInputUVs()
        {
            // Mouse on empty Space
            indexHover = -1;

            if (p.showPivot)
            {
                SetupPivotRects();
                // Mouse On Pivot Position Handle
                if (pivotRect.Contains(e.mousePosition)) indexHover = -2;
                // Mouse On Move Handle
                else if (pivotRectT.Contains(e.mousePosition)) indexHover = -3;
                // Mouse On Scale Handle
                else if (pivotRectS.Contains(e.mousePosition)) indexHover = -5;
            }
            if (indexHover == -1) // Mouse is not on Pivot
            {
                for (int i = 0; i <= vertices.Count; i++)
                {
                    Vector3[] line = i < vertices.Count ? GetUVGUICoordinates(vertices[i].w) : GetUVGUICoordinates(selectedProfile.loopUV);
                    Vector2 pos = Vector2.Lerp(line[1], line[0], ((float)i / vertices.Count) * 0.8f + 0.1f);
                    Rect r = new Rect(pos - vertexSize3.position, vertexSize3.size);

                    if (r.Contains(e.mousePosition)) indexHover = i;
                }
            }




            if (e.button == 0)
            {
                if (e.type == EventType.MouseDown && !mouseOnGUI)
                {
                    indexDrag = indexHover;

                    if (indexHover == -1) // Clicked in empty Area
                    {
                        dragStartPos = e.mousePosition;
                        if (!e.shift) selection.Clear();
                    }
                    else if (indexHover == -2) // Clicked On Pivot Handle
                    {
                        dragStartPos = selectedProfile.pivot;
                    }
                    else if (indexHover == -3) // Clicked On Pivot Move Handle
                    {
                        dragStartPos = selectedProfile.pivot;
                    }
                    else if (indexHover == -5) // Clicked On Pivot Scale Handle
                    {
                        dragStartPos = Vector3.one;
                    }
                    else // Clicked On Vertex
                    {
                        float uv = (indexDrag != vertices.Count) ? vertices[indexDrag].w : selectedProfile.loopUV;
                        dragStartPos = new Vector2(uv, uv);

                        if (e.shift && !selection.Contains(indexDrag))
                        {
                            Undo.RegisterCompleteObjectUndo(p, "Add to Selection");
                            selection.Add(indexDrag);
                        }
                        else
                        {
                            if (!selection.Contains(indexDrag))
                            {
                                Undo.RegisterCompleteObjectUndo(p, "New Selection");
                                selection.Clear();
                                selection.Add(indexDrag);
                            }
                            else if (e.control || e.command)
                            {
                                Undo.RegisterCompleteObjectUndo(p, "Remove From Selection");
                                selection.Remove(indexDrag);
                            }
                        }
                    }
                }


                if (e.type == EventType.MouseDrag)
                {
                    if (indexDrag == -1) // Drag Selection Frame
                    {
                        showSelectionFrame = true;

                        Vector2 min = new Vector2(Mathf.Min(dragStartPos.x, e.mousePosition.x), Mathf.Min(dragStartPos.y, e.mousePosition.y));
                        Vector2 max = new Vector2(Mathf.Max(dragStartPos.x, e.mousePosition.x), Mathf.Max(dragStartPos.y, e.mousePosition.y));
                        selectionFrameMinMax = new Rect { min = min, max = max + Vector2.one };

                        for (int i = 0; i <= vertices.Count; i++)
                        {
                            Vector3[] line = (i < vertices.Count) ? GetUVGUICoordinates(vertices[i].w) : GetUVGUICoordinates(selectedProfile.loopUV);
                            Vector2 pos = Vector2.Lerp(line[1], line[0], ((float)i / vertices.Count) * 0.8f + 0.1f);


                            if (selectionFrameMinMax.Contains(pos))
                            {
                                if (e.control || e.command) selection.Remove(i);
                                else if (!selection.Contains(i)) selection.Add(i);
                            }
                            else if (!e.shift) selection.Remove(i);
                        }
                    }


                    else if (indexDrag == -2) // Translate Pivot
                    {
                        RegisterDragUndo("Move Pivot");
                        dragStartPos += new Vector2(e.delta.x, -e.delta.y) / zoom;
                        Vector2 oldPos = selectedProfile.pivot;
                        Vector2 newPos = dragStartPos;
                        if (p.snap) newPos = new Vector2(Mathf.Round(dragStartPos.x / increment) * increment, Mathf.Round(dragStartPos.y / increment) * increment);
                        if (newPos != oldPos) selectedProfile.pivot += newPos - oldPos;
                    }


                    else if (indexDrag == -3) // Move Selection with Pivot
                    {
                        RegisterDragUndo("Move Selection with Pivot");

                        dragStartPos += new Vector2(e.delta.x, -e.delta.y) / zoom;
                        Vector2 oldPos = selectedProfile.pivot;
                        Vector2 newPos = dragStartPos;
                        if (p.snap) newPos = new Vector2(Mathf.Round(dragStartPos.x / increment) * increment, Mathf.Round(dragStartPos.y / increment) * increment);
                        if (newPos != oldPos) selectedProfile.pivot += newPos - oldPos;

                        if (newPos != oldPos)
                        {
                            foreach (int i in selection)
                            {
                                Vector2 delta = newPos - oldPos;

                                if (i < vertices.Count) vertices[i] += new Vector4(0, 0, 0, selectedProfile.uvIsU ? delta.x : delta.y);
                                else selectedProfile.loopUV += selectedProfile.uvIsU ? delta.x : delta.y;
                            }
                        }
                    }


                    else if (indexDrag == -5) // Scale Selection with Pivot
                    {
                        RegisterDragUndo("Scale Selection with Pivot");

                        float scaleDelta = (Mathf.Abs(e.delta.x) > Mathf.Abs(e.delta.y) ? e.delta.x : -e.delta.y) * 0.01f;

                        foreach (int i in selection)
                        {
                            float temp = (i < vertices.Count) ? vertices[i].w : selectedProfile.loopUV;
                            temp -= selectedProfile.uvIsU ? selectedProfile.pivot.x : selectedProfile.pivot.y;
                            temp *= 1 + scaleDelta;
                            temp += selectedProfile.uvIsU ? selectedProfile.pivot.x : selectedProfile.pivot.y;

                            if (i < vertices.Count) vertices[i] = new Vector4(vertices[i].x, vertices[i].y, vertices[i].z, temp);
                            else selectedProfile.loopUV = temp;
                        }
                    }


                    else // Move Point(s)
                    {
                        RegisterDragUndo("Move Selection");

                        dragStartPos += new Vector2(e.delta.x, -e.delta.y) / zoom;

                        float uv = (indexDrag != vertices.Count) ? vertices[indexDrag].w : selectedProfile.loopUV;
                        Vector2 oldPos = new Vector2(uv, uv);
                        Vector2 newPos = dragStartPos;
                        if (p.snap) newPos = new Vector2(Mathf.Round(dragStartPos.x / increment) * increment, Mathf.Round(dragStartPos.y / increment) * increment);

                        if (newPos != oldPos)
                        {
                            foreach (int i in selection)
                            {
                                Vector2 delta = newPos - oldPos;
                                if (i < vertices.Count) vertices[i] += new Vector4(0, 0, 0, selectedProfile.uvIsU ? delta.x : delta.y);
                                else selectedProfile.loopUV += selectedProfile.uvIsU ? delta.x : delta.y;
                            }
                        }
                    }
                }



                if (e.type == EventType.MouseUp || e.type == EventType.Ignore)
                {
                    indexDrag = -1;
                    showSelectionFrame = false;
                    isDragging = false;
                }
            }
        } //===========================================================================================================================================================


        Vector2 bevelPos; float bevelDist;
        void StartBevel()
        {
            bevelDist = 0;
            bevelPos = vertices[indexDrag];
            vertices.Insert(indexDrag, bevelPos);
        } //===========================================================================================================================================================
        void Bevel()
        {
            bevelDist += Event.current.delta.y * 0.01f;

            Vector2 prev = vertices[indexDrag - 1];
            Vector2 next = vertices[indexDrag + 2];

            Vector2 dirPrev = (prev - bevelPos).normalized;
            Vector2 dirNext = (next - bevelPos).normalized;

            vertices[indexDrag] = bevelPos + dirPrev * bevelDist;
            vertices[indexDrag + 1] = bevelPos + dirNext * bevelDist;

            AutoNormal(indexDrag, -1);
            AutoNormal(indexDrag + 1, 1);
        } //===========================================================================================================================================================









        void CalculateUVs()
        {
            float totalDistance = 0;

            Vector4 v = vertices[0]; v.w = 0; vertices[0] = v;

            for (int i = 1; i < vertices.Count; i++)
            {
                totalDistance += Vector2.Distance(vertices[i - 1], vertices[i]);
                v = vertices[i]; v.w = totalDistance; vertices[i] = v;
            }
            if (p.profiles[p.selectedProfile].close)
            {
                totalDistance += Vector2.Distance(vertices[0], vertices[vertices.Count - 1]);
            }

            if (p.normalizeUVs)
            {
                for (int i = 1; i < vertices.Count; i++)
                {
                    v = vertices[i]; v.w /= totalDistance; vertices[i] = v;
                }
            }
            p.profiles[p.selectedProfile].loopUV = p.normalizeUVs ? 1 : totalDistance;
            p.profiles[p.selectedProfile].profileLength = totalDistance;
        } //===========================================================================================================================================================



        void DrawUVs()
        {
            Handles.color = new Color32(255, 255, 255, 255);

            for (int i = 0; i < vertices.Count; i++) Handles.DrawAAPolyLine(3, GetUVGUICoordinates(vertices[i].w));
            if (p.profiles[p.selectedProfile].close) Handles.DrawAAPolyLine(3, GetUVGUICoordinates(p.profiles[p.selectedProfile].loopUV));

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3[] line = GetUVGUICoordinates(vertices[i].w);
                DrawUVHandles(Vector2.Lerp(line[1], line[0], ((float)i / vertices.Count) * 0.8f + 0.1f), i);
            }
            if (p.profiles[p.selectedProfile].close)
            {
                Vector3[] line = GetUVGUICoordinates(p.profiles[p.selectedProfile].loopUV);
                DrawUVHandles(Vector2.Lerp(line[1], line[0], 1 * 0.8f + 0.1f), vertices.Count);
            }
        } //===========================================================================================================================================================



        Vector3[] GetUVGUICoordinates(float uv)
        {
            Vector3[] line = new Vector3[2];
            if (p.profiles[p.selectedProfile].uvIsU)
            {
                float x = uv * zoom + offset.x;
                line[0] = new Vector3(x, 0, 0);
                line[1] = new Vector3(x, windowSize.y, 0);
            }
            else
            {
                float y = -uv * zoom + offset.y;
                line[1] = new Vector3(0 + 180, y, 0);
                line[0] = new Vector3(windowSize.x, y, 0);
            }
            return line;
        } //===========================================================================================================================================================



        void DrawUVHandles(Vector2 pos, int index)
        {
            Rect r = new Rect(pos - vertexSize3.position, vertexSize3.size);

            GUI.color = Color.white;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = colorBackground;
            GUI.DrawTexture(new Rect(pos - vertexSize2.position, vertexSize2.size), Texture2D.whiteTexture);

            if (p.showLabels) ShowVertexIDLabels(pos + new Vector2(2, -10), index);


            if (selection.Contains(index))
            {
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(pos - vertexSize1.position, vertexSize1.size), Texture2D.whiteTexture);
            }

            if (index == indexHover || index == indexDrag)
            {
                GUI.color = colorVertexHover;
                GUI.DrawTexture(new Rect(pos - vertexSize1.position, vertexSize1.size), Texture2D.whiteTexture);
            }
        } //===========================================================================================================================================================









        void DrawProfile()
        {
            for (int i = 0; i < p.profiles.Count; i++)
            {
                if (i != p.selectedProfile)
                {
                    DrawLinesUnselected(p.profiles[i].vertices, i);
                    DrawVerticesUnselected(p.profiles[i].vertices);
                }
            }

            DrawLines();
            DrawNormals();
            DrawVertices();
        } //===========================================================================================================================================================



        void DrawLines()
        {
            int addPointOnLine = -1;

            int c = p.profiles[p.selectedProfile].close ? 0 : 1;
            for (int i = 0; i < vertices.Count - c; i++)
            {
                Vector2 a = WorldToGUIPoint(vertices[i]);
                Vector2 b = WorldToGUIPoint(vertices[(i + 1) % vertices.Count]);

                if (HandleUtility.DistanceToLine(a, b) < 5 && indexHover == -1 && indexDrag == -1 && e.alt)
                {
                    Handles.color = new Color(2, 0.75f, 0.5f, 1);


                    GUI.color = new Color(1, 0.375f, 0.25f, 1);
                    Vector2 p = HandleUtility.ClosestPointToPolyLine(new Vector3[] { a, b });
                    GUI.DrawTexture(new Rect(p - vertexSize0.position, vertexSize0.size), Texture2D.whiteTexture);

                    addPointOnLine = i + 1;
                }
                else Handles.color = new Color32(255, 255, 255, 255);

                Handles.DrawAAPolyLine(3, new Vector3[] { a, b });
            }

            if (e.alt)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (addPointOnLine != -1)
                    {
                        vertices.Insert(addPointOnLine, GUIToWorldPoint(e.mousePosition));
                        selection.Clear();
                        selection.Add(addPointOnLine);
                        dragStartPos = vertices[addPointOnLine];
                        indexDrag = addPointOnLine;

                        AutoNormalPrevNext(indexDrag);
                    }
                    else
                    {
                        vertices.Add(GUIToWorldPoint(e.mousePosition));
                        selection.Clear();
                        selection.Add(vertices.Count - 1);
                        dragStartPos = vertices[vertices.Count - 1];
                        indexDrag = vertices.Count - 1;

                        AutoNormalPrevNext(indexDrag);
                    }
                }
                else if (indexDrag == -1 && addPointOnLine == -1)
                {
                    if (vertices.Count != 0)
                    {
                        Vector2 a = WorldToGUIPoint(vertices[vertices.Count - 1]);
                        Vector2 b = e.mousePosition;

                        Handles.color = new Color(2, 0.75f, 0.5f, 0.25f);
                        Handles.DrawAAPolyLine(3, new Vector3[] { a, b });
                    }
                    GUI.color = new Color(1, 0.375f, 0.25f, 1);
                    GUI.DrawTexture(new Rect(e.mousePosition - vertexSize0.position, vertexSize0.size), Texture2D.whiteTexture);
                }
            }
        } //===========================================================================================================================================================



        void DrawLinesUnselected(List<Vector4> verts, int profileIndex)
        {
            int c = p.profiles[profileIndex].close ? 0 : 1;

            bool hover = false;
            for (int i = 0; i < verts.Count - c; i++)
            {
                Vector2 a = WorldToGUIPoint(verts[i]);
                Vector2 b = WorldToGUIPoint(verts[(i + 1) % verts.Count]);

                if (HandleUtility.DistanceToLine(a, b) < 5 && indexDrag == -1 && !showSelectionFrame) hover = true;
            }
            if (hover && Event.current.button == 0 && Event.current.type == EventType.MouseDown)
            {
                Undo.RegisterCompleteObjectUndo(p, "Select Profile");
                p.selectedProfile = profileIndex;
            }

            for (int i = 0; i < verts.Count - c; i++)
            {
                Vector2 a = WorldToGUIPoint(verts[i]);
                Vector2 b = WorldToGUIPoint(verts[(i + 1) % verts.Count]);
                Handles.color = new Color(0.5f, 1.0f, 0.5f, (hover && indexHover == -1) ? 1.0f : 0.5f);
                Handles.DrawAAPolyLine(3, new Vector3[] { a, b });
            }
        } //===========================================================================================================================================================



        void DrawVertices()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 pos = WorldToGUIPoint(vertices[i]);

                if (i == indexDrag || selection.Contains(i))
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect(pos - vertexSize3.position, vertexSize3.size), Texture2D.whiteTexture);
                    GUI.color = colorBackground;
                    GUI.DrawTexture(new Rect(pos - vertexSize2.position, vertexSize2.size), Texture2D.whiteTexture);
                }

                bool colocated = false;
                for (int j = 0; j < vertices.Count; j++)
                {
                    if (j == i) continue;
                    if (vertices[j].x == vertices[i].x && vertices[j].y == vertices[i].y) colocated = true;
                }

                GUI.color = colorVertex;
                if (i == 0) GUI.color = new Color(1, 0.0f, 1, 1);
                if (colocated) GUI.color = colorVertexColocated;
                if (i == indexHover || i == indexDrag) GUI.color = colorVertexHover;

                GUI.DrawTexture(new Rect(pos - vertexSize1.position, vertexSize1.size), Texture2D.whiteTexture);

                if (p.showLabels) ShowVertexIDLabels(pos, i);
            }
        } //===========================================================================================================================================================


        void ShowVertexIDLabels(Vector2 pos, int index)
        {
            string labelText = index.ToString();
            if (index == vertices.Count) labelText = "Loop";

            GUI.color = Color.black;
            GUI.Label(new Rect(pos + new Vector2(0, 1), new Vector2(20, 20)), labelText, vertexLabels);
            GUI.Label(new Rect(pos + new Vector2(0, -1), new Vector2(20, 20)), labelText, vertexLabels);
            GUI.Label(new Rect(pos + new Vector2(1, 0), new Vector2(20, 20)), labelText, vertexLabels);
            GUI.Label(new Rect(pos + new Vector2(-1, -0), new Vector2(20, 20)), labelText, vertexLabels);

            GUI.color = Color.white;
            GUI.Label(new Rect(pos, new Vector2(20, 20)), labelText, vertexLabels);
        } //===========================================================================================================================================================



        void DrawVerticesUnselected(List<Vector4> verts)
        {
            for (int i = 0; i < verts.Count; i++)
            {
                Vector2 pos = WorldToGUIPoint(verts[i]);

                GUI.color = colorVertexUnselected;
                GUI.DrawTexture(new Rect(pos - vertexSize0.position, vertexSize0.size), Texture2D.whiteTexture);
            }
        } //===========================================================================================================================================================


        void DrawNormals()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector2 a = WorldToGUIPoint(vertices[i]);
                Vector2 n = new Vector2(Mathf.Sin(vertices[i].z), Mathf.Cos(vertices[i].z)).normalized;
                Vector2 b = a + new Vector2(-n.x, n.y) * 40;

                Rect r = new Rect(b - vertexSize3.position, vertexSize3.size);
                bool hover = r.Contains(e.mousePosition) && vertices[i].z != 999 && !(indexDrag == -2 || indexHover == -2 || indexDrag == -3 || indexHover == -3);

                if (e.type == EventType.MouseDown && e.button == 0 && hover) indexNormal = i;


                if (vertices[i].z != 999)
                {
                    if (indexNormal == i && e.type == EventType.MouseDrag && e.button == 0) // Modify Normal
                    {
                        RegisterDragUndo("Modify Normal");

                        Vector3 v = (e.mousePosition - a).normalized;

                        float angle = Mathf.Atan2(-v.x, v.y);

                        Vector4 temp = vertices[i];
                        temp.z = angle;
                        vertices[i] = temp;
                    }


                    if (indexNormal == i)
                    {
                        Handles.color = new Color(1, 1, 1, 0.02f);
                        Handles.DrawSolidDisc(a, Vector3.forward, 40);
                        Handles.color = new Color(1, 0.25f, 0, 0.25f);
                        Handles.DrawWireDisc(a, Vector3.forward, 40);
                    }


                    Handles.color = (hover || indexNormal == i) ? new Color(0, 1, 0, 1) : new Color(0, 0, 2, 1);
                    Handles.DrawAAPolyLine(3, new Vector3[] { a, b });

                    if (indexNormal == i)
                    {
                        Handles.color = new Color(1, 1, 1, 0.125f);
                        Handles.DrawAAPolyLine(3, new Vector3[] { b, e.mousePosition });

                        if (!selection.Contains(i)) selection.Add(i);
                    }

                    GUI.color = (hover || indexNormal == i) ? Color.green : Color.blue;
                    GUI.DrawTexture(new Rect(b - vertexSize0.position, vertexSize0.size), Texture2D.whiteTexture);
                }
                else // Hard Normals
                {
                    Vector2 current = vertices[i];
                    Vector2 previous = vertices[(int)Mathf.Repeat(i - 1, vertices.Count)];
                    Vector2 next = vertices[(int)Mathf.Repeat(i + 1, vertices.Count)];

                    Vector2 normal1 = Vector3.Cross(new Vector3(0, 0, 1), current - previous).normalized;
                    Vector2 normal2 = Vector3.Cross(new Vector3(0, 0, 1), next - current).normalized;
                    normal1.y = -normal1.y;
                    normal2.y = -normal2.y;

                    Handles.color = (hover || indexNormal == i) ? new Color(1, 0, 0, 1) : new Color(0, 0, 2, 1);
                    Vector2 pos1 = a + normal1 * 40;
                    Vector2 pos2 = a + normal2 * 40;

                    Handles.DrawAAPolyLine(3, new Vector3[] { a, pos1 });
                    Handles.DrawAAPolyLine(3, new Vector3[] { a, pos2 });
                }
            }

            if (e.type == EventType.MouseUp || e.type == EventType.Ignore && e.button == 0)
            {
                isDragging = false;
                indexNormal = -1;
            }
        } //===========================================================================================================================================================




        void AutoNormalPrevNext(int index)
        {
            if (!p.autoUpdateNormals) return;

            AutoNormal(index);

            if (p.profiles[p.selectedProfile].close || index != 0) AutoNormal((int)Mathf.Repeat(index - 1, vertices.Count));
            if (p.profiles[p.selectedProfile].close || index != vertices.Count - 1) AutoNormal((int)Mathf.Repeat(index + 1, vertices.Count));
        } //===========================================================================================================================================================



        void AutoNormal(int index, int mod = 0, bool set = false)
        {
            if (vertices.Count < 2) return;
            if (vertices[index].z == 999 && !set) return;

            Vector2 a = vertices[(int)Mathf.Repeat(index - 1, vertices.Count)];
            Vector2 b = vertices[index];
            Vector2 c = vertices[(int)Mathf.Repeat(index + 1, vertices.Count)];

            Vector2 dir = ((b - a).normalized + (c - b).normalized) / 2;

            if (mod == -1 || (!p.profiles[p.selectedProfile].close && index == vertices.Count - 1)) dir = (b - a).normalized;
            if (mod == 1 || (!p.profiles[p.selectedProfile].close && index == 0)) dir = (c - b).normalized;

            if (!p.profiles[p.selectedProfile].close && index == vertices.Count - 1) dir = (b - a).normalized;


            Vector2 normal = Vector3.Cross(new Vector3(0, 0, 1), dir);
            float angle = Mathf.Atan2(-normal.x, -normal.y);

            if (mod == 2 && ((index != 0 && index != vertices.Count - 1) || p.profiles[p.selectedProfile].close)) angle = 999;

            SetNormalAngle(index, angle);
        } //===========================================================================================================================================================


        void SetNormalAngle(int index, float angle)
        {
            Vector4 v = vertices[index];
            v.z = angle;
            vertices[index] = v;
        } //===========================================================================================================================================================











        void DrawDragRectangle()
        {
            Handles.color = Color.white;
            if (showSelectionFrame)
            {
                GUI.color = colorGridOdd;
                GUI.Box(selectionFrameMinMax, GUIContent.none);

                Vector2 a = new Vector2(selectionFrameMinMax.min.x + 1, selectionFrameMinMax.min.y + 1);
                Vector2 b = new Vector2(selectionFrameMinMax.min.x + 1, selectionFrameMinMax.max.y);
                Vector2 c = new Vector2(selectionFrameMinMax.max.x, selectionFrameMinMax.max.y);
                Vector2 d = new Vector2(selectionFrameMinMax.max.x, selectionFrameMinMax.min.y + 1);

                Handles.color = colorGridCenter;
                Handles.DrawDottedLine(a, b, 3);
                Handles.DrawDottedLine(b, c, 3);
                Handles.DrawDottedLine(d, c, 3);
                Handles.DrawDottedLine(a, d, 3);
            }
        } //===========================================================================================================================================================








        void Zoom(float delta)
        {
            Vector2 oldPos = GUIToWorldPoint(windowSize / 2);
            zoom += delta * Mathf.Log10(zoom);
            if (zoom < 10) zoom = 10;
            Vector2 newPos = GUIToWorldPoint(windowSize / 2);

            Vector2 posDelta = (newPos - oldPos);
            offset += new Vector2(posDelta.x, -posDelta.y) * zoom;
        } //===========================================================================================================================================================


        public enum FrameMode { All, Profile, Selection };

        void Frame(FrameMode frameMode = FrameMode.All)
        {
            bool proceed = mode == Mode.Shape;
            if (mode == Mode.UV)
            {
                for (int i = 0; i < vertices.Count; i++)
                {
                    if (selectedProfile.loopUV != 0)
                    {
                        proceed = true;
                        break;
                    }
                    if (vertices[i].w != 0)
                    {
                        proceed = true;
                        break;
                    }
                }
            }


            if (p == null || p.profiles == null || (p.profiles.Count == 1 && p.profiles[0].vertices.Count == 0) || !proceed)
            {
                zoom = Mathf.Min(windowSize.x - 200, windowSize.y - 200) / 2;
                offset = windowSize / 2;
                return;
            }


            Vector2 min = Vector2.one * Mathf.Infinity;
            Vector2 max = -Vector2.one * Mathf.Infinity;

            if (mode == Mode.Shape)
            {
                if (frameMode == FrameMode.Selection)
                {
                    for (int i = 0; i < selection.Count; i++)
                    {
                        Vector2 v = p.profiles[p.selectedProfile].vertices[selection[i]];
                        if (v.x < min.x) min.x = v.x;
                        if (v.y < min.y) min.y = v.y;
                        if (v.x > max.x) max.x = v.x;
                        if (v.y > max.y) max.y = v.y;
                    }
                    if (selection.Count == 1)
                    {
                        min.x -= 0.5f;
                        min.y -= 0.5f;
                        max.x += 0.5f;
                        max.y += 0.5f;
                    }
                }
                else if (frameMode == FrameMode.Profile)
                {
                    for (int i = 0; i < p.profiles[p.selectedProfile].vertices.Count; i++)
                    {
                        Vector2 v = p.profiles[p.selectedProfile].vertices[i];
                        if (v.x < min.x) min.x = v.x;
                        if (v.y < min.y) min.y = v.y;
                        if (v.x > max.x) max.x = v.x;
                        if (v.y > max.y) max.y = v.y;
                    }
                }
                else
                {
                    for (int j = 0; j < p.profiles.Count; j++)
                    {
                        for (int i = 0; i < p.profiles[j].vertices.Count; i++)
                        {
                            Vector2 v = p.profiles[j].vertices[i];
                            if (v.x < min.x) min.x = v.x;
                            if (v.y < min.y) min.y = v.y;
                            if (v.x > max.x) max.x = v.x;
                            if (v.y > max.y) max.y = v.y;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < p.profiles[p.selectedProfile].vertices.Count; i++)
                {
                    float w = p.profiles[p.selectedProfile].vertices[i].w;
                    if (w < min.x) min.x = w;
                    if (w < min.y) min.y = w;
                    if (w > max.x) max.x = w;
                    if (w > max.y) max.y = w;
                }

                float loopUV = p.profiles[p.selectedProfile].loopUV;
                if (loopUV < min.x) min.x = loopUV;
                if (loopUV < min.y) min.y = loopUV;
                if (loopUV > max.x) max.x = loopUV;
                if (loopUV > max.y) max.y = loopUV;
            }

            Vector2 size = new Vector2(max.x - min.x, max.y - min.y);
            zoom = Mathf.Min((windowSize.x - 300) / size.x, (windowSize.y - 200) / size.y);

            Vector2 center = new Vector2(min.x + max.x - ((180 / 1) / zoom), -min.y - max.y);
            offset = (windowSize * 0.5f) - (center * 0.5f * zoom);
        } //===========================================================================================================================================================



        public Vector2 RotateAround(Vector2 point, Vector2 center, float angle)
        {
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            point -= center;
            Vector2 rot = new Vector2(point.x * cos - point.y * sin, point.x * sin + point.y * cos);
            return rot + center;
        } //===========================================================================================================================================================



        Vector2 WorldToGUIPoint(Vector2 p)
        {
            p.y *= -1;
            return p * zoom + offset;
        } //===========================================================================================================================================================



        Vector2 GUIToWorldPoint(Vector2 p)
        {
            p -= offset;
            p /= zoom;
            p.y *= -1;
            return p;
        } //===========================================================================================================================================================














        // Waste
        //Vector2 mousePosition;
        //Vector2 mouseDelta;
        //float scrollDelta;
        //bool mouseDown0, mouseDrag0, mouseUp0;
        //bool mouseDown1, mouseDrag1, mouseUp1;
        //bool mouseDown2, mouseDrag2, mouseUp2;
        //void GetMouseInput()
        //{
        //    e = Event.current;

        //    if (e.button == 0)
        //    {
        //        mouseDown0 = e.type == EventType.MouseDown;
        //        mouseDrag0 = e.type == EventType.MouseDrag;
        //        mouseUp0   = e.type == EventType.MouseUp;
        //    }
        //    if (e.button == 1)
        //    {
        //        mouseDown1 = e.type == EventType.MouseDown;
        //        mouseDrag1 = e.type == EventType.MouseDrag;
        //        mouseUp1   = e.type == EventType.MouseUp;
        //    }
        //    if (e.button == 2)
        //    {
        //        mouseDown2 = e.type == EventType.MouseDown;
        //        mouseDrag2 = e.type == EventType.MouseDrag;
        //        mouseUp2   = e.type == EventType.MouseUp;
        //    }

        //    mousePosition = e.mousePosition;
        //    mouseDelta = e.delta;
        //    if (e.isScrollWheel) scrollDelta = e.delta.y; else scrollDelta = 0;
        //} //===========================================================================================================================================================
    }
}
