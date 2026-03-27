using UnityEngine;

public class UI_QuitGame : MonoBehaviour
{
   public void QuitGame()
   {
      PLAYERTWO.PlatformerProject.Fader.instance.FadeOut(() =>
      {
         Application.Quit();
      });
   }
}
