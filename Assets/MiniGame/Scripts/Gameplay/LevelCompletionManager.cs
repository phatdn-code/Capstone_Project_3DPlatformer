using UnityEngine;
using UnityEngine.SceneManagement;
using PLAYERTWO.PlatformerProject;

namespace MiniGame
{
    /// <summary>
    /// Quản lý các hành động khi người chơi hoàn thành level.
    /// Có thể restart level hiện tại, chuyển sang scene chỉ định,
    /// hoặc chuyển sang scene tiếp theo trong Build Settings.
    /// </summary>
    public class LevelCompletionManager : MonoBehaviour
    {
        /// <summary>
        /// VN: Nếu bật, khi hoàn thành level sẽ load lại scene hiện tại.
        /// Nếu tắt, game sẽ ưu tiên load targetScene nếu có,
        /// còn không thì chuyển sang scene tiếp theo.
        /// </summary>
        [Tooltip("Restart scene hiện tại thay vì chuyển scene khác.")]
        public bool restartCurrentScene = false;

        /// <summary>
        /// VN: Nếu có nhập scene này, HandleLevelComplete sẽ load scene này
        /// thay vì tự động qua scene tiếp theo.
        /// </summary>
        [Tooltip("Nếu có giá trị, sẽ load scene này thay vì scene tiếp theo.")]
        [SerializeField] private string targetScene;

        [Tooltip("ID dùng để đánh dấu minigame này đã hoàn thành.")]
        [SerializeField] private string miniGameId;

        [Tooltip("Bật nếu khi hoàn thành minigame sẽ lưu trạng thái clear.")]
        [SerializeField] private bool markMiniGameCompletedOnFinish = true;

        /// <summary>
        /// VN: Hàm được gọi khi level hoàn thành.
        /// </summary>
        public virtual void HandleLevelComplete()
        {
            if (markMiniGameCompletedOnFinish && Game.instance != null)
                Game.instance.MarkMiniGameCompleted(miniGameId);

            if (restartCurrentScene)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                return;
            }

            if (!string.IsNullOrEmpty(targetScene))
            {
                if (GameLoader.instance != null)
                {
                    GameLoader.instance.Load(targetScene);
                    return;
                }

                SceneManager.LoadScene(targetScene);
                return;
            }

            int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
                SceneManager.LoadScene(nextSceneIndex);

            else
            {
                var pause = GameObject.FindFirstObjectByType<PauseController>();

                if (pause != null)
                    pause.Pause();

                Debug.LogError("MiniGame: Trying to load next scene from the last scene.");
            }
        }
    }
}