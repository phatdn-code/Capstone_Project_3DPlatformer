using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	public abstract class EntityStatsManager<T> : MonoBehaviour where T : EntityStats<T>
	{
		public T[] stats;

		/// <summary>
		/// The instance of the current activated Stats.
		/// </summary>
		public T current { get; protected set; }

		/// <summary>
		/// Changes from the current stats to the desired one.
		/// </summary>
		/// <param name="to">The desired index of the Stats you want.</param>
		public virtual void Change(int to)
		{
			if (to >= 0 && to < stats.Length)
			{
				if (current != stats[to])
				{
					current = stats[to];
				}
			}
		}

		protected virtual void Start()
		{
			if (stats.Length > 0)
			{
				current = stats[0];
			}
		}
	}
}
