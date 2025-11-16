using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class LookAtMouse : MonoBehaviour
    {
        public static Vector3 mouseWorldPosition;

        // Use this for initialization
        void Start()
        {
            mouseWorldPosition = new Vector3(0, 8, -3);
        }

        // Update is called once per frame
        void Update()
        {
            if (ExampleInput.GetKeyLeftControl())
            {
                Ray ray = Camera.main.ScreenPointToRay(ExampleInput.MousePosition());
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    mouseWorldPosition = hit.point;
                }

            }

            transform.LookAt(mouseWorldPosition);
        }
    }
}