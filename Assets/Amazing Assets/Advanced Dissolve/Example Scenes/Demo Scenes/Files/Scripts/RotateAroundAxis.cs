using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class RotateAroundAxis : MonoBehaviour
    {
        public float rotateSpeed = 8.0f;

#if ENABLE_INPUT_SYSTEM
        Key keyQ = Key.Q;
        Key keyA = Key.A;
        Key keyW = Key.W;
        Key keyS = Key.S;
        Key keyE = Key.E;
        Key keyD = Key.D;
#else
        KeyCode keyQ = KeyCode.Q;
        KeyCode keyA = KeyCode.A;
        KeyCode keyW = KeyCode.W;
        KeyCode keyS = KeyCode.S;
        KeyCode keyE = KeyCode.E;
        KeyCode keyD = KeyCode.D;
#endif


        // Update is called once per frame
        void Update()
        {
            if (ExampleInput.GetKey(keyQ)) RotateUpDown(true);
            if (ExampleInput.GetKey(keyA)) RotateUpDown(false);
            if (ExampleInput.GetKey(keyW)) RotateLeftRight(true);
            if (ExampleInput.GetKey(keyS)) RotateLeftRight(false);
            if (ExampleInput.GetKey(keyE)) RotateForwardBackward(true);
            if (ExampleInput.GetKey(keyD)) RotateForwardBackward(false);
        }

        void RotateUpDown(bool rUP)
        {
            float fOrbitCircumfrance = 2F * rotateSpeed * Mathf.PI;
            float fDistanceRadians = (rotateSpeed / fOrbitCircumfrance) * 2 * Mathf.PI;
            if (rUP)
            {
                transform.RotateAround(transform.position, Vector3.up, -fDistanceRadians);
            }
            else
                transform.RotateAround(transform.position, Vector3.up, fDistanceRadians);
        }

        void RotateLeftRight(bool rLeft)
        {
            float fOrbitCircumfrance = 2F * rotateSpeed * Mathf.PI;
            float fDistanceRadians = (rotateSpeed / fOrbitCircumfrance) * 2 * Mathf.PI;
            if (rLeft)
            {
                transform.RotateAround(transform.position, Vector3.right, -fDistanceRadians);
            }
            else
                transform.RotateAround(transform.position, Vector3.right, fDistanceRadians);
        }

        void RotateForwardBackward(bool rForward)
        {
            float fOrbitCircumfrance = 2F * rotateSpeed * Mathf.PI;
            float fDistanceRadians = (rotateSpeed / fOrbitCircumfrance) * 2 * Mathf.PI;
            if (rForward)
            {
                transform.RotateAround(transform.position, Vector3.forward, -fDistanceRadians);
            }
            else
                transform.RotateAround(transform.position, Vector3.forward, fDistanceRadians);
        }
    }
}