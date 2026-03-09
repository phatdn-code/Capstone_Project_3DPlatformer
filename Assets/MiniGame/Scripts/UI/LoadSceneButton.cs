using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadSceneButton : MonoBehaviour {
	public string sceneName;
	public virtual void OnClick() {
		SceneManager.LoadScene(sceneName);	
	}
}
