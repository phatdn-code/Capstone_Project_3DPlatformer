using UnityEngine;
using UnityEngine.Splines;
using System.Collections;
using System.Collections.Generic;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Gravity Field")]
	public class GravityField : MonoBehaviour
	{
		public enum Shape
		{
			Parallel,
			Box,
			Sphere,
			Capsule,
			Cylinder,
			Spline,
			HalfPipe,
			Disc
		}

		[Header("Surface Settings")]
		public Shape shape;

		[SerializeField]
		[Min(0)]
		protected float m_height = 2f;

		[SerializeField]
		[Min(0)]
		public float m_radius = 0.5f;

		[SerializeField]
		protected Vector3 m_center;

		[SerializeField]
		protected Vector3 m_size = new Vector3(1, 1, 1);

		[Header("Field Settings")]
		public bool inverted;
		public bool detachOnExit;
		public bool resetRotationOnDetach;
		public int priority;

		[Tooltip("Rigidbody Only. Increases or decreases the gravity force.")]
		public float gravityMultiplier = 2f;

		public Collider trigger;

		protected SplineContainer m_spline;
		protected Entity m_tempEntity;

		protected List<Collider> m_ignoredColliders = new();
		protected Dictionary<Collider, Entity> m_entities = new();

		public Vector3 up => transform.up;
		public Vector3 right => transform.right;
		public Vector3 forward => transform.forward;

		public Vector3 localCenter => m_center;
		public Vector3 center => transform.position + transform.rotation * m_center;

		public Vector3 localSize => m_size;
		public Vector3 size => Vector3.Scale(m_size, transform.lossyScale);

		public Quaternion rotation => transform.rotation;

		public float height => m_height * transform.lossyScale.y;
		public float halfPipeHeight => m_height * transform.localScale.x;

		public float radius => m_radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
		public float halfPipeRadius => m_radius * Mathf.Max(transform.localScale.y, transform.localScale.y);

		public Vector3 top => center + transform.up * (height * 0.5f);
		public Vector3 bottom => center - transform.up * (height * 0.5f);

		public Vector3 halfPipeRight => center + transform.right * (halfPipeHeight * 0.5f);
		public Vector3 halfPipeLeft => center - transform.right * (halfPipeHeight * 0.5f);

		public Vector3 topSphere => center + transform.up * (height * 0.5f - radius);
		public Vector3 bottomSphere => center - transform.up * (height * 0.5f - radius);

		protected virtual void InitializeTag() => gameObject.tag = GameTags.GravityField;

		protected virtual void InitializeSpline()
		{
			if (shape != Shape.Spline) return;

			m_spline = GetComponent<SplineContainer>();
		}

		public virtual Vector3 GetGravityDirectionFrom(Vector3 point)
		{
			var direction = inverted ? -1 : 1;

			return shape switch
			{
				Shape.Box => -GravityHelper.GetUpDirectionFromBox(this, point) * direction,
				Shape.Sphere => -GravityHelper.GetUpDirectionFromSphere(this, point) * direction,
				Shape.Cylinder => -GravityHelper.GetUpDirectionFromCylinder(this, point) * direction,
				Shape.Capsule => -GravityHelper.GetUpDirectionFromCapsule(this, point) * direction,
				Shape.Spline => -GravityHelper.GetUpDirectionFromSpline(m_spline, point) * direction,
				Shape.HalfPipe => -GravityHelper.GetUpDirectionFromHalfPipe(this, point, !inverted),
				Shape.Disc => -GravityHelper.GetUpDirectionFromDisc(this, point) * direction,
				_ => -transform.up * direction
			};
		}

		protected virtual bool ValidCollider(Collider other) =>
			!m_ignoredColliders.Contains(other) && GameTags.IsEntity(other);

		protected virtual void CacheEntity(Collider other, out Entity entity)
		{
			if (m_entities.TryGetValue(other, out entity)) return;

			m_entities.Add(other, other.GetComponent<Entity>());
			entity = m_entities[other];
		}

		protected virtual void HandleEntity(Entity entity)
		{
			if (!entity.CanChangeToGravityField(this)) return;

			if (!entity.gravityField)
				entity.gravityField = this;
			else if (entity.gravityField.priority < priority)
				entity.gravityField = this;
			else if (entity.gravityField.priority == priority &&
				!entity.isGrounded && entity.verticalVelocity.y > 0 &&
				Vector3.Dot(entity.velocity, center - entity.position) > 0)
			{
				entity.verticalVelocity = Vector3.zero;
				entity.gravityField.IgnoreCollider(entity.controller);
				entity.gravityField = this;
			}
		}

		protected virtual void HandleDetach(Entity entity)
		{
			if (entity.gravityField != this) return;

			entity.gravityField = null;
			IgnoreCollider(entity.controller);

			if (!resetRotationOnDetach) return;

			var rotation = Quaternion.FromToRotation(entity.transform.up, Vector3.up);
			entity.transform.rotation = rotation * entity.transform.rotation;
		}

		public virtual void IgnoreCollider(Collider other)
		{
			if (m_ignoredColliders.Contains(other)) return;

			m_ignoredColliders.Add(other);
			StartCoroutine(AllowColliderRoutine(other));
		}

		public virtual void RemoveIgnoredColliders() => m_ignoredColliders.Clear();

		protected IEnumerator AllowColliderRoutine(Collider other)
		{
			yield return new WaitForSeconds(0.5f);

			if (m_ignoredColliders.Contains(other))
				m_ignoredColliders.Remove(other);
		}

		protected virtual void Awake()
		{
			InitializeTag();
			InitializeSpline();
		}

		protected virtual void OnDisable() => RemoveIgnoredColliders();

		protected virtual void OnTriggerExit(Collider other)
		{
			if (!ValidCollider(other)) return;
			if (!detachOnExit) return;

			CacheEntity(other, out m_tempEntity);
			HandleDetach(m_tempEntity);
		}

		protected virtual void OnTriggerStay(Collider other)
		{
			if (!ValidCollider(other)) return;

			CacheEntity(other, out m_tempEntity);
			HandleEntity(m_tempEntity);
		}
	}
}
