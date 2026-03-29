using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwissArmySpline
{
    public partial class SAS_CurveTools : MonoBehaviour
    {
        public GameObject meshGO;
        public Extruder[] extruders = new Extruder[0];
        public Pillar[] pillars = new Pillar[0];
        public Distributer[] distributers = new Distributer[0];
        public Instantiator[] instantiators = new Instantiator[0];
        public bool autoUpdate = true;
        public bool tangents = false;

        public bool showExtruders = true;
        public bool showPillars = true;
        public bool showDistributers = true;
        public bool showInstantiators = true;

        public enum PlaceMode { Random, Sequential, PingPong }

        Mesh mesh;
        List<Vector3> vertices;
        List<Vector3> normals;
        List<int> triangles;
        List<Vector2> uvs;

        List<int> submeshIndices;
        List<int> submeshStart;
        List<int> submeshEnd;





        [Serializable] public class Extruder
        {
            [HideInInspector] public string name;
            [Tooltip("An optional name override. Useful to keep things orderly.")]
            public string overrideName;

            [HideInInspector] public bool active = true;

            [Tooltip("The profile to extrude")]
            public SAS_Profile profile;

            [Tooltip("Scales the profile based on the percentage of the curve (in 0 to 1 range).")]
            public AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

            [Tooltip("The maximum allowed Angle Error. This is used for optimization. A lower value will result in a more accurate mesh.")]
            public float maxAngleError = 2;
            [Tooltip("The smallest distance between mesh segments. This is used for optimization. A lower value will result in a more accurate mesh.")]
            public float smallestStep = 0.1f;

            [Header("Spline")]
            [Tooltip("An offset in meters for the active start of this extruder.")]
            public float startOffset = 0;
            [Tooltip("An offset in meters for the active end of this extruder.")]
            public float endOffset = 0;

            [Header("Transform")]
            [Tooltip("An optional offset")]
            public Vector2 offset;
            [Tooltip("If enabled offset.y is in world space.")]
            public bool yUp = false;

            [Header("UV Modifiers")]
            [Tooltip("Scales the UVs, use this to get the texture tiling that you want.")]
            public Vector2 uvScale = Vector2.one;
            [Tooltip("Moves the UVs, use this to get the texture posittion that you want.")]
            public Vector2 uvOffset = Vector2.zero;
        } //================================================================================================================================================

        [Serializable] public class Distributer
        {
            [HideInInspector] public string name;
            [Tooltip("An optional name override. Useful to keep things orderly.")]
            public string overrideName;

            [HideInInspector] public bool active = true;

            [Tooltip("Specify one or more meshes that you want to distribute along the spline.")]
            public Mesh[] meshes = new Mesh[0];
            [Tooltip("Only used for multiple meshes. Specifies the order in which the meshes will be distributed.")]
            public PlaceMode placeMode = PlaceMode.Random;
            [Tooltip("If you want to use different materials on the mesh you can specify an index (Be aware that you should maintain a sequential indexing - e.g. 0, 1, 2, ... not 0, 2, 5). The materials can be set on the mesh renderer of the mesh GameObject.")]
            public int submeshIndex = 0;

            [Header("Spline")]
            [Tooltip("An offset in meters for the active start of this distributor.")]
            public float startOffset = 0;
            [Tooltip("An offset in meters for the active end of this pillar distributor.")]
            public float endOffset = 0;
            [Tooltip("The distance in meters between the distributed meshes.")]
            public float spacing = 1;

            [Header("Transform")]
            [Tooltip("Aligns the up vector of the mesh with the world up vector.")]
            public bool yUp = false;
            [Tooltip("A raycast can be used to place meshes at the ray hit point. Useful for distributing objects on an uneven surface.")]
            public bool useRayCast = false;
            [Tooltip("The direction of the raycast.")]
            public Vector3 rayDirection = Vector3.down;
            [Tooltip("If this is enabled objects will be oriented based on the ray hit normal.")]
            public bool alignToHitNormal = false;
            [Tooltip("Offsets the ray hit point along the ray direction. Useful to sink objects further into the ground.")]
            public float sink = 0;
            [Tooltip("An optional offset.")]
            public Vector2 offset;
            [Tooltip("An optional additive rotation.")]
            public Vector3 rotation;
            [Tooltip("An optional scale multiplier.")]
            public Vector3 scale = Vector3.one;

            [Tooltip("Scales the mesh based on the percentage of the curve (in 0 to 1 range).")]
            public AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

            [Header("Randomization")]
            [Tooltip("Adds positional randomization.")]
            public Vector3 randomizePos;
            [Tooltip("Adds rotational randomization.")]
            public Vector3 randomizeRot;
            [Tooltip("Adds scale randomization.")]
            public Vector3 randomizeScl;

            [Header("UV Modifiers")]
            [Tooltip("Scales the UVs, use this to get the texture tiling that you want.")]
            public Vector2 uvScale = Vector2.one;
            [Tooltip("Moves the UVs, use this to get the texture posittion that you want.")]
            public Vector2 uvOffset = Vector2.zero;
            [Tooltip("Adds a random UV offset. This can be useful to make things appear less artificial and repetitive.")]
            public Vector2 uvRandomization = Vector2.zero;
        } //================================================================================================================================================

        [Serializable] public class Instantiator
        {
            [HideInInspector] public string name;
            [Tooltip("An optional name override. Useful to keep things orderly.")]
            public string overrideName;

            [HideInInspector] public bool active = true;

            [Tooltip("Specify one or more game objects that you want to distribute along the spline.")]
            public GameObject[] gameObjects = new GameObject[0];
            [Tooltip("Only effective if multiple gameobjects are used. Specifies the order in which the meshes will be distributed.")]
            public PlaceMode placeMode = PlaceMode.Random;

            [Header("Spline")]
            [Tooltip("An offset in meters for the active start of this instantiator.")]
            public float startOffset = 0;
            [Tooltip("An offset in meters for the active end of this instantiator.")]
            public float endOffset = 0;
            [Tooltip("The distance in meters between the distributed game objects.")]
            public float spacing = 1;

            [Header("Transform")]
            [Tooltip("Aligns the up vector of the game object with the world up vector.")]
            public bool yUp = false;
            [Tooltip("A raycast can be used to place game objects at the ray hit point. Useful for distributing objects on an uneven surface.")]
            public bool useRayCast = false;
            [Tooltip("The direction of the raycast.")]
            public Vector3 rayDirection = Vector3.down;
            [Tooltip("If this is enabled objects will be oriented based on the ray hit normal.")]
            public bool alignToHitNormal = false;
            [Tooltip("Offsets the ray hit point along the ray direction. Useful to sink objects further into the ground.")]
            public float sink = 0;
            [Tooltip("An optional offset.")]
            public Vector2 offset;
            [Tooltip("An optional additive rotation.")]
            public Vector3 rotation;
            [Tooltip("An optional scale multiplier.")]
            public Vector3 scale = Vector3.one;

            [Header("Randomization")]
            [Tooltip("Adds positional randomization.")]
            public Vector3 randomizePos;
            [Tooltip("Adds rotational randomization.")]
            public Vector3 randomizeRot;
            [Tooltip("Adds scale randomization.")]
            public Vector3 randomizeScl;
        } //================================================================================================================================================

        [Serializable] public class Pillar
        {
            [HideInInspector] public string name;
            [Tooltip("An optional name override. Useful to keep things orderly.")]
            public string overrideName;

            [HideInInspector] public bool active = true;

            [Tooltip("The profile to extrude")]
            public SAS_Profile profile;

            [Header("Spline")]
            [Tooltip("An offset in meters for the active start of this pillar distributor.")]
            public float startOffset = 0;
            [Tooltip("An offset in meters for the active end of this pillar distributor.")]
            public float endOffset = 0;
            [Tooltip("The distance in meters between the distributed pillars.")]
            public float spacing = 2;

            [Header("Transform")]
            [Tooltip("Sets the offset to be relative to the world up vector.")]
            public bool offsetYUp = false;
            [Tooltip("Offsets the start of the pillar.")]
            public Vector2 offset;
            [Tooltip("Aligns the up vector of the pillar with the world up vector.")]
            public bool orientationYUp = false;
            [Tooltip("The length of the pillar.")]
            public float length = 1;
            [Tooltip("The angle around the curve in which this pillar should extend.")]
            public float angle = 0;
            [Tooltip("Spins the pillar around it self.")]
            public float spin = 0;
            [Tooltip("Scales the thickness of the pillar based on the percentage of the curve (in 0 to 1 range).")]
            public AnimationCurve scaleCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));

            [Tooltip("A raycast can be used to define the length of a pillar. Useful to prevent pillars sticking through surfaces.")]
            public bool useRayCast;
            [Tooltip("Sinks the endf of the pillar into the raycast hit surface if 'Use Raycast' is enabled.")]
            public float sink = 0;


            [Header("UV Modifiers")]
            [Tooltip("Scales the UVs, use this to get the texture tiling that you want.")]
            public Vector2 uvScale = Vector2.one;
            [Tooltip("Moves the UVs, use this to get the texture posittion that you want.")]
            public Vector2 uvOffset = Vector2.zero;
            [Tooltip("Adds a random offset to the UVs, useful to hide visual repetition.")]
            public Vector2 uvRandomization = Vector2.zero;
        } //================================================================================================================================================










        private void OnValidate()
        {
            //Execute();
        } //================================================================================================================================================

        public void Execute(bool force = false)
        {
            if (points.Count < 2)
            {
                if (mesh != null) mesh.Clear();
                return;
            }

            if (extruders.Length > 0 || distributers.Length > 0 || pillars.Length > 0 || instantiators.Length > 0)
            {
                if (meshGO == null) CreateMeshGO();
            }

            if (meshGO == null) return;
            if (meshGO == gameObject)
            {
                meshGO = null;
                return;
            }

            if (!autoUpdate && !force) return;
            PrepareMesh();

            submeshIndices = new List<int>();
            submeshStart = new List<int>();
            submeshEnd = new List<int>();

            for (int i = 0; i < extruders.Length; i++) Extrude(extruders[i]);
            for (int i = 0; i < pillars.Length; i++) PillarMaker(pillars[i]);
            for (int i = 0; i < distributers.Length; i++) Distribute(distributers[i]);

            if (vertices.Count == 0) return;
            UpdateMesh();
        } //================================================================================================================================================

        void CreateMeshGO()
        {
            meshGO = new GameObject("SAS_Mesh", typeof(MeshFilter), typeof(MeshRenderer));
            meshGO.transform.position = Vector3.zero;
            meshGO.transform.rotation = Quaternion.identity;
            meshGO.transform.localScale = Vector3.one;
            meshGO.transform.parent = transform.parent;
            int index = transform.GetSiblingIndex() + 1;
            meshGO.transform.SetSiblingIndex(index);
        } //================================================================================================================================================



        public void Spawn()
        {
            Transform parent = meshGO.transform;
            Clear();

            for (int i = 0; i < instantiators.Length; i++)
            {
                Instantiator d = instantiators[i];

                if (d.gameObjects == null) continue;
                if (d.gameObjects.Length == 0) continue;
                if (d.spacing < 0.1) continue;
                if (!d.active) continue;

                d.startOffset = Mathf.Clamp(d.startOffset, 0, totalLength);
                d.endOffset = Mathf.Clamp(d.endOffset, 0, totalLength);


                float distance = d.startOffset;
                Vector2 sampleSeparation = new Vector2(-0.0001f, 0.0001f);

                int sequence = -1;
                bool sequenceUp = true;


                float actualDistance = totalLength - d.endOffset - d.startOffset;
                int number = Mathf.RoundToInt(actualDistance / d.spacing);

                float spacing = actualDistance / number;
                int count = 0;
                int limit = (loop && d.startOffset == 0 && d.endOffset == 0) ? number - 1 : number;

                while (count <= limit)
                {
                    Point currPoint = GetPoint(Mathf.Max(0, distance - 0.001f), sampleSeparation);
                    int tCount = vertices.Count;

                    Vector3 randomRot = RandomizeV3(d.randomizeRot);
                    Vector3 randomPos = RandomizeV3(d.randomizePos);
                    Vector3 randomScl = new Vector3(RND() * d.randomizeScl.x, RND() * d.randomizeScl.y, RND() * d.randomizeScl.z);

                    Vector3 offsetX = currPoint.right * (d.offset.x + randomPos.x);
                    Vector3 offsetY = currPoint.up * (d.offset.y + randomPos.y);
                    Vector3 offsetZ = currPoint.forward * randomPos.z;
                    Vector3 offset = offsetX + offsetY + offsetZ;

                    if (d.yUp) currPoint.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(currPoint.forward, Vector3.up), Vector3.up);

                    Vector3 pos = currPoint.position + offset;
                    Quaternion rot = currPoint.rotation;

                    if (d.useRayCast && Physics.Raycast(new Ray(pos, currPoint.rotation * d.rayDirection), out RaycastHit hit, 100))
                    {
                        pos = hit.point + currPoint.rotation * d.rayDirection * d.sink;
                        if (d.alignToHitNormal)
                        {
                            rot = Quaternion.LookRotation(hit.normal, rot * Vector3.forward) * Quaternion.Euler(-90, 180, 0);
                        }
                    }

                    if (d.placeMode == PlaceMode.Random) sequence = Mathf.FloorToInt(RND() * d.gameObjects.Length);
                    else if (d.placeMode == PlaceMode.Sequential)
                    {
                        sequence++;
                        if (sequence >= d.gameObjects.Length) sequence = 0;
                    }
                    else if (d.placeMode == PlaceMode.PingPong)
                    {
                        if (sequenceUp) sequence++;
                        else sequence--;
                        if (sequence <= 0) sequenceUp = true;
                        if (sequence >= d.gameObjects.Length - 1) sequenceUp = false;
                    }

                    GameObject go = Instantiate(d.gameObjects[sequence], pos, rot * Quaternion.Euler(d.rotation + randomRot), parent);
                    go.transform.localScale = d.scale + randomScl;

                    distance += spacing;
                    count++;
                }
            }
        } //================================================================================================================================================

        public void Clear()
        {
            Transform parent = meshGO.transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        } //================================================================================================================================================



        void Distribute(Distributer d)
        {
            if (d.meshes == null) return;
            if (d.meshes.Length == 0) return;
            for (int i = 0; i < d.meshes.Length; i++) if (d.meshes[i] == null) return;
            if (d.spacing < 0.1) return;
            if (!d.active) return;

            submeshIndices.Add(d.submeshIndex);
            submeshStart.Add(triangles.Count);

            d.startOffset = Mathf.Clamp(d.startOffset, 0, totalLength);
            d.endOffset = Mathf.Clamp(d.endOffset, 0, totalLength);

            float distance = d.startOffset;
            Vector2 sampleSeparation = new Vector2(-0.0001f, 0.0001f);

            int sequence = -1;
            bool sequenceUp = true;


            float actualDistance = totalLength - d.endOffset - d.startOffset;
            int number = Mathf.RoundToInt(actualDistance / d.spacing);
            
            float spacing = actualDistance / number;
            int count = 0;
            int limit = (loop && d.startOffset == 0 && d.endOffset == 0) ? number - 1 : number;

            while (count <= limit)
            {
                Point currPoint = GetPoint(Mathf.Max(0,distance - 0.001f), sampleSeparation);
                int tCount = vertices.Count;


                if (d.placeMode == PlaceMode.Random) sequence = Mathf.FloorToInt(RND() * d.meshes.Length);
                else if (d.placeMode == PlaceMode.Sequential)
                {
                    sequence++;
                    if (sequence >= d.meshes.Length) sequence = 0;
                }
                else if (d.placeMode == PlaceMode.PingPong)
                {
                    if (sequenceUp) sequence++;
                    else sequence--;
                    if (sequence <= 0) sequenceUp = true;
                    if (sequence >= d.meshes.Length - 1) sequenceUp = false;
                }

                Vector3[] v = d.meshes[sequence].vertices;
                Vector3[] n = d.meshes[sequence].normals;
                Vector2[] uv = d.meshes[sequence].uv;
                int[] t = d.meshes[sequence].triangles;


                Vector3 randomRot = RandomizeV3(d.randomizeRot);
                Vector3 randomPos = RandomizeV3(d.randomizePos);
                Vector3 randomScl = new Vector3(RND() * d.randomizeScl.x, RND() * d.randomizeScl.y, RND() * d.randomizeScl.z);
                Vector2 randomUV  = new Vector2(RND() * d.uvRandomization.x, RND() * d.uvRandomization.y);

                Vector3 offsetX = currPoint.right * (d.offset.x + randomPos.x);
                Vector3 offsetY = (d.yUp ? Vector3.up : currPoint.up) * (d.offset.y + randomPos.y);
                Vector3 offsetZ = currPoint.forward * randomPos.z;

                //Vector3 offsetY = (d.yUp ? Vector3.up : currPoint.up) * (d.offset.y + randomPos.y);
                //if (d.yUp)
                //{
                //    offsetX = Vector3.ProjectOnPlane(offsetX, Vector3.up);
                //    offsetZ = Vector3.ProjectOnPlane(offsetZ, Vector3.up);
                //}

                Vector3 offset = offsetX + offsetY + offsetZ;


                if (d.yUp) currPoint.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(currPoint.forward, Vector3.up), Vector3.up);

                Vector3 pos = currPoint.position + offset;
                Quaternion rot = currPoint.rotation;

                if (d.useRayCast && Physics.Raycast(new Ray(pos, currPoint.rotation * d.rayDirection), out RaycastHit hit, 100))
                {
                    pos = hit.point + currPoint.rotation * d.rayDirection * d.sink;
                    if (d.alignToHitNormal)
                    {
                        rot = Quaternion.LookRotation(hit.normal, rot * Vector3.forward) * Quaternion.Euler(-90, 180, 0);
                    }
                }

                for (int i = 0; i < v.Length; i++)
                {
                    v[i] = Vector3.Scale(v[i], d.scale + randomScl) * d.scaleCurve.Evaluate(distance / totalLength);

                    Vector3 vertex = Quaternion.Euler(d.rotation + randomRot) * v[i];
                    Vector3 normal = Quaternion.Euler(d.rotation + randomRot) * n[i];

                    //vertices.Add(pos + rot * Vector3.Scale(vertex, d.scale + randomScl) * d.scaleCurve.Evaluate(distance / totalLength));
                    vertices.Add(pos + rot * vertex);


                    normals.Add(rot * normal);

                    if (uv != null && uv.Length == v.Length)
                    {
                        Vector2 uvNew = uv[i];
                        uvNew.x = uvNew.x * d.uvScale.x + d.uvOffset.x + randomUV.x;
                        uvNew.y = uvNew.y * d.uvScale.y + d.uvOffset.y + randomUV.y;
                        uvs.Add(uvNew);
                    }
                    else uvs.Add(Vector2.zero);
                }
                for (int i = 0; i < t.Length; i++) triangles.Add(tCount + t[i]);

                distance += spacing;
                count++;
            }
            submeshEnd.Add(triangles.Count);
        } //================================================================================================================================================



        void PillarMaker(Pillar d)
        {
            if (d.profile == null) return;
            if (d.spacing < 0.1) return;
            if (!d.active) return;


            for (int i = 0; i < d.profile.profiles.Count; i++)
            {
                SAS_Profile.Profile pf = d.profile.profiles[i];

                submeshIndices.Add(pf.submeshIndex);
                submeshStart.Add(triangles.Count);


                Vector4[] profileVerts = pf.vertices.ToArray();
                bool closed = pf.close;
                int vertsInProfile = closed ? profileVerts.Length + 1 : profileVerts.Length;
                for (int j = 0; j < profileVerts.Length; j++) if (profileVerts[j].z == 999) vertsInProfile++;




                d.startOffset = Mathf.Clamp(d.startOffset, 0, totalLength);
                d.endOffset = Mathf.Clamp(d.endOffset, 0, totalLength);

                float distance = d.startOffset;
                Vector2 sampleSeparation = new Vector2(-0.0001f, 0.0001f);

                float actualDistance = totalLength - d.endOffset - d.startOffset;
                int number = Mathf.RoundToInt(actualDistance / d.spacing);

                float spacing = actualDistance / number;
                int count = 0;
                int limit = (loop && d.startOffset == 0 && d.endOffset == 0) ? number - 1 : number;

                while (count <= limit)
                {
                    Point currPoint = GetPoint(Mathf.Max(0, distance - 0.001f), sampleSeparation);

                    Vector3 pos = currPoint.position;
                    pos += currPoint.right * d.offset.x;
                    if (d.offsetYUp) pos += Vector3.up * d.offset.y;
                    else pos += currPoint.up * d.offset.y;

                    Vector2 randomUV = new Vector2(RND() * d.uvRandomization.x, RND() * d.uvRandomization.y);
                    Vector4 uvMod = new Vector4(d.uvScale.x, d.uvScale.y, d.uvOffset.x + randomUV.x, d.uvOffset.y + randomUV.y);

                    Quaternion rot;
                    if (d.orientationYUp)
                    {
                        Vector3 forward = Quaternion.AngleAxis(d.spin, -Vector3.up) * currPoint.forward;
                        rot = Quaternion.LookRotation(-Vector3.up, forward);
                    }
                    else
                    {
                        Vector3 up = Quaternion.AngleAxis(d.angle, currPoint.forward) * -currPoint.up;
                        Vector3 forward = Quaternion.AngleAxis(d.spin, up) * currPoint.forward;
                        rot = Quaternion.LookRotation(up, forward);
                    }


                    float length = d.length;
                    if (d.useRayCast && Physics.Raycast(new Ray(pos, rot * Vector3.forward), out RaycastHit hit, length)) length = hit.distance;
                    length += d.sink;

                    float scale = d.scaleCurve.Evaluate(distance / totalLength);
                    BuildRing(pos,                                    rot, 0,      false, profileVerts, vertsInProfile, closed, scale, uvMod, pf.loopUV, pf.uvIsU);
                    BuildRing(pos + rot * (Vector3.forward * length), rot, length, true,  profileVerts, vertsInProfile, closed, scale, uvMod, pf.loopUV, pf.uvIsU);

                    distance += spacing;
                    count++;
                }

                submeshEnd.Add(triangles.Count);
            }
        } //================================================================================================================================================

















        void Extrude(Extruder extruder)
        {
            if (extruder.profile == null) return;
            if (!extruder.active) return;

            extruder.startOffset = Mathf.Clamp(extruder.startOffset, 0, totalLength);
            extruder.endOffset = Mathf.Clamp(extruder.endOffset, 0, totalLength);

            float actualLength = totalLength - extruder.startOffset - extruder.endOffset;
            if (actualLength <= 0) return;

            for (int i = 0; i < extruder.profile.profiles.Count; i++)
            {
                Vector4 uvMod = new Vector4(extruder.uvScale.x, extruder.uvScale.y, extruder.uvOffset.x, extruder.uvOffset.y);

                SAS_Profile.Profile pf = extruder.profile.profiles[i];

                submeshIndices.Add(pf.submeshIndex);
                submeshStart.Add(triangles.Count);


                Vector4[] profileVerts = pf.vertices.ToArray();
                bool closed = pf.close;
                int vertsInProfile = closed ? profileVerts.Length + 1 : profileVerts.Length;
                for (int j = 0; j < profileVerts.Length; j++) if (profileVerts[j].z == 999) vertsInProfile++;



                float stepLength = 0.05f;
                float distance = extruder.startOffset;
                Vector2 sampleSeparation = new Vector2(-0.01f, 0.01f);

                Point currPoint = GetPoint(distance, sampleSeparation); distance += stepLength;
                Point nextPoint = currPoint;
                Point prevPoint = currPoint;

                Vector3 offset = extruder.offset;



                if (extruder.startOffset == 0)
                {
                    SAS_CurveTools_Helper.Point p = points[0];
                    Vector3 right = p.rotation * Vector3.right;
                    Vector3 up = extruder.yUp ? Vector3.up : p.rotation * Vector3.up;

                    float scale = extruder.scaleCurve.Evaluate(0);
                    Vector3 pos = p.position + right * offset.x + up * offset.y;
                    BuildRing(pos, p.rotation, 0, false, profileVerts, vertsInProfile, closed, scale, uvMod, pf.loopUV, pf.uvIsU);
                }
                else
                {
                    float scale = extruder.scaleCurve.Evaluate(distance / totalLength);

                    Vector3 pos = currPoint.position;
                    pos += currPoint.right * offset.x;
                    pos += extruder.yUp ? Vector3.up * offset.y : currPoint.up * offset.y;

                    BuildRing(pos, currPoint.rotation, distance, false, profileVerts, vertsInProfile, closed, scale, uvMod, pf.loopUV, pf.uvIsU);
                }


                float limit = totalLength - extruder.endOffset;

                while (distance < limit - stepLength * 2)//b.totalLength - stepLength * 2)
                {
                    float angleForward = Vector3.Angle((currPoint.position - prevPoint.position), (nextPoint.position - currPoint.position));
                    float angleTwist = Vector3.Angle(currPoint.up, prevPoint.up);

                    if ((angleForward > extruder.maxAngleError || angleTwist > extruder.maxAngleError) && Vector3.Distance(currPoint.position, prevPoint.position) > extruder.smallestStep)
                    {
                        float scale = extruder.scaleCurve.Evaluate(distance / totalLength);

                        Vector3 pos = currPoint.position;
                        pos += currPoint.right * offset.x;
                        pos += extruder.yUp ? Vector3.up * offset.y : currPoint.up * offset.y;


                        BuildRing(pos, currPoint.rotation, distance, true, profileVerts, vertsInProfile, closed, scale, uvMod, pf.loopUV, pf.uvIsU);
                        prevPoint = currPoint;
                    }

                    currPoint = nextPoint;
                    distance += stepLength;
                    nextPoint = GetPoint(distance, sampleSeparation);
                }


                if (extruder.endOffset == 0)
                {
                    SAS_CurveTools_Helper.Point p;
                    if (loop) p = points[0];
                    else p = points[points.Count - 1];
                    Vector3 right = p.rotation * Vector3.right;
                    Vector3 up = extruder.yUp ? Vector3.up : p.rotation * Vector3.up;

                    float scale = extruder.scaleCurve.Evaluate(distance / totalLength);
                    Vector3 pos = p.position + right * offset.x + up * offset.y;
                    BuildRing(pos, p.rotation, distance, true, profileVerts, vertsInProfile, closed, scale, uvMod, pf.loopUV, pf.uvIsU);
                }
                else
                {
                    currPoint = GetPoint(totalLength - extruder.endOffset, sampleSeparation);
                    float scale = extruder.scaleCurve.Evaluate(distance / totalLength);

                    Vector3 pos = currPoint.position;
                    pos += currPoint.right * offset.x;
                    pos += extruder.yUp ? Vector3.up * offset.y : currPoint.up * offset.y;

                    BuildRing(pos, currPoint.rotation, distance, true, profileVerts, vertsInProfile, closed, scale, uvMod, pf.loopUV, pf.uvIsU);
                }

                submeshEnd.Add(triangles.Count);
            }
        } //================================================================================================================================================

        void BuildRing(Vector3 position, Quaternion rotation, float u, bool addTriangles, Vector4[] verts, int vertsInProfile, bool closed, float scaleCurve, Vector4 uvMod, float loopUV, bool uvIsU)
        {
            int vertCount = verts.Length;

            int index = 0;
            for (int i = 0; i < vertCount; i++)
            {
                Vector4 v = verts[i];

                Vector3 pos = rotation * new Vector3(v.x, v.y, 0) * scaleCurve;

                vertices.Add(position + pos);

                Vector2 uv;
                if (uvIsU) uv = new Vector2(v.w * uvMod.x + uvMod.z, u * uvMod.y + uvMod.w);
                else uv = new Vector2(u * uvMod.y + uvMod.w, v.w * uvMod.x + uvMod.z);

                uvs.Add(uv);
                if (addTriangles) AddTriangles(index, vertsInProfile);
                index++;

                if (v.z == 999) // Hard Normal
                {
                    vertices.Add(position + pos);
                    uvs.Add(uv);

                    Vector2 current = verts[i];
                    Vector2 previous = verts[(int)Mathf.Repeat(i - 1, vertCount)];
                    Vector2 next = verts[(int)Mathf.Repeat(i + 1, vertCount)];

                    Vector2 normal1 = Vector3.Cross(new Vector3(0, 0, 1), current - previous).normalized;
                    Vector2 normal2 = Vector3.Cross(new Vector3(0, 0, 1), next - current).normalized;
                    normals.Add(rotation * new Vector3(normal1.x, normal1.y, 0));
                    normals.Add(rotation * new Vector3(normal2.x, normal2.y, 0));

                    index++;
                }
                else
                {
                    float angle = v.z + Mathf.PI;
                    normals.Add(rotation * new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0));
                }
            }

            if (closed)
            {
                Vector4 v = verts[0];
                Vector3 pos = rotation * new Vector3(v.x, v.y, 0) * scaleCurve;
                vertices.Add(position + pos);



                if (v.z == 999) // Hard Normal
                {
                    Vector2 current = verts[0];
                    Vector2 previous = verts[vertCount - 1];

                    Vector2 normal1 = Vector3.Cross(new Vector3(0, 0, 1), current - previous).normalized;
                    normals.Add(rotation * new Vector3(normal1.x, normal1.y, 0));
                }
                else
                {
                    float angle = v.z + Mathf.PI;
                    normals.Add(rotation * new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0));
                }

                Vector2 uv;
                if (uvIsU) uv = new Vector2(loopUV * uvMod.x + uvMod.z, u * uvMod.y + uvMod.w);
                else uv = new Vector2(u * uvMod.y + uvMod.w, loopUV * uvMod.x + uvMod.z);

                uvs.Add(uv); // Todo!!!

                if (addTriangles) AddTriangles(vertCount, vertsInProfile);
            }
        } //================================================================================================================================================


        void AddTriangles(int i, int vertsPerRow)
        {
            if (i == 0) return;
            int baseIndex = vertices.Count - 1;

            int index0 = baseIndex;
            int index1 = baseIndex - 1;
            int index2 = baseIndex - vertsPerRow;
            int index3 = baseIndex - vertsPerRow - 1;

            triangles.Add(index2);
            triangles.Add(index1);
            triangles.Add(index0);
            triangles.Add(index2);
            triangles.Add(index3);
            triangles.Add(index1);
        } //================================================================================================================================================




        void PrepareMesh()
        {
            vertices = new List<Vector3>();
            normals = new List<Vector3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();

            MeshFilter mf = meshGO.GetComponent<MeshFilter>();
            mesh = mf.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh { name = "Mesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mf.sharedMesh = mesh;
            }
            else mesh.Clear();
        } //================================================================================================================================================

        public Material standardMaterial;


        void UpdateMesh()
        {
            mesh.vertices = vertices.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();

            int submeshCount = 1;
            for (int i = 0; i < submeshIndices.Count; i++)
            {
                if (submeshIndices[i] > submeshCount - 1) submeshCount++;
            }
            mesh.subMeshCount = submeshCount;

            for (int j = 0; j < submeshCount; j++)
            {
                List<int> newTriangles = new List<int>();
                for (int i = 0; i < submeshIndices.Count; i++)
                {
                    if (submeshIndices[i] == j)
                    {
                        List<int> part = triangles.GetRange(submeshStart[i], submeshEnd[i] - submeshStart[i]);
                        newTriangles.AddRange(part);
                    }
                }
                mesh.SetTriangles(newTriangles, j);
            }
            if (tangents)
            {
                Vector4[] tangents = new Vector4[mesh.vertexCount];
                mesh.tangents = tangents;
                mesh.RecalculateTangents();
            }


            mesh.RecalculateBounds();

            MeshRenderer mr = meshGO.GetComponent<MeshRenderer>();


#if UNITY_EDITOR
            Material defaultMaterial;
            defaultMaterial = standardMaterial;//UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");



            Material[] newMaterials = new Material[submeshCount];

            if (mr.sharedMaterials.Length > submeshCount)
            {
                Material[] temp = new Material[submeshCount];
                for (int i = 0; i < submeshCount; i++) temp[i] = mr.sharedMaterials[i];
                mr.sharedMaterials = temp;
            }
            Material[] mrMaterials = mr.sharedMaterials;



            if (mrMaterials == null || mrMaterials.Length == 0)
            {
                for (int i = 0; i < newMaterials.Length; i++) newMaterials[i] = defaultMaterial;
            }
            else
            {
                for (int i = 0; i < mrMaterials.Length; i++) newMaterials[i] = mrMaterials[i];

                for (int i = 0; i < newMaterials.Length; i++)
                {
                    if (newMaterials[i] == null) newMaterials[i] = defaultMaterial;
                }
            }
            mr.sharedMaterials = newMaterials;
#endif
        } //================================================================================================================================================

        float RND() { return UnityEngine.Random.value; }
        Vector3 RandomizeV3(Vector3 mod)
        {
            return new Vector3((UnityEngine.Random.value * 2 - 1) * mod.x, (UnityEngine.Random.value * 2 - 1) * mod.y, (UnityEngine.Random.value * 2 - 1) * mod.z);
        }
    }
}
