using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Level/Level")]
	public class Level : Singleton<Level>
	{
		public UnityEvent<Player> onPlayerChanged;

		protected Player m_player;
		protected PlayerCamera m_camera;

		/// <summary>
		/// Returns the Player activated in the current Level.
		/// </summary>
		public Player player
		{
			get
			{
				if (!m_player)
#if UNITY_6000_0_OR_NEWER
					m_player = FindFirstObjectByType<Player>();
#else
					m_player = FindObjectOfType<Player>();
#endif

				return m_player;
			}
			set
			{
				if (m_player == value)
					return;

				m_player = value;
				onPlayerChanged.Invoke(m_player);
			}
		}

		/// <summary>
		/// Returns the Player Camera activated in the current Level.
		/// </summary>
		public new PlayerCamera camera
		{
			get
			{
				if (!m_camera)
#if UNITY_6000_0_OR_NEWER
					m_camera = FindFirstObjectByType<PlayerCamera>();
#else
					m_camera = FindObjectOfType<PlayerCamera>();
#endif

				return m_camera;
			}
		}

		/// <summary>
		/// Returns true if the Level has been finished.
		/// </summary>
		public bool isFinished { get; set; }

		/// <summary>
		/// Returns the Game Level corresponding to this Level's scene.
		/// </summary>
		public GameLevel gameLevel { get; protected set; }

		/// <summary>
		/// Returns true if the Level has been completed at least once.
		/// </summary>
		public bool isCompleted => gameLevel?.wasCompletedOnce ?? false;

		protected Entity[] m_entities;
		protected Platform[] m_platforms;

		protected override void Awake()
		{
			base.Awake();
			InitializeGame();
			InitializeLevel();
		}

		protected virtual void Start()
		{
			InitializeEntities();
			InitializePlatforms();
		}

		protected virtual void Update()
		{
			UpdateEntities();
			UpdatePlatforms();
		}

		protected virtual void InitializeGame()
		{
			if (!Game.instance.dataLoaded)
				Game.instance.LoadOrCreateState(0);
		}

		protected virtual void InitializeLevel()
		{
			gameLevel = Game.instance.GetCurrentLevel();
		}

		protected virtual void InitializeEntities()
		{
#if UNITY_6000_0_OR_NEWER
			m_entities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
#else
			m_entities = FindObjectsOfType<Entity>();
#endif

			foreach (var entity in m_entities)
				entity.manualUpdate = true;
		}

		protected virtual void InitializePlatforms()
		{
#if UNITY_6000_0_OR_NEWER
			m_platforms = FindObjectsByType<Platform>(FindObjectsSortMode.None);
#else
			m_platforms = FindObjectsOfType<Platform>();
#endif
		}

		protected virtual void UpdateEntities()
		{
			for (int i = 0; i < m_entities.Length; i++)
			{
				try
				{
					m_entities[i].EntityUpdate();
				}
				catch (UnityException e)
				{
					Debug.LogError(
						$"Error updating entity {m_entities[i].name}: {e.Message}",
						m_entities[i]
					);
				}
			}
		}

		protected virtual void UpdatePlatforms()
		{
			for (int i = 0; i < m_platforms.Length; i++)
			{
				try
				{
					m_platforms[i].PlatformUpdate();
				}
				catch (UnityException e)
				{
					Debug.LogError(
						$"Error updating platform {m_platforms[i].name}: {e.Message}",
						m_platforms[i]
					);
				}
			}
		}

		/// <summary>
		/// Returns the array of stars collected in the Level.
		/// </summary>
		public virtual bool[] GetStarts() => (bool[])gameLevel.stars.Clone();

		/// <summary>
		/// Loads the finish scene of this Level.
		/// </summary>
		public virtual void FinishLevel() => gameLevel.FinishLevel();

		/// <summary>
		/// Loads the exit scene of this Level.
		/// </summary>
		public virtual void ExitLevel() => gameLevel.ExitLevel();

		/// <summary>
		/// Beats the Level with the given time, coins and stars.
		/// This method will update the Game Level with the new data and request a data saving.
		/// </summary>
		/// <param name="time">The time it took to beat the Level.</param>
		/// <param name="coins">The amount of coins collected in the Level.</param>
		/// <param name="stars">The array of stars collected in the Level.</param>
		public virtual void BeatLevel(float time, int coins, bool[] stars)
		{
			if (gameLevel == null)
				return;

			if (gameLevel.time == 0 || time < gameLevel.time)
				gameLevel.time = time;

			if (coins > gameLevel.coins)
				gameLevel.coins = coins;

			gameLevel.stars = (bool[])stars.Clone();
			gameLevel.beatenTimes++;
			Game.instance.RequestSaving();
		}
	}
}
