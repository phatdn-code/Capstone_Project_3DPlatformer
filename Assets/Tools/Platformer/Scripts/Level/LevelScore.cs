using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Level/Level Score")]
	public class LevelScore : Singleton<LevelScore>
	{
		/// <summary>
		/// Called when the amount of coins have changed.
		/// </summary>
		public UnityEvent<int> OnCoinsSet;

		/// <summary>
		/// Called when the collected stars array have changed.
		/// </summary>
		public UnityEvent<bool[]> OnStarsSet;

		/// <summary>
		/// Called after the level data is fully loaded.
		/// </summary>
		public UnityEvent OnScoreLoaded;

		/// <summary>
		/// Returns the amount of collected coins on the current Level.
		/// </summary>
		public int coins
		{
			get { return m_coins; }
			set
			{
				m_coins = value;
				OnCoinsSet?.Invoke(m_coins);
			}
		}

		/// <summary>
		/// Returns the time since the current Level started.
		/// </summary>
		public float time { get; protected set; }

		/// <summary>
		/// Returns true if the time counter should be updating.
		/// </summary>
		public bool stopTime { get; set; } = true;

		protected int m_coins;
		protected bool[] m_stars;

		/// <summary>
		/// Returns the array of stars on the current Level.
		/// </summary>
		public bool[] stars
		{
			set => m_stars = value;
			get
			{
				m_stars ??= new bool[Game.instance.starsPerLevel];
				return m_stars;
			}
		}

		protected Level m_level => Level.instance;

		/// <summary>
		/// Resets the Level Score to its default values.
		/// </summary>
		public virtual void ResetScore()
		{
			time = 0;
			coins = 0;

			if (m_level != null)
			{
				stars = m_level.GetStarts();
			}
		}

		/// <summary>
		/// Collect a given star from the Stars array.
		/// </summary>
		/// <param name="index">The index of the Star you want to collect.</param>
		public virtual void CollectStar(int index)
		{
			stars[index] = true;
			OnStarsSet?.Invoke(stars);
		}

		/// <summary>
		/// Sends the current score to the Game Level to persist the data.
		/// </summary>
		public virtual void Consolidate()
		{
			if (m_level)
				m_level.BeatLevel(time, coins, stars);
		}

		protected virtual void Start()
		{
			if (m_level)
				stars = m_level.GetStarts();

			OnScoreLoaded.Invoke();
		}

		protected virtual void Update()
		{
			if (!stopTime)
			{
				time += Time.deltaTime;
			}
		}
	}
}
