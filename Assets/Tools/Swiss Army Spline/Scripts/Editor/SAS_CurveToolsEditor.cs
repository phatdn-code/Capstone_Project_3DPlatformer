using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SwissArmySpline
{
    [CustomEditor(typeof(SAS_CurveTools))]
    public class SAS_CurveToolsEditor : Editor
    {

        private SAS_CurveTools c;
        static Transform t;

        private void OnEnable()
        {
            c = target as SAS_CurveTools;
            //Debug.Log("Enable");

            t = c.transform.Find("Temp Transform");
            if (t == null)
            {
                t = new GameObject { name = "Temp Transform", hideFlags = HideFlags.HideAndDontSave }.transform;
                t.parent = c.transform;
            }

            if (c.points.Count == 0)
            {
                c.points.Add(new SAS_CurveTools_Helper.Point { position = Vector3.zero, rotation = Quaternion.identity, tangentIn = 0, tangentOut = 0 });
                c.selectedIndex = 0;
                Tools.current = Tool.Move;
            }

            t.localPosition = c.points[c.selectedIndex].position;
            t.localRotation = c.points[c.selectedIndex].rotation;

            Tools.hidden = true;

            c.transform.position = Vector3.zero;
            c.transform.rotation = Quaternion.identity;
            c.transform.localScale = Vector3.one;
            c.transform.hideFlags = HideFlags.NotEditable;
            c.transform.hideFlags = HideFlags.HideInInspector;
            //UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(c.transform, false);

            Undo.undoRedoPerformed += MyUndoCallback;

            c.editable = true;
            c.zTest = true;
            c.showLabels = true;
            EditorWindow.FocusWindowIfItsOpen(typeof(SceneView));
        }

        private void OnDisable()
        {
            //Debug.Log("Disable");
            if (t != null) DestroyImmediate(t.gameObject);
            Tools.hidden = false;
            if (c != null) c.transform.hideFlags = HideFlags.None;
            Undo.undoRedoPerformed -= MyUndoCallback;
        }

        void MyUndoCallback() => (target as SAS_CurveTools).Execute();



        [MenuItem("GameObject/Swiss Army Spline/New Spline", false, 0)]
        static void CreateCustomGameObject(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Swiss Army Spline", typeof(SAS_CurveTools));
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }







        public override void OnInspectorGUI()
        {
            c = target as SAS_CurveTools;













            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 16;



            GUILayout.Space(10);

            if (c.editable)
            {
                if (c.selectedIndex != -1)
                {
                    GUI.backgroundColor = Color.green;
                    EditorGUILayout.BeginVertical("HelpBox");
                    GUI.backgroundColor = Color.white;


                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(new GUIContent("Point " + c.selectedIndex.ToString("N0"), "The currently selected point index."), style);

                    int add = 0;
                    EditorGUI.BeginChangeCheck();
                    if (GUILayout.Button(new GUIContent("<", "Select the previous point."), GUILayout.Width(20))) add = -1;
                    if (GUILayout.Button(new GUIContent(">", "Select the next point."), GUILayout.Width(20))) add = +1;
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Select Point " + Mathf.Repeat(c.selectedIndex + add, c.points.Count).ToString("N0"));
                        c.selectedIndex += add;
                        c.selectedIndex = (int)Mathf.Repeat(c.selectedIndex, c.points.Count);
                    }
                    EditorGUILayout.EndHorizontal();





                    EditorGUILayout.BeginVertical("HelpBox");



                    SerializedProperty p = serializedObject.FindProperty("points").GetArrayElementAtIndex(c.selectedIndex);

                    SerializedProperty pos = p.FindPropertyRelative("position");
                    EditorGUILayout.PropertyField(pos, new GUIContent("Position", "The position of this point in worldspace.")); t.localPosition = pos.vector3Value;

                    EditorGUI.BeginChangeCheck();
                    t.localRotation = c.points[c.selectedIndex].rotation;
                    Vector3 newInspectorRotation = EditorGUILayout.Vector3Field(new GUIContent("Rotation", "The rotation of this point in worldspace."), TransformUtils.GetInspectorRotation(t));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Rotation Changed");
                        TransformUtils.SetInspectorRotation(t, newInspectorRotation);
                        c.points[c.selectedIndex].rotation = t.rotation;
                    }


                    if (c.selectedIndex != 0 || c.loop) EditorGUILayout.PropertyField(p.FindPropertyRelative("tangentIn"), new GUIContent("Tangent In", "High values will make bigger radiuses."));
                    else EditorGUILayout.LabelField("");
                    if (c.selectedIndex != c.points.Count - 1 || c.loop) EditorGUILayout.PropertyField(p.FindPropertyRelative("tangentOut"), new GUIContent("TangentOut", "High values will make bigger radiuses."));
                    else EditorGUILayout.LabelField("");

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndVertical();
                }



                //GUI.backgroundColor = Color.red;
                //EditorGUILayout.BeginVertical("HelpBox");
                //GUI.backgroundColor = Color.white;
                //if (GUILayout.Button(new GUIContent("Reparameterize Curve", "This will ensure the distance along the curve is as accurate as possible. Usually you dont need to care about this, except if the value is not updated automatically (this only is the case if the meshing section below is unused). "))) c.CalculateLength();
                //GUI.backgroundColor = new Color(1, 1, 1, 0.25f);
                //EditorGUILayout.FloatField(new GUIContent("Total Curve Length", "As the title suggests, this is the total length of the curve (Only accurate if the curve has been reparameterized - see the tooltip of the button above)."), c.totalLength);
                //GUI.backgroundColor = Color.white;
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("distanceToSplineTime"), new GUIContent("Distance to Splinetime", "This maps the actual distance in meters along the curve to the interpolant value used internally. Basically its safe to ignore this field."));
                //EditorGUILayout.EndVertical();




                // Curve Options ========================================================================================================
                EditorGUILayout.BeginVertical("HelpBox");
                GUILayout.Label(new GUIContent("Curve Options", "Just options..."), style);
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginHorizontal("HelpBox", GUILayout.Width(110));
                GUILayout.Label(new GUIContent("Loop", "If enabled the curve will be a closed loop."), GUILayout.Width(80));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loop"), GUIContent.none, GUILayout.Width(16));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal("HelpBox");
                GUILayout.Label("Length: ");
                EditorGUILayout.FloatField(c.totalLength);
                //EditorGUILayout.LabelField(c.totalLength.ToString("N2"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();





                // Display Options ======================================================================================================
                //EditorGUILayout.BeginVertical("HelpBox");
                //GUILayout.Label(new GUIContent("Display Options", "Options related to the drawing of the curve."), style);
                //EditorGUILayout.BeginHorizontal();

                //EditorGUILayout.BeginHorizontal("HelpBox", GUILayout.Width(110));
                //GUILayout.Label(new GUIContent("Draw on Top", "If enabled the curve will always be drawn in the forground (This functionality can be forced on by some image effects, this is a issue of unity)."), GUILayout.Width(80));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("zTest"), GUIContent.none, GUILayout.Width(16));
                //EditorGUILayout.EndHorizontal();

                //EditorGUILayout.BeginHorizontal("HelpBox", GUILayout.Width(110));
                //GUILayout.Label(new GUIContent("Show Labels", "Shows the point indices in the scene view."), GUILayout.Width(80));
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("showLabels"), GUIContent.none, GUILayout.Width(16));
                //EditorGUILayout.EndHorizontal();

                //EditorGUILayout.BeginHorizontal("HelpBox");
                //GUILayout.Label(new GUIContent("Handle Size", "Scales the point gizmos in the scene view."), GUILayout.Width(75));
                //EditorGUI.BeginChangeCheck();
                //float handleSize = GUILayout.HorizontalSlider(c.handleSize, 0.05f, 1.5f);
                //if (EditorGUI.EndChangeCheck())
                //{
                //    Undo.RecordObject(c, "Change Handlesize");
                //    c.handleSize = handleSize;
                //}
                //EditorGUILayout.EndHorizontal();

                //EditorGUILayout.EndHorizontal();
                //EditorGUILayout.EndVertical();




                GUILayout.Space(10);
            }

            SetNames();

            GUI.backgroundColor = Color.blue;
            EditorGUILayout.BeginVertical("HelpBox");
            GUI.backgroundColor = Color.white;
            GUILayout.Label(new GUIContent("Meshing", "Everything related to mesh creation. All optional operations below (extruders, distributers, pillars) will add to a single final mesh."), style);

            EditorGUILayout.BeginVertical("HelpBox");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoUpdate"), new GUIContent("Auto Update", "Automatically update the mesh if values or points are changed."));
            if (GUILayout.Button(new GUIContent("Update", "Manually Update the mesh."))) c.Execute(true);

            
            if (GUILayout.Button(new GUIContent("Export OBJ", "Export the mesh to an .obj file, this can be useful if you want to use some of unity's importer functionality or make changes in a 3rd party 3d program.")))
            {
                ExportOBJ();
                //if (Event.current.type == EventType.Layout) return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("tangents"), new GUIContent("Tangents", "Create Tangents for the mesh. Needed for normal mapping."));


            EditorGUILayout.PropertyField(serializedObject.FindProperty("meshGO"), new GUIContent("Mesh GameObject", "A reference to the GameObject that holds the resulting mesh."));
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);











            // EXTRUDERS
            EditorGUILayout.BeginVertical("HelpBox");

            Rect r = EditorGUILayout.GetControlRect(true, 0);
            GUILayout.Label(new GUIContent("Extruders", "Extruders are used to take a custom 2d profile and extrude it along the curve."), GUILayout.Width(160));
            r.y += 2; r.height = 14;
            c.showExtruders = EditorGUI.Toggle(r, c.showExtruders, GUIStyle.none);
            r.x = r.width + 10;
            EditorGUI.Toggle(r, c.showExtruders, "Foldout");

            if (c.showExtruders)
            {
                EditorGUI.indentLevel++;
                SerializedProperty prop = serializedObject.FindProperty("extruders");
                for (int i = 0; i < c.extruders.Length; i++)
                {
                    EditorGUILayout.BeginVertical("HelpBox");

                    Rect controlRect = EditorGUILayout.GetControlRect(true, 0);
                    controlRect.x = controlRect.width;
                    controlRect.y += 2;
                    controlRect.width = 60;
                    controlRect.height = 16;
                    EditorGUI.PropertyField(controlRect, prop.GetArrayElementAtIndex(i).FindPropertyRelative("active"), GUIContent.none);
                    controlRect.x -= 27;
                    GUI.Label(controlRect, new GUIContent("Active", "If disabled this extruder will be ignored in the mesh generation."));

                    controlRect.width = 23;
                    controlRect.x -= 50;
                    if (i > 0) if (GUI.Button(controlRect, new GUIContent("▲", "Move this element up in the list."))) ChangeOrder(-1);
                    controlRect.x += 25;
                    if (i < c.extruders.Length - 1) if (GUI.Button(controlRect, new GUIContent("▼", "Move this element down in the list."))) ChangeOrder(+1);

                    void ChangeOrder(int dir)
                    {
                        Undo.RegisterCompleteObjectUndo(c, "Change Extruder Order");
                        List<SAS_CurveTools.Extruder> extruders = new List<SAS_CurveTools.Extruder>(c.extruders);
                        SAS_CurveTools.Extruder element = c.extruders[i];
                        extruders.RemoveAt(i);
                        extruders.Insert(i + dir, element);
                        c.extruders = extruders.ToArray();
                    }

                    if (ProfileButton(prop.GetArrayElementAtIndex(i), ref c.extruders[i].profile)) return;

                    EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(i), true);

                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;

                if (GUILayout.Button(new GUIContent("Add", "Add a new extruder to the list.")))
                {
                    Undo.RegisterCompleteObjectUndo(c, "Add Extruder");
                    List<SAS_CurveTools.Extruder> extruders = new List<SAS_CurveTools.Extruder>(c.extruders);
                    extruders.Add(new SAS_CurveTools.Extruder());
                    c.extruders = extruders.ToArray();
                    return;
                }
            }
            EditorGUILayout.EndVertical();


            GUILayout.Space(5);






            // PILLARS
            EditorGUILayout.BeginVertical("HelpBox");

            r = EditorGUILayout.GetControlRect(true, 0);
            GUILayout.Label(new GUIContent("Pillars", "Pillars are used to extrude a profile orthogonal to the curve. E.g. like a pillar of a bridge."), GUILayout.Width(160));
            r.y += 2; r.height = 14;
            c.showPillars = EditorGUI.Toggle(r, c.showPillars, GUIStyle.none);
            r.x = r.width + 10;
            EditorGUI.Toggle(r, c.showPillars, "Foldout");

            if (c.showPillars)
            {
                EditorGUI.indentLevel++;
                SerializedProperty prop = serializedObject.FindProperty("pillars");
                for (int i = 0; i < c.pillars.Length; i++)
                {
                    EditorGUILayout.BeginVertical("HelpBox");

                    Rect controlRect = EditorGUILayout.GetControlRect(true, 0);
                    controlRect.x = controlRect.width;
                    controlRect.y += 2;
                    controlRect.width = 60;
                    controlRect.height = 16;
                    EditorGUI.PropertyField(controlRect, prop.GetArrayElementAtIndex(i).FindPropertyRelative("active"), GUIContent.none);
                    controlRect.x -= 27;
                    GUI.Label(controlRect, new GUIContent("Active", "If disabled this pillar will be ignored in the mesh generation."));

                    controlRect.width = 23;
                    controlRect.x -= 50;
                    if (i > 0) if (GUI.Button(controlRect, new GUIContent("▲", "Move this element up in the list."))) ChangeOrder(-1);
                    controlRect.x += 25;
                    if (i < c.pillars.Length - 1) if (GUI.Button(controlRect, new GUIContent("▼", "Move this element down in the list."))) ChangeOrder(+1);

                    void ChangeOrder(int dir)
                    {
                        Undo.RegisterCompleteObjectUndo(c, "Change Pillar Order");
                        List<SAS_CurveTools.Pillar> pillars = new List<SAS_CurveTools.Pillar>(c.pillars);
                        SAS_CurveTools.Pillar element = c.pillars[i];
                        pillars.RemoveAt(i);
                        pillars.Insert(i + dir, element);
                        c.pillars = pillars.ToArray();
                    }

                    if (ProfileButton(prop.GetArrayElementAtIndex(i), ref c.pillars[i].profile)) return;

                    EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(i), true);
                    EditorGUILayout.EndVertical();
                }
                EditorGUI.indentLevel--;

                if (GUILayout.Button(new GUIContent("Add", "Add a new pillar to the list.")))
                {
                    Undo.RegisterCompleteObjectUndo(c, "Add Pillar");
                    List<SAS_CurveTools.Pillar> pillars = new List<SAS_CurveTools.Pillar>(c.pillars);
                    pillars.Add(new SAS_CurveTools.Pillar());
                    c.pillars = pillars.ToArray();
                    return;
                }
            }
            EditorGUILayout.EndVertical();





            GUILayout.Space(5);




            // DISTRIBUTERS
            EditorGUILayout.BeginVertical("HelpBox");

            r = EditorGUILayout.GetControlRect(true, 0);
            GUILayout.Label(new GUIContent("Distributers", "Distributers are used to spawn meshes along the path according to some rules."), GUILayout.Width(160));
            r.y += 2; r.height = 14;
            c.showDistributers = EditorGUI.Toggle(r, c.showDistributers, GUIStyle.none);
            r.x = r.width + 10;
            EditorGUI.Toggle(r, c.showDistributers, "Foldout");

            if (c.showDistributers)
            {
                EditorGUI.indentLevel++;
                SerializedProperty prop = serializedObject.FindProperty("distributers");
                for (int i = 0; i < c.distributers.Length; i++)
                {
                    EditorGUILayout.BeginVertical("HelpBox");

                    Rect controlRect = EditorGUILayout.GetControlRect(true, 0);
                    controlRect.x = controlRect.width;
                    controlRect.y += 2;
                    controlRect.width = 60;
                    controlRect.height = 16;
                    EditorGUI.PropertyField(controlRect, prop.GetArrayElementAtIndex(i).FindPropertyRelative("active"), GUIContent.none);
                    controlRect.x -= 27;
                    GUI.Label(controlRect, new GUIContent("Active", "If disabled this distributer will be ignored in the mesh generation."));

                    controlRect.width = 23;
                    controlRect.x -= 50;
                    if (i > 0) if (GUI.Button(controlRect, new GUIContent("▲", "Move this element up in the list."))) ChangeOrder(-1);
                    controlRect.x += 25;
                    if (i < c.distributers.Length - 1) if (GUI.Button(controlRect, new GUIContent("▼", "Move this element down in the list."))) ChangeOrder(+1);

                    void ChangeOrder(int dir)
                    {
                        Undo.RegisterCompleteObjectUndo(c, "Change Distributer Order");
                        List<SAS_CurveTools.Distributer> distributers = new List<SAS_CurveTools.Distributer>(c.distributers);
                        SAS_CurveTools.Distributer element = c.distributers[i];
                        distributers.RemoveAt(i);
                        distributers.Insert(i + dir, element);
                        c.distributers = distributers.ToArray();
                    }

                    EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(i), true);
                    EditorGUILayout.EndVertical();

                }
                EditorGUI.indentLevel--;

                if (GUILayout.Button(new GUIContent("Add", "Add a new distributer to the list.")))
                {
                    Undo.RegisterCompleteObjectUndo(c, "Add Distributer");
                    List<SAS_CurveTools.Distributer> distributers = new List<SAS_CurveTools.Distributer>(c.distributers);
                    distributers.Add(new SAS_CurveTools.Distributer());
                    c.distributers = distributers.ToArray();
                    return;
                }
            }
            EditorGUILayout.EndVertical();



            EditorGUILayout.EndVertical();
            GUILayout.Space(10);





            // Instantiators ================================================================================================
            GUI.backgroundColor = Color.cyan;
            EditorGUILayout.BeginVertical("HelpBox");
            GUI.backgroundColor = Color.white;
            GUILayout.Label(new GUIContent("Instantiation", "Everything related to object instantiation."), style);

            EditorGUILayout.BeginHorizontal();
            if (c.instantiators != null && c.instantiators.Length != 0)
            {
                if (GUILayout.Button(new GUIContent("Instantiate", "Instantiates or refreshes all game objects that are specified in the instatiators below."))) c.Spawn();
                if (GUILayout.Button(new GUIContent("Clear", "Deletes all instantiated game objects."))) c.Clear();
            }
            else
            {
                Color origColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.2f);
                GUILayout.Button("Instantiate");
                GUILayout.Button("Clear");
                GUI.color = origColor;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);


            EditorGUILayout.BeginVertical("HelpBox");

            r = EditorGUILayout.GetControlRect(true, 0);
            GUILayout.Label(new GUIContent("Instantiators", "Instantiators are similar to the distributers from the meshing section, however they are different in the sence that the objects will not be combined into one big mesh but will remain as their own game objects. That means they still have their functionality. Simple usecases would be spawning of lights or particle effects along a path."), GUILayout.Width(160));
            r.y += 2; r.height = 14;
            c.showInstantiators = EditorGUI.Toggle(r, c.showInstantiators, GUIStyle.none);
            r.x = r.width + 10;
            EditorGUI.Toggle(r, c.showInstantiators, "Foldout");

            if (c.showInstantiators)
            {
                EditorGUI.indentLevel++;
                SerializedProperty prop = serializedObject.FindProperty("instantiators");
                for (int i = 0; i < c.instantiators.Length; i++)
                {
                    EditorGUILayout.BeginVertical("HelpBox");

                    Rect controlRect = EditorGUILayout.GetControlRect(true, 0);
                    controlRect.x = controlRect.width;
                    controlRect.y += 2;
                    controlRect.width = 60;
                    controlRect.height = 16;
                    EditorGUI.PropertyField(controlRect, prop.GetArrayElementAtIndex(i).FindPropertyRelative("active"), GUIContent.none);
                    controlRect.x -= 27;
                    GUI.Label(controlRect, new GUIContent("Active", "If disabled this instantiator will be ignored."));

                    controlRect.width = 23;
                    controlRect.x -= 50;
                    if (i > 0) if (GUI.Button(controlRect, new GUIContent("▲", "Move this element up in the list."))) ChangeOrder(-1);
                    controlRect.x += 25;
                    if (i < c.instantiators.Length - 1) if (GUI.Button(controlRect, new GUIContent("▼", "Move this element down in the list."))) ChangeOrder(+1);

                    void ChangeOrder(int dir)
                    {
                        Undo.RegisterCompleteObjectUndo(c, "Change Instantiator Order");
                        List<SAS_CurveTools.Instantiator> instantiators = new List<SAS_CurveTools.Instantiator>(c.instantiators);
                        SAS_CurveTools.Instantiator element = c.instantiators[i];
                        instantiators.RemoveAt(i);
                        instantiators.Insert(i + dir, element);
                        c.instantiators = instantiators.ToArray();
                    }

                    EditorGUILayout.PropertyField(prop.GetArrayElementAtIndex(i), true);
                    EditorGUILayout.EndVertical();

                }
                EditorGUI.indentLevel--;

                if (GUILayout.Button(new GUIContent("Add", "Add a new instantiator to the list.")))
                {
                    Undo.RegisterCompleteObjectUndo(c, "Add Instantiator");
                    List<SAS_CurveTools.Instantiator> instantiators = new List<SAS_CurveTools.Instantiator>(c.instantiators);
                    instantiators.Add(new SAS_CurveTools.Instantiator());
                    c.instantiators = instantiators.ToArray();
                    return;
                }
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                c.CalculateLength();
                if (c.autoUpdate) c.Execute();
            }
            

            //DrawDefaultInspector(); 
        } //================================================================================================================================================



        bool ProfileButton(SerializedProperty p, ref SAS_Profile profile)
        {
            if (p.isExpanded)
            {
                Rect newRect = EditorGUILayout.GetControlRect(true, 0f);
                GUILayout.Space(-2);
                newRect.y += 40;
                newRect.height = 18;
                newRect.x += 70 + 36;
                newRect.width = 40;

                if (profile == null)
                {
                    if (GUI.Button(newRect, new GUIContent("New", "Create a new extrusion profile")))
                    {
                        var asset = CreateInstance<SAS_Profile>();
                        MonoScript script = MonoScript.FromScriptableObject(this);
                        string path = AssetDatabase.GetAssetPath(script);
                        path = path.Replace("Scripts/Editor/SAS_CurveToolsEditor.cs", "Profiles");
                        string absolutepath = EditorUtility.SaveFilePanel("Create a new profile...", path, "Profile", "asset");
                        if (absolutepath.Length > 0)
                        {
                            string relativepath = "Assets" + absolutepath.Substring(Application.dataPath.Length);
                            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(relativepath);
                            AssetDatabase.CreateAsset(asset, assetPathAndName);
                            AssetDatabase.SaveAssets();
                            profile = asset;
                            EditorWindow.GetWindow<SAS_ProfileCreator>().ChooseProfile(profile);
                            return true;
                        }
                    }
                }
                else if (EditorWindow.HasOpenInstances<SAS_ProfileCreator>())
                {
                    if (GUI.Button(newRect, new GUIContent("Done", "Close the profile editor and update the mesh.")))
                    {
                        EditorWindow.GetWindow<SAS_ProfileCreator>().Close();
                        c.Execute(true);
                        return true;
                    }
                }
                else if (GUI.Button(newRect, new GUIContent("Edit", "Opens the profile editor.")))
                {
                    EditorWindow.GetWindow<SAS_ProfileCreator>().ChooseProfile(profile);
                    return true;
                }
            }
            return false;
        } //================================================================================================================================================




        void SetNames()
        {
            if (c.extruders != null)
            {
                for (int i = 0; i < c.extruders.Length; i++)
                {
                    SAS_CurveTools.Extruder d = c.extruders[i];
                    d.name = (i + 1).ToString() + " ● ";
                    if (d.overrideName != "") d.name += d.overrideName + " ";
                    if (d.profile == null) { d.name += "(Unassigned Profile)"; continue; }
                    if (d.overrideName == "") d.name += d.profile.name;
                }
            }

            if (c.pillars != null)
            {
                for (int i = 0; i < c.pillars.Length; i++)
                {
                    SAS_CurveTools.Pillar d = c.pillars[i];
                    d.name = (i + 1).ToString() + " ● ";
                    if (d.overrideName != "") d.name += d.overrideName + " ";
                    if (d.profile == null) { d.name += "(Unassigned Profile)"; continue; }
                    if (d.overrideName == "") d.name += d.profile.name;
                }
            }

            if (c.distributers != null)
            {
                for (int i = 0; i < c.distributers.Length; i++)
                {
                    SAS_CurveTools.Distributer d = c.distributers[i];

                    d.name = (i + 1).ToString() + " ● ";
                    if (d.overrideName != "") d.name += d.overrideName + " ";

                    bool missing = false;
                    for (int j = 0; j < d.meshes.Length; j++) { if (d.meshes[j] == null) { missing = true; break; } }

                    if (missing || d.meshes.Length == 0) { d.name += "(Unassigned Meshes)"; continue; }

                    if (d.meshes.Length == 1 && d.overrideName == "") d.name += d.meshes[0].name;
                    if (d.meshes.Length > 1) { d.name += "(Multi Mesh)"; continue; }
                }
            }

            if (c.instantiators != null)
            {
                for (int i = 0; i < c.instantiators.Length; i++)
                {
                    SAS_CurveTools.Instantiator d = c.instantiators[i];

                    d.name = (i + 1).ToString() + " ● ";
                    if (d.overrideName != "") d.name += d.overrideName + " ";

                    bool missing = false;
                    for (int j = 0; j < d.gameObjects.Length; j++) { if (d.gameObjects[j] == null) { missing = true; break; } }

                    if (missing || d.gameObjects.Length == 0) { d.name += "(Unassigned Objects)"; continue; }

                    if (d.gameObjects.Length == 1 && d.overrideName == "") d.name += d.gameObjects[0].name;
                    if (d.gameObjects.Length > 1) { d.name += "(Multi Object)"; continue; }
                }
            }
        } //================================================================================================================================================















        float minDist;
        private void OnSceneGUI()
        {
            var sceneView = SceneView.currentDrawingSceneView;
            var sceneCam = sceneView.camera;

            c = target as SAS_CurveTools;
            if (c == null) return;


            if (!c.editable)
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

                Color color = Color.white;
                color.a = 0.125f;
                for (int i = 0; i < c.points.Count - 1; i++) SAS_CurveTools_Helper.DrawCurve(c.points[i], c.points[i + 1], color, c.transform);
                if (c.loop) SAS_CurveTools_Helper.DrawCurve(c.points[c.points.Count - 1], c.points[0], color, c.transform);

            }
            else
            {
                Events();

                int segmentIndex = -1;
                if (!IsMouseOnHandle()) segmentIndex = InsertPoint();


                if (!c.zTest) Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

                if (c.points.Count > 1)
                {
                    // Draw Curve
                    for (int i = 0; i < c.points.Count - 1; i++) if (i != segmentIndex) SAS_CurveTools_Helper.DrawCurve(c.points[i], c.points[i + 1], Color.white, c.transform);
                    if (c.loop) if (segmentIndex != c.points.Count - 1) SAS_CurveTools_Helper.DrawCurve(c.points[c.points.Count - 1], c.points[0], Color.white, c.transform);
                }
                for (int i = 0; i < c.points.Count; i++) SelectPoint(i);


                Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
                Handles.color = Color.white;
                if (c.selectedIndex != -1 && !Event.current.shift && !(Event.current.control || Event.current.command))
                {
                    if (Tools.current == Tool.Move) MovePoint(c.selectedIndex);
                    if (Tools.current == Tool.Rotate) RotatePoint(c.selectedIndex);
                    if (Tools.current == Tool.Scale) ScalePoint(c.selectedIndex);
                }
                //SceneView.RepaintAll();
                Selection.activeGameObject = c.gameObject;
            }




            if (Event.current.keyCode == KeyCode.F)
            {
                if (Event.current.type != EventType.Layout) 
                    Event.current.Use();

                Bounds bounds = new Bounds();

                Vector3 min, max;
                min.x = min.y = min.z = Mathf.Infinity;
                max.x = max.y = max.z = Mathf.NegativeInfinity;

                for (int i = 0; i < c.points.Count; i++)
                {
                    Vector3 pos = c.points[i].position;
                    //bounds.Encapsulate(c.points[i].position);
                    min.x = Mathf.Min(min.x, pos.x);
                    min.y = Mathf.Min(min.y, pos.y);
                    min.z = Mathf.Min(min.z, pos.z);
                    max.x = Mathf.Max(max.x, pos.x);
                    max.y = Mathf.Max(max.y, pos.y);
                    max.z = Mathf.Max(max.z, pos.z);
                }
                Vector3 center = (min + max) / 2.0f;
                Vector3 size = (max - min);
                bounds.center = center;
                bounds.size = size;


                SceneView.lastActiveSceneView.Frame(bounds, false);
            }

            DrawUI();
            SceneView.RepaintAll();
        } //================================================================================================================================================




        void DrawUI()
        {
            Handles.BeginGUI();
            var sceneCam = SceneView.currentDrawingSceneView.camera;
            float screenHeight = sceneCam.pixelHeight / EditorGUIUtility.pixelsPerPoint;
            float screenWidth  = sceneCam.pixelWidth  / EditorGUIUtility.pixelsPerPoint;

            Color boxColor = new Color(0.2f, 0.2f, 0.2f, 0.999f);
            GUIStyle tipStyle = new GUIStyle("Label");
            tipStyle.normal.textColor = Color.grey;
            tipStyle.hover.textColor = Color.grey;

            float sizeX = 550;
            float sizeY = 22;
            float padding = 4;
            float edgePadding = 5;
            float spacing = 8;
            float posX = edgePadding; //screenWidth - sizeX - edgePadding;
            float posY = screenHeight - sizeY - edgePadding;

            Rect r = new Rect(posX, posY, sizeX, sizeY);
            DrawBox(r, boxColor);

            r.x += padding;
            r.y += padding;
            r.height -= 2 * padding;
            r.width = 45;

            GUI.color = c.editable ? new Color(0.9f, 0.3f, 0.25f, 1f) : new Color(0.3f, 0.8f, 0.25f, 1f);
            if (GUI.Button(r, c.editable ? "STOP" : "EDIT"))
            {
                Undo.RecordObject(c, c.editable ? "Stop editing Path" : "Start editing Path");
                c.editable = !c.editable;
            }
            GUI.color = Color.white;
            r.height += 2; r.y -= 1;

            r.x += r.width + spacing; r.width = 16;
            c.zTest = GUI.Toggle(r, c.zTest, GUIContent.none);

            r.x += r.width; r.width = 79;
            GUI.Label(r, new GUIContent("Draw On Top", "If enable the path will be drawn on top of the geomertry in  the scene. Note: some image FX can interfere with this behaviour."));

            r.x += r.width + spacing; r.width = 16;
            c.showLabels = GUI.Toggle(r, c.showLabels, GUIContent.none);

            r.x += r.width; r.width = 92;
            GUI.Label(r, new GUIContent("Show Point IDs", "Shows/Hides the point ID labels."));

            r.x += r.width + 20; r.width = 300;
            GUI.Label(r, "(TIP: Add/Delete Points = Shift/Control Click)", tipStyle);

            Handles.EndGUI();
        } //================================================================================================================================================

        void DrawBox(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        } //================================================================================================================================================










        int InsertPoint()
        {
            int segmentIndex = -1;
            if (Event.current.shift && Event.current.button != 1)
            {
                Vector2 mousePos = Event.current.mousePosition;
                minDist = Mathf.Infinity;
                int polyLineIndex = -1;
                for (int i = 0; i < c.polyLine.Length - 1; i++)
                {
                    Vector2 p1 = HandleUtility.WorldToGUIPoint(c.polyLine[i    ]);
                    Vector2 p2 = HandleUtility.WorldToGUIPoint(c.polyLine[i + 1]);

                    float dst = HandleUtility.DistancePointToLineSegment(mousePos, p1, p2);

                    if (dst < minDist)
                    {
                        minDist = dst;
                        polyLineIndex = i;
                    }
                }
                if (minDist < 15f && c.points.Count > 1)
                {

                    Vector2 pointA = HandleUtility.WorldToGUIPoint(c.polyLine[polyLineIndex    ]);
                    Vector2 pointB = HandleUtility.WorldToGUIPoint(c.polyLine[polyLineIndex + 1]);

                    Vector2 closestPointOnLine = ClosestPointOnLineSegment(mousePos, pointA, pointB);
                    float dstToPointOnLine = (pointA - closestPointOnLine).magnitude;
                    float percentBetweenVertices = dstToPointOnLine / (pointA - pointB).magnitude;

                    Vector4 closestPoint = Vector4.Lerp(c.polyLine[polyLineIndex], c.polyLine[polyLineIndex + 1], percentBetweenVertices);
                    segmentIndex = c.GetIndex(closestPoint.w);// Mathf.FloorToInt(polyLineIndex / 10.0f);//c.GetIndex(distanceOnPath);

                    // Draw the curve we want to insert to 
                    SAS_CurveTools_Helper.Point p1 = c.points[segmentIndex];
                    SAS_CurveTools_Helper.Point p2 = (segmentIndex + 1 == c.points.Count) ? c.points[0] : c.points[segmentIndex + 1];
                    SAS_CurveTools_Helper.DrawCurve(p1, p2, Color.green);

                    Vector3 newPos = c.GetPointSimple(closestPoint.w);
                    float handleSize = HandleUtility.GetHandleSize(newPos) * 0.15f;
                    Handles.color = Color.green;
                    Handles.SphereHandleCap(0, newPos, Quaternion.identity, handleSize, EventType.Repaint);

                    if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                    {
                        Undo.RecordObject(c, "Insert Point");
                        c.InsertPoint(closestPoint.w);
                        OnPathChanged();
                    }

                    Vector2 ClosestPointOnLineSegment(Vector2 p, Vector2 a, Vector2 b)
                    {
                        Vector2 aB = b - a;
                        Vector2 aP = p - a;
                        float sqrLenAB = aB.sqrMagnitude;
                        if (sqrLenAB == 0) return a;
                        return a + aB * (Mathf.Clamp01(Vector2.Dot(aP, aB) / sqrLenAB));
                    }
                }
                else
                {
                    SAS_CurveTools_Helper.Point lastPoint = c.points[c.points.Count - 1];
                    Vector3 lastPos = lastPoint.position;
                    Vector3 lastUp = lastPoint.rotation * Vector3.up;
                    Vector3 lastForward = lastPoint.rotation * Vector3.forward;

                    Plane plane = new Plane(lastUp, lastPos);
                    Ray camRay = HandleUtility.GUIPointToWorldRay(mousePos);
                    plane.Raycast(camRay, out float hitDistance);

                    Vector3 newPos = camRay.origin + camRay.direction * hitDistance;

                    Vector3 vPos = Camera.current.WorldToViewportPoint(newPos);
                    if (vPos.x < 0 || vPos.x > 1 || vPos.y < 0 || vPos.y > 1 || vPos.z < 0) return - 1;


                    // Last Pos To New Pos
                    Vector3 dirLastPosToNewPos = lastPos - newPos;
                    float distLastPosToNewPos = dirLastPosToNewPos.magnitude;
                    dirLastPosToNewPos = dirLastPosToNewPos.normalized;

                    float outTangentLength = distLastPosToNewPos * 0.5f;
                    Vector3 outTangentPos = lastPos + lastForward * outTangentLength;

                    // New Pos To Out Tangent Pos
                    Vector3 dirNewPosToOutTangentPos = outTangentPos - newPos;
                    float distNewPosToOutTangent = dirNewPosToOutTangentPos.magnitude;
                    dirNewPosToOutTangentPos = dirNewPosToOutTangentPos.normalized;

                    float inTangentLength = distNewPosToOutTangent * 0.5f;
                    Vector3 inTangentPos = newPos + dirNewPosToOutTangentPos * inTangentLength;

                    Quaternion newRot = Quaternion.LookRotation(-dirNewPosToOutTangentPos, lastUp);


                    float newOutTangentLength = 0;
                    float firstInTangentLength = 0;
                    
                    SAS_CurveTools_Helper.Point firstPoint = c.points[0];
                    Vector3 firstPos = firstPoint.position;

                    // New Pos To First Pos
                    Vector3 dirNewPosToFirstPos = newPos - firstPos;
                    float distNewPosToFirstPos = dirNewPosToFirstPos.magnitude;
                    dirNewPosToFirstPos = dirNewPosToFirstPos.normalized;

                    newOutTangentLength = distNewPosToFirstPos * 0.5f;
                    Vector3 newOutTangentPos = newPos + (newRot * Vector3.forward) * outTangentLength;

                    // First Pos To New Out Tangent Pos
                    Vector3 dirFirstPosToNewOutTangentPos = newOutTangentPos - firstPos;
                    float distFirstPosToNewOutTangentPos = dirFirstPosToNewOutTangentPos.magnitude;
                    dirFirstPosToNewOutTangentPos = dirFirstPosToNewOutTangentPos.normalized;

                    firstInTangentLength = distFirstPosToNewOutTangentPos * 0.5f;
                    Vector3 firstInTangentPos = firstPos + (firstPoint.rotation * -Vector3.forward) * firstInTangentLength;

                    if (c.loop)
                    {
                        Color color = Color.green;
                        color.a = 0.1f;
                        Handles.DrawBezier(newPos, firstPos, newOutTangentPos, firstInTangentPos, color, null, 3);
                    }










                    // Draw Path Preview
                    Handles.color = Color.green;
                    Handles.DrawBezier(lastPos, newPos, outTangentPos, inTangentPos, Color.green, null, 3);
                    Handles.SphereHandleCap(0, newPos, Quaternion.identity, HandleUtility.GetHandleSize(newPos) * 0.15f, EventType.Repaint);




                    if (Event.current.button == 0 && Event.current.type == EventType.MouseDown)
                    {
                        Undo.RecordObject(c, "Append Point");

                        lastPoint.tangentOut = outTangentLength;

                        c.points[0].tangentIn = firstInTangentLength;


                        SAS_CurveTools_Helper.Point newPoint = new SAS_CurveTools_Helper.Point
                        {
                            position = newPos,
                            rotation = newRot,
                            tangentIn = inTangentLength,
                            tangentOut = newOutTangentLength
                        };
                        c.points.Add(newPoint);
                        c.selectedIndex = c.points.Count - 1;
                        OnPathChanged();
                    }
                }
            }
            return segmentIndex;
        } //================================================================================================================================================



        void Label(int index)
        {
            SAS_CurveTools_Helper.Point p = c.points[index];
            t.localPosition = p.position;
            Vector3 position = t.position;

            GUIContent content = new GUIContent(index.ToString("N0"));
            GUIStyle style = new GUIStyle();

            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            float offset = -style.CalcHeight(content, 100) * 0.5f;

            Vector2 contentOffset = new Vector2(-1, offset + 2);

            style.normal.textColor = new Color(0, 0, 0, 0.5f);
            style.contentOffset = contentOffset + new Vector2(2, 2);
            Handles.Label(position, content, style);


            style.normal.textColor = new Color(0, 0, 0, 0.25f);//Color.black;

            style.contentOffset = contentOffset + new Vector2(0, 0);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(1, 0);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(-1, 0);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(0, 1);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(1, 1);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(-1, 1);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(0, -1);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(1, -1);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(-1, -1);
            Handles.Label(position, content, style);

            style.normal.textColor = new Color(0, 0, 0, 0.125f);//Color.black;

            style.contentOffset = contentOffset + new Vector2(2, 0);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(-2, 0);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(0, 2);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(2, 2);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(-2, 2);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(0, -2);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(2, -2);
            Handles.Label(position, content, style);
            style.contentOffset = contentOffset + new Vector2(-2, -2);
            Handles.Label(position, content, style);

            style.contentOffset = contentOffset;
            style.normal.textColor = Color.white;
            Handles.Label(position, content, style);
        } //================================================================================================================================================



        bool mouseUp;

        void Events()
        {
            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            EventType eventType = e.GetTypeForControl(controlID);

            mouseUp = false;
            if (eventType == EventType.MouseUp) { mouseUp = true; }
        } //================================================================================================================================================





        private bool IsMouseOnHandle()
        {
            for (int i = 0; i < c.points.Count; i++)
            {
                t.localPosition = c.points[i].position;
                float handleSize = HandleUtility.GetHandleSize(t.position) * 0.33f;
                if (HandleUtility.DistanceToCircle(t.position, handleSize * 0.5f) == 0) return true;
            }
            return false;
        } //================================================================================================================================================



        private void SelectPoint(int index)
        {
            SAS_CurveTools_Helper.Point p = c.points[index];
            t.localPosition = p.position;
            t.localRotation = p.rotation;

            float handleSize = HandleUtility.GetHandleSize(t.position) * 0.33f;

            if (index == c.selectedIndex) Handles.color = Color.green;
            else Handles.color = Color.white;

            if (HandleUtility.DistanceToCircle(t.position, handleSize * 0.5f) == 0)
            {
                if (mouseUp)
                {
                    if (Event.current.control || Event.current.command)
                    {
                        if (c.points.Count == 1) return;
                        Undo.RecordObject(c, "Delete Point " + index.ToString("N0"));
                        c.points.RemoveAt(index);
                        if (index <= c.selectedIndex) c.selectedIndex -= 1;
                        OnPathChanged();
                        return;
                    }
                    else
                    {
                        Undo.RecordObject(c, "Select Point " + index.ToString("N0"));
                        c.selectedIndex = index;
                    }
                }
            }
            if (index == c.selectedIndex) Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            else if (!c.zTest) Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            if (Tools.current != Tool.Move && Tools.current != Tool.Rotate && Tools.current != Tool.Scale)
            {
                EditorGUI.BeginChangeCheck();
                t.position = Handles.FreeMoveHandle(t.position, handleSize, Vector3.zero, CustomHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(c, "Move Point " + index.ToString("N0")); 
                    p.position = t.localPosition;
                    OnPathChanged();
                }
            }
            else { Handles.FreeMoveHandle(t.position, handleSize, Vector3.zero, CustomHandleCap); }

            if (c.showLabels) Label(index);
        } //================================================================================================================================================


        static void CustomHandleCap(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Repaint) Handles.SphereHandleCap(controlId, position, rotation, size, eventType);//Handles.SphereCap(controlId, position, rotation, size); //Handles.DrawSphere(controlId, position, rotation, size);
            if (eventType == EventType.Layout) HandleUtility.AddControl(controlId, HandleUtility.DistanceToCircle(position, size * 0.5f));
        } //================================================================================================================================================

        private void MovePoint(int index)
        {
            SAS_CurveTools_Helper.Point p = c.points[index];
            t.localPosition = p.position;
            t.localRotation = p.rotation;

            EditorGUI.BeginChangeCheck();
            if (Tools.pivotRotation == PivotRotation.Local) t.position = Handles.PositionHandle(t.position, t.rotation);
            if (Tools.pivotRotation == PivotRotation.Global) t.position = Handles.PositionHandle(t.position, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Move Point " + index.ToString("N0"));
                p.position = t.localPosition;
                OnPathChanged();
            }
        } //================================================================================================================================================


        Quaternion startRot;
        private void RotatePoint(int index)
        {
            SAS_CurveTools_Helper.Point p = c.points[index];
            t.localPosition = p.position;
            t.localRotation = p.rotation;
            if (Tools.pivotRotation == PivotRotation.Global && Event.current.type == EventType.MouseDown) startRot = p.rotation;
            

            float size = HandleUtility.GetHandleSize(t.position);
            Color col = Color.blue; col.a = 0.5f; Handles.color = col;
            Handles.ArrowHandleCap(-1, t.position, t.rotation, size * 1.5f, EventType.Repaint);
            col = Color.green; col.a = 0.5f; Handles.color = col;
            Handles.ArrowHandleCap(-1, t.position, t.rotation * Quaternion.Euler(270, 0, 0), size * 0.75f, EventType.Repaint);
            col = Color.red; col.a = 0.5f; Handles.color = col;
            Handles.ArrowHandleCap(-1, t.position, t.rotation * Quaternion.Euler(0, 90, 0), size * 0.75f, EventType.Repaint);

            
            EditorGUI.BeginChangeCheck();
            if (Tools.pivotRotation == PivotRotation.Local) t.rotation = Handles.RotationHandle(t.rotation, t.position);
            if (Tools.pivotRotation == PivotRotation.Global) t.rotation = Handles.RotationHandle(Quaternion.identity, t.position);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Rotate Point " + index.ToString("N0"));                
                p.rotation = t.localRotation;
                if (Tools.pivotRotation == PivotRotation.Global) p.rotation *= startRot;
                OnPathChanged();
            }
        } //================================================================================================================================================


        private void ScalePoint(int index)
        {
            SAS_CurveTools_Helper.Point p = c.points[index];
            t.localPosition = p.position;
            t.localRotation = p.rotation;

            

            // TANGENT IN
            if (index != 0 || c.loop)
            {
                Handles.DrawAAPolyLine(2, new Vector3[] { t.position, t.position - t.forward * p.tangentIn });

                float handleSize = HandleUtility.GetHandleSize(t.position - t.forward * p.tangentIn) * 0.33f * 0.66f;

                EditorGUI.BeginChangeCheck();
                Vector3 pos = Handles.FreeMoveHandle(t.position - t.forward * p.tangentIn, handleSize, Vector3.zero, Handles.SphereHandleCap);
                pos = Vector3.Project(pos - t.position, t.forward);
                float value = t.InverseTransformDirection(pos).z < -0.1f ? pos.magnitude : 0.1f;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(c, "Modify Tangent");
                    p.tangentIn = value;
                    OnPathChanged();
                }
            }

            // TANGENT OUT
            if (index != c.points.Count - 1 || c.loop)
            {
                Handles.DrawAAPolyLine(2, new Vector3[] { t.position, t.position + t.forward * p.tangentOut });

                float handleSize = HandleUtility.GetHandleSize(t.position + t.forward * p.tangentOut) * 0.33f * 0.66f;

                EditorGUI.BeginChangeCheck();
                Vector3 pos = Handles.FreeMoveHandle(t.position + t.forward * p.tangentOut, handleSize, Vector3.zero, Handles.SphereHandleCap);
                pos = Vector3.Project(pos - t.position, t.forward);
                float value = t.InverseTransformDirection(pos).z > 0.1f ? pos.magnitude : 0.1f;

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(c, "Modify Tangent");
                    p.tangentOut = value;
                    OnPathChanged();
                }
            }
        } //================================================================================================================================================


        void OnPathChanged()
        {
            c.CalculateLength();
            if (c.autoUpdate) c.Execute();
        } //================================================================================================================================================

        void ExportOBJ()
        {
            string path = EditorUtility.SaveFilePanel("Save Mesh as OBJ", "", "mesh.obj", "obj");

            if (path.Length != 0)
            {
                MeshFilter mf = c.meshGO.GetComponent<MeshFilter>();
                MeshRenderer mr = c.meshGO.GetComponent<MeshRenderer>();
                Mesh m = mf.sharedMesh;
                Material[] mats = mr.sharedMaterials;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US", false);

                sb.Append("g ").Append(mf.name).Append("\n");
                foreach (Vector3 v in m.vertices)
                {
                    sb.Append(string.Format("v {0} {1} {2}\n", -v.x, v.y, v.z));
                }
                sb.Append("\n");
                foreach (Vector3 v in m.normals)
                {
                    sb.Append(string.Format("vn {0} {1} {2}\n", -v.x, v.y, v.z));
                }
                sb.Append("\n");
                foreach (Vector3 v in m.uv)
                {
                    sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
                }
                for (int material = 0; material < m.subMeshCount; material++)
                {
                    sb.Append("\n");
                    sb.Append("usemtl ").Append(mats[material].name).Append("\n");
                    sb.Append("usemap ").Append(mats[material].name).Append("\n");

                    int[] triangles = m.GetTriangles(material);
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n",
                            triangles[i + 2] + 1, triangles[i + 1] + 1, triangles[i + 0] + 1));
                    }
                }


                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                {
                    sw.Write(sb.ToString());
                }
                AssetDatabase.Refresh();
            }
        }
    }
}