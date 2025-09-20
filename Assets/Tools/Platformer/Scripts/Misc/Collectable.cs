using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(Collider))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Collectable")]
	public class Collectable : MonoBehaviour
	{
		[Header("General Settings")]
		public bool collectOnContact = true;
		public bool resetTransform;
		public int times = 1;
		public float ghostingDuration = 0.5f;
		public GameObject display;
		public AudioClip clip;
		public ParticleSystem particle;

		[Header("Visibility Settings")]
		public bool hidden;
		public float quickShowHeight = 2f;
		public float quickShowDuration = 0.25f;
		public float hideDuration = 0.5f;

		[Header("Life Time")]
		public bool hasLifeTime;
		public float lifeTimeDuration = 5f;

		[Header("Physics Settings")]
		public bool usePhysics;
		public float minForceToStopPhysics = 3f;
		public float collisionRadius = 0.5f;
		public float gravity = 15f;
		public float bounciness = 0.98f;
		public float maxBounceYVelocity = 10f;
		public bool randomizeInitialDirection = true;
		public Vector3 initialVelocity = new Vector3(0, 12, 0);
		public AudioClip collisionClip;

		[Space(15)]
		/// <summary>
		/// Called when it has been collected.
		/// </summary>
		public PlayerEvent onCollect;

		protected Collider m_collider;
		protected AudioSource m_audio;

		protected bool m_vanished;
		protected bool m_ghosting = true;
		protected float m_elapsedLifeTime;
		protected float m_elapsedGhostingTime;
		protected Vector3 m_velocity;

		protected static Transform m_container;

		protected const string k_containerName = "__COLLECTIBLES_CONTAINER__";

		protected const int k_verticalMinRotation = 0;
		protected const int k_verticalMaxRotation = 30;
		protected const int k_horizontalMinRotation = 0;
		protected const int k_horizontalMaxRotation = 360;

		public static Transform container
		{
			get
			{
				if (!m_container)
				{
					m_container = GameObject.Find(k_containerName)?.transform;

					if (!m_container)
						m_container = new GameObject(k_containerName).transform;
				}

				return m_container;
			}
		}

		protected virtual void InitializeAudio()
		{
			if (!TryGetComponent(out m_audio))
			{
				m_audio = gameObject.AddComponent<AudioSource>();
			}
		}

		protected virtual void InitializeCollider()
		{
			m_collider = GetComponent<Collider>();
			m_collider.isTrigger = true;
		}

		protected virtual void InitializeTransform()
		{
			if (!resetTransform)
				return;

			var initialRotation = usePhysics ? Quaternion.identity : transform.rotation;

			if (usePhysics && transform.parent.TryGetComponent<GravityHandler>(out var handler))
			{
				var upDirection = -handler.gravityDirection;
				initialRotation = Quaternion.FromToRotation(transform.up, upDirection);
				initialRotation *= transform.rotation;
			}

			transform.parent = container;
			transform.rotation = initialRotation;
		}

		protected virtual void InitializeDisplay()
		{
			display.SetActive(!hidden);
		}

		protected virtual void InitializeVelocity()
		{
			var direction = initialVelocity.normalized;
			var force = initialVelocity.magnitude;

			if (randomizeInitialDirection)
			{
				if (Level.instance.player.IsSideScroller)
				{
					var upward = Level.instance.player.transform.up;
					var pathForward = Level.instance.player.pathForward;
					var right = Vector3.Cross(upward, pathForward);
					var randomAngle = Random.Range(k_verticalMinRotation, k_verticalMaxRotation);
					direction = Quaternion.AngleAxis(randomAngle, right) * upward;
				}
				else
				{
					var randomZ = Random.Range(k_verticalMinRotation, k_verticalMaxRotation);
					var randomY = Random.Range(k_horizontalMinRotation, k_horizontalMaxRotation);
					direction = Quaternion.Euler(0, 0, randomZ) * direction;
					direction = Quaternion.Euler(0, randomY, 0) * direction;
					direction = transform.rotation * direction;
				}
			}

			m_velocity = direction * force;
		}

		/// <summary>
		/// The collection routine which is trigger the callbacks and activate the reactions.
		/// </summary>
		/// <param name="player">The Player which collected.</param>
		protected virtual IEnumerator CollectRoutine(Player player)
		{
			for (int i = 0; i < times; i++)
			{
				m_audio.Stop();
				m_audio.PlayOneShot(clip);
				onCollect.Invoke(player);
				yield return new WaitForSeconds(0.1f);
			}
		}

		protected virtual IEnumerator QuickShowRoutine()
		{
			var elapsedTime = 0f;
			var initialPosition = transform.position;
			var targetPosition = initialPosition + transform.up * quickShowHeight;

			display.SetActive(true);
			m_collider.enabled = false;

			while (elapsedTime < quickShowDuration)
			{
				var t = elapsedTime / quickShowDuration;
				transform.position = Vector3.Lerp(initialPosition, targetPosition, t);
				elapsedTime += Time.deltaTime;
				yield return null;
			}

			transform.position = targetPosition;
			yield return new WaitForSeconds(hideDuration);
			transform.position = initialPosition;
			Vanish();
		}

		/// <summary>
		/// Triggers the collection of this Collectable.
		/// </summary>
		/// <param name="player">The Player which collected.</param>
		public virtual void Collect(Player player)
		{
			if (!m_vanished && !m_ghosting)
			{
				if (!hidden)
				{
					Vanish();

					if (particle != null)
					{
						particle.Play();
					}
				}
				else
				{
					StartCoroutine(QuickShowRoutine());
				}

				StartCoroutine(CollectRoutine(player));
			}
		}

		public virtual void Vanish()
		{
			if (!m_vanished)
			{
				m_vanished = true;
				m_elapsedLifeTime = 0;
				display.SetActive(false);
				m_collider.enabled = false;
			}
		}

		protected virtual void HandleGhosting()
		{
			if (m_ghosting)
			{
				m_elapsedGhostingTime += Time.deltaTime;

				if (m_elapsedGhostingTime >= ghostingDuration)
				{
					m_elapsedGhostingTime = 0;
					m_ghosting = false;
				}
			}
		}

		protected virtual void HandleLifeTime()
		{
			if (hasLifeTime)
			{
				m_elapsedLifeTime += Time.deltaTime;

				if (m_elapsedLifeTime >= lifeTimeDuration)
				{
					Vanish();
				}
			}
		}

		protected virtual void HandleMovement()
		{
			m_velocity -= gravity * Time.deltaTime * transform.up;
		}

		protected virtual void HandleSweep()
		{
			var direction = m_velocity.normalized;
			var magnitude = m_velocity.magnitude;
			var distance = magnitude * Time.deltaTime;

			if (
				Physics.SphereCast(
					transform.position,
					collisionRadius,
					direction,
					out var hit,
					distance,
					Physics.DefaultRaycastLayers,
					QueryTriggerInteraction.Ignore
				) && !GameTags.IsPlayer(hit.collider)
			)
			{
				if (Level.instance.player.IsSideScroller)
					HandleBounceSideScroller(direction, magnitude, hit.normal);
				else
					HandleBounce(direction, magnitude, hit.normal);

				PlayCollisionSound();
			}

			transform.position += m_velocity * Time.deltaTime;
		}

		protected virtual void HandleBounce(Vector3 direction, float magnitude, Vector3 normal)
		{
			var bounceDirection = Vector3.Reflect(direction, normal);
			m_velocity = bounciness * magnitude * bounceDirection;
			var currentYBounce = Vector3.Dot(transform.up, m_velocity);
			m_velocity -= transform.up * currentYBounce;
			m_velocity += transform.up * Mathf.Min(currentYBounce, maxBounceYVelocity);

			if (currentYBounce <= minForceToStopPhysics)
				usePhysics = false;
		}

		protected virtual void HandleBounceSideScroller(
			Vector3 direction,
			float magnitude,
			Vector3 normal
		)
		{
			var pathForward = Level.instance.player.pathForward;
			var right = Vector3.Cross(transform.up, pathForward);
			var bounceDirection = Vector3.Reflect(direction, normal);
			bounceDirection -= right * Vector3.Dot(right, bounceDirection);
			bounceDirection.Normalize();
			m_velocity = bounciness * magnitude * bounceDirection;
		}

		protected virtual void PlayCollisionSound()
		{
			if (!m_audio || !collisionClip)
				return;

			m_audio.Stop();
			m_audio.PlayOneShot(collisionClip);
		}

		protected virtual void Awake()
		{
			InitializeAudio();
			InitializeCollider();
			InitializeTransform();
			InitializeDisplay();
			InitializeVelocity();
		}

		protected virtual void Update()
		{
			if (m_vanished)
				return;

			HandleGhosting();
			HandleLifeTime();

			if (usePhysics)
			{
				HandleMovement();
				HandleSweep();
			}
		}

		protected virtual void OnTriggerStay(Collider other)
		{
			if (collectOnContact && other.CompareTag(GameTags.Player))
			{
				if (other.TryGetComponent<Player>(out var player))
				{
					Collect(player);
				}
			}
		}

		protected virtual void OnDrawGizmos()
		{
			if (usePhysics)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(transform.position, collisionRadius);
			}
		}
	}
}
