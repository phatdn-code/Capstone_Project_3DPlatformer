using UnityEngine;

#if UNITY_EDITOR
namespace SwissArmySpline
{
    public class SAS_SetMeshPivot : MonoBehaviour
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public bool updateTransform = true;
        public bool editMode = false;

        public void Execute()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null) return;
            Mesh m = mf.sharedMesh;
            if (m == null) return;

            Vector3[] vertices = m.vertices;
            Vector3[] normals = m.normals;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];

                vertex -= position;
                vertex = Quaternion.Inverse(Quaternion.Euler(rotation)) * vertex;
                vertex.x *= 1 / scale.x;
                vertex.y *= 1 / scale.y;
                vertex.z *= 1 / scale.z;

                vertices[i] = vertex;
                normals[i] = Quaternion.Inverse(Quaternion.Euler(rotation)) * normals[i];
            }
            m.vertices = vertices;
            m.normals = normals;

            if (m.tangents != null && m.tangents.Length != 0)
            {
                m.RecalculateTangents();
            }
            m.RecalculateBounds();

            if (updateTransform)
            {
                transform.position = position;
                transform.rotation = Quaternion.Euler(rotation);
                transform.localScale = scale;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawSphere(position, 0.125f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(position, Quaternion.Euler(rotation) * Vector3.forward);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(position, Quaternion.Euler(rotation) * Vector3.right);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, Quaternion.Euler(rotation) * Vector3.up);
            Gizmos.matrix = Matrix4x4.TRS(position, Quaternion.Euler(rotation), scale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
#endif
