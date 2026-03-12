using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Fader")]
    public class Fader : Singleton<Fader>
    {
        [Header("Fade")]
        public float speed = 1f;

        protected Image m_image;

        public bool IsFading { get; private set; }

        /// <summary>
        /// Fades to black.
        /// </summary>
        public void FadeOut() => FadeOut(() => { });

        /// <summary>
        /// Fades from current alpha to transparent.
        /// </summary>
        public void FadeIn() => FadeIn(() => { });

        /// <summary>
        /// Instantly set black alpha first, then fade to transparent.
        /// </summary>
        public void FadeInFromBlack() => FadeInFromBlack(() => { });

        public void FadeOut(Action onFinished)
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutRoutine(onFinished));
        }

        public void FadeIn(Action onFinished)
        {
            StopAllCoroutines();
            StartCoroutine(FadeInRoutine(onFinished));
        }

        public void FadeInFromBlack(Action onFinished)
        {
            SetAlpha(1f);
            FadeIn(onFinished);
        }

        /// <summary>
        /// Set the fader alpha to a given value.
        /// </summary>
        public virtual void SetAlpha(float alpha)
        {
            if (m_image == null) return;

            var color = m_image.color;
            color.a = Mathf.Clamp01(alpha);
            m_image.color = color;
        }

        /// <summary>
        /// Increases the alpha to one and invokes the callback afterwards.
        /// </summary>
        protected virtual IEnumerator FadeOutRoutine(Action onFinished)
        {
            IsFading = true;

            while (m_image.color.a < 1f)
            {
                var color = m_image.color;
                color.a = Mathf.Min(color.a + speed * Time.deltaTime, 1f);
                m_image.color = color;
                yield return null;
            }

            IsFading = false;
            onFinished?.Invoke();
        }

        /// <summary>
        /// Decreases the alpha to zero and invokes the callback afterwards.
        /// </summary>
        protected virtual IEnumerator FadeInRoutine(Action onFinished)
        {
            IsFading = true;

            while (m_image.color.a > 0f)
            {
                var color = m_image.color;
                color.a = Mathf.Max(color.a - speed * Time.deltaTime, 0f);
                m_image.color = color;
                yield return null;
            }

            IsFading = false;
            onFinished?.Invoke();
        }

        protected override void Awake()
        {
            base.Awake();
            m_image = GetComponent<Image>();
        }
    }
}