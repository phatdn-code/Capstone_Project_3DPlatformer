using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Base class cho tất cả các loại boss
    /// </summary>
    [RequireComponent(typeof(BossHealth))]
    [RequireComponent(typeof(EnemyStatsManager))]
    [RequireComponent(typeof(EnemyStateManager))]
    [RequireComponent(typeof(WaypointManager))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Base Boss")]
    public abstract class BaseBoss : Enemy
    {
        [Header("Boss Settings")]
        [Tooltip("Hiệu ứng chuyển giai đoạn")]
        [SerializeField] private GameObject phaseTransitionEffect;
        
        [Header("Boss Events")]
        [Tooltip("Được gọi khi boss bắt đầu giai đoạn mới")]
        [SerializeField] private UnityEvent<int> OnBossPhaseStart;
        
        [Tooltip("Được gọi khi boss sử dụng kỹ năng đặc biệt")]
        [SerializeField] private UnityEvent<string> OnSpecialAbilityUsed;

        protected BossHealth m_bossHealth;
        protected float m_lastAttackTime;
        protected float m_specialAbilityCooldown;
        protected bool m_isAttacking = false;

        // Serialized fields
        [SerializeField] private BossPhase[] m_phases = new BossPhase[3];
        [SerializeField] private float m_attackInterval = 2f;
        [SerializeField] private float m_attackRange = 3f;

        /// <summary>
        /// Component quản lý máu boss
        /// </summary>
        public BossHealth bossHealth => m_bossHealth;

        /// <summary>
        /// Các giai đoạn của boss
        /// </summary>
        public BossPhase[] phases
        {
            get => m_phases;
            set => m_phases = value;
        }

        /// <summary>
        /// Thời gian giữa các đợt tấn công
        /// </summary>
        public float attackInterval
        {
            get => m_attackInterval;
            set => m_attackInterval = value;
        }

        /// <summary>
        /// Tầm tấn công
        /// </summary>
        public float attackRange
        {
            get => m_attackRange;
            set => m_attackRange = value;
        }

        /// <summary>
        /// Event khi boss bắt đầu giai đoạn mới
        /// </summary>
        public UnityEvent<int> OnBossPhaseStartEvent => OnBossPhaseStart;

        /// <summary>
        /// Event khi boss sử dụng kỹ năng đặc biệt
        /// </summary>
        public UnityEvent<string> OnSpecialAbilityUsedEvent => OnSpecialAbilityUsed;

        /// <summary>
        /// Giai đoạn hiện tại của boss
        /// </summary>
        public BossPhase currentPhase
        {
            get
            {
                if (m_phases.Length > 0 && m_bossHealth.currentPhase < m_phases.Length)
                    return m_phases[m_bossHealth.currentPhase];
                return null;
            }
        }

        /// <summary>
        /// Boss có đang trong quá trình chuyển giai đoạn không
        /// </summary>
        public bool isTransitioning => m_bossHealth.isTransitioning;

        protected virtual void Start()
        {
            InitializeBossHealth();
            InitializePhases();
            SetupPhaseEvents();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            if (m_bossHealth != null && !m_bossHealth.isDead && !m_bossHealth.isTransitioning)
            {
                UpdateBossBehavior();
            }
        }

        /// <summary>
        /// Khởi tạo component BossHealth
        /// </summary>
        private void InitializeBossHealth()
        {
            m_bossHealth = GetComponent<BossHealth>();
            if (m_bossHealth == null)
            {
                m_bossHealth = gameObject.AddComponent<BossHealth>();
                Debug.Log($"✅ Tạo BossHealth component cho {gameObject.name}");
            }
            else
            {
                Debug.Log($"✅ Tìm thấy BossHealth component cho {gameObject.name}");
            }
        }

        /// <summary>
        /// Khởi tạo các giai đoạn boss
        /// </summary>
        private void InitializePhases()
        {
            if (m_phases.Length == 0)
            {
                // Tạo 3 giai đoạn mặc định
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

        /// <summary>
        /// Lấy màu sắc cho từng giai đoạn
        /// </summary>
        private Color GetPhaseColor(int phase)
        {
            switch (phase)
            {
                case 0: return Color.red;
                case 1: return Color.yellow;
                case 2: return Color.magenta;
                default: return Color.white;
            }
        }

        /// <summary>
        /// Thiết lập các sự kiện giai đoạn
        /// </summary>
        private void SetupPhaseEvents()
        {
            m_bossHealth.OnPhaseChanged.AddListener(OnPhaseChanged);
            m_bossHealth.OnBossHealed.AddListener(OnBossHealed);
            m_bossHealth.OnBossDefeated.AddListener(OnBossDefeated);
        }

        /// <summary>
        /// Cập nhật hành vi boss - Override trong các class con
        /// </summary>
        protected virtual void UpdateBossBehavior()
        {
            if (currentPhase == null) return;

            // Cập nhật stats theo giai đoạn hiện tại
            UpdateStatsForCurrentPhase();

            // Kiểm tra tấn công
            if (CanAttack())
            {
                PerformAttack();
            }

            // Kiểm tra kỹ năng đặc biệt
            if (CanUseSpecialAbility())
            {
                UseSpecialAbility();
            }
        }

        /// <summary>
        /// Cập nhật stats theo giai đoạn hiện tại
        /// </summary>
        private void UpdateStatsForCurrentPhase()
        {
            if (currentPhase == null) return;

            // Cập nhật màu sắc
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = currentPhase.phaseColor;
                }
            }

            // Cập nhật kích thước
            transform.localScale = currentPhase.scale;
        }

        /// <summary>
        /// Kiểm tra có thể tấn công không - Override trong các class con
        /// </summary>
        protected virtual bool CanAttack()
        {
            if (player == null) return false;
            if (m_isAttacking) return false;

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= m_attackRange && Time.time >= m_lastAttackTime + m_attackInterval;
        }

        /// <summary>
        /// Thực hiện tấn công - Override trong các class con
        /// </summary>
        protected virtual void PerformAttack()
        {
            m_isAttacking = true;
            m_lastAttackTime = Time.time;

            // Tấn công player
            if (player != null)
            {
                player.ApplyDamage(currentPhase.damage, transform.position);
                Debug.Log($"{GetType().Name} tấn công với {currentPhase.damage} sát thương!");
            }

            // Hiệu ứng tấn công
            if (enemyEvents != null)
            {
                enemyEvents.OnPlayerContact?.Invoke();
            }

            // Reset trạng thái tấn công
            Invoke(nameof(ResetAttackState), currentPhase.attackSpeed);
        }

        /// <summary>
        /// Reset trạng thái tấn công
        /// </summary>
        protected virtual void ResetAttackState()
        {
            m_isAttacking = false;
        }

        /// <summary>
        /// Kiểm tra có thể sử dụng kỹ năng đặc biệt không
        /// </summary>
        private bool CanUseSpecialAbility()
        {
            if (currentPhase == null) return false;
            if (!currentPhase.canUseSpecialAbility) return false;
            return Time.time >= m_specialAbilityCooldown;
        }

        /// <summary>
        /// Sử dụng kỹ năng đặc biệt - Override trong các class con
        /// </summary>
        protected virtual void UseSpecialAbility()
        {
            m_specialAbilityCooldown = Time.time + currentPhase.specialAbilityCooldown;
            
            Debug.Log($"{GetType().Name} sử dụng: {currentPhase.specialAbilityName}");
            OnSpecialAbilityUsed?.Invoke(currentPhase.specialAbilityName);

            // Hiệu ứng kỹ năng đặc biệt
            if (phaseTransitionEffect != null)
            {
                Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
            }
        }

        /// <summary>
        /// Xử lý khi boss chuyển giai đoạn
        /// </summary>
        protected virtual void OnPhaseChanged(int newPhase)
        {
            Debug.Log($"Boss chuyển sang {m_phases[newPhase].phaseName}!");
            OnBossPhaseStart?.Invoke(newPhase);

            // Hiệu ứng chuyển giai đoạn
            if (phaseTransitionEffect != null)
            {
                Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
            }
        }

        /// <summary>
        /// Xử lý khi boss hồi phục
        /// </summary>
        protected virtual void OnBossHealed()
        {
            Debug.Log("Boss đã hồi phục hoàn toàn!");
        }

        /// <summary>
        /// Xử lý khi boss bị đánh bại
        /// </summary>
        protected virtual void OnBossDefeated()
        {
            Debug.Log("Boss đã bị đánh bại hoàn toàn!");
            // Có thể thêm hiệu ứng chết, drop items, etc.
        }

        /// <summary>
        /// Ghi đè phương thức ApplyDamage để sử dụng BossHealth
        /// </summary>
        public override void ApplyDamage(int amount, Vector3 origin)
        {
            if (m_bossHealth.isDead || m_bossHealth.isTransitioning) return;

            m_bossHealth.TakeDamage(amount);
            enemyEvents.OnDamage?.Invoke();
        }

        /// <summary>
        /// Reset boss về trạng thái ban đầu
        /// </summary>
        public void ResetBoss()
        {
            m_bossHealth.ResetBoss();
            m_lastAttackTime = 0f;
            m_specialAbilityCooldown = 0f;
            m_isAttacking = false;
        }
    }
}
