using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniGame
{
    public class RestartLevelButton : MonoBehaviour {

        public virtual void Restart() {
            Time.timeScale = 1;
            // Restart scene.
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
