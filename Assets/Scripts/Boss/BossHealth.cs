using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System;

namespace PLAYERTWO.PlatformerProject
{
    [DisallowMultipleComponent]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Health")]
    public class BossHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int m_maxHealth = 100;
        [SerializeField] private int m_currentHealth = 100;

        [Header("State Flags")]
        [SerializeField] public int currentPhase = 0;
        [SerializeField] public bool isTransitioning = false;
        [SerializeField] public bool isDead = false;

        [Header("Renderers (for Flash Effect)")]
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private float flashTime = 0.15f;

        [Header("Events")]
        public UnityEvent<int> OnPhaseChanged = new UnityEvent<int>();
        public UnityEvent OnBossHealed = new UnityEvent();
        public UnityEvent OnBossDefeated = new UnityEvent();
        public event Action<float> OnHealthChanged;

        private Color baseColor;
        private BossCore boss; // ✅ cache tại đây

        private void Start()
        {
            // Cache BossLinker một lần duy nhất
            boss = GetComponent<BossCore>();

            // Cache renderers
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
                baseColor = renderers[0].material.color;
        }

        public int MaxHealth => m_maxHealth;
        public int CurrentHealth => m_currentHealth;
        public float HealthPercentage => m_maxHealth > 0 ? (float)m_currentHealth / m_maxHealth : 0f;

        public void InitializePhase(int phaseIndex, int phaseMaxHealth)
        {
            isTransitioning = true;
            currentPhase = phaseIndex;
            m_maxHealth = Mathf.Max(1, phaseMaxHealth);
            m_currentHealth = m_maxHealth;
            isDead = false;

            OnHealthChanged?.Invoke(1f);
            OnBossHealed?.Invoke();
            OnPhaseChanged?.Invoke(currentPhase);

            isTransitioning = false;
        }

        public void TakeDamage(int amount)
        {
            if (isDead || isTransitioning) return;

            m_currentHealth = Mathf.Clamp(m_currentHealth - Mathf.Max(0, amount), 0, m_maxHealth);
            OnHealthChanged?.Invoke(HealthPercentage);

            Flash();

            if (boss != null && boss.BossAnim != null)
                boss.BossAnim.PlayTakeDamage();

            if (m_currentHealth <= 0)
            {
                isDead = true;
                OnBossDefeated?.Invoke();
            }
        }

        public void FullHealTo(int newMax)
        {
            m_maxHealth = Mathf.Max(1, newMax);
            m_currentHealth = m_maxHealth;
            isDead = false;

            OnHealthChanged?.Invoke(1f);
            OnBossHealed?.Invoke();
        }

        public void SetHealth(float value)
        {
            m_currentHealth = Mathf.Clamp((int)value, 0, m_maxHealth);
            OnHealthChanged?.Invoke(HealthPercentage);
        }

        private void Flash()
        {
            if (renderers == null || renderers.Length == 0) return;

            foreach (var r in renderers)
            {
                var mat = r.material;
                mat.DOColor(Color.red, flashTime * 0.5f)
                   .OnComplete(() => mat.DOColor(baseColor, flashTime * 0.5f));
            }
        }
    }
}
