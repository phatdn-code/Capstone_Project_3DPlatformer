using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(BoxCollider))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Item Box")]
	public class ItemBox : MonoBehaviour, IEntityContact
	{
		public Collectable[] collectables;
		public MeshRenderer itemBoxRenderer;
		public Material emptyItemBoxMaterial;

		[Space(15)]
		public UnityEvent onCollect;
		public UnityEvent onDisable;

		protected int m_index;
		protected bool m_enabled = true;
		protected Vector3 m_initialScale;

		protected BoxCollider m_collider;

		protected virtual void InitializeCollectables()
		{
			foreach (var collectable in collectables)
			{
				if (!collectable.hidden)
				{
					collectable.gameObject.SetActive(false);
				}
				else
				{
					collectable.collectOnContact = false;
				}
			}
		}

		public virtual void Collect(Player player)
		{
			if (m_enabled)
			{
				if (m_index < collectables.Length)
				{
					if (collectables[m_index].hidden)
					{
						collectables[m_index].Collect(player);
					}
					else
					{
						collectables[m_index].gameObject.SetActive(true);
					}

					m_index = Mathf.Clamp(m_index + 1, 0, collectables.Length);
					onCollect?.Invoke();
				}

				if (m_index == collectables.Length)
				{
					Disable();
				}
			}
		}

		public virtual void Disable()
		{
			if (m_enabled)
			{
				m_enabled = false;
				itemBoxRenderer.sharedMaterial = emptyItemBoxMaterial;
				onDisable?.Invoke();
			}
		}

		protected virtual void Start()
		{
			m_collider = GetComponent<BoxCollider>();
			m_initialScale = transform.localScale;
			InitializeCollectables();
		}

		public void OnEntityContact(Entity entity)
		{
			if (entity is Player player)
			{
				var offset = entity.height * 0.5f - entity.radius;
				var head = entity.position + entity.transform.up *
					(offset - Physics.defaultContactOffset);

				if (entity.verticalVelocity.y > 0 &&
					BoundsHelper.IsAbovePoint(m_collider, head))
				{
					Collect(player);
					entity.verticalVelocity = Vector3.zero;
				}
			}
		}
	}
}
