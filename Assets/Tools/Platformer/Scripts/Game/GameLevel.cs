using System;
using System.Linq;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[Serializable]
	public class GameLevel
	{
		[Header("General Settings")]
		[Tooltip("The name of the level. This will be displayed in the level selection.")]
		public string name;

		[Tooltip("The description of the level. This will be displayed in the level selection.")]
		public string description;

		[Tooltip("The image of the level. This will be displayed in the level selection.")]
		public Sprite image;

		[Header("Locking Settings")]
		[Tooltip(
			"This level will be inaccessible from the level selection unless manually unlocked from code."
		)]
		public bool locked;

		[Min(0)]
		[Tooltip(
			"If greater than 0, this property overrides the 'locked' flag and makes the level inaccessible if the total stars is not enough."
		)]
		public int requiredStars;

		[Header("Start Level Settings")]
		[Tooltip("Scene to load when the Level is played.")]
		public string scene;

		[Tooltip("Scene to load when the Level is played for the first time.")]
		public string firstTimeScene;

		[Header("Finish Level Settings")]
		[Tooltip("Unlocks the next level from the level list unless it's locked by stars counter.")]
		public bool unlockNextLevel;

		[Tooltip("Scene name to load when the Level is finished.")]
		public string nextScene;

		[Tooltip("Scene name to load when the Level is finished for the first time.")]
		public string firstTimeNextScene;

		[Header("Exit Level Settings")]
		[Tooltip(
			"Scene name to load when the Level is exited. If empty, it will load the global exit scene."
		)]
		public string exitScene;

		protected bool[] m_stars;

		/// <summary>
		/// Returns the amount of coins collected in the level.
		/// </summary>
		public int coins { get; set; }

		/// <summary>
		/// Returns the time in which this level has been beaten.
		/// </summary>
		/// <value></value>
		public float time { get; set; }

		/// <summary>
		/// Returns the array of collected or non collected stars.
		/// </summary>
		public bool[] stars
		{
			get
			{
				m_stars ??= new bool[Game.instance.starsPerLevel];
				return m_stars;
			}
			set => m_stars = value;
		}

		/// <summary>
		/// Returns the amount of times this level has been beaten.
		/// </summary>
		public int beatenTimes { get; set; }

		/// <summary>
		/// Returns true if this level has been completed at least once.
		/// </summary>
		public bool wasCompletedOnce => beatenTimes > 0;

		/// <summary>
		/// Returns the amount of collected stars in this level.
		/// </summary>
		public virtual int CollectedStarsCount() =>
			stars.Aggregate(0, (acc, star) => acc + (star ? 1 : 0));

		/// <summary>
		/// Loads this Game Level state from a given Game Data.
		/// </summary>
		/// <param name="data">The Game Data to read the state from.</param>
		public virtual void LoadState(LevelData data)
		{
			locked = data.locked;
			coins = data.coins;
			time = data.time;
			stars = data.stars;
			beatenTimes = data.beatenTimes;
		}

		/// <summary>
		/// Loads the scene of this Game Level. If the first time scene is set
		/// and the level has not been completed, it will load the first time scene instead.
		/// </summary>
		public virtual void StartLevel()
		{
			if (wasCompletedOnce || string.IsNullOrEmpty(firstTimeScene))
				GameLoader.instance.Load(scene);
			else
				GameLoader.instance.Load(firstTimeScene);
		}

		/// <summary>
		/// Loads the next scene of this Game Level. If the first time scene is set
		/// and the level has not been completed, it will load the first time scene instead.
		/// If the "unlock next level" flag is set, it will unlock the next level from the level list.
		/// </summary>
		public virtual void FinishLevel()
		{
			if (unlockNextLevel)
				Game.instance.UnlockNextLevel();

			if (beatenTimes > 1 || string.IsNullOrEmpty(firstTimeNextScene))
				GameLoader.instance.Load(nextScene);
			else
				GameLoader.instance.Load(firstTimeNextScene);
		}

		/// <summary>
		/// Loads the exit scene of this Game Level. If the exit scene is not set,
		/// it will load the global exit scene from the Game instance.
		/// </summary>
		public virtual void ExitLevel()
		{
			if (string.IsNullOrEmpty(exitScene))
				GameLoader.instance.Load(Game.instance.levelExitScene);
			else
				GameLoader.instance.Load(exitScene);
		}

		/// <summary>
		/// Returns this Level Data of this Game Level to be used by the Data Layer.
		/// </summary>
		public virtual LevelData ToData()
		{
			return new LevelData()
			{
				locked = locked,
				coins = coins,
				time = time,
				stars = (bool[])stars.Clone(),
				beatenTimes = beatenTimes,
			};
		}

		/// <summary>
		/// Returns a given time as a formatted string.
		/// </summary>
		/// <param name="time">The time you want to format.</param>
		/// <param name="minutesSeparator">The separator between minutes and seconds.</param>
		/// <param name="secondsSeparator">The separator between seconds and milliseconds.</param>
		public static string FormattedTime(
			float time,
			string minutesSeparator = "'",
			string secondsSeparator = "\""
		)
		{
			var minutes = Mathf.FloorToInt(time / 60f);
			var seconds = Mathf.FloorToInt(time % 60f);
			var milliseconds = Mathf.FloorToInt(time * 100f % 100f);
			return minutes.ToString("0")
				+ minutesSeparator
				+ seconds.ToString("00")
				+ secondsSeparator
				+ milliseconds.ToString("00");
		}
	}
}
