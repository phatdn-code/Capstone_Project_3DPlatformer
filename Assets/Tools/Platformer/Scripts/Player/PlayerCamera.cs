using Unity.Cinemachine;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(CinemachineCamera))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player Camera")]
	public class PlayerCamera : MonoBehaviour
	{
		[SerializeField]
		[Header("Camera Settings")]
		[Tooltip(
			"The reference to the Player this camera will follow. "
				+ "If not set, the first Player found in the scene will be used."
		)]
		protected Player m_player;

		[Tooltip("The maximum distance the camera can be from the Player.")]
		public float maxDistance = 15f;

		[Tooltip(
			"The initial angle of the camera. Values greater than 0 will look down at the Player."
		)]
		public float initialPitch = 20f;

		[Tooltip(
			"The initial yaw of the camera. Values greater than 0 will look to the right of the Player."
		)]
		public float initialYaw = 0f;

		[Tooltip("The height offset of the camera from the Player.")]
		public float heightOffset = 1f;

		[Header("Following Settings")]
		[Tooltip(
			"The dead zone for the vertical movement when the Player is grounded. "
				+ "Values greater than 0 will make the camera move up when the Player is above the dead zone"
		)]
		public float verticalUpDeadZone = 0.15f;

		[Tooltip(
			"The dead zone for the vertical movement when the Player is grounded. "
				+ "Values greater than 0 will make the camera move down when the Player is below the dead zone"
		)]
		public float verticalDownDeadZone = 0.15f;

		[Tooltip(
			"The dead zone for the vertical movement when the Player is in the air. "
				+ "Values greater than 0 will make the camera move up when the Player is above the dead zone"
		)]
		public float verticalAirUpDeadZone = 4f;

		[Tooltip(
			"The dead zone for the vertical movement when the Player is in the air. "
				+ "Values greater than 0 will make the camera move down when the Player is below the dead zone"
		)]
		public float verticalAirDownDeadZone = 0;

		[Tooltip("The maximum vertical speed the camera can move when following the Player.")]
		public float maxVerticalSpeed = 10f;

		[Tooltip(
			"The maximum vertical speed the camera can move when following the Player in the air."
		)]
		public float maxAirVerticalSpeed = 100f;

		[Tooltip("The speed at which the camera aligns with the Player's up vector.")]
		public float upwardRotationSpeed = 90f;

		[Header("Orbit Settings")]
		[Tooltip("Whether the camera can orbit around the Player by moving the mouse/look stick.")]
		public bool canOrbit = true;

		[Tooltip(
			"Whether the camera can orbit around the Player based on the Player's lateral velocity."
		)]
		public bool canOrbitWithVelocity = true;

		[Tooltip(
			"The multiplier for the orbit velocity based on the Player's lateral velocity. "
				+ "Higher values will make the camera orbit faster when the Player moves."
		)]
		public float orbitVelocityMultiplier = 5;

		[Range(0, 90)]
		[Tooltip("The maximum angle the camera can reach when looking up.")]
		public float verticalMaxRotation = 80;

		[Range(-90, 0)]
		[Tooltip("The minimum angle the camera can reach when looking down.")]
		public float verticalMinRotation = -20;

		[Header("Sensitivity Settings")]
		[Tooltip("The sensitivity of the camera when moving the mouse/look stick on the x-axis.")]
		public float xSensitivity = 1f;

		[Tooltip("The sensitivity of the camera when moving the mouse/look stick on the y-axis.")]
		public float ySensitivity = 1f;

		[Tooltip("The global sensitivity of the camera for both x and y axes.")]
		public float globalSensitivity = 1f;

		[Header("Lock On Settings")]
		[Tooltip(
			"The target the camera will lock on to. If set, the camera will "
				+ "ignore the orbit settings and focus on the target."
		)]
		public Transform lockTarget;

		[Tooltip(
			"The speed at which the camera will rotate on the x-axis to lock on the target. "
				+ "This helps to avoid sudden changes in the camera's pitch and smooths the movement."
		)]
		public float lockOnXSpeed = 180f;

		[Tooltip(
			"The speed at which the camera will rotate on the y-axis to lock on the target. "
				+ "This helps to avoid sudden changes in the camera's yaw and smooths the movement."
		)]
		public float lockOnYSpeed = 180f;

		protected float m_cameraDistance;
		protected float m_cameraTargetYaw;
		protected float m_cameraTargetPitch;
		protected float m_cameraPreviousYaw;

		protected Vector3 m_cameraTargetPosition;
		protected Quaternion m_currentUpRotation;

		protected Camera m_camera;
		protected CinemachineCamera m_virtualCamera;
		protected CinemachineThirdPersonFollow m_cameraBody;
		protected CinemachineBrain m_brain;

		protected Transform m_target;

		protected string k_targetName = "Player Follower Camera Target";

		public Player player => m_player ? m_player : Level.instance.player;

		/// <summary>
		/// Whether the camera is frozen and won't update its position or rotation.
		/// </summary>
		public bool freeze { get; set; }

		protected virtual void InitializeComponents()
		{
			m_camera = Camera.main;
			m_virtualCamera = GetComponent<CinemachineCamera>();
			m_cameraBody = gameObject.AddComponent<CinemachineThirdPersonFollow>();
			m_brain = m_camera.GetComponent<CinemachineBrain>();
		}

		protected virtual void InitializeFollower()
		{
			m_target = new GameObject(k_targetName).transform;
			m_target.position = player.transform.position;
		}

		protected virtual void InitializeCamera()
		{
			m_virtualCamera.Follow = m_target;
			m_virtualCamera.LookAt = player.transform;
			m_brain.WorldUpOverride = m_target;

			Reset();
		}

		protected virtual bool VerticalFollowingStates()
		{
			return player.states.IsCurrentOfType(typeof(SwimPlayerState))
				|| player.states.IsCurrentOfType(typeof(PoleClimbingPlayerState))
				|| player.states.IsCurrentOfType(typeof(WallDragPlayerState))
				|| player.states.IsCurrentOfType(typeof(LedgeHangingPlayerState))
				|| player.states.IsCurrentOfType(typeof(LedgeClimbingPlayerState))
				|| player.states.IsCurrentOfType(typeof(RailGrindPlayerState));
		}

		/// <summary>
		/// Resets the camera to its initial position and rotation.
		/// </summary>
		public virtual void Reset()
		{
			if (!player || !m_target)
				return;

			m_cameraDistance = maxDistance;
			m_cameraTargetPitch = initialPitch;
			m_cameraTargetYaw = m_cameraPreviousYaw = initialYaw;
			m_cameraTargetPosition = player.unsizedPosition + player.transform.up * heightOffset;
			m_target.SetPositionAndRotation(
				m_cameraTargetPosition,
				player.transform.rotation
					* Quaternion.Euler(m_cameraTargetPitch, m_cameraTargetYaw, 0)
			);
			m_currentUpRotation =
				Quaternion.FromToRotation(m_target.up, player.transform.up) * m_target.rotation;

			if (player.IsSideScroller)
			{
				var pathRotation = Quaternion.FromToRotation(m_target.right, player.pathForward);
				m_target.rotation = pathRotation * m_target.rotation;
			}

			m_cameraBody.CameraDistance = m_cameraDistance;
			m_brain.ManualUpdate();
		}

		protected virtual void HandleOffset()
		{
			var grounded = player.isGrounded && player.verticalVelocity.y <= 0;
			var target = player.unsizedPosition + player.transform.up * heightOffset;
			var head = target - m_cameraTargetPosition;

			var xOffset = Vector3.Dot(head, player.transform.right);
			var yOffset = Vector3.Dot(head, player.transform.up);
			var zOffset = Vector3.Dot(head, player.transform.forward);

			var targetXOffset = xOffset;
			var targetYOffset = 0f;
			var targetZOffset = zOffset;

			var maxGroundDelta = maxVerticalSpeed * Time.deltaTime;
			var maxAirDelta = maxAirVerticalSpeed * Time.deltaTime;

			if (grounded || VerticalFollowingStates())
			{
				if (yOffset > verticalUpDeadZone)
				{
					var offset = yOffset - verticalUpDeadZone;
					targetYOffset += Mathf.Min(offset, maxGroundDelta);
				}
				else if (yOffset < verticalDownDeadZone)
				{
					var offset = yOffset - verticalDownDeadZone;
					targetYOffset += Mathf.Max(offset, -maxGroundDelta);
				}
			}
			else if (yOffset > verticalAirUpDeadZone)
			{
				var offset = yOffset - verticalAirUpDeadZone;
				targetYOffset += Mathf.Min(offset, maxAirDelta);
			}
			else if (yOffset < verticalAirDownDeadZone)
			{
				var offset = yOffset - verticalAirDownDeadZone;
				targetYOffset += Mathf.Max(offset, -maxAirDelta);
			}

			var rightOffset = player.transform.right * targetXOffset;
			var upOffset = player.transform.up * targetYOffset;
			var forwardOffset = player.transform.forward * targetZOffset;

			m_cameraTargetPosition =
				m_cameraTargetPosition + rightOffset + upOffset + forwardOffset;
		}

		protected virtual void HandleOrbit()
		{
			if (canOrbit && !lockTarget)
			{
				var direction = player.inputs.GetLookDirection();

				if (direction.sqrMagnitude > 0)
				{
					var usingMouse = player.inputs.IsLookingWithMouse();
					var deltaTimeMultiplier = usingMouse ? Time.timeScale : Time.deltaTime;
					var xSensitivity = this.xSensitivity * globalSensitivity * deltaTimeMultiplier;
					var ySensitivity = this.ySensitivity * globalSensitivity * deltaTimeMultiplier;

					m_cameraTargetYaw += direction.x * xSensitivity;
					m_cameraTargetPitch -= direction.z * ySensitivity;
					m_cameraTargetPitch = ClampAngle(
						m_cameraTargetPitch,
						verticalMinRotation,
						verticalMaxRotation
					);
				}
			}
		}

		protected virtual void HandleVelocityOrbit()
		{
			if (canOrbitWithVelocity && player.isGrounded && !lockTarget)
			{
				var localVelocity = m_target.InverseTransformVector(player.velocity);
				m_cameraTargetYaw += localVelocity.x * orbitVelocityMultiplier * Time.deltaTime;
			}
		}

		protected virtual void MoveTarget()
		{
			var upRotationMaxDelta = upwardRotationSpeed * Time.deltaTime;
			var yawRotation = Quaternion.Euler(0.0f, m_cameraTargetYaw - m_cameraPreviousYaw, 0.0f);
			var pitchRotation = Quaternion.Euler(m_cameraTargetPitch, 0.0f, 0.0f);
			var upRotation =
				Quaternion.FromToRotation(m_target.up, player.transform.up) * m_target.rotation;
			var smoothUpRotation = Quaternion.RotateTowards(
				m_currentUpRotation,
				upRotation,
				upRotationMaxDelta
			);

			m_target.position = m_cameraTargetPosition;
			m_currentUpRotation = smoothUpRotation * yawRotation;
			m_target.rotation = m_currentUpRotation * pitchRotation;
			m_cameraBody.CameraDistance = m_cameraDistance;

			if (player.IsSideScroller)
			{
				var pathRotation = Quaternion.FromToRotation(m_target.right, player.pathForward);
				m_target.rotation = pathRotation * m_target.rotation;
			}
		}

		protected virtual void LockOnTarget()
		{
			if (!lockTarget)
				return;

			var direction = lockTarget.position - m_target.position;
			var upRotation = Quaternion.FromToRotation(player.transform.up, Vector3.up);
			var rotation = upRotation * Quaternion.LookRotation(direction);
			var euler = rotation.eulerAngles;
			var yawMaxDelta = lockOnYSpeed * Time.deltaTime;
			var pitchMaxDelta = lockOnXSpeed * Time.deltaTime;

			m_cameraTargetYaw = Mathf.MoveTowardsAngle(m_cameraTargetYaw, euler.y, yawMaxDelta);
			m_cameraTargetPitch = Mathf.MoveTowardsAngle(
				m_cameraTargetPitch,
				euler.x,
				pitchMaxDelta
			);
		}

		protected virtual void CachePreviousData()
		{
			m_cameraPreviousYaw = m_cameraTargetYaw;
		}

		protected virtual void AssignWorldUpOverride()
		{
			if (m_brain && m_target)
				m_brain.WorldUpOverride = m_target;
		}

		protected virtual void OnEnable()
		{
			AssignWorldUpOverride();
		}

		protected virtual void Start()
		{
			InitializeComponents();
			InitializeFollower();
			InitializeCamera();
		}

		protected virtual void LateUpdate()
		{
			if (freeze)
				return;

			HandleOrbit();
			HandleVelocityOrbit();
			LockOnTarget();
			HandleOffset();
			MoveTarget();
			CachePreviousData();
		}

		protected virtual float ClampAngle(float angle, float min, float max)
		{
			if (angle < -360)
				angle += 360;

			if (angle > 360)
				angle -= 360;

			return Mathf.Clamp(angle, min, max);
		}
	}
}
