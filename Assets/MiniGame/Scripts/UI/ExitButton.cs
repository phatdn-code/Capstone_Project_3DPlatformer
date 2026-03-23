using PLAYERTWO.PlatformerProject;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// A button to quit the application completely.
/// </summary>
public class ExitButton : MonoBehaviour
{
    [TitleGroup("Level Portal Pin")]
    [BoxGroup("Level Portal Pin/Scene")]
    [SerializeField] private string targetSceneName;

    public virtual void Activate()
    {
        Time.timeScale = 1;

        if (GameLoader.instance != null)
            GameLoader.instance.Load(targetSceneName);
    }

}
