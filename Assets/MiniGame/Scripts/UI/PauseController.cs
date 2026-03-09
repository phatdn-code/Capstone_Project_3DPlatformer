using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
#pragma warning disable CS0618

namespace MiniGame
{
	/// <summary>
	/// Listens to user input and shows the pause menu accordingly.
	/// </summary>
    public class PauseController : MonoBehaviour
    {
        public delegate void OnPauseAction();
        public static event OnPauseAction OnPauseEvent;

        public delegate void OnUnPauseAction();
        public static event OnUnPauseAction OnUnPauseEvent;

        public GameObject pausePanelObject;

        private bool _paused = false;

        private MusicController _musicController;

        void Start()
        {
            _musicController = GameObject.FindObjectOfType<MusicController>();
        }

        void Update()
        {
            if (CrossPlatformInputManager.GetButtonDown("Cancel"))
            {
                if (!_paused)
                {
                    if (!pausePanelObject.activeSelf)
                    {
                        Pause();
                    }
                }
                else
                {
                    if (pausePanelObject.activeSelf)
                    {
                        Unpause();
                    }
                }
            }
        }

        public virtual void Pause()
        {
            if (_paused || Time.timeScale == 0) {
                return;
            }
            
            _paused = true;
            Time.timeScale = 0f;
            pausePanelObject.SetActive(true);

            if (_musicController)
            {
                _musicController.Pause();
            }

            PublishPause();
        }

        public virtual void Unpause()
        {
            _paused = false;
            Time.timeScale = 1f;
            pausePanelObject.SetActive(false);

            if (_musicController)
            {
                _musicController.UnPause();
            }

            PublishUnPause();
        }

        public virtual void PublishPause()
        {
            if (OnPauseEvent != null)
            {
                OnPauseEvent();
            }
        }

        public virtual void PublishUnPause()
        {
            if (OnUnPauseEvent != null)
            {
                OnUnPauseEvent();
            }
        }
    }

}
