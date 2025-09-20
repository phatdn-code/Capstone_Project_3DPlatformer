using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(Collider))]
	[RequireComponent(typeof(Rigidbody))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Pickable")]
	public class Pickable : MonoBehaviour, IEntityContact
	{
		[Header("General Settings")]
		public Vector3 offset;
		public float releaseOffset = 0.5f;

		[Header("Respawn Settings")]
		public bool autoRespawn;
		public bool respawnOnHitHazards;
		public float respawnHeightLimit = -100;

		[Header("Attack Settings")]
		public bool attackEnemies = true;
		public int damage = 1;
		public float minDamageSpeed = 5f;

		[Space(15)]
		/// <summary>
		/// Called when this object is Picked.
		/// </summary>
		public UnityEvent onPicked;

		/// <summary>
		/// Called when this object is Released.
		/// </summary>
		public UnityEvent onReleased;

		/// <summary>
		/// Called when this object is respawned.
		/// </summary>
		public UnityEvent onRespawn;

		protected Collider m_collider;
		protected Rigidbody m_rigidBody;

		protected Vector3 m_initialPosition;
		protected Quaternion m_initialRotation;
		protected Transform m_initialParent;

		protected RigidbodyInterpolation m_interpolation;

		public bool beingHold { get; protected set; }

		public virtual void PickUp(Transform slot)
		{
			if (!beingHold)
			{
				beingHold = true;
				transform.parent = slot;
				transform.localPosition = Vector3.zero + offset;
				transform.localRotation = Quaternion.identity;
				m_rigidBody.isKinematic = true;
				m_collider.isTrigger = true;
				m_interpolation = m_rigidBody.interpolation;
				m_rigidBody.interpolation = RigidbodyInterpolation.None;
				onPicked?.Invoke();
			}
		}

		public virtual void Release(Vector3 direction, float force)
		{
			if (beingHold)
			{
				transform.parent = m_initialParent;
				transform.position += direction * (releaseOffset + 0.01f);
				m_collider.isTrigger = m_rigidBody.isKinematic = beingHold = false;
				m_rigidBody.interpolation = m_interpolation;
#if UNITY_6000_0_OR_NEWER
				m_rigidBody.linearVelocity = direction * force;
#else
				m_rigidBody.velocity = direction * force;
#endif
				onReleased?.Invoke();
			}
		}

		public virtual void Respawn()
		{
#if UNITY_6000_0_OR_NEWER
			m_rigidBody.linearVelocity = Vector3.zero;
#else
			m_rigidBody.velocity = Vector3.zero;
#endif
			transform.parent = m_initialParent;
			transform.SetPositionAndRotation(m_initialPosition, m_initialRotation);
			m_rigidBody.isKinematic = m_collider.isTrigger = beingHold = false;
			onRespawn?.Invoke();
		}

		public void OnEntityContact(Entity entity)
		{
			if (
				attackEnemies
				&& entity is Enemy
#if UNITY_6000_0_OR_NEWER
				&& m_rigidBody.linearVelocity.magnitude > minDamageSpeed
#else
				&& m_rigidBody.velocity.magnitude > minDamageSpeed
#endif
			)
			{
				entity.ApplyDamage(damage, transform.position);
			}
		}

		protected virtual void EvaluateHazardRespawn(Collider other)
		{
			if (autoRespawn && respawnOnHitHazards && other.CompareTag(GameTags.Hazard))
			{
				Respawn();
			}
		}

		protected virtual void Start()
		{
			m_collider = GetComponent<Collider>();
			m_rigidBody = GetComponent<Rigidbody>();
			m_initialPosition = transform.localPosition;
			m_initialRotation = transform.localRotation;
			m_initialParent = transform.parent;
		}

		protected virtual void Update()
		{
			if (autoRespawn && transform.position.y <= respawnHeightLimit)
			{
				Respawn();
			}
		}

		protected virtual void OnTriggerEnter(Collider other) => EvaluateHazardRespawn(other);

		protected virtual void OnCollisionEnter(Collision collision) =>
			EvaluateHazardRespawn(collision.collider);
	}
}
