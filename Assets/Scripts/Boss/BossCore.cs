using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Base class for all bosses — kế thừa Enemy.
    /// Quản lý phase, liên kết BossHealth, phát sự kiện cho UI/Manager.
    /// </summary>
    [RequireComponent(typeof(BossHealth))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Core")]
    public abstract class BossCore : Enemy
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Phases")]
        [SerializeField] protected BossPhase[] m_phases = new BossPhase[3];

        [Header("Boss Events")]
        [SerializeField] private UnityEvent<int> OnBossPhaseStart = new UnityEvent<int>();
        [SerializeField] private UnityEvent<string> OnSpecialAbilityUsed = new UnityEvent<string>();

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME REFERENCES ===

        protected BossHealth m_bossHealth;

        #endregion

        //─────────────────────────────────────────────
        #region === STATE VARIABLES ===

        protected bool m_isAttacking;
        protected float m_lastAttackTime;

        #endregion

        //─────────────────────────────────────────────
        #region === PROPERTIES ===

        public BossHealth bossHealth => m_bossHealth;
        public BossPhase[] phases { get => m_phases; set => m_phases = value; }

        public UnityEvent<int> OnBossPhaseStartEvent => OnBossPhaseStart;
        public UnityEvent<string> OnSpecialAbilityUsedEvent => OnSpecialAbilityUsed;

        public BossPhase currentPhase =>
            (m_phases != null && m_phases.Length > 0 && m_bossHealth.currentPhase < m_phases.Length)
            ? m_phases[m_bossHealth.currentPhase]
            : null;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        protected virtual void Start()
        {
            InitializeBossHealth();
            InitializeDefaultPhasesIfNeeded();
            HookHealthEvents();

            // Khởi tạo phase đầu tiên
            ApplyPhaseVisual(0, instant: true);
            m_bossHealth.InitializePhase(0, m_phases[0].maxHealth);
            OnBossPhaseStart.Invoke(0);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (m_bossHealth == null || m_bossHealth.isDead || m_bossHealth.isTransitioning)
                return;

            // Cho lớp con (vd: SoldierRobot) xử lý hành vi
            UpdateBossBehavior();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === INITIALIZATION ===

        private void InitializeBossHealth()
        {
            var linker = GetComponent<BossLinker>();

            if (linker != null && linker.bossHealth != null)
                m_bossHealth = linker.bossHealth;

            // Fallback an toàn (chỉ khi prefab chưa có BossLinker)
            else m_bossHealth = GetComponent<BossHealth>();

        }


        private void InitializeDefaultPhasesIfNeeded()
        {
            if (m_phases != null && m_phases.Length > 0) return;

            m_phases = new BossPhase[3];
            for (int i = 0; i < 3; i++)
            {
                m_phases[i] = new BossPhase
                {
                    phaseName = $"Phase {i + 1}",
                    maxHealth = 150 + i * 100,
                    moveSpeed = 4 + i,
                    attackSpeed = Mathf.Max(0.5f, 1.5f - i * 0.2f),
                    damage = 10 + i * 5,
                    sightRange = 10 + i * 5,
                    phaseColor = (i == 2) ? Color.red : Color.white,
                    scale = Vector3.one * (1f + i * 0.15f),
                    canUseSpecialAbility = (i == 2),
                    specialAbilityName = (i == 2) ? "Rage Mode" : "",
                    specialAbilityCooldown = (i == 2) ? 5f : 0f
                };
            }
        }

        private void HookHealthEvents()
        {
            // Khi BossHealth báo defeated, quyết định phase tiếp theo hoặc kết thúc
            m_bossHealth.OnBossDefeated.AddListener(HandlePhaseOrDefeat);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === PHASE MANAGEMENT ===

        private void HandlePhaseOrDefeat()
        {
            int nextPhase = m_bossHealth.currentPhase + 1;

            if (m_phases != null && nextPhase < m_phases.Length)
            {
                // Sang phase tiếp theo
                ApplyPhaseVisual(nextPhase, instant: false);
                m_bossHealth.InitializePhase(nextPhase, m_phases[nextPhase].maxHealth);
                OnBossPhaseStart.Invoke(nextPhase);
            }

            // Hết phase → boss thua
            else OnBossDefeated();
        }

        private void ApplyPhaseVisual(int phaseIndex, bool instant)
        {
            var phase = m_phases[phaseIndex];

            // Color
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (instant) r.material.color = phase.phaseColor;
                else r.material.DOColor(phase.phaseColor, 0.35f);
            }

            // Scale
            if (instant)
                transform.localScale = phase.scale;

            else transform.DOScale(phase.scale, 0.35f).SetEase(Ease.OutBack);
        }

        protected virtual void OnBossDefeated()
        {
            // Cho lớp con override nếu muốn thêm hiệu ứng khác
            transform.DOScale(0f, 0.5f).SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SPECIAL ABILITY ===

        /// <summary>
        /// Cho lớp con gọi khi dùng kỹ năng đặc biệt để thông báo UI/Manager.
        /// </summary>
        protected void NotifySpecialUsed(string nameOrId)
        {
            OnSpecialAbilityUsed.Invoke(nameOrId);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === ABSTRACT METHODS ===

        /// <summary>
        /// Lớp con override để định nghĩa hành vi boss trong mỗi frame.
        /// </summary>
        protected abstract void UpdateBossBehavior();

        #endregion
    }
}
