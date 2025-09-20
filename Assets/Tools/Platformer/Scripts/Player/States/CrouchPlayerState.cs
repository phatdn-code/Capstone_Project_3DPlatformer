using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/States/Crouch Player State")]
	public class CrouchPlayerState : PlayerState
	{
		protected override void OnEnter(Player player)
		{
			player.ResizeCollider(player.stats.current.crouchHeight);
			player.playerEvents.OnCrouchStarted.Invoke();
		}

		protected override void OnExit(Player player)
		{
			player.ResizeCollider(player.originalHeight);
			player.playerEvents.OnCrouchEnded.Invoke();
		}

		protected override void OnStep(Player player)
		{
			player.Gravity();
			player.SnapToGround();
			player.Fall();
			player.Decelerate(player.stats.current.crouchFriction);
			player.FaceVelocityInPaths();

			var inputDirection = player.inputs.GetMovementDirection();

			if (player.inputs.GetCrouchAndCraw() || !player.canStandUp)
			{
				if (inputDirection.sqrMagnitude > 0 && !player.holding)
				{
					if (player.lateralVelocity.sqrMagnitude == 0)
					{
						player.states.Change<CrawlingPlayerState>();
					}
				}
				else if (player.inputs.GetJumpDown())
				{
					player.Backflip(player.stats.current.backflipBackwardForce);
				}
			}
			else
			{
				player.states.Change<IdlePlayerState>();
			}
		}

		public override void OnContact(Player player, Collider other) { }
	}
}
