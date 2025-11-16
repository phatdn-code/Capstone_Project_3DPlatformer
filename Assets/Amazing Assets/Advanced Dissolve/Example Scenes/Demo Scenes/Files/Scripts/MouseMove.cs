using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class MouseMove : MonoBehaviour
    { 
        // Update is called once per frame 
        void Update()
        {
            if (ExampleInput.GetKeyLeftShift() == false &&
                ExampleInput.GetKeyRightShift() == false &&
                (ExampleInput.GetKeyLeftControl() || ExampleInput.GetKeyRightControl()))
            {
                transform.position -= transform.up * ExampleInput.GetMouseScrollDeltaY();
            }
        }
    } 
} 