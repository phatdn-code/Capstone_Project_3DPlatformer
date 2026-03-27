using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class UI_QuitGame : MonoBehaviour
    {
        public void QuitGame()
        {
            Fader.instance.FadeOut(() =>
            {
                Application.Quit();
            });
        }
    }
}
