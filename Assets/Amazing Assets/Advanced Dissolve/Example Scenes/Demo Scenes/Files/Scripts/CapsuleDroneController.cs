using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class CapsuleDroneController : MonoBehaviour
    {
        public AdvancedDissolveGeometricCutoutController geometricCutoutController;
        public ParticleSystem plasma;
        ParticleSystem.EmissionModule paslamEmissionModule;

        LineRenderer lineRenderer;

        // Use this for initialization
        void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();

            lineRenderer.positionCount = 2;

            paslamEmissionModule = plasma.emission;
        }

        // Update is called once per frame
        void Update()
        {
            if(ExampleInput.GetMouseButton(0))
            {
                Vector3 startPoint = transform.position;
                Vector3 endPoint = LookAtMouse.mouseWorldPosition;

                geometricCutoutController.SetTargetStartPointPosition(AdvancedDissolveKeywords.CutoutGeometricCount.One, startPoint);
                geometricCutoutController.SetTargetEndPointPosition(AdvancedDissolveKeywords.CutoutGeometricCount.One, endPoint);



                if (lineRenderer.enabled == false)
                    lineRenderer.enabled = true;

                lineRenderer.SetPosition(0, startPoint);
                lineRenderer.SetPosition(1, endPoint);


                plasma.gameObject.transform.position = endPoint;
                paslamEmissionModule.enabled = true;
            }

            if (ExampleInput.GetMouseButtonUp(0))
            {
                lineRenderer.enabled = false;

                paslamEmissionModule.enabled = false;
            }
        }
    }
}