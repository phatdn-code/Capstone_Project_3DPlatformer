using UnityEngine;

namespace MiniGame
{
	/// <summary>
	/// Lets user disable/enable sound globally.
	/// </summary>
	public class SoundButton : MonoBehaviour {

		public virtual void TurnOff() {
			AudioListener.volume = 0f;
		}

		public virtual void TurnOn() {
			AudioListener.volume = 1f;
		}

	}

}
