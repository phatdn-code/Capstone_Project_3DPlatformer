using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(Collider))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Conveyor Belt")]
	public class ConveyorBelt : MonoBehaviour, IEntityContact
	{
		public float speed = 5;

		protected Collider m_collider;

		public Vector3 velocity => transform.forward * speed;

		protected virtual void Start()
		{
			m_collider = GetComponent<Collider>();
		}

		public virtual void OnEntityContact(Entity entity)
		{
			if (BoundsHelper.IsBellowPoint(m_collider, entity.stepPosition))
				entity.controller.Move(velocity * Time.deltaTime);
		}

		protected virtual void OnCollisionStay(Collision collision)
		{
			var point = collision.GetContact(0).point;

			if (!BoundsHelper.IsBellowPoint(m_collider, point)) return;

			if (collision.body is Rigidbody rb)
				rb.MovePosition(rb.position + velocity * Time.deltaTime);
		}
	}
}
