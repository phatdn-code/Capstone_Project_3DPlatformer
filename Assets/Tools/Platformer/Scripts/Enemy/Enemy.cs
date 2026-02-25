using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(EnemyStatsManager))]
    [RequireComponent(typeof(EnemyStateManager))]
    [RequireComponent(typeof(WaypointManager))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/Enemy")]
    public class Enemy : Entity<Enemy>
    {
        #region ===== ENUM / SETTINGS =====

        /// <summary>
        /// Chế độ tấn công bổ sung (ngoài ContactAttack).
        /// None: chỉ chạm là trừ máu
        /// Animated: có thêm tấn công bằng animation
        /// </summary>
        public enum ExtraAttackMode
        {
            None,
            Animated
        }

        [Header("Enemy Settings")]
        public EnemyEvents enemyEvents;

        [Header("Extra Attack (Optional)")]
        public ExtraAttackMode extraAttackMode = ExtraAttackMode.None;

        [Min(0.1f)]
        public float extraAttackRange = 1.4f;

        [Min(0f)]
        public float extraAttackCooldown = 1.0f;
        public bool extraUseContactDamage = true;
        public int extraOverrideDamage = 1;

        #endregion

        #region ===== RUNTIME STATE / CACHE =====

        protected Collider[] m_sightOverlaps = new Collider[1024];

        // Dùng để đánh dấu đang trong animation attack (để FollowState xử lý đứng lại / không trượt qua player).
        private bool m_extraAttacking;
        private float m_nextExtraAttackTime;

        /// <summary> Player mà enemy đang nhìn thấy/đang rượt. </summary>
        public Player player { get; protected set; }

        /// <summary> Enemy Stats Manager. </summary>
        public EnemyStatsManager stats { get; protected set; }

        /// <summary> Waypoint Manager. </summary>
        public WaypointManager waypoints { get; protected set; }

        /// <summary> Health component của enemy. </summary>
        public Health health { get; protected set; }

        #endregion

        #region ===== INITIALIZE =====

        /// <summary>
        /// Khởi tạo StatsManager.
        /// </summary>
        protected virtual void InitializeStatsManager() => stats = GetComponent<EnemyStatsManager>();

        /// <summary>
        /// Khởi tạo WaypointManager.
        /// </summary>
        protected virtual void InitializeWaypointsManager() => waypoints = GetComponent<WaypointManager>();

        /// <summary>
        /// Khởi tạo Health.
        /// </summary>
        protected virtual void InitializeHealth() => health = GetComponent<Health>();

        /// <summary>
        /// Gán tag Enemy theo hệ thống GameTags.
        /// </summary>
        protected virtual void InitializeTag() => tag = GameTags.Enemy;

        #endregion

        #region ===== DAMAGE / LIFE CYCLE =====

        /// <summary>
        /// Enemy nhận sát thương: giảm máu, gọi event, chết thì disable controller.
        /// </summary>
        public override void ApplyDamage(int amount, Vector3 origin)
        {
            if (health == null) return;

            if (!health.isEmpty && !health.recovering)
            {
                health.Damage(amount);
                enemyEvents?.OnDamage?.Invoke();

                if (health.isEmpty)
                {
                    controller.enabled = false;
                    enemyEvents?.OnDie?.Invoke();
                }
            }
        }

        /// <summary>
        /// Hồi sinh enemy: reset máu, bật lại controller.
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

        #region ===== MOVEMENT HELPERS =====

        /// <summary>
        /// Tăng tốc theo hướng, dùng stats hiện tại (turningDrag/acceleration/topSpeed).
        /// </summary>
        public virtual void Accelerate(Vector3 direction, float acceleration, float topSpeed)
        {
            if (stats != null && stats.current != null)
                Accelerate(direction, stats.current.turningDrag, acceleration, topSpeed);
        }

        /// <summary>
        /// Giảm tốc về 0 theo deceleration trong stats.
        /// </summary>
        public virtual void Decelerate()
        {
            if (stats != null && stats.current != null)
                Decelerate(stats.current.deceleration);
        }

        /// <summary>
        /// Ma sát: giảm tốc theo friction trong stats.
        /// </summary>
        public virtual void Friction()
        {
            if (stats != null && stats.current != null)
                Decelerate(stats.current.friction);
        }

        /// <summary>
        /// Trọng lực: áp lực kéo xuống theo gravity trong stats.
        /// </summary>
        public virtual void Gravity()
        {
            if (stats != null && stats.current != null)
                Gravity(stats.current.gravity);
        }

        /// <summary>
        /// Dính đất: áp lực ép xuống mặt đất theo snapForce trong stats.
        /// </summary>
        public virtual void SnapToGround()
        {
            if (stats != null && stats.current != null)
                SnapToGround(stats.current.snapForce);
        }

        /// <summary>
        /// Quay mặt về hướng chỉ định theo rotationSpeed trong stats.
        /// </summary>
        public virtual void FaceDirectionSmooth(Vector3 direction)
        {
            if (stats != null && stats.current != null)
                FaceDirection(direction, stats.current.rotationSpeed);
        }

        #endregion

        #region ===== CONTACT ATTACK (CHẠM LÀ TRỪ MÁU) =====

        /// <summary>
        /// Tấn công khi chạm Player (giữ nguyên cơ chế cũ).
        /// </summary>
        public virtual void ContactAttack(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            if (!other.TryGetComponent(out Player player)) return;
            if (stats == null || stats.current == null) return;

            var stepping = controller.bounds.max + Vector3.down * stats.current.contactSteppingTolerance;

            // Nếu player đang grounded HOẶC enemy không đứng "trên đầu" player (tránh đánh kiểu dẫm nhầm)
            if (player.isGrounded || !BoundsHelper.IsBellowPoint(controller.collider, stepping))
            {
                if (stats.current.contactPushback)
                    lateralVelocity = -localForward * stats.current.contactPushBackForce;

                player.ApplyDamage(stats.current.contactDamage, transform.position);
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
        /// Thử bắt đầu đòn đánh bằng animation (check mode/range/cooldown).
        /// Gọi từ FollowEnemyState khi đủ gần.
        /// </summary>
        public void TryStartExtraAttack()
        {
            if (extraAttackMode != ExtraAttackMode.Animated) return;
            if (m_extraAttacking) return;
            if (Time.time < m_nextExtraAttackTime) return;
            if (player == null) return;

            float dist = Vector3.Distance(position, player.position);
            if (dist > extraAttackRange) return;

            m_extraAttacking = true;
            m_nextExtraAttackTime = Time.time + extraAttackCooldown;

            // Trigger animation "Attack"
            var enemyAnimator = GetComponent<EnemyAnimator>();
            if (enemyAnimator != null && enemyAnimator.animator != null)
                enemyAnimator?.TriggerAttack();
        }

        /// <summary>
        /// Animation Event: gọi ở frame "trúng đòn" trong clip Attack.
        /// Lúc này mới trừ máu Player.
        /// </summary>
        public void ExtraAttackHit_AnimationEvent()
        {
            Debug.Log("AttackHit fired");
            if (player == null) return;

            float dist = Vector3.Distance(position, player.position);
            if (dist > extraAttackRange + 0.2f) return;

            int dmg = (extraUseContactDamage && stats != null && stats.current != null)
                ? stats.current.contactDamage
                : extraOverrideDamage;

            player.ApplyDamage(dmg, transform.position);
        }

        /// <summary>
        /// Animation Event: gọi ở cuối clip Attack để kết thúc trạng thái đang đánh.
        /// </summary>
        public void ExtraAttackEnd_AnimationEvent()
        {
            Debug.Log("AttackEnd fired");
            m_extraAttacking = false;
        }

        #endregion

        #region ===== SIGHT / DETECTION =====

        /// <summary>
        /// Xử lý nhìn thấy player (spot) và mất mục tiêu (escape).
        /// spotRange: khoảng phát hiện
        /// viewRange: khoảng mất mục tiêu
        /// </summary>
        protected virtual void HandleSight()
        {
            // Chưa có player: quét theo spotRange
            if (!player && stats != null && stats.current != null && m_sightOverlaps != null)
            {
                var overlaps = Physics.OverlapSphereNonAlloc(position, stats.current.spotRange, m_sightOverlaps);

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
            // Đã có player: kiểm tra điều kiện mất mục tiêu
            else if (player != null)
            {
                var distance = Vector3.Distance(position, player.position);

                bool playerDead = player.health != null && player.health.current == 0;
                bool tooFar = stats != null && stats.current != null && distance > stats.current.viewRange;

                if (playerDead || tooFar)
                {
                    player = null;
                    enemyEvents?.OnPlayerScaped?.Invoke();
                }
            }
        }

        /// <summary>
        /// Khi phát hiện player: nếu bật followTargetOnSight thì chuyển sang FollowEnemyState.
        /// </summary>
        protected virtual void OnPlayerSpotted()
        {
            if (stats != null && stats.current != null && stats.current.followTargetOnSight && states != null)
                states.Change<FollowEnemyState>();
        }

        #endregion

        #region ===== UNITY LIFECYCLE =====

        /// <summary>
        /// Update loop của Entity: mỗi frame xử lý sight.
        /// </summary>
        protected override void OnUpdate()
        {
            HandleSight();
        }

        /// <summary>
        /// Awake: init các component và tag.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            InitializeTag();
            InitializeStatsManager();
            InitializeWaypointsManager();
            InitializeHealth();
        }

        #endregion
    }
}