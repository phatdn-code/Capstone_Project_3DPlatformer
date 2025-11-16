using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class ObjectOrbitalMove : MonoBehaviour
    {
        public Transform target;

        float fDistance;
        public float rotateSpeed = 10;

        public float maxDistance = 20;



#if ENABLE_INPUT_SYSTEM
        Key keyD = Key.D;
        Key keyA = Key.A;
        Key keyW = Key.W;
        Key keyS = Key.S;
#else
        KeyCode keyD = KeyCode.D;
        KeyCode keyA = KeyCode.A;
        KeyCode keyW = KeyCode.W;
        KeyCode keyS = KeyCode.S;
#endif


        // Use this for initialization
        void Start()
        {
            fDistance = Vector3.Distance(transform.position, target.position);
        }

        // Update is called once per frame
        void Update()
        {
            if (ExampleInput.GetKey(keyD)) RotateLeftRight(true);
            if (ExampleInput.GetKey(keyA)) RotateLeftRight(false);
            if (ExampleInput.GetKey(keyW)) RotateUpDown(true);
            if (ExampleInput.GetKey(keyS)) RotateUpDown(false);


            if (ExampleInput.GetKeyLeftControl())
                MoveForwardBackward();
        }

        void RotateLeftRight(bool bLeft)
        {
            float fOrbitCircumfrance = 2F * fDistance * Mathf.PI;
            float fDistanceRadians = (rotateSpeed / fOrbitCircumfrance) * 2 * Mathf.PI;
            if (bLeft)
            {
                transform.RotateAround(target.position, Vector3.up, -fDistanceRadians);
            }
            else
                transform.RotateAround(target.position, Vector3.up, fDistanceRadians);
        }

        void RotateUpDown(bool bUp)
        {
            Vector3 forwardV = (transform.position - target.position).normalized;
            Vector3 upV = transform.up;
            Vector3 rotateAxis = Vector3.Cross(forwardV, upV);

            float dot = Vector3.Dot(forwardV, Vector3.up);


            float fOrbitCircumfrance = 2F * fDistance * Mathf.PI;
            float fDistanceRadians = (rotateSpeed / fOrbitCircumfrance) * 2 * Mathf.PI;
            if (bUp)
            {
                if (dot < 0.8f)
                    transform.RotateAround(target.position, rotateAxis, fDistanceRadians);
            }
            else
            {
                if (dot > 0.1f)
                    transform.RotateAround(target.position, rotateAxis, -fDistanceRadians);
            }
        }

        void MoveForwardBackward()
        {
            float t = Vector3.Distance(transform.position, target.position) / maxDistance;

            t -= ExampleInput.GetMouseScrollDeltaY() * 0.02f;
            t = Mathf.Clamp(t, 0.35f, 1);


            Vector3 mVector = (transform.position - target.position).normalized;

            transform.position = target.position + mVector * t * maxDistance;
        }

    }
}