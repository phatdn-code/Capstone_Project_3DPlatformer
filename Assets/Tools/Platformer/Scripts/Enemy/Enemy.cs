using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    #region ===== DATA TYPES =====

    /// <summary>
    /// Cấu hình mapping tối giản: chỉ cần Type + Mode.
    /// - NormalHit + Trigger: đánh thường (Trigger)
    /// - RollAttack + Bool: roll (Bool)
    /// </summary>
    [System.Serializable]
    public class EnemyAttackAnimConfig
    {
        public EnemyAttackType type = EnemyAttackType.NormalHit;
        public AttackAnimMode mode = AttackAnimMode.Trigger;
    }

    #endregion

    [RequireComponent(typeof(EnemyStatsManager))]
    [RequireComponent(typeof(EnemyStateManager))]
    [RequireComponent(typeof(WaypointManager))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/Enemy")]
    public class Enemy : Entity<Enemy>
    {
        #region ===== INSPECTOR =====

        [Header("Enemy Settings")]
        public EnemyEvents enemyEvents;

        [Header("Extra Attack (Optional)")]
        public ExtraAttackMode extraAttackMode = ExtraAttackMode.None;

        [Min(0.1f)] public float extraAttackRange = 1.4f;
        [Min(0f)] public float extraAttackCooldown = 1.0f;

        public bool extraUseContactDamage = true;
        public int extraOverrideDamage = 1;

        [Header("Attack Animation Configs")]
        public EnemyAttackAnimConfig[] attackAnimConfigs;

        [Header("Roll Attack Settings")]
        public bool enableRollAttack = true;
        [Min(0.1f)] public float rollAttackRange = 4.0f;
        [Min(0.1f)] public float rollTopSpeed = 8.0f;
        [Min(0.1f)] public float rollAcceleration = 40.0f;
        [Min(0.05f)] public float rollStopDistance = 0.4f;
        [Min(0f)] public float rollCooldown = 2.0f;

        #endregion

        #region ===== REFERENCES / PROPERTIES =====

        /// <summary> Player mà enemy đang nhìn thấy/đang rượt. </summary>
        public Player player { get; protected set; }

        /// <summary> Enemy Stats Manager. </summary>
        public EnemyStatsManager stats { get; protected set; }

        /// <summary> Waypoint Manager. </summary>
        public WaypointManager waypoints { get; protected set; }

        /// <summary> Health component của enemy. </summary>
        public Health health { get; protected set; }

        private EnemyAnimator m_enemyAnimator;

        #endregion

        #region ===== RUNTIME CACHE =====

        private readonly Collider[] m_sightOverlaps = new Collider[1024];

        // Extra attack (attack thường dạng animation)
        private bool m_extraAttacking;
        private float m_nextExtraAttackTime;

        // Roll runtime
        private bool m_rollAttacking;
        private Vector3 m_rollTargetPos;
        private float m_nextRollTime;

        // Cache hash cho Bool "Attack" (dùng cho RollAttack + Bool theo yêu cầu)
        private static readonly int s_attackBoolHash = Animator.StringToHash("Attack");

        #endregion

        #region ===== INITIALIZE =====

        /// <summary>
        /// Gán tag Enemy theo hệ thống GameTags.
        /// </summary>
        protected virtual void InitializeTag() => tag = GameTags.Enemy;

        /// <summary>
        /// Lấy reference EnemyStatsManager.
        /// </summary>
        protected virtual void InitializeStatsManager() => stats = GetComponent<EnemyStatsManager>();

        /// <summary>
        /// Lấy reference WaypointManager.
        /// </summary>
        protected virtual void InitializeWaypointsManager() => waypoints = GetComponent<WaypointManager>();

        /// <summary>
        /// Lấy reference Health.
        /// </summary>
        protected virtual void InitializeHealth() => health = GetComponent<Health>();

        /// <summary>
        /// Lấy reference EnemyAnimator (cache để khỏi GetComponent lặp).
        /// </summary>
        protected virtual void InitializeEnemyAnimator() => m_enemyAnimator = GetComponent<EnemyAnimator>();

        #endregion

        #region ===== LIFE CYCLE / DAMAGE =====

        /// <summary>
        /// Enemy nhận sát thương: giảm máu, gọi event, chết thì tắt controller.
        /// </summary>
        public override void ApplyDamage(int amount, Vector3 origin)
        {
            if (health == null) return;
            if (health.isEmpty || health.recovering) return;

            health.Damage(amount);
            enemyEvents?.OnDamage?.Invoke();

            if (health.isEmpty)
            {
                controller.enabled = false;
                enemyEvents?.OnDie?.Invoke();
            }
        }

        /// <summary>
        /// Hồi sinh enemy: reset máu và bật lại controller.
        /// </summary>
        public virtual void Revive()
        {
            if (health == null) return;
            if (!health.isEmpty) return;

            health.ResetHealth();
            controller.enabled = true;
            enemyEvents?.OnRevive?.Invoke();
        }

        #endregion

        #region ===== MOVEMENT HELPERS (STATS-BASED) =====

        /// <summary>
        /// Tăng tốc theo hướng, dùng turningDrag/acceleration/topSpeed trong stats hiện tại.
        /// </summary>
        public virtual void Accelerate(Vector3 direction, float acceleration, float topSpeed)
        {
            if (stats?.current == null) return;
            Accelerate(direction, stats.current.turningDrag, acceleration, topSpeed);
        }

        /// <summary>
        /// Giảm tốc về 0 theo deceleration trong stats.
        /// </summary>
        public virtual void Decelerate()
        {
            if (stats?.current == null) return;
            Decelerate(stats.current.deceleration);
        }

        /// <summary>
        /// Ma sát: giảm tốc theo friction trong stats.
        /// </summary>
        public virtual void Friction()
        {
            if (stats?.current == null) return;
            Decelerate(stats.current.friction);
        }

        /// <summary>
        /// Trọng lực: áp lực kéo xuống theo gravity trong stats.
        /// </summary>
        public virtual void Gravity()
        {
            if (stats?.current == null) return;
            Gravity(stats.current.gravity);
        }

        /// <summary>
        /// Dính đất: ép xuống mặt đất theo snapForce trong stats.
        /// </summary>
        public virtual void SnapToGround()
        {
            if (stats?.current == null) return;
            SnapToGround(stats.current.snapForce);
        }

        /// <summary>
        /// Quay mặt về hướng chỉ định theo rotationSpeed trong stats.
        /// </summary>
        public virtual void FaceDirectionSmooth(Vector3 direction)
        {
            if (stats?.current == null) return;
            FaceDirection(direction, stats.current.rotationSpeed);
        }

        #endregion

        #region ===== CONTACT ATTACK (CHẠM LÀ TRỪ MÁU) =====

        /// <summary>
        /// Tấn công khi chạm Player (cơ chế cũ): chạm là trừ máu, có thể pushback.
        /// </summary>
        public virtual void ContactAttack(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            if (!other.TryGetComponent(out Player p)) return;
            if (stats?.current == null) return;

            var stepping = controller.bounds.max + Vector3.down * stats.current.contactSteppingTolerance;

            if (p.isGrounded || !BoundsHelper.IsBellowPoint(controller.collider, stepping))
            {
                if (stats.current.contactPushback)
                    lateralVelocity = -localForward * stats.current.contactPushBackForce;

                p.ApplyDamage(stats.current.contactDamage, transform.position);
                enemyEvents?.OnPlayerContact?.Invoke();
            }
        }

        /// <summary>
        /// Unity Trigger: chạm collider trigger thì gọi ContactAttack.
        /// </summary>
        protected virtual void OnTriggerEnter(Collider other)
        {
            ContactAttack(other);
        }

        #endregion

        #region ===== EXTRA ATTACK (ANIMATION) =====

        /// <summary>
        /// Enemy có đang trong quá trình đánh animation không?
        /// (FollowEnemyState dùng để đứng lại/không trượt qua player)
        /// </summary>
        public bool IsExtraAttacking() => m_extraAttacking;

        /// <summary>
        /// Thử bắt đầu đòn đánh animation (check mode/range/cooldown).
        /// Gọi từ FollowEnemyState khi đủ gần.
        /// </summary>
        public void TryStartExtraAttack()
        {
            if (extraAttackMode != ExtraAttackMode.Animated) return;
            if (m_extraAttacking) return;
            if (Time.time < m_nextExtraAttackTime) return;
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > extraAttackRange) return;

            m_extraAttacking = true;
            m_nextExtraAttackTime = Time.time + extraAttackCooldown;

            // NormalHit + Trigger: bắn trigger đánh thường
            PlayAttack(EnemyAttackType.NormalHit, true);
        }

        /// <summary>
        /// Animation Event: gọi ở frame "trúng đòn" trong clip Attack để trừ máu Player.
        /// </summary>
        public void ExtraAttackHit_AnimationEvent()
        {
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > extraAttackRange + 0.2f) return;

            int dmg = (extraUseContactDamage && stats?.current != null)
                ? stats.current.contactDamage
                : extraOverrideDamage;

            player.ApplyDamage(dmg, transform.position);
        }

        /// <summary>
        /// Animation Event: gọi ở cuối clip Attack để kết thúc trạng thái đang đánh.
        /// </summary>
        public void ExtraAttackEnd_AnimationEvent()
        {
            m_extraAttacking = false;
        }

        #endregion

        #region ===== ATTACK DISPATCH (ONE ENTRY) =====

        /// <summary>
        /// Enemy có đang roll attack không?
        /// </summary>
        public bool IsRollAttacking() => m_rollAttacking;

        /// <summary>
        /// Tìm config tương ứng cho attack type (nếu prefab không cấu hình thì trả null).
        /// </summary>
        private EnemyAttackAnimConfig GetAttackConfig(EnemyAttackType type)
        {
            if (attackAnimConfigs == null) return null;

            for (int i = 0; i < attackAnimConfigs.Length; i++)
            {
                var cfg = attackAnimConfigs[i];
                if (cfg != null && cfg.type == type) return cfg;
            }

            return null;
        }

        /// <summary>
        /// Gọi animation theo rule cố định:
        /// - NormalHit + Trigger: gọi TriggerAttack()
        /// - RollAttack + Bool: animator.SetBool("Attack", true/false)
        /// </summary>
        public void PlayAttack(EnemyAttackType type, bool active)
        {
            if (m_enemyAnimator == null || m_enemyAnimator.animator == null) return;

            var cfg = GetAttackConfig(type);
            if (cfg == null) return;

            // NormalHit + Trigger
            if (cfg.type == EnemyAttackType.NormalHit && cfg.mode == AttackAnimMode.Trigger)
            {
                if (!active) return;
                m_enemyAnimator.TriggerAttack();
                return;
            }

            // RollAttack + Bool => Bool "Attack"
            if (cfg.type == EnemyAttackType.RollAttack && cfg.mode == AttackAnimMode.Bool)
            {
                m_enemyAnimator.animator.SetBool(s_attackBoolHash, active);
                return;
            }
        }

        /// <summary>
        /// Kiểm tra có thể bắt đầu roll attack hay không (bật roll + có player + cooldown + có config RollAttack/Bool).
        /// </summary>
        public bool CanStartRollAttack()
        {
            if (!enableRollAttack) return false;
            if (player == null) return false;
            if (m_rollAttacking) return false;
            if (Time.time < m_nextRollTime) return false;

            // Prefab không cấu hình RollAttack/Bool thì tuyệt đối không roll
            var cfg = GetAttackConfig(EnemyAttackType.RollAttack);
            if (cfg == null || cfg.mode != AttackAnimMode.Bool) return false;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            return dist <= rollAttackRange;
        }

        /// <summary>
        /// Bắt đầu roll attack: khóa vị trí player 1 lần và bật Bool "Attack" trong Animator.
        /// </summary>
        public void StartRollAttack()
        {
            if (!CanStartRollAttack()) return;

            m_rollTargetPos = player.transform.position; // mark 1 lần

            m_rollAttacking = true;
            m_nextRollTime = Time.time + rollCooldown;

            PlayAttack(EnemyAttackType.RollAttack, true);
        }

        /// <summary>
        /// Update roll attack mỗi frame: lao tới điểm đã khóa, tới gần thì kết thúc roll.
        /// </summary>
        public void StepRollAttack()
        {
            if (!m_rollAttacking) return;

            Vector3 toTarget = m_rollTargetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= rollStopDistance * rollStopDistance)
            {
                EndRollAttack();
                return;
            }

            Vector3 dir = toTarget.normalized;

            Accelerate(dir, rollAcceleration, rollTopSpeed);
            FaceDirectionSmooth(dir);
        }

        /// <summary>
        /// Kết thúc roll attack: tắt Bool "Attack" và reset trạng thái.
        /// </summary>
        public void EndRollAttack()
        {
            if (!m_rollAttacking) return;

            m_rollAttacking = false;

            PlayAttack(EnemyAttackType.RollAttack, false);

            if (stats != null && stats.current != null)
            {
                float followTop = stats.current.followTopSpeed;
                lateralVelocity = Vector3.ClampMagnitude(lateralVelocity, followTop);
            }
        }

        #endregion

        #region ===== SIGHT / DETECTION =====

        /// <summary>
        /// Xử lý phát hiện player (spot) và mất mục tiêu (escape).
        /// spotRange: khoảng phát hiện
        /// viewRange: khoảng mất mục tiêu
        /// </summary>
        protected virtual void HandleSight()
        {
            if (!player)
            {
                if (stats?.current == null) return;

                int overlaps = Physics.OverlapSphereNonAlloc(transform.position, stats.current.spotRange, m_sightOverlaps);
                for (int i = 0; i < overlaps; i++)
                {
                    var col = m_sightOverlaps[i];
                    if (col == null) continue;

                    if (col.CompareTag(GameTags.Player) && col.TryGetComponent<Player>(out var spotted))
                    {
                        player = spotted;
                        OnPlayerSpotted();
                        enemyEvents?.OnPlayerSpotted?.Invoke();
                        return;
                    }
                }
            }
            else
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                bool playerDead = player.health != null && player.health.current == 0;
                bool tooFar = stats?.current != null && distance > stats.current.viewRange;

                if (playerDead || tooFar)
                {
                    player = null;
                    enemyEvents?.OnPlayerScaped?.Invoke();
                }
            }
        }

        /// <summary>
        /// Khi phát hiện player: nếu followTargetOnSight bật thì chuyển sang FollowEnemyState.
        /// </summary>
        protected virtual void OnPlayerSpotted()
        {
            if (stats?.current == null) return;
            if (!stats.current.followTargetOnSight) return;
            if (states == null) return;

            states.Change<FollowEnemyState>();
        }

        #endregion

        #region ===== UNITY LIFECYCLE =====

        /// <summary>
        /// Vòng update của Entity: mỗi frame xử lý sight.
        /// </summary>
        protected override void OnUpdate()
        {
            HandleSight();
        }

        /// <summary>
        /// Awake: init component + tag + cache animator.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            InitializeTag();
            InitializeStatsManager();
            InitializeWaypointsManager();
            InitializeHealth();
            InitializeEnemyAnimator();
        }

        #endregion
    }
}