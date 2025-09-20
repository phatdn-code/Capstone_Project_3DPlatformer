using System;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	public abstract class EntityStateManager : MonoBehaviour
	{
		public EntityStateManagerEvents events;
	}

	public abstract class EntityStateManager<T> : EntityStateManager where T : Entity<T>
	{
		protected List<EntityState<T>> m_list = new List<EntityState<T>>();

		protected Dictionary<Type, EntityState<T>> m_states = new Dictionary<Type, EntityState<T>>();

		/// <summary>
		/// Returns the instance of the current Entity State.
		/// </summary>
		/// <value></value>
		public EntityState<T> current { get; protected set; }

		/// <summary>
		/// Returns the instance of the last Entity State.
		/// </summary>
		/// <value></value>
		public EntityState<T> last { get; protected set; }

		/// <summary>
		/// Return the index of the current Entity State.
		/// </summary>
		public int index => m_list.IndexOf(current);

		/// <summary>
		/// Return the index of the current Entity State.
		/// </summary>
		public int lastIndex => m_list.IndexOf(last);

		/// <summary>
		/// Return the instance of the Entity associated with this Entity State Manager.
		/// </summary>
		public T entity { get; protected set; }

		protected abstract List<EntityState<T>> GetStateList();

		protected virtual void InitializeEntity() => entity = GetComponent<T>();

		protected virtual void InitializeStates()
		{
			m_list = GetStateList();

			foreach (var state in m_list)
			{
				var type = state.GetType();

				if (!m_states.ContainsKey(type))
				{
					m_states.Add(type, state);
				}
			}

			if (m_list.Count > 0)
			{
				current = m_list[0];
			}
		}

		/// <summary>
		/// Change to a given Entity State based on its index on the States list.
		/// </summary>
		/// <param name="to">The index of the State you want to change to.</param>
		public virtual void Change(int to)
		{
			if (to >= 0 && to < m_list.Count)
			{
				Change(m_list[to]);
			}
		}

		/// <summary>
		/// Change to a given Entity State based on its class type.
		/// </summary>
		/// <typeparam name="TState">The class of the state you want to change to.</typeparam>
		public virtual void Change<TState>() where TState : EntityState<T>
		{
			var type = typeof(TState);

			if (m_states.ContainsKey(type))
			{
				Change(m_states[type]);
			}
		}

		/// <summary>
		/// Changes to a given Entity State based on its instance.
		/// </summary>
		/// <param name="to">The instance of the Entity State you want to change to.</param>
		public virtual void Change(EntityState<T> to)
		{
			if (to != null && Time.timeScale > 0)
			{
				if (current != null)
				{
					current.Exit(entity);
					events.onExit.Invoke(current.GetType());
					last = current;
				}

				current = to;
				current.Enter(entity);
				events.onEnter.Invoke(current.GetType());
				events.onChange?.Invoke();
			}
		}

		/// <summary>
		/// Returns true if the type of the current State matches a given one.
		/// </summary>
		/// <param name="type">The type you want to compare to.</param>
		public virtual bool IsCurrentOfType(params Type[] types)
		{
			if (current == null)
				return false;

			foreach (var type in types)
			{
				if (current.GetType() == type)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if the manager has a State of a given type.
		/// </summary>
		/// <param name="type">The Type of the State you want to find.</param>
		public virtual bool ContainsStateOfType(Type type) => m_states.ContainsKey(type);

		public virtual void Step()
		{
			if (current != null && Time.timeScale > 0)
			{
				current.Step(entity);
			}
		}

		public virtual void OnContact(Collider other)
		{
			if (current != null && Time.timeScale > 0)
			{
				current.OnContact(entity, other);
			}
		}

		protected virtual void Start()
		{
			InitializeEntity();
			InitializeStates();
		}
	}
}
