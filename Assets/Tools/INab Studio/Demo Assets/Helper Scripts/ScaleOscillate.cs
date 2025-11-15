using UnityEngine;


namespace INab.Demo
{
    [ExecuteAlways]
    public class ScaleOscillate : MonoBehaviour
    {
        public AnimationCurve scaleCurve = new AnimationCurve(
            new Keyframe(0f, 0f),   // Time: 0, Value: 0
            new Keyframe(0.5f, 1f), // Time: 0.5, Value: 1
            new Keyframe(1f, 0f)    // Time: 1, Value: 0
        );

        public Vector3 minScale = new Vector3(0.3f, 0.3f, 0.3f);
        public Vector3 maxScale = new Vector3(1, 1, 1);
        public float oscillationSpeed = 0.4f;

        private float elapsedTime;

        public bool updateInEditor = false;


        private void Update()
        {
            if (updateInEditor || Application.isPlaying)
            {
                elapsedTime += Time.deltaTime * oscillationSpeed;
                elapsedTime = Mathf.Repeat(elapsedTime, 1);

                float curveValue = scaleCurve.Evaluate(elapsedTime);
                transform.localScale = Vector3.Lerp(minScale, maxScale, curveValue);
            }
        }
    }
}