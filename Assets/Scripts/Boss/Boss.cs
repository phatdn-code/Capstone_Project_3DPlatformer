using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Base class cho tất cả các loại boss
    /// Quản lý máu, phase, hành vi và sự kiện chung
    /// </summary>
    [RequireComponent(typeof(BossHealth))]
    [RequireComponent(typeof(EnemyStatsManager))]
    [RequireComponent(typeof(EnemyStateManager))]
    [RequireComponent(typeof(WaypointManager))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Base Boss")]
    public abstract class BaseBoss : Enemy
    {
        // ───────────────────────────────────────────────
        // Serialized Fields
        // ───────────────────────────────────────────────
        [Header("Boss Settings")]
        [Tooltip("Hiệu ứng khi boss đổi giai đoạn hoặc dùng kỹ năng đặc biệt")]
        [SerializeField] private GameObject phaseTransitionEffect;

        [Header("Boss Events")]
        [Tooltip("Được gọi khi boss bắt đầu giai đoạn mới")]
        [SerializeField] private UnityEvent<int> OnBossPhaseStart;

        [Tooltip("Được gọi khi boss sử dụng kỹ năng đặc biệt")]
        [SerializeField] private UnityEvent<string> OnSpecialAbilityUsed;

        [Tooltip("Danh sách các giai đoạn của boss")]
        [SerializeField] private BossPhase[] m_phases = new BossPhase[3];

        [Tooltip("Khoảng thời gian giữa 2 đòn tấn công thường")]
        [SerializeField] private float m_attackInterval = 2f;

        [Tooltip("Tầm tấn công thường của boss")]
        [SerializeField] private float m_attackRange = 3f;

        // ───────────────────────────────────────────────
        // Protected Fields
        // ───────────────────────────────────────────────
        protected BossHealth m_bossHealth;
        protected float m_lastAttackTime;           // thời điểm boss tấn công gần nhất
        protected float m_specialAbilityCooldown;   // thời điểm skill đặc biệt sẵn sàng
        protected bool m_isAttacking = false;       // flag đang tấn công

        // ───────────────────────────────────────────────
        // Public Properties
        // ───────────────────────────────────────────────
        public BossHealth bossHealth => m_bossHealth;
        public BossPhase[] phases { get => m_phases; set => m_phases = value; }
        public float attackInterval { get => m_attackInterval; set => m_attackInterval = value; }
        public float attackRange { get => m_attackRange; set => m_attackRange = value; }
        public UnityEvent<int> OnBossPhaseStartEvent => OnBossPhaseStart;
        public UnityEvent<string> OnSpecialAbilityUsedEvent => OnSpecialAbilityUsed;

        /// <summary> Trả về giai đoạn hiện tại của boss </summary>
        public BossPhase currentPhase =>
            (m_phases.Length > 0 && m_bossHealth.currentPhase < m_phases.Length)
                ? m_phases[m_bossHealth.currentPhase] : null;

        /// <summary> Cho biết boss có đang trong quá trình chuyển phase không </summary>
        public bool isTransitioning => m_bossHealth.isTransitioning;

        // ───────────────────────────────────────────────
        // Unity Lifecycle
        // ───────────────────────────────────────────────

        /// <summary>
        /// Hàm Start của Unity
        /// Khởi tạo BossHealth, các phase và gắn sự kiện
        /// </summary>
        protected virtual void Start()
        {
            InitializeBossHealth();
            InitializePhases();
            SetupPhaseEvents();
        }

        /// <summary>
        /// Hàm update (kế thừa từ Enemy)
        /// Liên tục gọi hành vi boss nếu còn sống và không chuyển phase
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (m_bossHealth != null && !m_bossHealth.isDead && !m_bossHealth.isTransitioning)
                UpdateBossBehavior();
        }

        // ───────────────────────────────────────────────
        // Initialization
        // ───────────────────────────────────────────────

        /// <summary> Tìm hoặc thêm component BossHealth </summary>
        private void InitializeBossHealth()
        {
            m_bossHealth = GetComponent<BossHealth>();

            if (m_bossHealth == null)
            {
                m_bossHealth = gameObject.AddComponent<BossHealth>();
                Debug.Log($"✅ Tạo BossHealth component cho {gameObject.name}");
            }
        }

        /// <summary> Nếu chưa có phases thì tạo mặc định 3 phase với stats tăng dần </summary>
        private void InitializePhases()
        {
            if (m_phases.Length == 0)
            {
                m_phases = new BossPhase[3];
                for (int i = 0; i < 3; i++)
                {
                    m_phases[i] = new BossPhase
                    {
                        phaseName = $"Giai đoạn {i + 1}",
                        maxHealth = 100,
                        moveSpeed = 5f + i * 2f,
                        attackSpeed = 1f - i * 0.2f,
                        damage = 10 + i * 5,
                        sightRange = 10f + i * 5f,
                        phaseColor = GetPhaseColor(i),
                        scale = Vector3.one * (1f + i * 0.2f),
                        canUseSpecialAbility = i > 0,
                        specialAbilityName = i > 0 ? $"Kỹ năng đặc biệt {i}" : "",
                        specialAbilityCooldown = 5f - i * 1f
                    };
                }
            }
        }

        /// <summary> Lấy màu sắc hiển thị cho từng phase </summary>
        private Color GetPhaseColor(int phase)
        {
            switch (phase)
            {
                case 0: return Color.white;
                case 1: return Color.white;
                case 2: return Color.red;
                default: return Color.white;
            }
        }

        /// <summary> Gắn các sự kiện phase từ BossHealth </summary>
        private void SetupPhaseEvents()
        {
            m_bossHealth.OnPhaseChanged.AddListener(OnPhaseChanged);
            m_bossHealth.OnBossHealed.AddListener(OnBossHealed);
            m_bossHealth.OnBossDefeated.AddListener(OnBossDefeated);
        }

        // ───────────────────────────────────────────────
        // Boss Behavior
        // ───────────────────────────────────────────────

        /// <summary> Hàm chính điều khiển hành vi boss: update stats, attack, skill </summary>
        protected virtual void UpdateBossBehavior()
        {
            if (currentPhase == null) return;

            UpdateStatsForCurrentPhase();

            if (CanAttack()) PerformAttack();
            if (CanUseSpecialAbility()) UseSpecialAbility();
        }

        /// <summary> Cập nhật màu & scale của boss theo phase hiện tại </summary>
        private void UpdateStatsForCurrentPhase()
        {
            if (currentPhase == null) return;

            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                    renderer.material.color = currentPhase.phaseColor;
            }

            transform.localScale = currentPhase.scale;
        }

        // ───────────────────────────────────────────────
        // Combat
        // ───────────────────────────────────────────────

        /// <summary> Kiểm tra điều kiện để boss tấn công thường </summary>
        protected virtual bool CanAttack()
        {
            if (player == null || m_isAttacking) return false;
            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= m_attackRange && Time.time >= m_lastAttackTime + m_attackInterval;
        }

        /// <summary> Thực hiện tấn công thường vào player </summary>
        protected virtual void PerformAttack()
        {
            m_isAttacking = true;
            m_lastAttackTime = Time.time;

            if (player != null)
            {
                player.ApplyDamage(currentPhase.damage, transform.position);
                Debug.Log($"{GetType().Name} tấn công với {currentPhase.damage} sát thương!");
            }

            enemyEvents?.OnPlayerContact?.Invoke();
            Invoke(nameof(ResetAttackState), currentPhase.attackSpeed);
        }

        /// <summary> Reset lại trạng thái sau khi tấn công </summary>
        protected virtual void ResetAttackState()
        {
            m_isAttacking = false;
        }

        /// <summary> Kiểm tra điều kiện có thể dùng kỹ năng đặc biệt </summary>
        private bool CanUseSpecialAbility()
        {
            if (currentPhase == null || !currentPhase.canUseSpecialAbility) return false;
            return Time.time >= m_specialAbilityCooldown;
        }

        /// <summary> Thực thi kỹ năng đặc biệt và gọi event </summary>
        protected virtual void UseSpecialAbility()
        {
            m_specialAbilityCooldown = Time.time + currentPhase.specialAbilityCooldown;
            Debug.Log($"{GetType().Name} sử dụng: {currentPhase.specialAbilityName}");

            OnSpecialAbilityUsed?.Invoke(currentPhase.specialAbilityName);

            if (phaseTransitionEffect != null)
                Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
        }

        // ───────────────────────────────────────────────
        // Event Handlers
        // ───────────────────────────────────────────────

        /// <summary> Xử lý khi boss chuyển sang giai đoạn mới </summary>
        protected virtual void OnPhaseChanged(int newPhase)
        {
            Debug.Log($"Boss chuyển sang {m_phases[newPhase].phaseName}!");
            OnBossPhaseStart?.Invoke(newPhase);

            if (phaseTransitionEffect != null)
                Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
        }

        /// <summary> Xử lý khi boss hồi máu đầy (event từ BossHealth) </summary>
        protected virtual void OnBossHealed()
        {
            Debug.Log("Boss đã hồi phục hoàn toàn!");
        }

        /// <summary> Xử lý khi boss bị hạ gục hoàn toàn </summary>
        protected virtual void OnBossDefeated()
        {
            Debug.Log("Boss đã bị đánh bại hoàn toàn!");
        }

        // ───────────────────────────────────────────────
        // Misc
        // ───────────────────────────────────────────────

        /// <summary> Gọi khi boss nhận damage từ bên ngoài </summary>
        public override void ApplyDamage(int amount, Vector3 origin)
        {
            if (m_bossHealth.isDead || m_bossHealth.isTransitioning) return;
            m_bossHealth.TakeDamage(amount);
            enemyEvents.OnDamage?.Invoke();
        }

        /// <summary> Reset lại toàn bộ trạng thái boss (dùng khi restart level) </summary>
        public void ResetBoss()
        {
            m_bossHealth.ResetBoss();
            m_lastAttackTime = 0f;
            m_specialAbilityCooldown = 0f;
            m_isAttacking = false;
        }
    }
}
