using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Level/Level Pauser")]
	public class LevelPauser : Singleton<LevelPauser>
	{
		/// <summary>
		/// Called when the Level is Paused.
		/// </summary>
		public UnityEvent OnPause;

		/// <summary>
		/// Called when the Level is unpaused.
		/// </summary>
		public UnityEvent OnUnpause;

		[Tooltip("The UI Container that will be shown when the Level is paused.")]
		public UIContainer pauseScreen;

		protected float m_lastToggleTime;

		/// <summary>
		/// Returns true if it's possible to pause the Level.
		/// </summary>
		public bool canPause { get; set; }

		/// <summary>
		/// Returns true if the Level is paused.
		/// </summary>
		public bool paused { get; protected set; }

		/// <summary>
		/// Sets the pause state based on a given value.
		/// </summary>
		/// <param name="value">The state you want to set the pause to.</param>
		public virtual void Pause(bool value)
		{
			if (paused == value || m_lastToggleTime == Time.unscaledTime)
				return;

			if (!paused)
				Pause();
			else
				Unpause();

			m_lastToggleTime = Time.unscaledTime;
		}

		protected virtual void Pause()
		{
			if (!canPause)
				return;

			Game.LockCursor(false);
			paused = true;
			Time.timeScale = 0;

			if (pauseScreen)
			{
				pauseScreen.SetActive(true);
				pauseScreen.Show();
			}

			OnPause?.Invoke();
		}

		protected virtual void Unpause()
		{
			Game.LockCursor();
			paused = false;
			Time.timeScale = 1;

			if (pauseScreen)
				pauseScreen.Hide();

			OnUnpause?.Invoke();
		}
	}
}
