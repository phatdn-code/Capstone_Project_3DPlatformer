using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEngine.VFX
{
    /// <summary>
    /// Sampling information structure
    /// - coord is a barycentric coordinate
    /// - index is an index of triangle
    /// </summary>
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer), Serializable]
    public struct TriangleSampling
    {
        public Vector2 coord;
        public uint index;
    }

}

namespace INab.CommonVFX
{
    /// <summary>
    /// Cache of mesh data
    /// Contains raw attributes extracted from a readable Mesh
    /// </summary>
    public class MeshData
    {
        public struct Vertex
        {
            public Vector3 position;
            public Color color;
            public Vector3 normal;
            public Vector4 tangent;

            public static Vertex operator +(Vertex a, Vertex b)
            {
                var r = new Vertex()
                {
                    position = a.position + b.position,
                    color = a.color + b.color,
                    normal = a.normal + b.normal,
                    tangent = a.tangent + b.tangent,
                };

                return r;
            }

            public static Vertex operator *(float a, Vertex b)
            {
                var r = new Vertex()
                {
                    position = a * b.position,
                    color = a * b.color,
                    normal = a * b.normal,
                    tangent = a * b.tangent,
                };

                return r;
            }
        };

        public struct Triangle
        {
            public uint a, b, c;
        };

        public Vertex[] vertices;
        public Triangle[] triangles;
        public double[] accumulatedTriangleArea;
    }


    static public class VFXMeshSamplingHelper
    {
        public static Mesh RendererToMesh(Renderer meshRenderer)
        {
            Mesh mesh;

            if (meshRenderer is SkinnedMeshRenderer)
            {
                mesh = (meshRenderer as SkinnedMeshRenderer).sharedMesh;
            }
            else
            {
                mesh = (meshRenderer as MeshRenderer).GetComponent<MeshFilter>().sharedMesh;
            }

            return mesh;
        }

        /// <summary>
        /// Extract and compute the accumulative sum of triangle area needed for uniform sampling
        /// </summary>
        public static MeshData ComputeDataCache(Mesh input,bool useSubMesh,int submeshIndex)
        {
            int indexCount;
            int indexStart = 0;
            int vertexCount;
            int firstVertex = 0;

            if (useSubMesh)
            {
                indexCount = input.GetSubMesh(submeshIndex).indexCount;
                indexStart = input.GetSubMesh(submeshIndex).indexStart;
                vertexCount = input.GetSubMesh(submeshIndex).vertexCount;
                firstVertex = input.GetSubMesh(submeshIndex).firstVertex;
            }
            else
            {
                vertexCount = input.vertexCount;
                indexCount = input.triangles.Length;

            }

            var positions = input.vertices;
            var normals = input.normals;
            var tangents = input.tangents;
            var colors = input.colors;

            if (useSubMesh)
            {
                positions = positions.Skip(firstVertex).Take(Mathf.Min(vertexCount, positions.Length - firstVertex)).ToArray();
                normals = normals.Skip(firstVertex).Take(Mathf.Min(vertexCount, normals.Length - firstVertex)).ToArray();
                tangents = tangents.Skip(firstVertex).Take(Mathf.Min(vertexCount, tangents.Length - firstVertex)).ToArray();
                colors = colors.Skip(firstVertex).Take(Mathf.Min(vertexCount, colors.Length - firstVertex)).ToArray();
            }



            normals = normals.Length == vertexCount ? normals : null;
            tangents = tangents.Length == vertexCount ? tangents : null;
            colors = colors.Length == vertexCount ? colors : null;

            var meshData = new MeshData();
            meshData.vertices = new MeshData.Vertex[vertexCount];
            for (int i = 0; i < vertexCount; ++i)
            {
                meshData.vertices[i] = new MeshData.Vertex()
                {
                    position = positions[i],
                    color = colors != null ? colors[i] : Color.white,
                    normal = normals != null ? normals[i] : Vector3.up,
                    tangent = tangents != null ? tangents[i] : Vector4.one,
                };
            }

            //meshData.triangles = new MeshData.Triangle[input.triangles.Length / 3];
            meshData.triangles = new MeshData.Triangle[indexCount / 3];
            var triangles = input.triangles;

            if(useSubMesh)triangles = triangles.Skip(indexStart).Take(indexCount).ToArray();

            for (uint i = 0; i < meshData.triangles.Length; ++i)
            {
                meshData.triangles[i] = new MeshData.Triangle()
                {
                    a = (uint)(triangles[i * 3 + 0] - firstVertex),
                    b = (uint)(triangles[i * 3 + 1] - firstVertex),
                    c = (uint)(triangles[i * 3 + 2] - firstVertex),
                };
            }

            if (meshData.triangles.Length >= 1)
            {
                meshData.accumulatedTriangleArea = new double[meshData.triangles.Length];
                meshData.accumulatedTriangleArea[0] = ComputeTriangleArea(meshData, 0);
                for (uint i = 1; i < meshData.triangles.Length; ++i)
                {
                    meshData.accumulatedTriangleArea[i] = meshData.accumulatedTriangleArea[i - 1] + ComputeTriangleArea(meshData, i);
                }
            }
            else
            {
                meshData.accumulatedTriangleArea = new double[0];
            }

            return meshData;
        }

