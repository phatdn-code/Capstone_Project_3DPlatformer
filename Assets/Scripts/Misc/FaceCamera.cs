using UnityEngine;

/// <summary>
/// Faces this object toward the camera (billboard). Works for any Transform.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    [Header("Camera (optional)")]
    [SerializeField] private Camera targetCamera;   // If null, uses Camera.main

    [Header("Rotation")]
    [SerializeField] private bool keepUpright = true; // Keep Y-up (no tilt)

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        Vector3 camPos = targetCamera.transform.position;

        if (keepUpright)
        {
            Vector3 dir = camPos - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        // Full billboard facing camera (can tilt)
        else transform.rotation = Quaternion.LookRotation(transform.position - camPos);
    }
}
