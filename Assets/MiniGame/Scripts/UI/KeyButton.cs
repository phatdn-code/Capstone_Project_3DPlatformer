using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;

namespace MiniGame
{
	/// <summary>
	/// Binds any of the keys specified to a virtual button on CrossPlatformInput.
	/// </summary>
	[RequireComponent(typeof (Button))]
	public class KeyButton : MonoBehaviour
	{
		public string[] axis;
		public KeyCode[] keys;

		public Button button { get; private set; }

		void Awake()
		{
			button = GetComponent<Button>();
		}

		void Update()
		{
			if (!button.interactable)
			{
				return;
			}

			bool keyDown = false;

			foreach (string key in axis)
			{
				if (CrossPlatformInputManager.GetButtonDown(key))
				{
					keyDown = true;
					break;
				}
			}

			foreach (KeyCode key in keys)
			{
				if (Input.GetKeyDown(key))
				{
					keyDown = true;
					break;
				}
			}

			if (keyDown)
			{
				button.onClick.Invoke();
			}
		}
	}
}