        /// <summary>
        /// Compute interpolated vertices with triangle index and barycentric coordinates
        /// </summary>
        public static MeshData.Vertex GetInterpolatedVertex(MeshData meshData, TriangleSampling sampling)
        {
            var triangle = meshData.triangles[sampling.index];
            var u = sampling.coord.x;
            var v = sampling.coord.y;
            var w = 1.0f - u - v;

            var A = meshData.vertices[triangle.a];
            var B = meshData.vertices[triangle.b];
            var C = meshData.vertices[triangle.c];

            var r = u * A + v * B + w * C;

            r.normal = r.normal.normalized;
            var tangent = new Vector3(r.tangent.x, r.tangent.y, r.tangent.z).normalized;
            r.tangent = new Vector4(tangent.x, tangent.y, tangent.z, r.tangent.w > 0.0f ? 1.0f : -1.0f);

            return r;
        }

        /// <summary>
        /// Return a new uniform sampled position using the accumulated triangle area
        /// </summary>
        public static TriangleSampling GetNextSampling(MeshData meshData, System.Random rand)
        {
            var areaPosition = rand.NextDouble() * meshData.accumulatedTriangleArea.Last();
            uint areaIndex = FindIndexOfArea(meshData, areaPosition);

            var randUV = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble());

            //http://inis.jinr.ru/sl/vol1/CMC/Graphics_Gems_1,ed_A.Glassner.pdf
            //p24 uniform distribution from two numbers in triangle generating barycentric coordinate
            //Alternatively, we can use "A Low-Distortion Map Between Triangle and Square" https://hal.archives-ouvertes.fr/hal-02073696v1/document
            float s = randUV.x;
            float t = Mathf.Sqrt(randUV.y);
            float u = 1.0f - t;
            float v = (1 - s) * t;
            float w = s * t; //Not stored, recomputed using 1 - u - v

            return new TriangleSampling
            {
                coord = new Vector2(u, v),
                index = areaIndex
            };
        }

        static double ComputeTriangleArea(MeshData meshData, uint triangleIndex)
        {
            var t = meshData.triangles[triangleIndex];
            var A = meshData.vertices[t.a].position;
            var B = meshData.vertices[t.b].position;
            var C = meshData.vertices[t.c].position;
            return 0.5f * Vector3.Cross(B - A, C - A).magnitude;
        }

        static uint FindIndexOfArea(MeshData meshData, double area)
        {
            uint min = 0;
            uint max = (uint)meshData.accumulatedTriangleArea.Length - 1;
            uint mid = max >> 1;
            while (max >= min)
            {
                if (mid > meshData.accumulatedTriangleArea.Length)
                    throw new InvalidOperationException("Cannot Find FindIndexOfArea");

                if (meshData.accumulatedTriangleArea[mid] >= area &&
                    (mid == 0 || (meshData.accumulatedTriangleArea[mid - 1] < area)))
                {
                    return mid;
                }
                else if (area < meshData.accumulatedTriangleArea[mid])
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
                mid = (min + max) >> 1;
            }
            throw new InvalidOperationException("Cannot FindIndexOfArea");
        }
    }
}