using UnityEngine;
using PLAYERTWO.PlatformerProject;

public class UI_LevelController : MonoBehaviour
{
	public UIContainer settingScreen;

	protected LevelFinisher m_finisher => LevelFinisher.instance;
	// protected LevelRespawner m_respawner => LevelRespawner.instance;
	// protected LevelScore m_score => LevelScore.instance;
	protected LevelPauser m_pauser => LevelPauser.instance;

	public virtual void Pause(bool value)
	{
		m_pauser.Pause(value);
	}

	public virtual void Setting(bool value)
	{
		if (value)
		{
			settingScreen.SetActive(true);
			settingScreen.Show();
		}
		else if(settingScreen.gameObject.activeSelf)
		{
			settingScreen.Hide();
		}
	}

	public virtual void Exit() => m_finisher.Exit();

}

