using UnityEngine;

namespace INab.Demo
{
    public class Simple3rdCameraFollow : MonoBehaviour
    {
        public bool manualControl = false;
        public Transform playerTransform;
        public Vector3 offset;
        public bool lookAtTarget = true;
        public float mouseSensitivity = 4.0f;
        private float currentX = 0.0f;
        private float currentY = 0.0f;
        public float yAngleMin = -50.0f;
        public float yAngleMax = 50.0f;

        void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            // Lock the cursor to the center of the screen and hide it
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void LateUpdate()
        {
            if (!manualControl) LateUpdateLogic();
        }

        public void LateUpdateLogic()
        {
            // Control offset.z with the mouse scroll wheel
            offset.z -= Input.GetAxis("Mouse ScrollWheel") * 2;

            offset.z = Mathf.Max(0.1f, offset.z);

            // Get mouse input and adjust the current angles based on sensitivity
            currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
            currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            currentY = Mathf.Clamp(currentY, yAngleMin, yAngleMax); // Clamp the vertical angle to prevent flipping

            if (playerTransform != null)
            {
                Vector3 dir = new Vector3(0, 0, -offset.z);
                Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
                transform.position = playerTransform.position + rotation * dir;

                // If lookAtTarget is enabled, look at the player
                if (lookAtTarget)
                {
                    transform.LookAt(playerTransform.position);
                }
                else
                {
                    // If not looking at the target, still apply the rotation to face the direction
                    transform.rotation = rotation;
                }
            }
        }

    }
}