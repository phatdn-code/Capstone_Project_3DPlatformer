using UnityEngine;

namespace INab.Demo
{
    [ExecuteAlways]
    public class EllipseFloat : MonoBehaviour
    {
        public Transform centralObject; // The central object around which the target object will move
        public float rotationSpeed = 50f; // Rotation speed around the central object
        public float horizontalRadius = 2f; // Horizontal radius of the ellipse
        public float verticalRadius = 1f; // Vertical radius of the ellipse
        public float minVerticalOffset = 0;
        public float maxVerticalOffset = 2;

        public AnimationCurve scaleCurve = new AnimationCurve(
           new Keyframe(0f, 0f),   // Time: 0, Value: 0
           new Keyframe(0.5f, 1f), // Time: 0.5, Value: 1
           new Keyframe(1f, 0f)    // Time: 1, Value: 0
       );

        private float verticalOffset = 0;

        public bool updateInEditor = false;

        private void Update()
        {
            if (updateInEditor || Application.isPlaying)
            {
                // Calculate the position of the target object on the ellipse
                float angle = Time.time * rotationSpeed;
                Vector3 newPosition = GetPositionOnEllipse(angle);

                float offset = Mathf.Repeat(angle, 360);
                offset /= 360;

                offset = scaleCurve.Evaluate(offset);

                verticalOffset = Mathf.Lerp(minVerticalOffset, maxVerticalOffset, offset);

                // Move the target object to the new position
                transform.position = newPosition;

                transform.LookAt(centralObject);
            }
        }

        private Vector3 GetPositionOnEllipse(float angle)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * horizontalRadius;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * verticalRadius;
            return centralObject.position + new Vector3(x, verticalOffset, z);
        }
    }
}