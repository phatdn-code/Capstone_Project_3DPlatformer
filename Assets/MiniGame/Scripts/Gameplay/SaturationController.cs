using System.Collections;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace MiniGame
{
	/// <summary>
	/// Listens to the take-off events and tweens saturation on the ColorCorrectionCurves 
	/// effect if one is found on the camera.
	/// </summary>
	public class SaturationController : MonoBehaviour
    {
        private ColorCorrectionCurves _colorCorrectionFx;
        private float _saturationTweenStartTime;

        void Start()
        {
            _colorCorrectionFx = GameObject.FindFirstObjectByType<ColorCorrectionCurves>();
            if (_colorCorrectionFx)
            {
                AirplaneTakeOffDetector.OnTakeOffEvent += OnTakeOff;
            }
        }

        void OnDisable()
        {
            AirplaneTakeOffDetector.OnTakeOffEvent -= OnTakeOff;
        }

        private void OnTakeOff()
        {
            StartCoroutine(OnTakeOffCore());
        }

        private IEnumerator OnTakeOffCore()
        {
            // Make a short pause to build suspense.
            yield return new WaitForSeconds(0.5f);

            // Play camera saturation animation.
            _saturationTweenStartTime = Time.time;
            var wait = new WaitForEndOfFrame();
            while (_colorCorrectionFx.saturation < 0.99f)
            {
	            float deltaTime = Time.time - _saturationTweenStartTime;
				_colorCorrectionFx.saturation = Mathf.SmoothStep(0f, 1f, deltaTime * 1.2f);
                yield return wait;
            }

            _colorCorrectionFx.saturation = 1f;
        }
    }

}
