using UnityEngine;

namespace INab.Demo
{
    [ExecuteAlways]
    public class HeighFloatAndRotate : MonoBehaviour
    {
        public float rotationSpeed = 100f;
        public float floatSpeed = 0.5f;
        public float floatHeightMin = 0f;
        public float floatHeightMax = 1.5f;

        public enum Axis
        {
            X, Y, Z
        }

        public Axis axis = Axis.Y;

        public bool updateInEditor = false;

        private void Update()
        {

            if (updateInEditor || Application.isPlaying)
            {
                switch (axis)
                {
                    case Axis.X:
                        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime, Space.World);
                        break;
                    case Axis.Y:
                        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
                        break;
                    case Axis.Z:
                        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.World);
                        break;
                }

                Vector3 newPosition = Vector3.zero;

                float lerp = Mathf.Lerp(floatHeightMin, floatHeightMax, Mathf.Sin(Time.time * floatSpeed) * 0.5f + 0.5f);

                switch (axis)
                {
                    case Axis.X:
                        newPosition = new Vector3(lerp, transform.localPosition.y, transform.localPosition.z);
                        break;
                    case Axis.Y:
                        newPosition = new Vector3(transform.localPosition.x, lerp, transform.localPosition.z);
                        break;
                    case Axis.Z:
                        newPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, lerp);
                        break;
                }

                transform.localPosition = newPosition;
            }
        }
    }
}