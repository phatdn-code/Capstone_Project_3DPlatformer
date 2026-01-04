using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using System;
using System.Collections;

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

        [Header("Take Damage Anim Gate (Optional)")]
        [SerializeField] private bool useTakeDamageAnimGate;
        [SerializeField] private int takeDamageAnimThreshold = 20;
        private int _takeDamageAnimAccum = 0;

        [Header("Phase Break (Phase 1 -> Phase 2)")]
        [SerializeField] private bool enablePhase1Break = true;
        [SerializeField, Min(0f)] private float phase1BreakDelay = 1.5f;

        private bool _isPhase1BreakRunning = false;

        public int MaxHealth => m_maxHealth;
        public int CurrentHealth => m_currentHealth;
        public float HealthPercentage => m_maxHealth > 0 ? (float)m_currentHealth / m_maxHealth : 0f;

        #endregion
        //─────────────────────────────────────────────

        #region === STATE FLAGS ===

        [Header("State Flags")]
        [HideInInspector] public int currentPhase = 0;
        [HideInInspector] public bool isTransitioning = false;
        [HideInInspector] public bool isDead = false;

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

        /// <summary>Bật/tắt cơ chế chỉ play TakeDamage animation khi đủ ngưỡng damage.</summary>
        public void SetTakeDamageAnimGate(bool enabled, int threshold = 20, bool resetAccum = true)
        {
            useTakeDamageAnimGate = enabled;
            takeDamageAnimThreshold = Mathf.Max(1, threshold);

            if (resetAccum)
                _takeDamageAnimAccum = 0;
        }


        /// <summary>
        /// Boss nhận sát thương
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (isDead || isTransitioning)
                return;

            int dmg = Mathf.Max(0, amount);

            m_currentHealth = Mathf.Clamp(m_currentHealth - dmg, 0, m_maxHealth);
            OnHealthChanged?.Invoke(HealthPercentage);

            Flash();

            // ✅ Gate TakeDamage animation (chỉ boss nào bật mới áp dụng)
            if (boss?.BossAnim != null)
            {
                // Hành vi cũ: trúng là play anim ngay
                if (!useTakeDamageAnimGate)
                    boss.BossAnim.PlayTakeDamage();

                else
                {
                    // Hành vi mới: tích luỹ đủ ngưỡng mới play anim
                    _takeDamageAnimAccum += dmg;

                    if (_takeDamageAnimAccum >= takeDamageAnimThreshold)
                    {
                        _takeDamageAnimAccum = 0;
                        boss.BossAnim.PlayTakeDamage();
                    }
                }
            }

            if (m_currentHealth <= 0)
            {
                // Phase 1 break: play stagger like "20 damage", then transition to Phase 2 with full HP.
                if (enablePhase1Break && currentPhase == 0 && !_isPhase1BreakRunning
                    && boss != null && boss.Phases != null && (currentPhase + 1) < boss.Phases.Length)
                {
                    StartCoroutine(Phase1BreakAndTransition());
                    return;
                }

                // Real defeat (no next phase)
                isDead = true;
                OnBossDefeated?.Invoke();
            }
        }

        private IEnumerator Phase1BreakAndTransition()
        {
            _isPhase1BreakRunning = true;
            isTransitioning = true; // block further damage + boss update loop

            // Force stagger on DragonRobot (same behavior as damage >= 20)
            var dragon = boss as DragonRobot;
            if (dragon != null)
                dragon.ForceStaggerAndRetreatForPhaseBreak();

            else boss?.BossAnim?.PlayTakeDamage(); // fallback if this boss isn't DragonRobot

            yield return new WaitForSeconds(phase1BreakDelay);

            isTransitioning = false;
            OnBossDefeated?.Invoke();
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
