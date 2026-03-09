using UnityEngine;
using System.Collections;
using System;

namespace MiniGame
{
	/// <summary>
	/// Utility class to fade in/out UI elements.
	/// </summary>
    public class Fader : MonoBehaviour
    {
        public static void FadeAlpha(MonoBehaviour container, CanvasGroup group, bool fadeIn, float speed,
                Action onComplete = null)
        {
            if (group.gameObject.activeSelf)
            {
                container.StartCoroutine(Fader.FadeAlpha(group, fadeIn, speed, onComplete));
            }
        }

        public static IEnumerator FadeAlpha(CanvasGroup group, bool fadeIn, float speed, Action onComplete = null)
        {
            // Use Time.realtimeSinceStartup instead of deltaTime so that tweens are not affected by timeScale.
            float timeLast = Time.realtimeSinceStartup;
            float timeCurrent = timeLast;

            while ((fadeIn && group.alpha < 1f) || (!fadeIn && group.alpha > 0f))
            {
                timeCurrent = Time.realtimeSinceStartup;
                group.alpha += (fadeIn ? 1 : -1) * speed * (timeCurrent - timeLast);
                timeLast = timeCurrent;
                yield return null;
            }

            if (onComplete != null)
            {
                onComplete();
            }
        }

    }

}
