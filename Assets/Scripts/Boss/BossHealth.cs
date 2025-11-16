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
        //─────────────────────────────────────────────
        #region === HEALTH SETTINGS ===

        [Header("Health Settings")]
        [SerializeField] private int m_maxHealth = 100;
        [SerializeField] private int m_currentHealth = 100;

        public int MaxHealth => m_maxHealth;
        public int CurrentHealth => m_currentHealth;
        public float HealthPercentage => m_maxHealth > 0 ? (float)m_currentHealth / m_maxHealth : 0f;

        #endregion
        //─────────────────────────────────────────────

        #region === STATE FLAGS ===

        [Header("State Flags")]
        [SerializeField] public int currentPhase = 0;
        [SerializeField] public bool isTransitioning = false;
        [SerializeField] public bool isDead = false;

        #endregion
        //─────────────────────────────────────────────

        #region === FLASH EFFECT ===

        [Header("Renderers (Flash Effect)")]
        private Renderer[] renderers;      // Danh sách renderer của boss
        private Color baseColor;           // Màu gốc
        [SerializeField] private float flashTime = 0.15f;

        #endregion
        //─────────────────────────────────────────────

        #region === EVENTS ===

        [Header("Events")]
        public UnityEvent<int> OnPhaseChanged = new UnityEvent<int>();
        public UnityEvent OnBossHealed = new UnityEvent();
        public UnityEvent OnBossDefeated = new UnityEvent();
        public event Action<float> OnHealthChanged;   // Trả ra % máu

        #endregion
        //─────────────────────────────────────────────

        #region === RUNTIME ===

        private BossCore boss;     // Cache BossCore

        #endregion
        //─────────────────────────────────────────────

        #region === UNITY ===

        private void Start()
        {
            // Lấy BossCore
            boss = GetComponent<BossCore>();

            // Cache tất cả renderers
            renderers = GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
                baseColor = renderers[0].sharedMaterial.color;
        }

        #endregion
        //─────────────────────────────────────────────


        #region === PHASE / HEALTH CONTROL ===

        /// <summary>
        /// Khởi tạo phase mới cho boss
        /// </summary>
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

        /// <summary>
        /// Boss nhận sát thương
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (isDead || isTransitioning)
                return;

            m_currentHealth = Mathf.Clamp(m_currentHealth - Mathf.Max(0, amount), 0, m_maxHealth);
            OnHealthChanged?.Invoke(HealthPercentage);

            Flash();

            if (boss?.BossAnim != null)
                boss.BossAnim.PlayTakeDamage();

            if (m_currentHealth <= 0)
            {
                isDead = true;
                OnBossDefeated?.Invoke();
            }
        }

        /// <summary>
        /// Boss hồi máu full và thiết lập lại max HP
        /// </summary>
        public void FullHealTo(int newMax)
        {
            m_maxHealth = Mathf.Max(1, newMax);
            m_currentHealth = m_maxHealth;
            isDead = false;

            OnHealthChanged?.Invoke(1f);
            OnBossHealed?.Invoke();
        }

        /// <summary>
        /// Set lại máu theo giá trị bên ngoài
        /// </summary>
        public void SetHealth(float value)
        {
            m_currentHealth = Mathf.Clamp((int)value, 0, m_maxHealth);
            OnHealthChanged?.Invoke(HealthPercentage);
        }

        #endregion
        //─────────────────────────────────────────────


        #region === FLASH EFFECT LOGIC ===

        /// <summary>
        /// Tạo hiệu ứng chớp đỏ khi bị đánh (KHÔNG phá material)
        /// </summary>
        private void Flash()
        {
            if (renderers == null || renderers.Length == 0)
                return;

            foreach (var r in renderers)
            {
                var mat = r.sharedMaterial;

                mat.DOColor(Color.red, flashTime * 0.5f)
                    .OnComplete(() =>
                        mat.DOColor(baseColor, flashTime * 0.5f)
                    );
            }
        }

        #endregion
        //─────────────────────────────────────────────
    }
}
