using UnityEngine;

/// <summary>
/// A button to quit the application completely.
/// </summary>
public class ExitButton : MonoBehaviour {

	public virtual void Activate() {
		Application.Quit();
	}

}
