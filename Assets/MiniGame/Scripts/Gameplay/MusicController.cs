using System.Collections;
using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace MiniGame
{
    /// <summary>
    /// Starts, stops and pauses music playback.
    /// </summary>
    public class MusicController : MonoBehaviour
    {
        /// <summary>
        /// Music during main menu.
        /// </summary>
        public AudioSource menu;
        /// <summary>
        /// Music during in-level gameplay.
        /// </summary>
        public AudioSource gameplay;
        /// <summary>
        /// Should the music be started when the game starts?
        /// </summary>
        public bool playOnStart = true;
        /// <summary>
        /// Should the music be changed from menu to gameplay?
        /// </summary>
        public bool changeMusicOnTakeOff = true;

        /// <summary>
        /// If Change Music On Take Off is set to true, this is the rate at which menu theme will fade out
        /// after the user presses Play button.
        /// </summary>
        public float menuMusicFadeOutSpeed = 1.0f;

        private float _initMenuVolume;

        void Awake()
        {
            if (menu != null && this.enabled)
            {
                _initMenuVolume = menu.volume;
                if (playOnStart)
                {
                    menu.Play();
                }
            }

            AudioManager.Instance.StopMusics();
        }

        void OnEnable()
        {
            if (changeMusicOnTakeOff)
            {
                UIEventsPublisher.OnPlayEvent += OnPlayClicked;
                AirplaneTakeOffDetector.OnTakeOffEvent += StartGameplay;
            }
        }

        void OnDisable()
        {
            UIEventsPublisher.OnPlayEvent -= OnPlayClicked;
            AirplaneTakeOffDetector.OnTakeOffEvent -= StartGameplay;
        }

        public virtual void Pause()
        {
            if (menu && menu.isPlaying)
            {
                menu.Pause();
            }

            if (gameplay && gameplay.isPlaying)
            {
                gameplay.Pause();
            }
        }

        public virtual void UnPause()
        {
            if (menu && !menu.isPlaying)
            {
                menu.UnPause();
            }

            if (gameplay && !gameplay.isPlaying)
            {
                gameplay.UnPause();
            }
        }

        private void OnPlayClicked()
        {
            // Fade out music.
            StartCoroutine(FadeOutMenu());
        }

        private IEnumerator FadeOutMenu()
        {
            float tweenStartTime = Time.realtimeSinceStartup;
            var wait = new WaitForEndOfFrame();
            float tweenOutProgress = 1f;

            while (tweenOutProgress > 0.01f)
            {
                // Smooth values change.
                tweenOutProgress = Mathf.SmoothStep(1f, 0f, (Time.realtimeSinceStartup - tweenStartTime) * 0.1f * menuMusicFadeOutSpeed);
                menu.volume = _initMenuVolume * tweenOutProgress;
                yield return wait;
            }

            menu.Stop();
            menu.volume = _initMenuVolume;
        }

        private IEnumerator FadeOutGameplay()
        {
            float initVolume = gameplay.volume;
            float tweenStartTime = Time.realtimeSinceStartup;
            var wait = new WaitForEndOfFrame();
            float tweenOutProgress = 1f;

            while (tweenOutProgress > 0.01f)
            {
                // Smooth values change.
                tweenOutProgress = Mathf.SmoothStep(1f, 0f, (Time.realtimeSinceStartup - tweenStartTime) * 0.5f);
                gameplay.volume = initVolume * tweenOutProgress;
                yield return wait;
            }

            gameplay.Pause();
            gameplay.volume = initVolume;
        }

        private void StartGameplay()
        {
            // Make a short pause to build suspense.
            Invoke("StartGameplayCore", 0.5f);
        }

        private void StartGameplayCore()
        {
            if (gameplay && !gameplay.isPlaying)
            {
                gameplay.Play();
            }
        }

        private void HandleReviveRequest()
        {
            if (gameplay != null)
            {
                StartCoroutine(FadeOutGameplay());
            }
        }

        private void HandleRevive()
        {
            if (gameplay != null)
            {
                StopAllCoroutines();
                StartGameplayCore();
            }
        }
    }

}
