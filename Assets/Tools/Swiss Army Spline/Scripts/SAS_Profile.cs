using System.Collections.Generic;
using UnityEngine;

namespace SwissArmySpline
{
    [CreateAssetMenu(fileName = "Profile", menuName = "Swiss Army Spline/New Profile", order = 1)]
    public class SAS_Profile : ScriptableObject
    {
        [HideInInspector] public List<Profile> profiles = new List<Profile>();

        [HideInInspector] public int selectedProfile = 0;
        [HideInInspector] public bool snap = true;
        [HideInInspector] public bool showLabels = true;
        [HideInInspector] public bool showPivot = true;
        [HideInInspector] public bool autoUpdateNormals = true;
        [HideInInspector] public bool normalizeUVs = true;


        [System.Serializable]
        public class Profile
        {
            public List<Vector4> vertices = new List<Vector4>();
            public bool close;
            public bool uvIsU = true; // Horizontal vs Vertical in UV Space
            public float profileLength;
            public float loopUV;

            public List<int> selection = new List<int>();
            public Vector2 pivot;
            public int submeshIndex = 0;

            public void Clone(Profile source)
            {
                vertices = new List<Vector4>();
                for (int i = 0; i < source.vertices.Count; i++)
                {
                    Vector4 v = new Vector4();
                    v.x = source.vertices[i].x;
                    v.y = source.vertices[i].y;
                    v.z = source.vertices[i].z;
                    v.w = source.vertices[i].w;
                    vertices.Add(v);
                }

                selection = new List<int>();
                for (int i = 0; i < source.selection.Count; i++)
                {
                    selection.Add(source.selection[i]);
                }

                source.selection.Clear();

                close = source.close;
                uvIsU = source.uvIsU;
                profileLength = source.profileLength;
                loopUV = source.loopUV;
                pivot = source.pivot;
                submeshIndex = source.submeshIndex;
            }
        }
    }
}
