using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class SceneBackgroundMusic : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField] private int musicIndex;

        [Header("Option")]
        [SerializeField] private bool playOnStart = true;

        // Tự phát nhạc nền khi vào scene
        private void Start()
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