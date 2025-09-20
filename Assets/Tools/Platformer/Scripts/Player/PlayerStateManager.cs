using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(Player))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player State Manager")]
	public class PlayerStateManager : EntityStateManager<Player>
	{
		[ClassTypeName(typeof(PlayerState))]
		public string[] states;

		protected override List<EntityState<Player>> GetStateList()
		{
			return PlayerState.CreateListFromStringArray(states);
		}
	}
}
