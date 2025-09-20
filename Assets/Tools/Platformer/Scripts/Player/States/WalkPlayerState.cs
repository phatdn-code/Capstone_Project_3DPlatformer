using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/States/Walk Player State")]
	public class WalkPlayerState : PlayerState
	{
		protected override void OnEnter(Player player) { }

		protected override void OnExit(Player player) { }

		protected override void OnStep(Player player)
		{
			player.Gravity();
			player.SnapToGround();
			player.Jump();
			player.Fall();
			player.Spin();
			player.PickAndThrow();
			player.Dash();
			player.RegularSlopeFactor();
			player.DecelerateToTopSpeed();

			var inputDirection = player.inputs.GetMovementCameraDirection(out var magnitude);

			if (inputDirection.sqrMagnitude > 0)
			{
				var dot = Vector3.Dot(inputDirection, player.lateralVelocity);

				if (dot >= player.stats.current.brakeThreshold)
				{
					player.Accelerate(inputDirection, magnitude);
					player.FaceDirectionSmooth(player.lateralVelocity);
				}
				else
				{
					player.states.Change<BrakePlayerState>();
				}
			}
			else
			{
				player.Friction();

				if (player.lateralVelocity.sqrMagnitude <= 0)
				{
					player.states.Change<IdlePlayerState>();
				}
			}

			if (player.inputs.GetCrouchAndCraw())
			{
				player.states.Change<CrouchPlayerState>();
			}
		}

		public override void OnContact(Player player, Collider other) { }
	}
}
