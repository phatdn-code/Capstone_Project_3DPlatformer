using UnityEngine;
using UnityEngine.UI;

namespace MiniGame
{
	/// <summary>
	/// Fades in an image contained on current object when UIEventsPublisher.OnPlayEvent is published.
	/// </summary>
    [RequireComponent(typeof(Image))]
    public class FadeInOnStart : MonoBehaviour
    {
        /// <summary>
        /// If this should be triggered by UIEventsPublisher.OnPlayEvent event.
        /// </summary>
        public bool listenToPlayEvent = true;
        
		/// <summary>
		/// Duration of fade-in in seconds.
		/// </summary>
        public float duration = 1f;

        void Start()
        {
            if (listenToPlayEvent)
            {
                UIEventsPublisher.OnPlayEvent += FadeIn;
            }
        }
        
        void OnDestroy()
        {
            UIEventsPublisher.OnPlayEvent -= FadeIn;
        }
        
        public virtual void FadeIn()
        {
            var image = GetComponent<Image>();
            if (image != null)
            {
                image.canvasRenderer.SetAlpha(0.0f);
                image.enabled = true;
                image.CrossFadeAlpha(1.0f, duration, false);
            }
        }
    }

}