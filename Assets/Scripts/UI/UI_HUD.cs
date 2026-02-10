using TMPro;
using UnityEngine;
using PLAYERTWO.PlatformerProject;
using UnityEngine.UI;

public class UI_HUD : MonoBehaviour
{
	[Header("Format Settings")]
	[Tooltip("The format to display the retries counter.")]
	public string retriesFormat = "00";

	[Tooltip("The format to display the coins counter.")]
	public string coinsFormat = "000";

	[Tooltip("The format to display the health counter.")]
	public string healthFormat = "0";

	[Header("UI Elements")]
	[Tooltip("The text to display the retries counter.")]
	public TMP_Text retries;

	[Tooltip("The text to display the coins counter.")]
	public TMP_Text coins;

	[Tooltip("The text to display the health counter.")]
	public TMP_Text health;

	[Tooltip("The images to display the stars.")]
	public Image[] starsImages;

	protected Game m_game;
	protected LevelScore m_score;

	protected Player m_player => Level.instance.player;

	protected virtual void Start()
	{
		m_game = Game.instance;
		m_score = LevelScore.instance;
		m_score.OnScoreLoaded.AddListener(() =>
		{
			m_score.OnCoinsSet.AddListener(UpdateCoins);
			m_score.OnStarsSet.AddListener(UpdateStars);
			m_game.OnRetriesSet.AddListener(UpdateRetries);
			m_player.health.onChange.AddListener(UpdateHealth);
			Refresh();
		});

		Level.instance.onPlayerChanged.AddListener(
			(player) => player.health.onChange.AddListener(UpdateHealth)
		);
	}

	/// <summary>
	/// Set the coin counter to a given value.
	/// </summary>
	protected virtual void UpdateCoins(int value)
	{
		coins.text = value.ToString(coinsFormat);
	}

	/// <summary>
	/// Set the retries counter to a given value.
	/// </summary>
	protected virtual void UpdateRetries(int value)
	{
		retries.text = value.ToString(retriesFormat);
	}

	/// <summary>
	/// Called when the Player Health changed.
	/// </summary>
	protected virtual void UpdateHealth()
	{
		health.text = m_player.health.current.ToString(healthFormat);
	}

	/// <summary>
	/// Set the stars images enabled state to match a boolean array.
	/// </summary>
	protected virtual void UpdateStars(bool[] value)
	{
		for (int i = 0; i < starsImages.Length; i++)
		{
			starsImages[i].enabled = value[i];
		}
	}

	/// <summary>
	/// Called to force an updated on the HUD.
	/// </summary>
	public virtual void Refresh()
	{
		UpdateCoins(m_score.coins);
		UpdateRetries(m_game.retries);
		UpdateHealth();
		UpdateStars(m_score.stars);
	}
}
