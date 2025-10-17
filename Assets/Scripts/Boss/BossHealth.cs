using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Quản lý máu, phase, trạng thái sống/chết của Boss.
    /// Gửi event cho UI & BossCore khi máu thay đổi hoặc boss bị hạ.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Health")]
    public class BossHealth : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

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
        public event Action<float> OnHealthChanged; // normalized [0..1]

        #endregion

        //─────────────────────────────────────────────
        #region === PRIVATE RUNTIME DATA ===

        private Color baseColor;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            // Lấy renderer nếu chưa gán trong Inspector
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
                baseColor = renderers[0].material.color;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === INITIALIZATION ===

        /// <summary>Gán controller chính cho BossHealth.</summary>

        /// <summary>Khởi tạo lại thông tin phase mới.</summary>
        public void InitializePhase(int phaseIndex, int phaseMaxHealth)
        {
            isTransitioning = true;
            currentPhase = phaseIndex;

            m_maxHealth = Mathf.Max(1, phaseMaxHealth);
            m_currentHealth = m_maxHealth;
            isDead = false;

            // Gửi event
            OnHealthChanged?.Invoke(1f);
            OnBossHealed?.Invoke();
            OnPhaseChanged?.Invoke(currentPhase);

            isTransitioning = false;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === PROPERTIES ===

        public int MaxHealth => m_maxHealth;
        public int CurrentHealth => m_currentHealth;
        public float HealthPercentage => m_maxHealth > 0 ? (float)m_currentHealth / m_maxHealth : 0f;

        #endregion

        //─────────────────────────────────────────────
        #region === HEALTH OPERATIONS ===

        /// <summary>Boss nhận sát thương.</summary>
        public void TakeDamage(int amount)
        {
            if (isDead || isTransitioning) return;

            m_currentHealth = Mathf.Clamp(m_currentHealth - Mathf.Max(0, amount), 0, m_maxHealth);
            OnHealthChanged?.Invoke(HealthPercentage);
            Flash();

            if (m_currentHealth <= 0)
            {
                isDead = true;
                OnBossDefeated?.Invoke(); // BossCore sẽ xử lý chuyển phase
            }
        }

        /// <summary>Hồi máu đầy và cập nhật max health mới.</summary>
        public void FullHealTo(int newMax)
        {
            m_maxHealth = Mathf.Max(1, newMax);
            m_currentHealth = m_maxHealth;
            isDead = false;

            OnHealthChanged?.Invoke(1f);
            OnBossHealed?.Invoke();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === VISUAL FEEDBACK ===

        /// <summary>Hiệu ứng flash khi boss bị trúng đòn.</summary>
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

        #endregion
    }
}
