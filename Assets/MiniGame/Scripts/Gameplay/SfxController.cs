using UnityEngine;

namespace MiniGame
{
    public class SfxController : MonoBehaviour
    {
        [Tooltip("2D audio source to play sound effects on.")]
        public AudioSource audioSource;

        [Space]

        [Tooltip("Sound played when time is running low.")]
        public AudioClip lowTimeSound;

        [Tooltip("Sound played when time runs out (level failed).")]
        public AudioClip levelFailSound;

        [Tooltip("Sound played when player is revived.")]
        public AudioClip userRevivedSound;

        void OnEnable()
        {
            if (audioSource == null)
            {
                enabled = false;
                return;
            }

            CountdownController.OnTimeLowEvent += HandleTimeLow;
            CountdownController.OnTimeEmptyEvent += HandleTimeEmpty;
        }

        void OnDisable()
        {
            CountdownController.OnTimeLowEvent -= HandleTimeLow;
            CountdownController.OnTimeEmptyEvent -= HandleTimeEmpty;
        }

        /// <summary>
        /// Khi thời gian sắp hết
        /// </summary>
        private void HandleTimeLow()
        {
            if (lowTimeSound != null)
            {
                audioSource.PlayOneShot(lowTimeSound);
            }
        }

        /// <summary>
        /// Khi hết thời gian
        /// </summary>
        private void HandleTimeEmpty()
        {
            if (levelFailSound != null)
            {
                audioSource.PlayOneShot(levelFailSound);
            }
        }

        /// <summary>
        /// Khi người chơi revive
        /// </summary>
        private void HandleRevive()
        {
            if (userRevivedSound != null)
            {
                audioSource.PlayOneShot(userRevivedSound);
            }
        }
    }
}