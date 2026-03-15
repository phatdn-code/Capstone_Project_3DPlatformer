using System.Collections;
using UnityEngine;
using Sirenix.OdinInspector;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Phát nhạc nền và chủ động gọi fade khi vào scene.
    /// </summary>
    public class SceneIntroManager : MonoBehaviour
    {
        [Header("Music")]
        [ShowIf(nameof(playOnStart))]
        [SerializeField] private int musicIndex;

        [SerializeField] private bool playOnStart = true;

        [Header("Cursor")]
        [SerializeField] private bool unlockCursorOnStart = false;

        [FoldoutGroup("Fade On Start")]
        [SerializeField] private bool fadeInOnStart = true;

        [FoldoutGroup("Fade On Start"), ShowIf(nameof(fadeInOnStart))]
        [SerializeField, Range(0f, 1f)] private float startAlpha = 1f;

        [FoldoutGroup("Fade On Start"), ShowIf(nameof(fadeInOnStart))]
        [SerializeField] private float startDelay = 0f;

        /// <summary>
        /// Khi vào scene: mở cursor nếu cần, gọi fade rồi phát nhạc nếu cần.
        /// </summary>
        private IEnumerator Start()
        {
            ApplyCursorStateOnStart();

            yield return StartCoroutine(PlayStartFade());
            PlaySceneMusic();
        }

        /// <summary>
        /// Mở cursor khi vào scene nếu được bật.
        /// </summary>
        private void ApplyCursorStateOnStart()
        {
            if (!unlockCursorOnStart)
                return;

            Game.LockCursor(false);
        }

        /// <summary>
        /// Gọi Fader để fade màn hình từ alpha ban đầu xuống 0.
        /// </summary>
        private IEnumerator PlayStartFade()
        {
            if (!fadeInOnStart)
                yield break;

            if (Fader.instance == null)
            {
                Debug.LogWarning("Không tìm thấy Fader trong scene.");
                yield break;
            }

            Fader.instance.SetAlpha(startAlpha);

            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            Fader.instance.FadeIn();
        }

        /// <summary>
        /// Phát nhạc nền của scene.
        /// </summary>
        private void PlaySceneMusic()
        {
            if (!playOnStart)
                return;

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("Không tìm thấy AudioManager trong scene.");
                return;
            }

            AudioManager.Instance.PlayMusic(musicIndex);
        }
    }
}