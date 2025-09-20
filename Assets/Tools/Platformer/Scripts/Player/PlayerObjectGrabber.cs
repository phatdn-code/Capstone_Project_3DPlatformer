using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(Player))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player Object Grabber")]
	public class PlayerObjectGrabber : MonoBehaviour
	{
		[Header("Grabbing Settings")]
		[Tooltip("The slot where the grabbed object will be placed.")]
		public Transform grabberSlot;

		[Tooltip("The distance from the Player where the grabbing sphere will be cast.")]
		public float grabDistance = 0.5f;

		[Tooltip("The radius of the sphere used to detect objects to grab.")]
		public float grabRadius = 1f;

		[Tooltip("The layer mask used to detect objects to grab.")]
		public LayerMask grabLayer = ~0;

		protected Player m_player;
		protected Pickable m_currentGrabbed;
		protected Pickable m_tempPickable;

		protected Collider[] m_grabBuffer = new Collider[128];

		/// <summary>
		/// Returns whether the Player is currently grabbing an object.
		/// </summary>
		public bool isGrabbing { get; protected set; }

		/// <summary>
		/// Returns the current Player Stats.
		/// </summary>
		public PlayerStats stats => m_player.stats.current;

		protected virtual void Start()
		{
			InitializeGrabSlot();
			InitializePlayer();
		}

		protected virtual void InitializeGrabSlot()
		{
			if (!grabberSlot)
				LogGrabSlotWarning();
		}

		protected virtual void InitializePlayer() => m_player = GetComponent<Player>();

		protected virtual bool CanGrabObjects() =>
			grabberSlot && !isGrabbing && (m_player.isGrounded || stats.canPickUpOnAir);

		/// <summary>
		/// Handles the grabbing input.
		/// </summary>
		public virtual void HandleGrabbing()
		{
			if (!stats.canPickUp || !m_player.inputs.GetPickAndDropDown())
				return;

			if (isGrabbing)
				Throw();
			else
				TryGrab();
		}

		/// <summary>
		/// Attempts to grab an object in front of the Player.
		/// </summary>
		/// <returns>True if an object was grabbed, false otherwise.</returns>
		public virtual bool TryGrab()
		{
			var origin = m_player.unsizedPosition + transform.forward * grabDistance;
			var overlaps = Physics.OverlapSphereNonAlloc(
				origin,
				grabRadius,
				m_grabBuffer,
				grabLayer
			);

			for (var i = 0; i < overlaps; i++)
			{
				if (m_grabBuffer[i].TryGetComponent(out m_tempPickable))
				{
					Grab(m_tempPickable);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Grabs the specified object.
		/// </summary>
		/// <param name="pickable">The object you want to grab.</param>
		public virtual void Grab(Pickable pickable)
		{
			if (!CanGrabObjects())
				return;

			m_currentGrabbed = pickable;
			pickable.PickUp(grabberSlot);
			isGrabbing = true;
			pickable.onRespawn.AddListener(Release);
			m_player.playerEvents.OnPickUp.Invoke();
		}

		/// <summary>
		/// Releases the currently grabbed object.
		/// </summary>
		public virtual void Release()
		{
			if (!isGrabbing)
				return;

			m_currentGrabbed = null;
			isGrabbing = false;
		}

		/// <summary>
		/// Releases the currently grabbed object applying a force in the direction the Player is moving.
		/// </summary>
		public virtual void Throw()
		{
			if (!isGrabbing)
				return;

			var lateralSpeed = m_player.lateralVelocity.magnitude;
			var force = lateralSpeed * stats.throwVelocityMultiplier;
			m_currentGrabbed.Release(transform.forward, force);
			m_currentGrabbed = null;
			isGrabbing = false;
			m_player.playerEvents.OnThrow?.Invoke();
		}

		protected virtual void LogGrabSlotWarning()
		{
			Debug.LogWarning(
				$"No grab slot found on {name}. The Player will be unable to grab objects.",
				this
			);
		}
	}
}
