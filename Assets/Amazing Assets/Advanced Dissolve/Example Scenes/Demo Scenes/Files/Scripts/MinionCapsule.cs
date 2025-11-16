using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class MinionCapsule : MonoBehaviour
    {
        public AdvancedDissolveGeometricCutoutController geometricCutoutController;
        public AdvancedDissolve.AdvancedDissolveKeywords.CutoutGeometricCount countID;

        public Transform target;

        public GameObject drone;
        public LineRenderer lineRenderer;


        float fDistance;
        float rotateSpeed = 10;
        
        
        float deltaDesigion;
        float deltaDesigionRandomTime;

        float deltaShooting;


        int rotateUpDownLeftRight;
        float deltaRotate;

        

        private void Start()
        {
            fDistance = Vector3.Distance(transform.position, target.position);


            deltaDesigionRandomTime = Random.Range(1f, 3f);
            deltaShooting = 0;


            rotateUpDownLeftRight = Random.Range(0, 4);
            deltaRotate = Random.Range(1f, 4f);

            rotateSpeed = 7 * Random.Range(0.3f, 1.0f);

            lineRenderer.positionCount = 2;
        }

        private void Update()
        {
            //Shooting//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            deltaShooting -= Time.deltaTime;
            if (deltaShooting < 0)
                deltaDesigion += Time.deltaTime;


            if (deltaDesigion > deltaDesigionRandomTime)
            {
                //Long shoot or small
                if (Random.value > 0.05f)
                    deltaShooting = Random.Range(0.025f, 0.05f);
                else
                    deltaShooting = Random.Range(2f, 8f);


                deltaDesigion = 0;
                deltaDesigionRandomTime = deltaShooting + Random.Range(1f, 4f);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            deltaRotate -= Time.deltaTime;
            if (deltaRotate > 0)
            {
                if (rotateUpDownLeftRight == 0)
                    RotateUpDown(true);
                else if (rotateUpDownLeftRight == 1)
                    RotateUpDown(false);
                else if (rotateUpDownLeftRight == 2)
                    OrbitTower(true);
                else if (rotateUpDownLeftRight == 3)
                    OrbitTower(false);
            }
            if (deltaRotate < -0.5f)
            {
                if (rotateUpDownLeftRight == 0)
                {
                    if (Random.value > 0.5f)
                        rotateUpDownLeftRight = 1;
                    else
                        rotateUpDownLeftRight = Random.Range(2, 4);
                }
                else if (rotateUpDownLeftRight == 1)
                {
                    if (Random.value > 0.3f)
                        rotateUpDownLeftRight = 0;
                    else
                        rotateUpDownLeftRight = Random.Range(2, 4);
                }
                else
                {
                    if (Random.value > 0.3f)
                        rotateUpDownLeftRight = Random.Range(2, 4);
                    else
                        rotateUpDownLeftRight = Random.Range(0, 2);
                }


                deltaRotate = Random.Range(1f, 4f);

                rotateSpeed = 7 * Random.Range(0.3f, 1.0f);
            }

            //If falls down -> move up
            Vector3 correctPos = transform.position;
            if (correctPos.y < 11)
            {
                correctPos.y = 11;

                deltaRotate -= 0.3f;
            }
            transform.position = correctPos;

            drone.transform.LookAt(GetTargetPoint());



            //Update shader data only during shooting
            if (deltaShooting > 0.01f)
            {
                UpdateShaderData();


                lineRenderer.enabled = true;
                lineRenderer.SetPosition(0, drone.transform.position);
                lineRenderer.SetPosition(1, target.position);
            }
            else
                lineRenderer.enabled = false;
        }


        void UpdateShaderData()
        {
            Vector3 startPoint = drone.transform.position;
            Vector3 endPoint = startPoint + drone.transform.forward * 50;

            geometricCutoutController.SetTargetStartPointPosition(countID, startPoint);
            geometricCutoutController.SetTargetEndPointPosition(countID, endPoint);
            geometricCutoutController.SetTargetRadius(countID, 1.25f);
        }

        protected void OrbitTower(bool bLeft)
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

        protected void RotateUpDown(bool bUp)
        {
            Vector3 forwardV = (transform.position - GetTargetPoint()).normalized;
            Vector3 upV = transform.up;
            Vector3 rotateAxis = Vector3.Cross(forwardV, upV);

           float dot = Vector3.Dot(forwardV, Vector3.up);
            dot = Mathf.Clamp(dot, 0.3f, 0.9f);

            float fOrbitCircumfrance = 2F * fDistance * Mathf.PI;
            float fDistanceRadians = (rotateSpeed / fOrbitCircumfrance) * 2 * Mathf.PI;
            if (bUp)
            {
                if (dot < 0.89f)
                    transform.RotateAround(target.position, rotateAxis, fDistanceRadians);
            }
            else
            {
                if (dot > 0.31f)
                    transform.RotateAround(target.position, rotateAxis, -fDistanceRadians);
            }
        }

        public Vector3 GetTargetPoint()
        {
            Vector3 pos = target.position;
            pos.y = 6;

            return pos;
        }
    }
}