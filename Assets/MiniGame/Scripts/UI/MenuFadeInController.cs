using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace MiniGame
{
	/// <summary>
	/// Controls the sequence of events during start of the game (showing menus, turning on/off components, etc).
	/// </summary>
	public class MenuFadeInController : MonoBehaviour
	{
		public GameObject mainMenu;

        [Space]
		public CanvasGroup playButton;
		public CanvasGroup controlsButton;

		IEnumerator Start()
		{
			// Init vars.
			if (mainMenu == null)
			{
				mainMenu = GameObject.Find("MainMenu");
				if (mainMenu == null)
				{
					Debug.LogError("Can't find MainMenu object in the scene.");
					yield break;
				}
			}

			mainMenu.SetActive(true);

            if (playButton)
            {
                playButton.interactable = false;
                playButton.alpha = 0;
            }

            if (controlsButton)
            {
                controlsButton.interactable = false;
                controlsButton.alpha = 0;
            }

			// Screen fade in
			yield return new WaitForSeconds(3);

            if (playButton)
            {
                // Buttons fade in
                Fader.FadeAlpha(this, playButton, true, 0.7f);
                playButton.interactable = true;
                yield return new WaitForSeconds(0.5f);
            }

            if (controlsButton)
            {
                Fader.FadeAlpha(this, controlsButton, true, 0.7f);
                controlsButton.interactable = true;
                yield return new WaitForSeconds(0.5f);
            }
		}
	}
}