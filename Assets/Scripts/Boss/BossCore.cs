using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

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

        [Header("Encounter Settings")]
        [SerializeField] private bool startInactive = true; // ← True cho cinematic intro
        private bool isEncounterActive = false;
        public bool IsEncounterActive => isEncounterActive;

        [Header("Phases Configuration")]
        [SerializeField] protected BossPhase[] m_phases = new BossPhase[3];

        [Header("Boss Events")]
        [SerializeField] private UnityEvent<int> OnBossPhaseStart = new UnityEvent<int>();
        [SerializeField] private UnityEvent<string> OnSpecialAbilityUsed = new UnityEvent<string>();

        #endregion


        //─────────────────────────────────────────────
        #region === RUNTIME REFERENCES ===

        protected BossHealth m_bossHealth;
        protected BossUI m_bossUI;
        protected BossAnimationBase m_bossAnim;
        protected BossPhaseTransitionBase m_phaseTransition;
        protected BossFinalSequenceBase m_finalPhase;

        #endregion


        //─────────────────────────────────────────────
        #region === STATE VARIABLES ===

        protected bool m_isAttacking;
        protected float m_lastAttackTime;

        private bool isInCutscene;
        public bool IsInCutscene
        {
            get => isInCutscene;
            set => isInCutscene = value;
        }

        #endregion


        //─────────────────────────────────────────────
        #region === PUBLIC ACCESSORS ===

        // ─── Core Components ────────────────────────────────
        /// <summary>BossHealth component reference.</summary>
        public BossHealth BossHealth => m_bossHealth;

        /// <summary>BossAnimationBase component reference.</summary>
        public BossAnimationBase BossAnim => m_bossAnim;

        /// <summary>BossUI component reference.</summary>
        public BossUI BossUI => m_bossUI;

        // ─── Phase & Sequence Components ─────────────────────
        /// <summary>Handles phase transition logic between boss stages.</summary>
        public BossPhaseTransitionBase PhaseTransition => m_phaseTransition;

        /// <summary>Handles the final phase sequence (defeat or cinematic).</summary>
        public BossFinalSequenceBase FinalPhase => m_finalPhase;

        // ─── Phase Data ─────────────────────────────────────
        /// <summary>Array of defined boss phases (modifiable at runtime).</summary>
        public BossPhase[] Phases
        {
            get => m_phases;
            set => m_phases = value;
        }

        // ─── State & Events ─────────────────────────────────
        /// <summary>Returns true if boss is still alive.</summary>
        public bool IsAlive => m_bossHealth != null && m_bossHealth.CurrentHealth > 0;

        /// <summary>Invoked when a new boss phase starts.</summary>
        public UnityEvent<int> OnBossPhaseStartEvent => OnBossPhaseStart;

        /// <summary>Invoked when the boss uses a special ability.</summary>
        public UnityEvent<string> OnSpecialAbilityUsedEvent => OnSpecialAbilityUsed;

        // ─── Dynamic Info ───────────────────────────────────
        /// <summary>Gets the currently active boss phase.</summary>
        public BossPhase CurrentPhase =>
            (m_phases != null && m_phases.Length > 0 && m_bossHealth.currentPhase < m_phases.Length)
            ? m_phases[m_bossHealth.currentPhase]
            : null;

        #endregion


        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        protected virtual void Start()
        {
            InitializeBoss();
            InitializeDefaultPhasesIfNeeded();
            HookHealthEvents();
            m_bossUI.Bind(this);

            if (!startInactive)
            {
                // Khởi động boss ngay lập tức nếu không phải cinematic
                ApplyPhaseVisual(0, instant: true);
                m_bossHealth.InitializePhase(0, m_phases[0].maxHealth);
                OnBossPhaseStart.Invoke(0);
                isEncounterActive = true;
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!isEncounterActive) return;
            if (m_bossHealth == null || m_bossHealth.isDead || m_bossHealth.isTransitioning) return;

            UpdateBossBehavior();
        }

        #endregion


        //─────────────────────────────────────────────
        #region === INITIALIZATION ===

        private void InitializeBoss()
        {
            m_bossHealth = GetComponent<BossHealth>();
            m_bossAnim = GetComponent<BossAnimationBase>();
            m_phaseTransition = GetComponent<BossPhaseTransitionBase>();
            m_finalPhase = GetComponent<BossFinalSequenceBase>();
            m_bossUI = GetComponent<BossUI>();
        }

        private void InitializeDefaultPhasesIfNeeded()
        {
            if (m_phases != null && m_phases.Length > 0)
                return;

            m_phases = new BossPhase[3];
            for (int i = 0; i < 3; i++)
            {
                m_phases[i] = new BossPhase
                {
                    phaseName = $"Phase {i + 1}",
                    maxHealth = 150 + i * 100,
                    moveSpeed = 4 + i,
                    phaseColor = (i != 0) ? Color.red : Color.white,
                    scale = Vector3.one * (1f + i * 0.15f),
                };
            }
        }

        private void HookHealthEvents()
        {
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
                if (m_phaseTransition != null)
                    StartCoroutine(m_phaseTransition.ExecuteTransition(nextPhase));

                else DefaultPhaseTransition(nextPhase);
            }
            else
            {
                if (m_finalPhase != null)
                    StartCoroutine(m_finalPhase.ExecuteFinalSequence());
            }
        }

        private void DefaultPhaseTransition(int nextPhase)
        {
            m_bossHealth.InitializePhase(nextPhase, m_phases[nextPhase].maxHealth);
            ApplyPhaseVisual(nextPhase, instant: true);
            OnBossPhaseStart.Invoke(nextPhase);

            Debug.LogWarning($"[BossCore] {name} is using DefaultPhaseTransition (no custom transition script found).");
        }

        public void ApplyPhaseVisual(int phaseIndex, bool instant)
        {
            var phase = m_phases[phaseIndex];

            // Color Transition
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (instant) r.sharedMaterial.color = phase.phaseColor;
                else r.sharedMaterial.DOColor(phase.phaseColor, 0.35f);
            }

            // Scale Transition
            if (instant) transform.localScale = phase.scale;
            else transform.DOScale(phase.scale, 0.35f).SetEase(Ease.OutBack);
        }

        #endregion


        //─────────────────────────────────────────────
        #region === ENCOUNTER CONTROL ===

        /// <summary>
        /// Kích hoạt trận chiến (gọi từ trigger hoặc cutscene).
        /// </summary>
        public void StartBattle()
        {
            if (isEncounterActive) return;

            ApplyPhaseVisual(0, instant: true);
            m_bossHealth.InitializePhase(0, m_phases[0].maxHealth);
            OnBossPhaseStart.Invoke(0);
            isEncounterActive = true;

            OnBattleStarted();
        }

        /// <summary>
        /// Cho class con override để bắt đầu vòng lặp tấn công hoặc animation mở đầu.
        /// </summary>
        protected virtual void OnBattleStarted() { }

        #endregion


        //─────────────────────────────────────────────
        #region === SPECIAL ABILITY ===

        /// <summary>
        /// Thông báo cho UI hoặc Manager khi boss dùng kỹ năng đặc biệt.
        /// </summary>
        protected void NotifySpecialUsed(string nameOrId)
        {
            OnSpecialAbilityUsed.Invoke(nameOrId);
        }

        #endregion


        //─────────────────────────────────────────────
        #region === ABSTRACT METHODS ===

        /// <summary>
        /// Được override ở lớp con để cập nhật hành vi boss mỗi frame.
        /// </summary>
        protected abstract void UpdateBossBehavior();

        #endregion
    }
}
