using System;
using UnityEngine;

namespace SwissArmySpline
{
    public class SAS_Animator : MonoBehaviour
    {
        [Tooltip("The path used to animate the objects along.")]
        public SAS_CurveTools path;
        [Tooltip("Specify one or more wagons.")]
        public Wagon[] wagons = new Wagon[0];
        [Tooltip("How far apart in the forward axis are the wheels. This is used to calculate the angle of the object.")]
        public Vector2 wheelSpacing = new Vector2(-0.5f, 0.5f);
        [Tooltip("If 'Use Gravity' is enabled you can specify how much drag opposes the motion.")]
        public float drag = 0.25f;
        [Tooltip("Offsets the animated objects from the path.")]
        public Vector2 offset;
        [Tooltip("Useful if you want to move objects in the opposite direction of the path.")]
        public bool flipForward;
        [Tooltip("At which distance along the path should the objects be placed?")]
        public float distance;
        [NonSerialized] public float currentSpeed;
        [Tooltip("Useful for rollercoasters or similar movement concepts.")]
        public bool useGravity;
        [Tooltip("The speed of the objects based on the percentage of the path (in 0 to 1 range).")]
        public AnimationCurve velocityCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [Tooltip("Turn this off if you want to animate objects through your own code.")]
        public bool autoUpdate = true;
        bool error;
        [Serializable] public struct Wagon
        {
            [Tooltip("The transform that should be animated.")]
            public Transform transform;
            [Tooltip("Offsets the transform along the distance of the path. Can be used to separate wagons from each other.")]
            public float distanceOffset;
        } //================================================================================================================================================



        void Start()  { ErrorChecking(); }
        void Update() { if (autoUpdate) AutoUpdate(); }



        public void MoveObjectsToStartPosition()
        {
            ErrorChecking();
            if (error) return;
            SetDistance(distance);
        } //================================================================================================================================================
        public void ManualUpdateSpeed(float speed)
        {
            if (error) return;
            for (int i = 0; i < wagons.Length; i++) UpdateTransform(wagons[i].transform, wagons[i].distanceOffset);
            distance += speed * Time.deltaTime;
        } //================================================================================================================================================
        public void SetDistance(float distance)
        {
            if (error) return;
            for (int i = 0; i < wagons.Length; i++) UpdateTransform(wagons[i].transform, wagons[i].distanceOffset);
            this.distance = distance;
        } //================================================================================================================================================




        void ErrorChecking()
        {
            error = false;

            if (path == null)
            {
                Debug.LogError("Swiss Army Spline Error => " + gameObject.name + ": No path assigned!", gameObject);
                error = true;
            }

            if (wagons.Length == 0)
            {
                Debug.LogError("Swiss Army Spline Error => " + gameObject.name + ": No wagons assigned!", gameObject);
                error = true;
            }
            else
            {
                bool missingWagons = false;
                for (int i = 0; i < wagons.Length; i++)
                {
                    if (wagons[i].transform == null) missingWagons = true;
                }
                if (missingWagons) Debug.LogError("Swiss Army Spline Error => " + gameObject.name + ": Not all wagons have a transform assigned!", gameObject);
            }
        } //================================================================================================================================================



        void AutoUpdate()
        {
            if (error) return;

            for (int i = 0; i < wagons.Length; i++)
            {
                SAS_CurveTools.Point p = UpdateTransform(wagons[i].transform, wagons[i].distanceOffset);

                if (useGravity)
                {
                    float sign = Vector3.Dot(p.forward.normalized, Physics.gravity.normalized) >= 0 ? 1 : -1;
                    currentSpeed += (Vector3.Project(Physics.gravity, p.forward).magnitude * sign * Time.deltaTime) / wagons.Length;
                }
                else currentSpeed = velocityCurve.Evaluate(Mathf.Repeat(distance, path.totalLength) / path.totalLength) / 3.6f;
            }
            if (useGravity) currentSpeed -= currentSpeed * drag * Time.deltaTime;
            distance += currentSpeed * Time.deltaTime;
        } //================================================================================================================================================



        SAS_CurveTools.Point UpdateTransform(Transform t, float distanceOffset)
        {
            float repeatDistance = Mathf.Repeat(distance - distanceOffset, path.totalLength);
            if (!path.loop) t.gameObject.SetActive(repeatDistance >= 1 && repeatDistance <= path.totalLength - 1);
            SAS_CurveTools.Point p = path.GetPoint(repeatDistance, wheelSpacing, true);
            t.position = p.position + (p.right * offset.x) + (p.up * offset.y);
            t.rotation = p.rotation * (flipForward ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
            return p;
        } //================================================================================================================================================
    }
}
