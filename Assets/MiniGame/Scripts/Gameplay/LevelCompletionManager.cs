using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniGame
{
    /// <summary>
    /// Quản lý các hành động khi người chơi hoàn thành level.
    /// Có thể restart level hiện tại hoặc chuyển sang level tiếp theo.
    /// </summary>
    public class LevelCompletionManager : MonoBehaviour
    {
        /// <summary>
        /// Nếu bật, khi hoàn thành level sẽ load lại scene hiện tại.
        /// Nếu tắt, game sẽ chuyển sang scene tiếp theo.
        /// </summary>
        [Tooltip("Restart scene hiện tại thay vì chuyển sang scene tiếp theo.")]
        public bool restartCurrentScene = false;

        /// <summary>
        /// Hàm được gọi khi level hoàn thành.
        /// </summary>
        public virtual void HandleLevelComplete()
        {
            // Nếu chọn restart level hiện tại
            if (restartCurrentScene)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            else
            {
                // Lấy index của scene tiếp theo
                int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

                // Nếu tồn tại scene tiếp theo trong Build Settings
                if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
                {
                    SceneManager.LoadScene(nextSceneIndex);
                }
                else
                {
                    // Không còn scene nào phía sau → đây là level cuối

                    // Tạm dừng game
                    var pause = GameObject.FindFirstObjectByType<PauseController>();
                    if (pause != null)
                    {
                        pause.Pause();
                    }

                    // Log lỗi để báo rằng đang cố load scene không tồn tại
                    Debug.LogError("MiniGame: Trying to load next scene from the last scene.");
                }
            }
        }
    }
}