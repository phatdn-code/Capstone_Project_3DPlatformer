using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[CreateAssetMenu(
		fileName = "NewPlayerStats",
		menuName = "PLAYER TWO/Platformer Project/Player/New Player Stats"
	)]
	public class PlayerStats : EntityStats<PlayerStats>
	{
		[Header("General Stats")]
		[Tooltip("Force applied downwards when the Player is grounded to keep it grounded.")]
		public float snapForce = 15f;

		[Tooltip(
			"Force applied to snap the Player to the center of the current spline path (if any)."
		)]
		public float snapToPathForce = 10f;

		[Tooltip("Force applied to the Player when sliding down slopes.")]
		public float slideForce = 10f;

		[Tooltip(
			"Speed in degrees per second applied to the skin rotation when facing a direction."
		)]
		public float rotationSpeed = 970f;

		[Tooltip("Downward force applied to the Player when it's not grounded but moving upwards.")]
		public float gravity = 38f;

		[Tooltip(
			"Downward force applied to the Player when it's not grounded and moving downwards."
		)]
		public float fallGravity = 65f;

		[Tooltip("The maximum speed the Player can reach when falling.")]
		public float gravityTopSpeed = 50f;

		[Header("Pick'n Throw Stats")]
		[Tooltip("If true, the Player can pick up objects.")]
		public bool canPickUp = true;

		[Tooltip("If true, the Player can pick up objects while in the air.")]
		public bool canPickUpOnAir = false;

		[Tooltip("If true, the Player can jump while holding an object.")]
		public bool canJumpWhileHolding = true;

		[Tooltip("Additional force multiplier applied to the object when throwing it.")]
		public float throwVelocityMultiplier = 1.5f;

		[Header("Motion Stats")]
		[Tooltip("If true, the Player velocity will be modified by the slope angle.")]
		public bool applySlopeFactor = true;

		[Tooltip(
			"If true, the top speed will be modified by the input magnitude, allowing the Player to walk slower."
		)]
		public bool applyInputMagnitude = true;

		[Tooltip(
			"If true, the Player will decelerate to the top speed after breaking the threshold."
		)]
		public bool decelerateWhenOverTopSpeed = true;

		[Tooltip("Speed in units per second applied to the Player when walking.")]
		public float acceleration = 13f;

		[Tooltip("Speed in units per second applied to the Player when walking backwards.")]
		public float deceleration = 28f;

		[Tooltip(
			"Deceleration in units per second applied when decelerating to the top speed. "
				+ "This value wont be used if decelerateWhenOverTopSpeed is false."
		)]
		public float decelerationToTopSpeed = 10;

		[Tooltip(
			"The speed in units per second at which the Player decelerates when releasing the inputs."
		)]
		public float friction = 28f;

		[Tooltip(
			"Friction in units per second applied to the Player when the directional input is released and the Player is on a slope."
		)]
		public float slopeFriction = 18f;

		[Tooltip(
			"The minimum top speed the Player can reach. This value is used when the input magnitude is low."
		)]
		public float minTopSpeed = 2f;

		[Tooltip("The maximum speed the Player can reach when walking.")]
		public float topSpeed = 6f;

		[Tooltip(
			"Drag in units per second applied to the Player when redirecting its velocity towards the input direction."
		)]
		public float turningDrag = 28f;

		[Tooltip("Speed in units per second applied to the Player when moving in the air.")]
		public float airAcceleration = 32f;

		[Tooltip(
			"The dot product threshold used to determine if the Player is trying to move in the opposite direction."
		)]
		public float brakeThreshold = -0.8f;

		[Tooltip("Force applied to the Player when moving upwards on a slope.")]
		public float slopeUpwardForce = 25f;

		[Tooltip("Force applied to the Player when moving downwards on a slope.")]
		public float slopeDownwardForce = 28f;

		[Header("Running Stats")]
		[Tooltip("If true, the Player can run.")]
		public bool canRun = true;

		[Tooltip(
			"If true, the Player can \"run\" (move faster by holding run button) while in the air."
		)]
		public bool canRunOnAir = true;

		[Tooltip(
			"Speed in units per second applied to the Player when running. This overrides the acceleration value."
		)]
		public float runningAcceleration = 16f;

		[Tooltip(
			"The maximum speed the Player can reach when running. This overrides the top speed value."
		)]
		public float runningTopSpeed = 7.5f;

		[Tooltip(
			"Drag in units per second applied to the Player when redirecting its velocity towards the input "
				+ "direction while running. This overrides the turning drag value."
		)]
		public float runningTurningDrag = 14f;

		[Header("Jump Stats")]
		[Tooltip("How many jumps the Player can perform.")]
		public int multiJumps = 1;

		[Tooltip("The time in seconds the Player can still jump after leaving the ground.")]
		public float coyoteJumpThreshold = 0.15f;

		[Tooltip("The maximum force the jump can reach.")]
		public float maxJumpHeight = 17f;

		[Tooltip("The minimum force the jump can reach.")]
		public float minJumpHeight = 10f;

		[Header("Crouch Stats")]
		[Tooltip("The height of the Player when crouching.")]
		public float crouchHeight = 1f;

		[Tooltip(
			"The speed in units per second at which the Player decelerates when releasing the inputs while crouching."
		)]
		public float crouchFriction = 10f;

		[Header("Fall Damage Stats")]
		[Tooltip("If true, the Player can take fall damage.")]
		public bool canTakeFallDamage = true;

		[Tooltip("The amount of damage the Player takes when landing after falling.")]
		public int baseFallDamage = 1;

		[Tooltip("Additional damage applied to the Player per falling time interval.")]
		public int additionalFallDamage = 1;

		[Tooltip(
			"The time in seconds the Player needs to be falling before landing to take damage."
		)]
		public float minFallDurationToTakeDamage = 0.5f;

		[Tooltip("The minimum speed the Player needs to be falling to take damage.")]
		public float minFallSpeedToTakeDamage = 40f;

		[Tooltip(
			"Interval in seconds before adding new additional damage to the final fall damage."
		)]
		public float fallDamageInterval = 0.3f;

		[Header("Crawling Stats")]
		[Tooltip("Speed in units per second applied to the Player when crawling.")]
		public float crawlingAcceleration = 8f;

		[Tooltip(
			"The speed in units per second at which the Player decelerates when releasing the inputs while crawling."
		)]
		public float crawlingFriction = 32f;

		[Tooltip("The maximum speed the Player can reach when crawling.")]
		public float crawlingTopSpeed = 2.5f;

		[Tooltip(
			"Drag in units per second applied to the Player when redirecting its velocity towards the input direction while crawling."
		)]
		public float crawlingTurningSpeed = 3f;

		[Header("Wall Drag Stats")]
		[Tooltip("If true, the Player can drag on walls when touching them.")]
		public bool canWallDrag = true;

		[Tooltip("If true, the Player can't move for a short period after wall jumping.")]
		public bool wallJumpLockMovement = true;

		[Tooltip("The time in seconds before gravity is applied while dragging on a wall.")]
		public float wallDragGravityDelay = 0.1f;

		[Tooltip("The minimum distance the Player needs to be from the ground to drag on a wall.")]
		public float minGroundDistanceToDrag = 0.5f;

		[Tooltip("The minimum angle the wall needs to be to drag on it.")]
		public float minWallAngleToDrag = 60;

		[Tooltip("The layers of the colliders that the Player can drag on.")]
		public LayerMask wallDragLayers;

		[Tooltip("The offset applied to the Player skin when dragging on a wall.")]
		public Vector3 wallDragSkinOffset;

		[Tooltip("Downwards force applied to the Player when dragging on a wall.")]
		public float wallDragGravity = 12f;

		[Tooltip("The force applied away from the wall when jumping from it.")]
		public float wallJumpDistance = 8f;

		[Tooltip("The force applied upwards when jumping from the wall.")]
		public float wallJumpHeight = 15f;

		[Header("Pole Climb Stats")]
		[Tooltip("If true, the Player can climb poles when touching them.")]
		public bool canPoleClimb = true;

		[Tooltip("The offset applied to the Player skin when climbing a pole.")]
		public Vector3 poleClimbSkinOffset;

		[Tooltip("The acceleration speed applied to the Player when climbing up.")]
		public float climbUpAcceleration = 6f;

		[Tooltip("The maximum speed the Player can reach when climbing up.")]
		public float climbUpTopSpeed = 3f;

		[Tooltip("The acceleration speed applied to the Player when climbing down.")]
		public float climbDownAcceleration = 20f;

		[Tooltip("The maximum speed the Player can reach when climbing down.")]
		public float climbDownTopSpeed = 8f;

		[Tooltip("The speed at which the Player decelerates when releasing the inputs.")]
		public float climbFriction = 15f;

		[Tooltip("The maximum speed the Player can reach when rotating around the pole.")]
		public float climbRotationTopSpeed = 2f;

		[Tooltip("The acceleration speed applied to the Player when rotating around the pole.")]
		public float climbRotationAcceleration = 5f;

		[Tooltip("The jump force applied away from the pole when jumping from it.")]
		public float poleJumpDistance = 8f;

		[Tooltip("The jump force applied upwards when jumping from the pole.")]
		public float poleJumpHeight = 15f;

		[Header("Swimming Stats")]
		[Tooltip(
			"When entering the enter, the Player's vertical speed will be clamped to this value."
		)]
		public float waterMaxVerticalSpeedOnEnter = 20f;

		[Tooltip(
			"When entering the water, the Player's velocity will be multiplied by this value."
		)]
		[Range(0, 1)]
		public float waterConversion = 0.35f;

		[Tooltip("The rotation speed of the Player, in degrees per second, when swimming.")]
		public float waterRotationSpeed = 360f;

		[Tooltip("An upward force applied to the Player when underwater.")]
		public float waterUpwardsForce = 8f;

		[Tooltip("The jump force applied to the Player when jumping out of the water.")]
		public float waterJumpHeight = 15f;

		[Tooltip(
			"The drag speed applied to the Player when turning in water. More drag means quicker turning."
		)]
		public float waterTurningDrag = 2.5f;

		[Tooltip("The acceleration speed applied to the Player when swimming forward.")]
		public float swimAcceleration = 4f;

		[Tooltip("The maximum speed of the Player when swimming forward.")]
		public float swimTopSpeed = 4f;

		[Tooltip("The acceleration speed applied to the Player when swimming upwards.")]
		public float swimUpwardsAcceleration = 6f;

		[Tooltip("The maximum speed of the Player when swimming upwards.")]
		public float swimUpwardsTopSpeed = 10f;

		[Tooltip("The acceleration speed applied to the Player when swimming downwards.")]
		public float swimDownwardsAcceleration = 6f;

		[Tooltip("The maximum speed of the Player when swimming downwards.")]
		public float swimDownwardsTopSpeed = 10f;

		[Tooltip("The deceleration speed applied to the Player when it's not swimming.")]
		public float swimDeceleration = 3f;

		[Header("Spin Stats")]
		[Tooltip("If true, the Player can perform the spin attack.")]
		public bool canSpin = true;

		[Tooltip("If true, the Player can perform the spin attack while in the air.")]
		public bool canAirSpin = true;

		[Tooltip("The duration in seconds of the spin attack.")]
		public float spinDuration = 0.5f;

		[Tooltip("Upward force applied to the Player when starts spinning in the air.")]
		public float airSpinUpwardForce = 10f;

		[Tooltip("The amount of spins the Player can perform in the air.")]
		public int allowedAirSpins = 1;

		[Header("Hurt Stats")]
		[Tooltip("Upward force applied to the Player when getting hurt.")]
		public float hurtUpwardForce = 10f;

		[Tooltip(
			"Backwards force away from the hurting source applied to the Player when getting hurt."
		)]
		public float hurtBackwardsForce = 5f;

		[Tooltip("Backwards force applied to the Player when getting hurt while swimming.")]
		public float hurtBackwardsWaterForce = 5;

		[Tooltip("Downwards force applied to the Player when getting hurt while swimming.")]
		public float hurtDownwardsWaterForce = 3;

		[Tooltip("The time in seconds the Player needs to wait before being able to move.")]
		public float hurtWaterCoolDown = 0.5f;

		[Tooltip("Deceleration speed applied to the Player when swimming after getting hurt.")]
		public float hurtWaterDrag = 10;

		[Tooltip(
			"The time in seconds the Player needs to wait before being able to move after getting hurt."
		)]
		public float hurtStunRecoverTime = 2f;

		[Header("Air Dive Stats")]
		[Tooltip("If true, the Player can perform the air dive.")]
		public bool canAirDive = true;

		[Tooltip(
			"If true, the slope factor will be applied to the air dive when sliding in the ground."
		)]
		public bool applyDiveSlopeFactor = true;

		[Tooltip("Forward force applied to the Player when performing the air dive.")]
		public float airDiveForwardForce = 16f;

		[Tooltip(
			"Speed at which the Player decelerates when releasing the inputs while air diving."
		)]
		public float airDiveFriction = 32f;

		[Tooltip(
			"Speed at which the Player decelerates when releasing the inputs while air diving on a slope."
		)]
		public float airDiveSlopeFriction = 12f;

		[Tooltip("Force applied when moving upwards on a slope while air diving.")]
		public float airDiveSlopeUpwardForce = 35f;

		[Tooltip("Force applied when moving downwards on a slope while air diving.")]
		public float airDiveSlopeDownwardForce = 40f;

		[Tooltip("Upward force applied to the Player when recovering from the air dive.")]
		public float airDiveGroundLeapHeight = 10f;

		[Tooltip("The rotation speed of the Player, in degrees per second, when air diving.")]
		public float airDiveRotationSpeed = 45f;

		[Header("Stomp Attack Stats")]
		[Tooltip("If true, the Player can perform the stomp attack.")]
		public bool canStompAttack = true;

		[Tooltip("Downward force applied to the Player when performing the stomp attack.")]
		public float stompDownwardForce = 20f;

		[Tooltip(
			"The duration in seconds the Player stays in the air when performing the stomp attack."
		)]
		public float stompAirTime = 0.8f;

		[Tooltip(
			"The duration in seconds the Player stays on the ground when performing the stomp attack."
		)]
		public float stompGroundTime = 0.5f;

		[Tooltip("Upward force applied to the Player when recovering from the stomp attack.")]
		public float stompGroundLeapHeight = 10f;

		[Header("Ledge Hanging Stats")]
		[Tooltip("If true, the Player can hang from ledges when touching them.")]
		public bool canLedgeHang = true;

		[Tooltip("The layers of the colliders that the Player can hang from.")]
		public LayerMask ledgeHangingLayers;

		[Tooltip("The offset applied to the Player skin when hanging from a ledge.")]
		public Vector3 ledgeHangingSkinOffset;

		[Tooltip("The maximum forward distance the ledge can be from the Player to be detected.")]
		public float ledgeMaxForwardDistance = 0.1f;

		[Tooltip("The maximum downward distance the ledge can be from the Player to be detected.")]
		public float ledgeMaxDownwardDistance = 0.25f;

		[Tooltip("The maximum distance from the Player's side to detected walls from a ledge.")]
		public float ledgeSideMaxDistance = 0.5f;

		[Tooltip("An offset applied vertically to the Player's ledge wall detection.")]
		public float ledgeSideHeightOffset = 0.15f;

		[Tooltip("The radius of the sphere casted to detect walls from a ledge.")]
		public float ledgeSideCollisionRadius = 0.25f;

		[Tooltip("The acceleration speed applied to the Player when moving on a ledge.")]
		public float ledgeMovementAcceleration = 3f;

		[Tooltip("The maximum speed the Player can reach when moving on a ledge.")]
		public float ledgeMovementTopSpeed = 1.5f;

		[Tooltip(
			"The deceleration speed applied to the Player when releasing the inputs on a ledge."
		)]
		public float ledgeMovementFriction = 10f;

		[Header("Ledge Climbing Stats")]
		[Tooltip("If true, the Player can climb ledges when hanging from them.")]
		public bool canClimbLedges = true;

		[Tooltip("The layers of the colliders that the Player can climb.")]
		public LayerMask ledgeClimbingLayers;

		[Tooltip("The offset applied to the Player skin when climbing a ledge.")]
		public Vector3 ledgeClimbingSkinOffset;

		[Tooltip(
			"The duration it takes for the Player to climb a ledge. Try syncing this with the animation speed."
		)]
		public float ledgeClimbingDuration = 1f;

		[Header("Backflip Stats")]
		[Tooltip("If true, the Player can perform the backflip.")]
		public bool canBackflip = true;

		[Tooltip("If true, the Player can perform the backflip while turning direction.")]
		public bool canBackflipWhileTurning = true;

		[Tooltip(
			"If true, the Player will be unable to redirect its velocity for a short period after performing the backflip."
		)]
		public bool backflipLockMovement = true;

		[Tooltip(
			"Speed in units per second applied to the Player when moving in the air while backflipping."
		)]
		public float backflipAirAcceleration = 12f;

		[Tooltip(
			"The drag speed applied to the Player when turning while backflipping. Greater values mean quicker turning."
		)]
		public float backflipTurningDrag = 2.5f;

		[Tooltip("The maximum speed the Player can reach when backflipping.")]
		public float backflipTopSpeed = 7.5f;

		[Tooltip("Upward force applied to the Player when performing the backflip.")]
		public float backflipJumpHeight = 23f;

		[Tooltip("Downward force applied to the Player when performing the backflip.")]
		public float backflipGravity = 35f;

		[Tooltip(
			"Backward force applied to the Player when performing the backflip from the crouch/crawling state."
		)]
		public float backflipBackwardForce = 4f;

		[Tooltip(
			"Backward force applied to the Player when performing the backflip from the brake state."
		)]
		public float backflipBackwardTurnForce = 8f;

		[Header("Gliding Stats")]
		[Tooltip("If true, the Player can enter the glide state.")]
		public bool canGlide = true;

		[Tooltip("An downwards force applied to the Player when gliding.")]
		public float glidingGravity = 10f;

		[Tooltip("The maximum downwards speed the Player can reach when gliding.")]
		public float glidingMaxFallSpeed = 2f;

		[Tooltip(
			"The drag speed applied to the Player when turning while gliding. Greater values mean quicker turning."
		)]
		public float glidingTurningDrag = 8f;

		[Tooltip("The rotation speed of the Player, in degrees per second, when gliding.")]
		public float glidingRotationSpeed = 180f;

		[Header("Dash Stats")]
		[Tooltip("If true, the Player can perform the dash in the air.")]
		public bool canAirDash = true;

		[Tooltip("If true, the Player can perform the dash on the ground.")]
		public bool canGroundDash = true;

		[Tooltip("The forward force applied to the Player when dashing.")]
		public float dashForce = 25f;

		[Tooltip("The duration in seconds of the dash.")]
		public float dashDuration = 0.3f;

		[Tooltip("Duration in seconds the Player needs to wait before dashing again.")]
		public float groundDashCoolDown = 0.5f;

		[Tooltip("The amount of dashes the Player can perform in the air.")]
		public float allowedAirDashes = 1;

		[Header("Rail Grinding Stats")]
		[Tooltip("If true, the Player will use custom collision detection for rail grinding.")]
		public bool useCustomCollision = true;

		[Tooltip("Offset applied to the Player when detecting the grindable surface.")]
		public float grindRadiusOffset = 0.26f;

		[Tooltip("The minimum speed the Player will have when starts grinding.")]
		public float minGrindInitialSpeed = 10f;

		[Tooltip("The minimum speed the Player can reach when grinding.")]
		public float minGrindSpeed = 5f;

		[Tooltip("The maximum speed the Player can reach when grinding.")]
		public float grindTopSpeed = 25f;

		[Tooltip("Force applied when moving downwards on a slope while grinding.")]
		public float grindDownSlopeForce = 40f;

		[Tooltip("Force applied when moving upwards on a slope while grinding.")]
		public float grindUpSlopeForce = 30f;

		[Header("Rail Grinding Brake")]
		[Tooltip("If true, the Player can brake (decelerate) while grinding.")]
		public bool canGrindBrake = true;

		[Tooltip("The deceleration speed applied to the Player when braking while grinding.")]
		public float grindBrakeDeceleration = 10;

		[Header("Rail Grinding Dash Stats")]
		[Tooltip("If true, the Player can dash while grinding.")]
		public bool canGrindDash = true;

		[Tooltip("If true, the slope factor will be applied to the grinding speed.")]
		public bool applyGrindingSlopeFactor = true;

		[Tooltip("The duration in seconds before the Player can dash again while grinding.")]
		public float grindDashCoolDown = 0.5f;

		[Tooltip("Force applied to the Player when dashing while grinding.")]
		public float grindDashForce = 25f;
	}
}
