using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(Collider), typeof(AudioSource))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Breakable")]
	public class Breakable : MonoBehaviour
	{
		[Header("General Settings")]
		[Tooltip("The object to disable when this object breaks.")]
		public GameObject display;

		[Tooltip("The sound to play when this object breaks.")]
		public AudioClip clip;

		[Tooltip("The sound to play when this object takes damage.")]
		public AudioClip damageClip;

		[Header("Damage Settings")]
		[Tooltip("The initial health points of this object.")]
		public int initialHP = 1;

		[Tooltip("The amount of time in seconds before this object can take damage again.")]
		public float damageCooldown = 0.5f;

		[Header("Breakable Events")]
		[Tooltip("Called when this object takes damage.")]
		public UnityEvent<int> OnDamage;

		[Tooltip("Called when this object breaks.")]
		public UnityEvent OnBreak;

		protected Collider m_collider;
		protected AudioSource m_audio;
		protected Rigidbody m_rigidBody;

		protected float m_lastDamageTime;

		public int HP { get; protected set; }

		public bool broken => HP <= 0;
		public bool canTakeDamage => Time.time - m_lastDamageTime >= damageCooldown;

		protected virtual void InitializeAudio()
		{
			if (!TryGetComponent(out m_audio))
				m_audio = gameObject.AddComponent<AudioSource>();
		}

		protected virtual void InitializeCollider()
		{
			m_collider = GetComponent<Collider>();
		}

		protected virtual void InitializeHP() => HP = initialHP;

		public virtual void ApplyDamage(int amount)
		{
			if (broken || !canTakeDamage)
				return;

			HP = Mathf.Max(0, HP - amount);

			if (HP > 0)
				Damage(amount);
			else
				Break();
		}

		protected virtual void Damage(int amount)
		{
			PlayAudioClip(damageClip);
			m_lastDamageTime = Time.time;
			OnDamage.Invoke(amount);
		}

		protected virtual void Break()
		{
			DisableRigidbody();
			PlayAudioClip(clip);
			display.SetActive(false);
			m_collider.enabled = false;
			OnBreak?.Invoke();
		}

		protected virtual void DisableRigidbody()
		{
			if (!m_rigidBody)
				return;

			m_rigidBody.isKinematic = true;
		}

		protected virtual void PlayAudioClip(AudioClip clip)
		{
			if (clip)
				m_audio.PlayOneShot(clip);
		}

		protected virtual void Start()
		{
			InitializeAudio();
			InitializeCollider();
			TryGetComponent(out m_rigidBody);
			InitializeHP();
		}
	}
}
