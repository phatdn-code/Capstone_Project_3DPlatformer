using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class UIChangeSize : MonoBehaviour
    {
        public AdvancedDissolveGeometricCutoutController geometricCutoutController;

        public Light spotLight;
        public float radius = 5;


#if ENABLE_INPUT_SYSTEM
        Key keyPlus = Key.NumpadPlus;
        Key keyKeypadPlus = Key.Equals;
        Key keyMinus = Key.Minus;
        Key keyKeypadMinus = Key.NumpadMinus;
#else
        KeyCode keyPlus = KeyCode.Plus;
        KeyCode keyKeypadPlus = KeyCode.KeypadPlus;
        KeyCode keyMinus = KeyCode.Minus;
        KeyCode keyKeypadMinus = KeyCode.KeypadMinus;
#endif


        // Update is called once per frame
        void Update()
        {
            if (ExampleInput.GetKey(keyPlus) || ExampleInput.GetKey(keyKeypadPlus))
            {
                radius += Time.deltaTime * 3;
                radius = Mathf.Clamp(radius, 1, 20);
            }

            if (ExampleInput.GetKey(keyMinus) || ExampleInput.GetKey(keyKeypadMinus))
            {
                radius -= Time.deltaTime * 3;
                radius = Mathf.Clamp(radius, 1, 20);
            }


            //Update mask object (first mask object only)
            //Note, in demo scenes we have only one mask controller
            switch (geometricCutoutController.type)
            {
                case AdvancedDissolveKeywords.CutoutGeometricType.Sphere:
                    geometricCutoutController.target1Radius = radius;
                    break;
                case AdvancedDissolveKeywords.CutoutGeometricType.Cube:
                    transform.localScale = Vector3.one * radius;
                    break;
                case AdvancedDissolveKeywords.CutoutGeometricType.Capsule:
                    geometricCutoutController.target1Radius = radius;
                    break;
                case AdvancedDissolveKeywords.CutoutGeometricType.ConeSmooth:
                    {
                        spotLight.spotAngle = radius * 2;
                    }
                    break;
            }
        }
    }
}