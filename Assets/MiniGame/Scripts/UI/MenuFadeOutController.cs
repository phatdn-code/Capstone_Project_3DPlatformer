using UnityEngine;
using UnityEngine.UI;

namespace MiniGame
{
	/// <summary>
	/// Manages timing of fading out the Main Menu.
	/// </summary>
	public class MenuFadeOutController : MonoBehaviour
	{
		public CanvasGroup playButton;
		public CanvasGroup controlsButton;

		void Start()
		{
            UIEventsPublisher.OnPlayEvent += FadeOut;
		}
        
        void OnDeactivate()
        {
            UIEventsPublisher.OnPlayEvent -= FadeOut;
        }

		public virtual void FadeOut()
		{
			// Disable buttons.
			if (playButton) playButton.interactable = false;
			if (controlsButton) controlsButton.interactable = false;

			// UI fade out.
			if (playButton) Fader.FadeAlpha(this, playButton, false, 1f);
			if (controlsButton) Fader.FadeAlpha(this, controlsButton, false, 1.5f);
		}
	}
}