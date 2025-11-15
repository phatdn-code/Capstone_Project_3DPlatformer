using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.VFX;
using System;

namespace INab.CommonVFX
{
    [Serializable]
    public class VFXUniformMeshBaker
    {
        public static string GraphicsBufferName = "UniformMeshBuffer";
        public int SampleCount
        {
            get => Mathf.Min((int)(_sampleCount * Mathf.Pow(SampleCountMultiplier, 2)), 100000);
        }

        [Tooltip("Amount of points from which particles would be spawned.")]
        [SerializeField]
        private int _sampleCount = 2048;

        [Range(0.01f, 10f), SerializeField]
        [Tooltip("Multiply sample count by this value to control density of the particles. Keep this as low as possible.")]
        public float SampleCountMultiplier = 1f;

        [SerializeField]
        private TriangleSampling[] m_BakedSampling;

        private GraphicsBuffer m_Buffer;

        // New Freature - baking buffer per submesh.
        [SerializeField] public bool UsePerSubmeshBaking = false;
        [SerializeField] public int SubmeshIndex = 0; 

        private void ComputeBakedSampling(VisualEffect visualEffect, Mesh mesh)
        {
            if (visualEffect == null)
            {
                Debug.LogWarning("UniformBaker expects a VisualEffect on the shared game object.");
                return;
            }

            if (!visualEffect.HasGraphicsBuffer(GraphicsBufferName))
            {
                //Debug.LogWarningFormat("Graphics Buffer property '{0}' is invalid.", GraphicsBufferName);
                return;
            }

            var meshData = VFXMeshSamplingHelper.ComputeDataCache(mesh, UsePerSubmeshBaking, SubmeshIndex);

            if (UsePerSubmeshBaking)
            {
                var submesh = mesh.GetSubMesh(SubmeshIndex);
                _sampleCount = submesh.indexCount / 3;
                if(visualEffect.HasUInt("Start Triangle Index")) visualEffect.SetUInt("Start Triangle Index", (uint)submesh.indexStart / 3);
            }
            else
            { 
                _sampleCount = meshData.triangles.Length;
                if (visualEffect.HasUInt("Start Triangle Index")) visualEffect.SetUInt("Start Triangle Index", 0);
            }

            var rand = new System.Random(123); // use random number as seed
            m_BakedSampling = new TriangleSampling[SampleCount];
            for (int i = 0; i < SampleCount; ++i)
            {
                m_BakedSampling[i] = VFXMeshSamplingHelper.GetNextSampling(meshData, rand);
            }
        }
        private void UpdateGraphicsBuffer()
        {
            if (m_BakedSampling == null) return;

            if (SampleCount != m_BakedSampling.Length)
            {
                //Debug.LogErrorFormat("The length of baked data mismatches with sample count : {0} vs {1}", SampleCount, m_BakedSampling.Length);
                return;
            }

            if (m_Buffer != null)
            {
                m_Buffer.Release();
                m_Buffer = null;
            }

            m_Buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SampleCount, Marshal.SizeOf(typeof(TriangleSampling)));
            m_Buffer.SetData(m_BakedSampling);
        }
        private void BindGraphicsBuffer(VisualEffect vfx)
        {
            if (vfx == null) return;
            if (vfx.HasGraphicsBuffer(GraphicsBufferName)) vfx.SetGraphicsBuffer(GraphicsBufferName, m_Buffer);
        }

        public void Update(VisualEffect visualEffect, Renderer renderer)
        {
            if (m_BakedSampling == null || m_BakedSampling.Length < 1)
            {
                Bake(visualEffect, renderer);
            }
            else if (m_Buffer == null)
            {
                Bake(visualEffect, renderer);
            }

            if (!visualEffect.HasGraphicsBuffer(GraphicsBufferName))
            {
                //Debug.Log("There is no graphics buffer in the vfx graph.");
                return;
            }
        }
        public void OnDisable()
        {
            if (m_Buffer != null)
            {
                m_Buffer.Release();
                m_Buffer = null;
            }
        }
        public void Bake(VisualEffect visualEffect, Renderer renderer)
        {
            ComputeBakedSampling(visualEffect, VFXMeshSamplingHelper.RendererToMesh(renderer));
            UpdateGraphicsBuffer();
            BindGraphicsBuffer(visualEffect);
        }
        public void SetGraphicsBuffer(VisualEffect visualEffect)
        {
            UpdateGraphicsBuffer();
            BindGraphicsBuffer(visualEffect);
        }

    }
}