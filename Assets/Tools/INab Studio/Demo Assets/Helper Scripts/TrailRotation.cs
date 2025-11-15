using UnityEngine;

namespace INab.Demo
{
    [ExecuteAlways]
    public class TrailRotation : MonoBehaviour
    {
        public Vector3 defaultRotation = new Vector3(0, 0, 0);

        public bool enableRotation = true;
        public float rotationSpeed = 700;
        public Vector3 rotationAxis = Vector3.up;

        public float pauseDuration = 1f;
        public float smoothDamp = 2;

        private float anglePassed = 0;

        private float pauseTimer = 0f;
        private bool isRotating = true;

        private float currentVelocity = 0;
        private float currentRotationSpeed = 0;


        public bool updateInEditor = false;

        void Update()
        {
            if (!enableRotation)
            {
                transform.eulerAngles = defaultRotation;
                anglePassed = 0;
                return;
            }

            if (updateInEditor || Application.isPlaying)
            {
                if (isRotating)
                {
                    currentRotationSpeed = Mathf.SmoothDamp(currentRotationSpeed, rotationSpeed, ref currentVelocity, smoothDamp, 1000f);

                    float angle = currentRotationSpeed * Time.deltaTime;
                    anglePassed += angle;

                    transform.Rotate(rotationAxis, angle);

                    // Check if it's time to pause
                    if (anglePassed >= 360)
                    {
                        currentRotationSpeed = 0;
                        anglePassed %= 360;
                        isRotating = false;
                    }
                }
                else
                {
                    // Increment pause timer
                    pauseTimer += Time.deltaTime;

                    // Check if it's time to rotate again
                    if (pauseTimer >= pauseDuration)
                    {
                        isRotating = true;
                        pauseTimer = 0f; // Reset timer for the next pause
                    }
                }
            }
        }

        // Method to toggle rotation on and off
        public void ToggleRotation()
        {
            enableRotation = !enableRotation;
            // Reset timers and state to ensure smooth restart
            isRotating = enableRotation;
            pauseTimer = 0f;
        }
    }
}