using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class MinionCube : MonoBehaviour
    {
        public AdvancedDissolveGeometricCutoutController geometricCutoutController;
        public AdvancedDissolve.AdvancedDissolveKeywords.CutoutGeometricCount countID;


        float delta;

        Vector3 scale;
        float moveSpeed;
        Vector3 randomRotate;
        float rotateSpeed;

        private void OnEnable()
        {
            geometricCutoutController.SetTargetStartPointTransform(countID, transform);
        }

        // Use this for initialization
        void Start()
        {
            Init();
        }

        // Update is called once per frame
        void Update()
        {
            delta += Time.deltaTime * 0.5f;
            transform.localScale = scale * Mathf.Clamp01(delta * delta * 5);

            transform.Translate(Vector3.down * Time.deltaTime * moveSpeed, Space.World);

            transform.Rotate(randomRotate, rotateSpeed * Time.deltaTime, Space.Self);


            if (transform.position.y < 0)
                Init();
        }

        void Init()
        {
            delta = 0;

            transform.position = new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(25.0f, 35.0f), Random.Range(-5.0f, 5.0f));
            transform.rotation = Random.rotation;
            transform.localScale = Vector3.zero;

            scale = new Vector3(Random.Range(1.0f, 8.0f), Random.Range(1.0f, 8.0f), Random.Range(1.0f, 8.0f));


            randomRotate = Random.onUnitSphere;
            rotateSpeed = Random.Range(2.0f, 6.0f);

            moveSpeed = Random.Range(2.0f, 6.0f);
        }
    }
}